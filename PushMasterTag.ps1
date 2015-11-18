$addMasterTagCommand = @'
cmd.exe /C git tag v%APPVEYOR_BUILD_VERSION%
cmd.exe /C git push origin v%APPVEYOR_BUILD_VERSION%
'@

#execute on different branches than master only
if($env:appveyor_repo_branch -ne 'master') {
  # if commit message has "release" add new tag and push it
  if($env:appveyor_repo_commit_message -like "*release*")
  {
    Invoke-Expression -Command:$addMasterTagCommand
  }
}