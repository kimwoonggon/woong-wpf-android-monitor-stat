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
    "actions/setup-java@v4",
    "gradle/actions/setup-gradle@v4",
    "./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace",
    "actions/upload-artifact@v4",
    "android/app/build/outputs/apk/debug/*.apk",
    "android/app/build/outputs/apk/release/*.apk",
    "android/app/build/outputs/apk/androidTest/debug/*.apk",
    "android/app/build/reports/tests/testDebugUnitTest/**"
)

foreach ($snippet in $requiredSnippets) {
    if (-not $workflow.Contains($snippet)) {
        throw "Android CI workflow is missing required snippet: $snippet"
    }
}

Write-Host "Android CI workflow validation passed: $WorkflowPath"
