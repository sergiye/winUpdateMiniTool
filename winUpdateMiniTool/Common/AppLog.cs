using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace winUpdateMiniTool.Common;

internal class AppLog {
  private static readonly AppLog mInstance = new AppLog();
  private readonly Dispatcher mDispatcher;
  private readonly List<string> mLogList = [];

  private AppLog() {
    mDispatcher = Dispatcher.CurrentDispatcher;
    Logger += LineLogger;
  }

  public static void Line(string str, params object[] args) {
    Line(string.Format(str, args));
  }

  public static void Line(string line) {
    if (mInstance != null)
      mInstance.LogLine(line);
  }

  private void LogLine(string line) {
    mDispatcher.BeginInvoke(new Action(() => {
      mLogList.Add(line);
      while (mLogList.Count > 100)
        mLogList.RemoveAt(0);

      Logger?.Invoke(this, new LogEventArgs(line));
    }));
  }

  public static List<string> GetLog() {
    return mInstance.mLogList;
  }

  public static event EventHandler<LogEventArgs> Logger;

  private static void LineLogger(object sender, LogEventArgs args) {
    Console.WriteLine(@"LOG: " + args.Message);
  }

  public class LogEventArgs(string message) : EventArgs {
    public string Message { get; } = message;
  }
}
