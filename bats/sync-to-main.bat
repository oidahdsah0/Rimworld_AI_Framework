@echo off
echo 正在同步 extra 分支到 main 分支...
echo.

:: 检查当前分支
git branch --show-current > temp.txt
set /p CURRENT_BRANCH=<temp.txt
del temp.txt

echo 当前分支: %CURRENT_BRANCH%

:: 如果不在 extra 分支，切换到 extra
if not "%CURRENT_BRANCH%"=="extra" (
    echo 切换到 extra 分支...
    git checkout extra
    if errorlevel 1 (
        echo 错误: 无法切换到 extra 分支
        pause
        exit /b 1
    )
)

:: 确保 extra 分支是最新的
echo 拉取 extra 分支最新更改...
git pull origin extra
if errorlevel 1 (
    echo 错误: 无法拉取 extra 分支
    pause
    exit /b 1
)

:: 将 extra 分支强制推送到 main
echo 将 extra 分支内容推送到 main 分支...
git push origin extra:main --force
if errorlevel 1 (
    echo 错误: 无法推送到 main 分支
    pause
    exit /b 1
)

:: 切换到 main 分支并同步
echo 切换到本地 main 分支并同步...
git checkout main
if errorlevel 1 (
    echo 错误: 无法切换到 main 分支
    pause
    exit /b 1
)

git pull origin main
if errorlevel 1 (
    echo 错误: 无法同步本地 main 分支
    pause
    exit /b 1
)

:: 切回 extra 分支
git checkout extra

echo.
echo ✅ 同步完成！main 分支现在与 extra 分支完全一致。
echo.
pause
