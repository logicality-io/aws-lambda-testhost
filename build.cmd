@ECHO OFF

docker build ^
 -f build.dockerfile ^
 --tag aws-lambda-testhost-build .

docker run --rm -it ^
 -v /var/run/docker.sock:/var/run/docker.sock ^
 -v %cd%/artifacts:/repo/artifacts ^
 -v %cd%/.git:/repo/.git ^
 -v %cd%/temp:/repo/temp ^
 -e FEEDZ_LOGICALITY_API_KEY=%FEEDZ_LOGICALITY_API_KEY% ^
 -e NUGET_PACKAGES=/repo/temp/nuget-packages ^
 --network host ^
 -e BUILD_NUMBER=%GITHUB_RUN_NUMBER% ^
 aws-lambda-testhost-build ^
 dotnet run -p build/Build.csproj -c Release -- %*

if errorlevel 1 (
  echo Docker build failed: Exit code is %errorlevel%
  exit /b %errorlevel%
)
