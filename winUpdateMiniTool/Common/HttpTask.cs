using System;
using System.IO;
using System.Net;
using System.Windows.Threading;

namespace winUpdateMiniTool.Common;

internal class HttpTask {
  //const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
  private const int MfBufferSize = 1024;
  private readonly Dispatcher mDispatcher;
  private readonly string mUrl;
  private byte[] bufferRead;
  private bool canceled;
  private DateTime lastTime;
  private int mLength = -1;
  private int mOffset = -1;
  private int mOldPercent = -1;
  private HttpWebRequest request;
  private HttpWebResponse response;
  private Stream streamResponse;
  private Stream streamWriter;

  public HttpTask(string url, string dlPath, string dlName = null, bool update = false) {
    mUrl = url;
    DlPath = dlPath;
    DlName = dlName;

    bufferRead = null;
    request = null;
    response = null;
    streamResponse = null;
    streamWriter = null;
    mDispatcher = Dispatcher.CurrentDispatcher;
  }

  public string DlPath { get; }

  public string DlName { get; private set; }

  // Abort the request if the timer fires.
  /*private static void TimeoutCallback(object state, bool timedOut)
{
  if (timedOut)
  {
      HttpWebRequest request = state as HttpWebRequest;
      if (request != null)
          request.Abort();
  }
}*/

  public bool Start() {
    canceled = false;
    try {
      // Create a HttpWebrequest object to the desired URL. 
      request = (HttpWebRequest)WebRequest.Create(mUrl);
      //myHttpWebRequest.AllowAutoRedirect = false;

      /**
      * If you are behind a firewall and you do not have your browser proxy setup
      * you need to use the following proxy creation code.

      // Create a proxy object.
      WebProxy myProxy = new WebProxy();

      // Associate a new Uri object to the _wProxy object, using the proxy address
      // selected by the user.
      myProxy.Address = new Uri("http://myproxy");


      // Finally, initialize the Web request object proxy property with the _wProxy
      // object.
      myHttpWebRequest.Proxy=myProxy;
      ***/

      bufferRead = new byte[MfBufferSize];
      mOffset = 0;

      // Start the asynchronous request.
      var result = request.BeginGetResponse(RespCallback, this);

      // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
      //ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), request, DefaultTimeout, true);          
      return true;
    }
    catch (Exception e) {
      Console.WriteLine("\nMain Exception raised!");
      Console.WriteLine("\nMessage:{0}", e.Message);
    }

    return false;
  }

  public void Cancel() {
    canceled = true;
    request?.Abort();
  }

