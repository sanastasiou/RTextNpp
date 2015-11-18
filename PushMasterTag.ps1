$command = @'
cmd.exe /C git fetch origin
cmd.exe /C git remote show origin
cmd.exe /C git checkout -b master origin/master
cmd.exe /C git checkout %APPVEYOR_REPO_BRANCH%
cmd.exe /C echo After %APPVEYOR_REPO_BRANCH% checkout...
cmd.exe /C git branch -a
cmd.exe /C git merge --strategy=ours --no-commit master
cmd.exe /C git commit -m "Merging commit %APPVEYOR_REPO_COMMIT% from branch %APPVEYOR_REPO_BRANCH% - %APPVEYOR_REPO_COMMIT_MESSAGE%"
cmd.exe /C git checkout master
cmd.exe /C git merge %APPVEYOR_REPO_BRANCH%
cmd.exe /C git add *AssemblyInfo.cs
cmd.exe /C git commit -m "Bumped up version to : v%APPVEYOR_BUILD_VERSION%"
cmd.exe /C git status
#  - git tag -a -m "RTextNpp Tag : v%APPVEYOR_BUILD_VERSION%" v%APPVEYOR_BUILD_VERSION%
#  - git tag -l
cmd.exe /C git push origin master
'@

#execute on different branches than master only
if($env:appveyor_repo_branch -ne 'master') {
  # push tag into master branch
  Invoke-Expression -Command:$command
}