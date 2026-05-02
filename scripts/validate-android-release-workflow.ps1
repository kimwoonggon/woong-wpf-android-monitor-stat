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
    "Fail when Android release signing secrets are missing",
    "Android releases require ANDROID_KEYSTORE_BASE64, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS, and ANDROID_KEY_PASSWORD.",
    "apksigner",
    "apksigner verify --verbose",
    "release-readiness.json",
    "versionCode",
    "versionName",
    "signedApkSha256",
    "Get-FileHash -Algorithm SHA256",
    "ConvertTo-Json",
    "productionSyncBaseUrlConfigured",
    "syncDefaultOptIn",
    "syncDefaultOptIn = `$false",
    "playPublishingMode",
    'playPublishingMode = "manual"',
    "emulatorEvidenceRequiredBeforePublicRelease",
    "emulatorEvidenceRequiredBeforePublicRelease = `$true",
    "ANDROID_EMULATOR_EVIDENCE_PATH",
    "emulatorEvidenceStatus",
    "emulatorEvidencePath",
    "app-release-unsigned.apk",
    '$env:RUNNER_TEMP/android-release-aligned.apk',
    "woong-monitor-android-release-signed.apk",
    "artifacts/android-release/woong-monitor-android-release-signed.apk",
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
    "*.jks",
    "woong-monitor-android-debug.apk",
    "woong-monitor-android-test.apk",
    "woong-monitor-android-release-unsigned.apk",
    "artifacts/android-release/woong-monitor-android-release-aligned.apk",
    'woong-monitor-android-apks-${{ github.ref_name }}',
    "steps.signing.outputs.enabled",
    "The release workflow will publish the installable debug APK and unsigned release APK"
)

foreach ($snippet in $forbiddenSnippets) {
    if ($workflow.Contains($snippet)) {
        throw "Android release workflow contains forbidden snippet: $snippet"
    }
}

Write-Host "Android release workflow validation passed: $WorkflowPath"
