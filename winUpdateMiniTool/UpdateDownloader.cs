using System;
using System.Collections.Generic;
using System.IO;
using winUpdateMiniTool.Common;

namespace winUpdateMiniTool;

/// <summary>
///     Handles the downloading of updates.
/// </summary>
internal class UpdateDownloader {
  private bool canceled;
  private int mCurrentTask;
  private HttpTask mCurTask;
  private List<Task> mDownloads;
  private string mInfo = "";
  private List<MsUpdate> mUpdates;

  /// <summary>
  ///     Starts the download process.
  /// </summary>
  /// <param name="downloads">List of tasks to download.</param>
  /// <param name="updates">Optional list of updates.</param>
  /// <returns>True if the download process started successfully, otherwise false.</returns>
  public bool Download(List<Task> downloads, List<MsUpdate> updates = null) {
    if (mDownloads != null)
      return false;

    canceled = false;
    mDownloads = downloads;
    mCurrentTask = 0;
    mInfo = "";
    mUpdates = updates;

    DownloadNextFile();
    return true;
  }

  /// <summary>
  ///     Checks if the downloader is currently busy.
  /// </summary>
  /// <returns>True if the downloader is busy, otherwise false.</returns>
  public bool IsBusy() {
    return mDownloads != null;
  }

  /// <summary>
  ///     Cancels the current download operations.
  /// </summary>
  public void CancelOperations() {
    if (mCurTask != null)
      mCurTask.Cancel();
  }

  /// <summary>
  ///     Initiates the download of the next file in the queue.
  /// </summary>
  private void DownloadNextFile() {
    while (!canceled && mDownloads.Count > mCurrentTask) {
      var download = mDownloads[mCurrentTask];

      if (mUpdates != null)
        foreach (var update in mUpdates)
          if (update.Kb.Equals(download.Kb)) {
            mInfo = update.Title;
            break;
          }

      mCurTask = new HttpTask(download.Url, download.Path, download.FileName, true); // todo update flag
      mCurTask.Progress += OnProgress;
      mCurTask.Finished += OnFinished;
      if (mCurTask.Start())
        return;
      // Failed to start this task lets try another one
      mCurrentTask++;
    }

    FinishedEventArgs args = new() {
      Downloads = mDownloads
    };
    mDownloads = null;
    args.Updates = mUpdates;
    mUpdates = null;
    Finished?.Invoke(this, args);
  }

  /// <summary>
  ///     Event handler for progress updates.
  /// </summary>
  /// <param name="sender">The source of the event.</param>
  /// <param name="args">The event data.</param>
  private void OnProgress(object sender, HttpTask.ProgressEventArgs args) {
    Progress?.Invoke(this,
        new WuAgent.ProgressArgs(mDownloads.Count,
            mDownloads.Count == 0 ? 0 : (100 * mCurrentTask + args.Percent) / mDownloads.Count,
            mCurrentTask + 1,
            args.Percent, mInfo));
  }

  /// <summary>
  ///     Event handler for when a download finishes.
  /// </summary>
  /// <param name="sender">The source of the event.</param>
  /// <param name="args">The event data.</param>
  private void OnFinished(object sender, HttpTask.FinishedEventArgs args) {
    if (!args.Cancelled) {
      var download = mDownloads[mCurrentTask];
      if (!args.Success) {
        AppLog.Line("Download failed: {0}", args.GetError());
        if (mCurTask.DlName != null && File.Exists(mCurTask.DlPath + @"\" + mCurTask.DlName))
          AppLog.Line("An older version is present and will be used.");
        else
          download.Failed = true;
      }

      download.FileName = mCurTask.DlName;
      mDownloads[mCurrentTask] = download;
      mCurTask = null;

      mCurrentTask++;
    }
    else {
      canceled = true;
    }

    DownloadNextFile();
  }

  /// <summary>
  ///     Event triggered when all downloads are finished.
  /// </summary>
  public event EventHandler<FinishedEventArgs> Finished;

  /// <summary>
  ///     Event triggered to report download progress.
  /// </summary>
  public event EventHandler<WuAgent.ProgressArgs> Progress;

  /// <summary>
  ///     Represents a download task.
  /// </summary>
  public struct Task {
    public string Url;
    public string Path;
    public string FileName;
    public bool Failed;
    public string Kb;
  }

  /// <summary>
  ///     Event arguments for the Finished event.
  /// </summary>
  public class FinishedEventArgs : EventArgs {
    public List<Task> Downloads;
    public List<MsUpdate> Updates;

    /// <summary>
    ///     Indicates whether all downloads were successful.
    /// </summary>
    public bool Success {
      get {
        foreach (var task in Downloads)
          if (task.Failed)
            return false;
        return true;
      }
    }
  }
}
