# Windows Update Mini Tool: Features

This page describes what the application can do and where each feature lives in the UI.

## Requirements

- Windows 7 – 11 or Windows Server 2012 – 2025.
- Administrator rights are needed to install or remove updates, to change Windows Update policies and to clean the update cache. When started without them, the tool tries to restart itself elevated.
- Three builds are published: `winUpdateMiniTool.exe` (.NET Framework 4.7.2), `winUpdateMiniTool.core.exe` (.NET 8, x64) and `winUpdateMiniTool.arm64.exe` (.NET 8, ARM64).

## Update lists

The buttons on the left switch between four lists:

- **Windows Updates**: updates available for installation.
- **Installed Updates**: updates reported as installed by the last search.
- **Hidden Updates**: updates you have hidden, so they are no longer offered.
- **Update History**: the Windows Update history of this machine, with the result of every operation.

Lists can be grouped by category (**Group Updates**), sorted by clicking a column header and filtered with **Search Filter** (`Ctrl+F`). `Ctrl+C` copies the selected rows. Selecting an update with a KB number shows a **Support URL** link to its Microsoft support article.

## Operations

The toolbar above the list works with the checked updates:

- **Search**: searches for updates using the selected update source.
- **Download**: downloads the checked updates.
- **Install**: downloads and installs the checked updates.
- **Uninstall**: removes checked installed updates (only updates Windows reports as removable).
- **Hide / Unhide**: hides pending updates or brings hidden ones back.
- **Get Links**: copies the direct download links of the checked updates to the clipboard.
- **Cancel**: stops the running operation.

After installation, the tool can restart Windows automatically: enable **Restart automatically after install** and choose a delay. A delayed restart can still be cancelled from the confirmation dialog.

## Search and download options

- **Update source** (the drop-down in the **Options** group): choose between the registered update services, such as Windows Update or Microsoft Update.
- **Register Microsoft Update**: adds the Microsoft Update service, so updates for other Microsoft products (Office and others) are offered too.
- **Include superseded**: also lists updates that were replaced by newer ones.
- **Offline Mode**: searches against the offline scan catalog `wsusscn2.cab` instead of an online service. With **Download wsusscn2.cab** the latest catalog is downloaded before each search.
- **'Manual' Download/Install**: downloads update files directly into the `Updates` folder next to the application, bypassing the Windows Update service, and installs them with `wusa`, `msiexec`, `DISM` or the update's own installer. Every file must carry a valid Microsoft digital signature, otherwise it is not installed.

## Windows Update policies

The **Auto Update** group changes the Windows Update group policies (administrator rights required):

- **Automatic Update (default)**, **Disable Automatic Update**, **Notification Only**, **Download Only** or **Scheduled & Installation** with a day and time.
- **Disable Update Facilitators**: disables the Update Orchestrator and Windows Update Medic services, so Windows cannot re-enable automatic updates. A reboot is needed for this to take full effect.
- **Block Access to WU Servers**: prevents Windows from contacting Windows Update servers.
- **Hide WU Settings Page**: hides the Windows Update page in the Settings app (Windows 10 and later).
- **Disable Store Auto Update**: stops automatic updates of Microsoft Store apps.
- **Include Drivers**: controls whether driver updates are offered together with quality updates.

Windows Home editions ignore most of these policies and Pro editions honor only some of them, so the tool disables the options that have no effect on the current edition.

## Background mode

- **Run at Windows startup (in tray)** (Options menu): starts the tool at logon and keeps it in the notification area. When enabled while the tool runs as administrator, a scheduled task starts it with administrator rights and without a UAC prompt; otherwise a registry entry starts it with normal rights.
- The drop-down in the **Background tasks** group sets how often updates are searched for automatically: daily, weekly or monthly. It is available only while **Run at Windows startup (in tray)** is enabled. The search only starts when the computer has been idle for a while.
- A notification appears when new updates are found, or when no successful search has been possible for a long time.

## Other features

- **File > Clean cache**: stops the Windows Update service, deletes downloaded files from `%windir%\SoftwareDistribution\Download` and starts the service again.
- **File > Optimize kernel size**: disables reserved storage, cleans up the component store and compresses the Windows system files. This cannot be easily undone.
- **File > Restore default settings**: resets the tool options and the Windows Update policies it manages.
- **Options > Tools > Windows Update Service**: starts or stops the Windows Update service.
- **Options > Always run as Administrator**: registers a scheduled task that starts the tool elevated without a UAC prompt. Keep the tool in a protected folder, such as Program Files, when you use this option.
- **Options > Auto-update application**: installs new versions of the tool automatically. **Help > Check for new version** checks manually.
- **Options > Themes**: Light, Dark or Auto mode, plus custom themes from JSON files (see the main README).
- **Options > Select UI font**: changes the font of the main window.

## Command line

| Option | Description |
|---|---|
| `-tray` | Start minimized to the notification area. |
| `-update` | Search for updates right after start. |
| `-online [service id]` | Use online search, optionally with the given update service ID. |
| `-offline [download\|no_download]` | Use offline mode, optionally forcing whether `wsusscn2.cab` is downloaded. |
| `-manual` | Enable manual download and installation. |
| `-provisioned` | Lock the Auto Update policy settings. |
| `-onclose "command"` | Run a command when the tool closes. |
| `-console` | Show a console window with diagnostic output. |
| `-help` | Show the list of options. |

## Configuration files

Settings are stored in `winUpdateMiniTool.ini` next to the executable. If that folder is not writable, the tool uses `Downloads\winUpdateMiniTool` instead. A few settings can only be changed in the file, in the `[Options]` section:

- `IdleDelay`: idle minutes required before an automatic search starts (default 20).
- `OfflineCab`: download URL of the offline scan catalog.
- `LoadLists=1`: load the update lists saved by the previous session at start.
- `Refresh=1`: search again automatically after installing or removing updates.

**Per-update marks.** Create `Updates.ini` in the working folder with one section per KB number to highlight updates in the lists:

```ini
; Select: check the update in the Windows Updates list
; BlackList: show it struck through in the Windows Updates list
; Remove: check it in the Installed Updates list
; Color: row background, a color name or #RGB/#RRGGBB
[KB5005565]
Select=1
BlackList=0
Remove=0
Color=#FFD0D0
```

**Custom tools.** Every subfolder of `Tools` next to the executable becomes an entry in the **Options > Tools** menu. It is described by `<folder>\<folder>.ini`:

```ini
[Root]
Name=My tool
Exec="tool.exe" /arguments
; run without a window
Silent=1
```

Instead of `Exec`, a tool can define a submenu with `Entries=N` and sections `[Entry1]` … `[EntryN]`, each with `Name`, `Exec` and `Silent`.

`Tools\Tools.ini` can run commands at start and exit:

```ini
[OnStart]
; start the Windows Update service
EnableWuAuServ=1
Exec=command
Silent=1

[OnClose]
; stop and disable the Windows Update service
DisableWuAuServ=1
Exec=command
Silent=1
```

Values must not contain trailing comments: Windows INI parsing keeps them as part of the value.

Commands from these files run with the tool's administrator rights, so make sure only administrators can modify the application folder. If the folder can be modified without administrator rights, the `OnStart` and `OnClose` commands are skipped, and a Tools menu entry asks for confirmation before running.
