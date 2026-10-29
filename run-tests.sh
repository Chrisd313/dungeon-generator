#!/usr/bin/env bash
# Runs EditMode tests headlessly. Close the project in the Editor first.
# Usage: ./run-tests.sh [test-filter]
set -u
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.2.6f2/Editor/Unity.exe}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
RESULTS="$ROOT/Logs/TestResults.xml"
mkdir -p "$ROOT/Logs"

args=(-batchmode -nographics -projectPath "$ROOT" -runTests -testPlatform EditMode
      -testResults "$RESULTS" -logFile "$ROOT/Logs/test-run.log")
if [ $# -gt 0 ]; then args+=(-testFilter "$1"); fi

"$UNITY" "${args[@]}"
code=$?

if [ -f "$RESULTS" ]; then
  grep -o '<test-run [^>]*>' "$RESULTS" | grep -oE '(total|passed|failed|skipped)="[0-9]+"' | tr '\n' ' '
  echo
  grep -oE '<test-case [^>]*result="Failed"[^>]*>' "$RESULTS" | grep -oE 'fullname="[^"]*"' || true
else
  echo "No results file. Compile errors?"
  grep -E "error CS|Scripts have compiler errors" "$ROOT/Logs/test-run.log" | head -20
fi
exit $code
