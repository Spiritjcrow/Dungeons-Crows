#!/usr/bin/env bash
set -euo pipefail

UNITY_DIR="${UNITY_DIR:-$CI_PROJECT_DIR/UnityProject}"
RESULT_DIR="$UNITY_DIR/TestResults"
LOG_DIR="$UNITY_DIR/Logs"
mkdir -p "$RESULT_DIR" "$LOG_DIR"

if [[ -z "${UNITY_SERIAL:-}" || -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "Unity CI activation is not configured."
  echo "Set UNITY_SERIAL, UNITY_EMAIL and UNITY_PASSWORD as masked GitLab CI/CD variables."
  exit 2
fi

echo "Activating Unity ${UNITY_VERSION:-unknown} for test execution..."
unity-editor   -batchmode   -nographics   -quit   -serial "$UNITY_SERIAL"   -username "$UNITY_EMAIL"   -password "$UNITY_PASSWORD"   -projectPath "$UNITY_DIR"   -logFile "$LOG_DIR/activation.log"

cleanup() {
  echo "Returning Unity license..."
  unity-editor     -batchmode     -nographics     -quit     -returnlicense     -username "$UNITY_EMAIL"     -password "$UNITY_PASSWORD"     -logFile "$LOG_DIR/return-license.log" || true
}
trap cleanup EXIT

echo "Running Dungeons & Crows Unity EditMode tests..."
set +e
xvfb-run --auto-servernum --server-args='-screen 0 1280x720x24'   unity-editor   -batchmode   -nographics   -projectPath "$UNITY_DIR"   -runTests   -testPlatform editmode   -testResults "$RESULT_DIR/editmode-results.xml"   -logFile "$LOG_DIR/editmode.log"
code=$?
set -e

if [[ $code -ne 0 ]]; then
  echo "Unity EditMode validation failed with exit code $code."
  exit "$code"
fi

echo "Unity EditMode validation passed."
