# projectbaluga ![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)

`projectbaluga` is a kiosk-oriented WPF application that hosts a locked-down WebView2 browser. It is designed for environments where the machine should only display a limited set of web pages and remain resilient against tampering.

## Key Features

- **Restricted navigation** – URLs are validated against a whitelist and defaults can be configured.
- **Watchdog protection** – A companion DLL (`projectbaluga.dll`) monitors the main executable; if it stops, the system is forced to shut down.
- **Hardened admin access** – Admin credential is stored as a PBKDF2 hash protected with DPAPI; unlocking dialogs throttle repeated attempts.
- **Network awareness** – Loss of connectivity or manual lockdown redirects to a dedicated lock screen.
- **Configurable settings** – A built-in settings dialog lets authorized users update URLs, admin password, and power management options.

## Build & Run

1. Install the .NET Framework 4.7.2 developer pack and the WebView2 runtime.
2. Build the solution: `msbuild projectbaluga.sln` (or `dotnet build` with the appropriate SDK).
3. Run `projectbaluga.exe`; `projectbaluga.dll` must reside alongside the executable.

## Repository Layout

- `projectbaluga/` – WPF application source.
- `Watchdog/` – Process watchdog library (builds `projectbaluga.dll`).
- `projectbaluga.sln` – Solution file tying everything together.

