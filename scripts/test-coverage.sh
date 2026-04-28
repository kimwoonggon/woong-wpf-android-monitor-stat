#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

rm -rf "${repo_root}/artifacts/TestResults" "${repo_root}/artifacts/coverage"
mkdir -p "${repo_root}/artifacts/TestResults"
mkdir -p "${repo_root}/artifacts/coverage"

cd "${repo_root}"

dotnet test Woong.MonitorStack.sln \
  --collect:"XPlat Code Coverage" \
  --settings coverage.runsettings \
  --results-directory artifacts/TestResults

dotnet tool restore
dotnet tool run reportgenerator \
  "-reports:artifacts/TestResults/**/coverage.cobertura.xml" \
  "-targetdir:artifacts/coverage" \
  "-reporttypes:Html;MarkdownSummaryGithub;Cobertura"
