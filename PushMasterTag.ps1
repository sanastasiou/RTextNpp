$command = @'
cmd.exe /C git remote update origin
cmd.exe /C git remote show origin
cmd.exe /C git branch -a
cmd.exe /C git fetch origin
cmd.exe /C git checkout -b master --track remotes/origin/master
#cmd.exe /C git pull origin master
#cmd.exe /C git merge %APPVEYOR_REPO_BRANCH%
#cmd.exe /C git tag -a master/v%APPVEYOR_BUILD_VERSION%
#cmd.exe /C git push origin master/v%APPVEYOR_BUILD_VERSION%
'@

if($env:appveyor_repo_branch -ne 'master') {
  # push tag into master branch
  Invoke-Expression -Command:$command
}