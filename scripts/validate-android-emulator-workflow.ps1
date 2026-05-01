param(
    [string]$WorkflowPath = ".github/workflows/android-emulator-manual.yml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $WorkflowPath)) {
    throw "Android manual emulator workflow is missing: $WorkflowPath"
}

$workflow = Get-Content -Raw -Path $WorkflowPath

$requiredSnippets = @(
    "name: Android Emulator Manual",
    "workflow_dispatch:",
    "Manual, optional emulator validation",
    "runner capacity is acceptable",
    "runs-on: ubuntu-latest",
    "actions/checkout@v4",
    "actions/setup-java@v4",
    "android-actions/setup-android@v3",
    "gradle/actions/setup-gradle@v4",
    "reactivecircus/android-emulator-runner@v2",
    "working-directory: android",
    "chmod +x ./gradlew",
    "./gradlew connectedDebugAndroidTest --no-daemon --stacktrace",
    "scripts/validate-android-emulator-workflow.ps1 -WorkflowPath .github/workflows/android-emulator-manual.yml",
    "actions/upload-artifact@v4",
    "android/app/build/reports/androidTests/connected/**"
)

foreach ($snippet in $requiredSnippets) {
    if (-not $workflow.Contains($snippet)) {
        throw "Android manual emulator workflow is missing required snippet: $snippet"
    }
}

$forbiddenSnippets = @(
    "push:",
    "pull_request:",
    "schedule:"
)

foreach ($snippet in $forbiddenSnippets) {
    if ($workflow.Contains($snippet)) {
        throw "Android manual emulator workflow must be workflow_dispatch-only; found forbidden snippet: $snippet"
    }
}

Write-Host "Android manual emulator workflow validation passed: $WorkflowPath"
