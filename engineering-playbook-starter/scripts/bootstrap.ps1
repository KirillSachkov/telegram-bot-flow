param(
  [string]$TargetDir = ".",
  [string]$PlaybookDir = ".playbook"
)

$ErrorActionPreference = "Stop"

Write-Host "[bootstrap] target: $TargetDir"

New-Item -ItemType Directory -Force -Path "$TargetDir/.github" | Out-Null
New-Item -ItemType Directory -Force -Path "$TargetDir/.cursor/rules" | Out-Null
New-Item -ItemType Directory -Force -Path "$TargetDir/.cline" | Out-Null

Copy-Item -Force "$PlaybookDir/integration/github/PULL_REQUEST_TEMPLATE.md" "$TargetDir/.github/PULL_REQUEST_TEMPLATE.md"
Copy-Item -Force "$PlaybookDir/integration/copilot/instructions.example.md" "$TargetDir/.github/copilot-instructions.md"
Copy-Item -Force "$PlaybookDir/integration/cursor/.cursorrules.example" "$TargetDir/.cursor/rules/project-rules.mdc"
Copy-Item -Force "$PlaybookDir/integration/cline/rules.example.md" "$TargetDir/.cline/rules.md"

Write-Host "[bootstrap] completed"
