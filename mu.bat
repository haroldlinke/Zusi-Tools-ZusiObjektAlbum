@echo off
rem E:\projects\FeedBuilder\FeedBuilder.exe E:\projects\vs2017\ZusiStart\FeedBuilder.config -Build
rem if %ERRORLEVEL% neq 0 goto error

rem E:\projects\vs2017\FeedFix\FeedFix\bin\Release\FeedFix.exe "E:\projects\vs2017\ZusiStart\ZusiStart\Update\feed_zusistart.xml" zusistart
rem if %ERRORLEVEL% neq 0 goto error

rem "C:\Program Files (x86)\WinSCP\WinSCP.com" /ini=nul /script=upload.scp
rem if %ERRORLEVEL% neq 0 goto error

rem ---------------------------------------------------------------------------
rem create portable

rem delete old archive
del "D:\development\transfer\ZusiObjektAlbum.zip"

rem create new archive
cd "D:\development\vs2019\ZusiObjektAlbum\ZusiObjektAlbum\bin\x64\Debug"

rem Erstellen der portable.txt --> nacharbeiten!!
rem "C:\Program Files\7-Zip\7z.exe" a -bb1 -r -tzip "D:\development\transfer\ZusiObjektAlbum.zip" > D:\development\vs2019\ZusiObjektAlbum\portable.txt

"C:\Program Files\7-Zip\7z.exe" a -r -tzip "D:\development\transfer\ZusiObjektAlbum.zip" @D:\development\vs2019\ZusiObjektAlbum\portable.txt
cd "D:\development\vs2019\ZusiObjektAlbum"

rem goto exit
goto upload
rem create WinSCP script
echo open ftp://webadmin1:n$DvYWFA#6@www.sovoma.de/ > portable.scp
echo cd /downloads >> portable.scp
echo lcd "D:\development\transfer" >> portable.scp
echo put ZusiObjektAlbum.zip >> portable.scp
echo exit >> portable.scp
goto exit

rem upload
:upload
"C:\Program Files (x86)\WinSCP\WinSCP.com" /ini=nul /script=portable.scp
if %ERRORLEVEL% neq 0 goto error

:exit
exit /b 0

:error
echo "oops, something went wrong"
exit /b 1