# Android Emulator Evidence Runbook

This runbook is the local operator path for Android screenshot and app-switch
evidence. It is for visible developer QA only; it is not product telemetry.

## Start The Emulator

From the repository root:

```powershell
$env:ANDROID_HOME="$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT="$env:LOCALAPPDATA\Android\Sdk"
powershell -ExecutionPolicy Bypass -File scripts\start-android-emulator-stable.ps1 -AvdName Medium_Phone -Restart
```

Confirm adb can see the target device and Android finished booting:

```powershell
adb devices -l
adb -s emulator-5554 shell getprop sys.boot_completed
```

`sys.boot_completed` should print `1`. If more than one device appears in
`adb devices -l`, pass the intended serial to every evidence script with
`-DeviceSerial emulator-5554`.

## Screenshot Evidence

Run after the emulator is booted:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-ui-snapshots.ps1 -DeviceSerial emulator-5554
```

Artifacts:

```text
artifacts/android-ui-snapshots/latest/report.md
artifacts/android-ui-snapshots/latest/manifest.json
artifacts/android-ui-snapshots/latest/visual-review-prompt.md
```

If no emulator or device is connected, the script exits successfully with
`Status: BLOCKED`. Start the emulator, confirm `adb devices -l`, then rerun
after starting the emulator with the same `-DeviceSerial` value.

## App-Switch Evidence

Use the stable emulator launcher before Chrome/app-switch QA. Then run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\run-android-app-switch-qa.ps1 -DeviceSerial emulator-5554
```

Artifacts:

```text
artifacts/android-app-switch-qa/latest/report.md
artifacts/android-app-switch-qa/latest/manifest.json
artifacts/android-app-switch-qa/latest/room-assertions.json
```

If no emulator or device is connected, this script also writes
`Status: BLOCKED` with rerun guidance. Start the emulator, confirm
`adb devices -l`, then rerun after starting the emulator with
`-DeviceSerial emulator-5554`.

## Privacy Boundary

- Screenshot evidence captures Woong Monitor UI only.
- App-switch QA launches Chrome with `about:blank`, then returns to Woong
  before capturing Woong UI evidence.
- No Chrome screenshots are captured.
- No Chrome UI hierarchy is captured.
- No typed text, form input, passwords, messages, clipboard contents, browser
  page contents, or global touch coordinates are collected.
- Evidence artifacts stay local under ignored `artifacts/` folders.
