$command = @'
#cmd.exe /C git push origin master/v%APPVEYOR_BUILD_VERSION%
'@

if($env:appveyor_repo_branch -ne 'master') {
  # push tag into master branch
  Invoke-Expression -Command:$command
}