  private void Finish(int success, int errCode, Exception error = null) {
    // Release the HttpWebResponse resource.
    if (response != null) {
      response.Close();
      streamResponse?.Close();
      streamWriter?.Close();
    }

    response = null;
    request = null;
    streamResponse = null;
    bufferRead = null;

    if (success == 1) {
      try {
        if (File.Exists(DlPath + @"\" + DlName))
          File.Delete(DlPath + @"\" + DlName);
        File.Move(DlPath + @"\" + DlName + ".tmp", DlPath + @"\" + DlName);
      }
      catch {
        AppLog.Line("Failed to rename download {0}", DlPath + @"\" + DlName + ".tmp");
        DlName += ".tmp";
      }

      try {
        File.SetLastWriteTime(DlPath + @"\" + DlName, lastTime);
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
      } // set last mod time
    }
    else if (success == 2) {
      AppLog.Line("File already downloaded {0}", DlPath + @"\" + DlName);
    }
    else {
      try {
        File.Delete(DlPath + @"\" + DlName + ".tmp");
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
      } // delete partial file

      AppLog.Line("Failed to download file {0}", DlPath + @"\" + DlName);
    }

    Finished?.Invoke(this, new FinishedEventArgs(success > 0 ? 0 : canceled ? -1 : errCode, error));
  }

  private static string GetNextTempFile(string path, string baseName) {
    for (var i = 0; i < 10000; i++)
      if (!File.Exists(Path.Combine(path, $"{baseName}_{i}.tmp")))
        return $"{baseName}_{i}";
    return baseName;
  }

  private static void RespCallback(IAsyncResult asynchronousResult) {
    var success = 0;
    var errCode = 0;
    Exception error = null;
    var task = (HttpTask)asynchronousResult.AsyncState;
    try {
      // State of request is asynchronous.
      task.response = (HttpWebResponse)task.request.EndGetResponse(asynchronousResult);

      errCode = (int)task.response.StatusCode;

      Console.WriteLine(@"The server at {0} returned {1}", task.response.ResponseUri, task.response.StatusCode);

      var fileName = Path.GetFileName(task.response.ResponseUri.ToString());
      task.lastTime = DateTime.Now;

      Console.WriteLine(@"With headers:");
      foreach (var key in task.response.Headers.AllKeys) {
        Console.WriteLine(@"	{0}:{1}", key, task.response.Headers[key]);

        if (key.Equals("Content-Length", StringComparison.CurrentCultureIgnoreCase)) {
          task.mLength = int.Parse(task.response.Headers[key]);
        }
        else if (key.Equals("Content-Disposition", StringComparison.CurrentCultureIgnoreCase)) {
          var cd = task.response.Headers[key];
          fileName = cd.Substring(cd.IndexOf("filename=", StringComparison.Ordinal) + 9).Replace("\"", "");
        }
        else if (key.Equals("Last-Modified", StringComparison.CurrentCultureIgnoreCase)) {
          task.lastTime = DateTime.Parse(task.response.Headers[key]);
        }
      }

      //Console.WriteLine(task.lastTime);

      task.DlName ??= fileName;

      FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
      if (testInfo.Exists && testInfo.LastWriteTime == task.lastTime && testInfo.Length == task.mLength) {
        task.request.Abort();
        success = 2;
      }
      else {
        // prepare download filename
        if (!Directory.Exists(task.DlPath))
          Directory.CreateDirectory(task.DlPath);
        if (task.DlName.Length == 0 || task.DlName[0] == '?')
          task.DlName = GetNextTempFile(task.DlPath, "Download");

        FileInfo info = new(task.DlPath + @"\" + task.DlName + ".tmp");
        if (info.Exists)
          info.Delete();

        // Read the response into a Stream object.
        task.streamResponse = task.response.GetResponseStream();

        task.streamWriter = info.OpenWrite();

        // Begin the Reading of the contents of the HTML page and print it to the console.
        task.streamResponse!.BeginRead(task.bufferRead, 0, MfBufferSize, ReadCallBack, task);
        return;
      }
    }
    catch (WebException e) {
      if (e.Response != null) {
        var fileName = Path.GetFileName(e.Response.ResponseUri.LocalPath);

        task.DlName ??= fileName;

        FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
        if (testInfo.Exists)
          success = 2;
      }

      if (success == 0) {
        errCode = -2;
        error = e;
        Console.WriteLine(@"
RespCallback Exception raised!");
        Console.WriteLine(@"
Message:{0}", e.Message);
        Console.WriteLine(@"
Status:{0}", e.Status);
      }
    }
    catch (Exception e) {
      errCode = -2;
      error = e;
      Console.WriteLine(@"
RespCallback Exception raised!");
      Console.WriteLine(@"
Message:{0}", e.Message);
    }

    task.mDispatcher.Invoke(() => { task.Finish(success, errCode, error); });
  }

  private static void ReadCallBack(IAsyncResult asyncResult) {
    var success = 0;
    var errCode = 0;
    Exception error = null;
    var task = (HttpTask)asyncResult.AsyncState;
    try {
      var read = task.streamResponse.EndRead(asyncResult);
      // Read the HTML page and then print it to the console.
      if (read > 0) {
        task.streamWriter.Write(task.bufferRead, 0, read);
        task.mOffset += read;

        var percent = task.mLength > 0 ? (int)(100L * task.mOffset / task.mLength) : -1;
        if (percent != task.mOldPercent) {
          task.mOldPercent = percent;
          task.mDispatcher.Invoke(() => { task.Progress?.Invoke(task, new ProgressEventArgs(percent)); });
        }

        // setup next read
        task.streamResponse.BeginRead(task.bufferRead, 0, MfBufferSize, ReadCallBack, task);
        return;
      }

      // this is done on finish
      //task.streamWriter.Close();
      //task.streamResponse.Close();
      success = 1;
    }
    catch (Exception e) {
      errCode = -3;
      error = e;
      Console.WriteLine(@"ReadCallBack Exception raised!");
      Console.WriteLine(@"Message:{0}", e.Message);
    }

    task.mDispatcher.Invoke(() => { task.Finish(success, errCode, error); });
  }

  public event EventHandler<FinishedEventArgs> Finished;
  public event EventHandler<ProgressEventArgs> Progress;

  public class FinishedEventArgs(int errCode = 0, Exception error = null) : EventArgs {
    public bool Success => errCode == 0;
    public bool Cancelled => errCode == -1;

    public string GetError() {
      if (error != null)
        return error.ToString();
      return errCode switch {
        0 => "Ok",
        -1 => "Canceled",
        _ => errCode.ToString()
      };
    }
  }

  public class ProgressEventArgs(int percent) : EventArgs {
    public readonly int Percent = percent;
  }
}
