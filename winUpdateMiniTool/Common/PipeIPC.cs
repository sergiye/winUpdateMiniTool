using sergiye.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace winUpdateMiniTool.Common;

internal class PipeIpc(string pipeName) {
  // Delegate for passing received message back to caller
  public delegate void DelegateMessage(PipeServer pipe, string data);

  private readonly Dispatcher mDispatcher = Dispatcher.CurrentDispatcher;
  private readonly List<PipeServer> serverPipes = [];

  private readonly List<PipeClient> clientPipes = [];

  public event DelegateMessage PipeMessage;

  public void Listen() {
    PipeServer serverPipe = new(pipeName);
    serverPipes.Add(serverPipe);

    serverPipe.DataReceived += (sndr, data) => {
      mDispatcher.Invoke(() => { PipeMessage?.Invoke(serverPipe, data); });
    };

    serverPipe.Connected += (sndr, args) => { mDispatcher.Invoke(Listen); };

    serverPipe.PipeClosed += (sndr, args) => { mDispatcher.Invoke(() => { serverPipes.Remove(serverPipe); }); };
  }

  public PipeClient Connect(int timeOut = 10000) {
    PipeClient clientPipe = new(".", pipeName);
    if (!clientPipe.Connect(timeOut))
      return null;

    clientPipes.Add(clientPipe);

    clientPipe.PipeClosed += (sndr, args) => { mDispatcher.Invoke(() => { clientPipes.Remove(clientPipe); }); };

    return clientPipe;
  }

  internal class PipeTmpl<T> where T : PipeStream {
    protected T PipeStream;
    public event EventHandler<string> DataReceived;
    public event EventHandler<EventArgs> PipeClosed;

    public void Close() {
      PipeStream.Flush();
      PipeStream.WaitForPipeDrain();
      PipeStream.Close();
      PipeStream.Dispose();
      PipeStream = null;
    }

    protected bool IsConnected() {
      return PipeStream.IsConnected;
    }

    public Task Send(string str) {
      var bytes = Encoding.UTF8.GetBytes(str);
      var data = BitConverter.GetBytes(bytes.Length);
      var buff = data.Concat(bytes).ToArray();

      return PipeStream.WriteAsync(buff, 0, buff.Length);
    }

    protected void InitAsyncReader() {
      new Action<PipeTmpl<T>>(p => {
        p.RunAsyncByteReader(b => {
          DataReceived?.Invoke(this, Encoding.UTF8.GetString(b).TrimEnd('\0'));
        });
      })(this);
    }

    private void RunAsyncByteReader(Action<byte[]> asyncReader) {
      var len = sizeof(int);
      var buff = new byte[len];

      // read the length
      PipeStream.ReadAsync(buff, 0, len).ContinueWith(ret => {
        if (ret.Result == 0) {
          PipeClosed?.Invoke(this, EventArgs.Empty);
          return;
        }

        // read the data
        len = BitConverter.ToInt32(buff, 0);
        buff = new byte[len];
        PipeStream.ReadAsync(buff, 0, len).ContinueWith(ret2 => {
          if (ret2.Result == 0) {
            PipeClosed?.Invoke(this, EventArgs.Empty);
            return;
          }

          asyncReader(buff);
          RunAsyncByteReader(asyncReader);
        });
      });
    }

    public void Flush() {
      PipeStream.Flush();
    }
  }

  internal class PipeServer : PipeTmpl<NamedPipeServerStream> {
    public PipeServer(string pipeName) {
      PipeSecurity pipeSa = new();
      pipeSa.SetAccessRule(new PipeAccessRule(new SecurityIdentifier(FileOps.MF_SID_WORLS),
          PipeAccessRights.FullControl, AccessControlType.Allow));
      var buffLen = 1029; // 4 + 1024 + 1
      PipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut,
          NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous,
          buffLen, buffLen, pipeSa);
      PipeStream.BeginWaitForConnection(PipeConnected, null);
    }

    public event EventHandler<EventArgs> Connected;

    private void PipeConnected(IAsyncResult asyncResult) {
      PipeStream.EndWaitForConnection(asyncResult);
      Connected?.Invoke(this, EventArgs.Empty);
      InitAsyncReader();
    }
  }

  internal class PipeClient : PipeTmpl<NamedPipeClientStream> {
    private readonly ConcurrentStack<string> messageQueue = new(); // LIFO

    public PipeClient(string serverName, string pipeName) {
      PipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    }

    public bool Connect(int timeOut = 10000) {
      try {
        PipeStream.Connect(timeOut);
      }
      catch {
        return false; // timeout
      }

      DataReceived += (sndr, data) => { messageQueue.Push(data); };

      InitAsyncReader();
      return true;
    }

    public string Read(int timeOut = 10000) {
      messageQueue.Clear();
      var ticksEnd = DateTime.Now.Ticks + timeOut * 10000;
      while (ticksEnd > DateTime.Now.Ticks) {
        Application.DoEvents();
        if (!IsConnected() || messageQueue.Count > 0)
          break;
      }

      return Read();
    }

    private string Read() {
      // the MessageQueue is a last in first out type of container, so we need to reverse it
      var ret = string.Join("\0", messageQueue.ToArray().Reverse());
      messageQueue.Clear();
      return ret;
    }
  }
}
