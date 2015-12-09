$addMasterTagCommand = @'
cmd.exe /C git fetch origin
cmd.exe /C git remote show origin
cmd.exe /C git checkout -b master origin/master
cmd.exe /C git checkout %APPVEYOR_REPO_BRANCH%
cmd.exe /C git branch -a
cmd.exe /C git merge --strategy=ours --no-commit master
cmd.exe /C git commit -m "Merging commit %APPVEYOR_REPO_COMMIT% from branch %APPVEYOR_REPO_BRANCH% - %APPVEYOR_REPO_COMMIT_MESSAGE%"
cmd.exe /C git checkout master
cmd.exe /C git merge %APPVEYOR_REPO_BRANCH%
cmd.exe /C git push origin master
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