@echo off
setlocal enabledelayedexpansion

echo Cleaning JetBrains decompiler comments from C# files...
set count=0

for /r %%f in (*.cs) do (
    findstr /m "Decompiled with JetBrains decompiler" "%%f" >nul 2>&1
    if not errorlevel 1 (
        echo Processing: %%f
        
        REM Create temporary file
        set "tempfile=%%f.tmp"
        
        REM Process the file line by line
        set "skip=0"
        (for /f "usebackq delims=" %%a in ("%%f") do (
            set "line=%%a"
            
            REM Check if this is the start of decompiler comments
            echo !line! | findstr /c:"// Decompiled with JetBrains decompiler" >nul
            if not errorlevel 1 (
                set "skip=1"
            ) else (
                if !skip! equ 1 (
                    REM Check if we're still in the comment block
                    echo !line! | findstr /r "^// Type:" >nul
                    if not errorlevel 1 (
                        REM Still in comment block, skip
                    ) else (
                        echo !line! | findstr /r "^// Assembly:" >nul
                        if not errorlevel 1 (
                            REM Still in comment block, skip
                        ) else (
                            echo !line! | findstr /r "^// MVID:" >nul
                            if not errorlevel 1 (
                                REM Still in comment block, skip
                            ) else (
                                echo !line! | findstr /r "^// Assembly location:" >nul
                                if not errorlevel 1 (
                                    REM Still in comment block, skip
                                ) else (
                                    echo !line! | findstr /x "//" >nul
                                    if not errorlevel 1 (
                                        REM Still in comment block, skip
                                    ) else (
                                        REM End of comment block
                                        set "skip=0"
                                        echo !line!
                                    )
                                )
                            )
                        )
                    )
                ) else (
                    echo !line!
                )
            )
        )) > "!tempfile!"
        
        REM Replace original file with cleaned version
        move "!tempfile!" "%%f" >nul
        set /a count+=1
        echo   - Cleaned
    )
)

echo.
echo Processed !count! files.
echo Done!
