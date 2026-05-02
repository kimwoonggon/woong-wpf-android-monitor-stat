param(
    [string]$WorkflowPath = ".github/workflows/android-ci.yml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $WorkflowPath)) {
    throw "Android CI workflow is missing: $WorkflowPath"
}

$workflow = Get-Content -Raw -Path $WorkflowPath

$requiredSnippets = @(
    "name: Android CI",
    "push:",
    "pull_request:",
    "permissions:",
    "contents: read",
    "actions/checkout@v4",
    "actions/setup-java@v4",
    "android-actions/setup-android@v3",
    "gradle/actions/setup-gradle@v4",
    "chmod +x ./gradlew",
    "working-directory: android",
    "./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace",
    "actions/upload-artifact@v4",
    "android/app/build/outputs/apk/debug/*.apk",
    "android/app/build/outputs/apk/androidTest/debug/*.apk",
    "android/app/build/reports/tests/testDebugUnitTest/**",
    "android/app/build/test-results/testDebugUnitTest/**",
    "name: woong-monitor-android-debug-apk",
    "name: woong-monitor-android-test-apk",
    "name: woong-monitor-android-unit-test-report",
    "if: always()",
    "if-no-files-found: ignore"
)

foreach ($snippet in $requiredSnippets) {
    if (-not $workflow.Contains($snippet)) {
        throw "Android CI workflow is missing required snippet: $snippet"
    }
}

$forbiddenSnippets = @(
    "connectedDebugAndroidTest",
    "reactivecircus/android-emulator-runner",
    "emulator"
)

foreach ($snippet in $forbiddenSnippets) {
    if ($workflow.Contains($snippet)) {
        throw "Android CI workflow contains forbidden snippet: $snippet"
    }
}

Write-Host "Android CI workflow validation passed: $WorkflowPath"
