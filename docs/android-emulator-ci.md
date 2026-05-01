# Android Manual Emulator CI

The Android emulator-backed workflow is manual and optional. It is intentionally
gated by `workflow_dispatch` so `connectedDebugAndroidTest` does not run on
every branch update.

Use the workflow when emulator evidence is useful and runner capacity is acceptable.
The regular Android CI workflow remains the cheaper default for unit tests and
APK packaging.

The manual workflow runs from `android/` with the Gradle wrapper:

```powershell
./gradlew connectedDebugAndroidTest --no-daemon --stacktrace
```

Remaining caveats:

- GitHub-hosted emulator startup can be slow or flaky.
- The workflow should be rerun only when the failure looks infrastructure
  related rather than product related.
- Local emulator screenshot scripts remain the better source for visual
  evidence when a developer machine already has an attached emulator.
