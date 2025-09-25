@echo off

rmdir /s /q .vs
rmdir /s /q .idea
del /s /q winUpdateMiniTool\FodyWeavers.xsd

for /f "delims=" %%e in ('dir /A:D /S /B *bin^|find /i "\bin"') do @if exist "%%e" (@rmdir /S /Q %%e)
for /f "delims=" %%e in ('dir /A:D /S /B *obj^|find /i "\obj"') do @if exist "%%e" (@rmdir /S /Q %%e)
for /f "delims=" %%e in ('dir /A:D /S /B *.vs^|find /i "\.vs"') do @if exist "%%e" (@rmdir /S /Q %%e)

git reflog expire --expire=1.days.ago --expire-unreachable=now --all
if errorlevel 1 goto error

git gc
if errorlevel 1 goto error

goto exit
:error
pause
:exit
