#!/usr/bin/env bash
set -euo pipefail

TARGET_DIR="${1:-.}"
PLAYBOOK_DIR="${PLAYBOOK_DIR:-.playbook}"

echo "[bootstrap] target: ${TARGET_DIR}"

mkdir -p "${TARGET_DIR}/.github"
mkdir -p "${TARGET_DIR}/.cursor/rules"
mkdir -p "${TARGET_DIR}/.cline"

cp -f "${PLAYBOOK_DIR}/integration/github/PULL_REQUEST_TEMPLATE.md" "${TARGET_DIR}/.github/PULL_REQUEST_TEMPLATE.md"
cp -f "${PLAYBOOK_DIR}/integration/copilot/instructions.example.md" "${TARGET_DIR}/.github/copilot-instructions.md"
cp -f "${PLAYBOOK_DIR}/integration/cursor/.cursorrules.example" "${TARGET_DIR}/.cursor/rules/project-rules.mdc"
cp -f "${PLAYBOOK_DIR}/integration/cline/rules.example.md" "${TARGET_DIR}/.cline/rules.md"

echo "[bootstrap] completed"
