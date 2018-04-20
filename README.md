# Windows Update Mini Tool

## Overview

`Windows Update Mini Tool` is a tool to manage updates of Microsoft products on the Windows operating system.
It uses the ["Windows Update Agent API"](https://docs.microsoft.com/en-us/windows/win32/wua_sdk/portal-client) to identify as well as download and install missing updates.
It allows the user fine control of updates on modern (Windows 10) operating system versions, comparable to what windows 7 and 8.1 offered.

The tool only gathers information's about installed and missing updates, all data are processed local on the users own device, no personal information's of any kind are send to the cloud.

This tool is inspired by the [Windows Update Mini Tool (WUMT)](https://www.majorgeeks.com/files/details/windows_update_minitool.html), however in comparison to WUMT it is written in pure .NET instead of C/C++, and it is open source.