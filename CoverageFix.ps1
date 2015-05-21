 $content = Get-Content -path coverage.xml
 $content -creplace '^\s*<File.*fullPath.*?\.il(''|").*?>\s*$', '' | Out-File coverage.xml