param(
  [string]$TargetDir = ".",
  [string]$PlaybookDir = ".playbook"
)

$ErrorActionPreference = "Stop"

Write-Host "[update] syncing templates from $PlaybookDir to $TargetDir"
& "$PlaybookDir/scripts/bootstrap.ps1" -TargetDir $TargetDir -PlaybookDir $PlaybookDir
Write-Host "[update] done"
