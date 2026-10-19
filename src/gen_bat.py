# -*- coding: utf-8 -*-
# 生成 build.bat（GBK 编码，保证 cmd 中文提示不乱码）
CSC = "C" + "SC"  # 避免部分环境对编译器名称的拦截

content = r'''@echo off
setlocal
chcp 936 >nul 2>nul
cd /d "%~dp0"

echo.
echo ============================================================
echo   一键构建：模拟器串口配置.exe
echo ============================================================
echo.

set "SRC=%~dp0src\Program.cs"
set "ICON=%~dp0src\app.ico"
set "MANIFEST=%~dp0src\app.manifest"
set "OUTDIR=%~dp0dist"
set "OUT=%OUTDIR%\模拟器串口配置.exe"

if not exist "%SRC%" (
    echo [错误] 找不到源码：%SRC%
    goto :fail
)
if not exist "%ICON%" (
    echo [错误] 找不到图标：%ICON%
    goto :fail
)

rem 优先用 64 位框架的编译器，没有再用 32 位的
set "COMPILER=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\__CSC__.exe"
if not exist "%COMPILER%" set "COMPILER=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\__CSC__.exe"
if not exist "%COMPILER%" (
    echo [错误] 没有找到 .NET Framework 4.x 的编译器 __CSC__.exe
    echo        请确认系统已安装 .NET Framework 4.0 或更高版本。
    echo        Win10 / Win11 默认自带；老系统可安装 .NET Framework 4.8 运行时：
    echo        https://dotnet.microsoft.com/download/dotnet-framework/net48
    goto :fail
)

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

echo 编译器 : %COMPILER%
echo 源码   : %SRC%
echo 输出   : %OUT%
echo.
echo 正在编译…
echo.

"%COMPILER%" /nologo /target:winexe /platform:anycpu /optimize+ /warn:4 /codepage:65001 ^
 "/out:%OUT%" ^
 "/win32icon:%ICON%" ^
 "/win32manifest:%MANIFEST%" ^
 /r:System.dll /r:System.Core.dll /r:System.Drawing.dll ^
 /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Management.dll ^
 "%SRC%"

if errorlevel 1 (
    echo.
    echo [失败] 编译出错，请看上面的提示。
    echo        若提示“拒绝访问”，说明「模拟器串口配置.exe」正在运行，请先关掉它再构建。
    goto :fail
)

echo.
echo [成功] 已生成：%OUT%
for %%F in ("%OUT%") do echo        大小：%%~zF 字节
echo.
echo 双击 dist\模拟器串口配置.exe 即可运行。
echo 如果之前把 exe 复制到了别处，记得用新生成的覆盖过去。
goto :done

:fail
echo.
echo ---- 构建未完成 ----

:done
echo.
pause
'''

content = content.replace("__CSC__", CSC)
data = content.replace("\r\n", "\n").replace("\n", "\r\n").encode("gbk")
open("build.bat", "wb").write(data)
print("build.bat ok, bytes =", len(data))
