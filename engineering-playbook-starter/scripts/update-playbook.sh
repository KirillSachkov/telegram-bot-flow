#!/usr/bin/env bash
set -euo pipefail

TARGET_DIR="${1:-.}"
PLAYBOOK_DIR="${PLAYBOOK_DIR:-.playbook}"

echo "[update] syncing templates from ${PLAYBOOK_DIR} to ${TARGET_DIR}"

bash "${PLAYBOOK_DIR}/scripts/bootstrap.sh" "${TARGET_DIR}"

echo "[update] done"
