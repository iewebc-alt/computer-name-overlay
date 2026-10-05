# Computer Name Overlay

A small Windows 10/11 utility that shows the computer's name in the lower-right corner of the screen. The translucent text stays above ordinary application windows, lets mouse clicks pass through, and leaves the user's wallpaper unchanged.

[Русская инструкция](docs/README.ru.md) · [Privacy](PRIVACY.md) · [Code signing policy](CODE_SIGNING.md) · [License](LICENSE)

## Download and run

Download the Windows ZIP from [Releases](https://github.com/iewebc-alt/computer-name-overlay/releases), extract it to a permanent folder, and run `ComputerNameOverlay.exe`. No installer or administrator rights are needed. Requires the Windows .NET Framework 4.x runtime included with Windows 10/11; nothing is downloaded at runtime.

**Signing status: unsigned / SignPath application preparation.** An unsigned release is explicitly named `*-unsigned.zip`. There is no claim of SignPath approval or sponsorship. Only packages ending in `*-signed.zip` and containing a valid Authenticode signature should be treated as signed.

The tray icon offers **Show on all monitors**, **Start when I sign in to Windows** (off by default), and **Exit**. The tray icon may be hidden behind the taskbar's overflow arrow. Move the EXE to its final folder before enabling startup; re-enable startup after moving it.

The overlay reads `Environment.MachineName`. A Windows restart may be necessary after renaming the computer. Only one instance runs in each Windows session. No telemetry, network calls, updater, driver, service, or scheduled task is included.

## Limits and domain deployment

Exclusive fullscreen applications, the lock screen and the secure UAC desktop can cover the overlay. Other topmost windows can temporarily cover it; the overlay refreshes its topmost position every two seconds. Visibility on every possible background cannot be guaranteed. A borderless window is preferable for games.

Code signing authenticates the publisher and file integrity; it does not override AppLocker, WDAC or organizational application-control rules. An administrator must approve the application under the organization's policy. This project does not work around execution restrictions.

## System changes and uninstall

Enabling startup adds the `ComputerNameOverlay` value to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. The multi-monitor preference is saved under `HKCU\Software\ComputerNameOverlay`. Both changes are user initiated. No machine-wide settings or wallpaper files are changed.

To remove: disable startup in the tray menu, choose **Exit**, and delete the extracted files. You may also delete `HKCU\Software\ComputerNameOverlay` to remove the remaining preference. If the EXE was moved or deleted first, remove only the `ComputerNameOverlay` startup value manually.

## Build and verify

On Windows, from a trusted checkout in PowerShell:

```powershell
.\build.ps1
.\test.ps1
.\package.ps1
```

Build scripts run only on a development/CI machine where allowed. End users only run the EXE. Scripts do not change execution policy. The build uses the Windows .NET Framework C# compiler with no downloaded libraries, NuGet packages, or bundled third-party code. Output goes to `artifacts/unsigned`; ZIPs and SHA-256 checksums go to `artifacts/release`.

Tests cover transparent pixels, contrasting text edges, long-name fitting, mouse/activation window styles, layered-window rendering and topmost placement. They briefly create a test overlay and save a generic preview; manual Windows 10/11 and multi-monitor testing is still needed. Builds are traceable to source commits; bit-for-bit reproducibility is not claimed.

GitHub Actions builds and tests pushes and pull requests. The release workflow builds tagged source on a GitHub-hosted Windows runner. Signing is disabled until the approved SignPath account is configured. See [maintainer setup](docs/SIGNPATH_SETUP.md).

## Code signing policy

See [CODE_SIGNING.md](CODE_SIGNING.md) for signing status, roles, release approval and privacy information. The [SignPath application draft](docs/SIGNPATH_APPLICATION.md) lists the remaining owner-supplied information. This is a new project; acceptance by SignPath Foundation is not guaranteed.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) and [SECURITY.md](SECURITY.md). Licensed under the [MIT License](LICENSE).
