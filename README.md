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
3. Run `projectbaluga.exe`; copy `projectbaluga.dll` from the `Watchdog` project into the same directory so the watchdog can attach.

## Deployment

To deploy the application as a kiosk:

1. Build the solution and copy `projectbaluga.exe` together with `projectbaluga.dll` to the target machine.
2. Configure Windows to launch the executable at logon or replace `explorer.exe` as the shell. For example:

   ```powershell
   reg add "HKLM\Software\Microsoft\Windows NT\CurrentVersion\Winlogon" /v Shell /t REG_SZ /d "C:\\path\\to\\projectbaluga.exe" /f
   ```

3. For additional resilience, host the watchdog as a Windows service. Create or use a small service host that references `projectbaluga.dll` and registers `ProcessWatchdog.Start`, then install it:

   ```powershell
   sc create ProjectBalugaWatchdog binPath= "C:\\path\\to\\WatchdogService.exe" start= auto
   sc start ProjectBalugaWatchdog
   ```

   The service monitors the main kiosk process and forces a shutdown if it exits unexpectedly.

### Initial Password

On first run the application looks for an environment variable named `PROJECTBALUGA_INITIAL_PASSWORD`. If present, its value is used as the initial administrator password and stored securely. When the variable is absent the application will prompt for a password and require one to be set before continuing.

## Repository Layout

- `projectbaluga/` – WPF application source.
- `Watchdog/` – Process watchdog library (builds `projectbaluga.dll`).
- `projectbaluga.sln` – Solution file tying everything together.

