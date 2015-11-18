$command = @'
#cmd.exe /C git push origin remotes/origin/master
'@

if($env:appveyor_repo_branch -ne 'master') {
  # push tag into master branch
  Invoke-Expression -Command:$command
}