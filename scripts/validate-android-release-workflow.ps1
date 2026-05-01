param(
    [string]$WorkflowPath = ".github/workflows/android-release.yml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $WorkflowPath)) {
    throw "Android release workflow is missing: $WorkflowPath"
}

$workflow = Get-Content -Raw -Path $WorkflowPath

$requiredSnippets = @(
    "name: Android Release",
    "tags:",
    "workflow_dispatch:",
    "permissions:",
    "contents: write",
    "actions/setup-java@v4",
    "android-actions/setup-android@v3",
    "gradle/actions/setup-gradle@v4",
    "scripts/validate-android-release-workflow.ps1 -WorkflowPath .github/workflows/android-release.yml",
    "./gradlew testDebugUnitTest assembleDebug assembleRelease assembleDebugAndroidTest --no-daemon --stacktrace",
    "ANDROID_KEYSTORE_BASE64",
    "ANDROID_KEYSTORE_PASSWORD",
    "ANDROID_KEY_ALIAS",
    "ANDROID_KEY_PASSWORD",
    "Android signing secrets are missing",
    "apksigner",
    "apksigner verify --verbose",
    "app-release-unsigned.apk",
    "woong-monitor-android-debug.apk",
    "woong-monitor-android-test.apk",
    "woong-monitor-android-release-unsigned.apk",
    'woong-monitor-android-apks-${{ github.ref_name }}',
    "artifacts/android-release/*.apk",
    "woong-monitor-android-release",
    "softprops/action-gh-release@v2",
    "startsWith(github.ref, 'refs/tags/')"
)

foreach ($snippet in $requiredSnippets) {
    if (-not $workflow.Contains($snippet)) {
        throw "Android release workflow is missing required snippet: $snippet"
    }
}

$forbiddenSnippets = @(
    "*.jks"
)

foreach ($snippet in $forbiddenSnippets) {
    if ($workflow.Contains($snippet)) {
        throw "Android release workflow contains forbidden snippet: $snippet"
    }
}

Write-Host "Android release workflow validation passed: $WorkflowPath"
