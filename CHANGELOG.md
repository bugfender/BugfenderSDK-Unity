# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

## [3.0.1]

### Added

- `Bugfender.EnableNativeLogCapture()` — forward native system logs to Bugfender (**Android:** logcat; **iOS 15+:** NSLog/OSLog).
- `Resources/bugfender_native_log_capture.txt` — set to `true` to enable native log capture during initialization (same behavior as the API above).
- Optional overrides using text files under `Assets/Resources/` (see [docs](https://docs.bugfender.com/docs/platforms/hybrid-platforms/bugfender-for-unity)), including `bugfender_app_key.txt` (one-line app key; useful for CI or per-build config). If a file is missing or empty, Inspector values apply where applicable.

### Fixed

- iOS Simulator build error with recent Xcode versions.

### Changed

- SDK reports type `unity` and build version `30001` to the Bugfender backend when initializing, so sessions and devices can be identified as Unity SDK traffic.

### Compatibility

- **Unity:** 2022.3 or later (including Unity 6). Tested with Unity 6000.3.11f1.
- **iOS:** Xcode 15+; iOS Simulator.
- **Android:** ARM64 devices.

### Documentation

- [Bugfender for Unity](https://docs.bugfender.com/docs/platforms/hybrid-platforms/bugfender-for-unity)
- [GitHub Releases](https://github.com/bugfender/BugfenderSDK-Unity/releases)

### Installation

See [README.md](README.md). Quick UPM (git URL): `https://github.com/bugfender/BugfenderSDK-Unity.git` — optional pin: `#v3.0.1`.

[Unreleased]: https://github.com/bugfender/BugfenderSDK-Unity/compare/v3.0.1...HEAD
[3.0.1]: https://github.com/bugfender/BugfenderSDK-Unity/releases/tag/v3.0.1
