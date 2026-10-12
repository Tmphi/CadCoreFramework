# WiX Installer

Produces a single-click MSI that installs CadCoreFramework into AutoCAD's
auto-load folder. End users do not need to NETLOAD anything, do not need
admin rights, and uninstall through Windows Settings → Apps.

## What's shipped

| File                         | Purpose |
| :--------------------------- | :------ |
| `CadCoreFramework.wxs`       | WiX 4 source: declares product GUID, install location, UI, upgrade behavior. |
| `build-msi.ps1`              | One-command runner that invokes `wix build` with the right args. |

## Prerequisites

- **WiX 4+ global tool** (`dotnet tool install --global wix`). `build-msi.ps1`
  installs it automatically if missing.
- **The packaged bundle** at `dist/CadCoreFramework.bundle/` — run
  `.\package.ps1` first.

## Build

```powershell
# From repo root:
.\Deployment\installer\build-msi.ps1 -Version 1.2.0.0
```

Output: `dist/CadCoreFramework.msi`.

## Where it installs

Per-user, into:

```
%APPDATA%\Autodesk\ApplicationPlugins\CadCoreFramework.bundle\
```

AutoCAD scans that folder on every startup and auto-loads anything with a
valid `PackageContents.xml` (via `LoadOnAutoCADStartup="True"`). No `NETLOAD`,
no registry entries.

## Upgrade behavior

`MajorUpgrade` is enabled. Installing MSI version N when N-1 is installed
automatically uninstalls N-1 first. **Always bump `-Version`** between
releases, otherwise WiX will refuse the install.

## Before shipping to real users

Edit `CadCoreFramework.wxs` and change:

1. **`UpgradeCode`** — the GUID at the top. This must be **unique per product**
   and **never change** for the lifetime of your product. Generate once:
   `[guid]::NewGuid().ToString('D').ToUpper()` and paste in.
2. **`Manufacturer`** — your company/author name.
3. **`Name`** — your product's display name shown in Add/Remove Programs.
4. (Optional) Uncomment the `WixUILicenseRtf` block and ship a `License.rtf`
   if you want the installer to show a EULA.

## Signing

Code-signed MSIs don't trigger SmartScreen warnings:

```powershell
signtool sign /tr http://timestamp.digicert.com /td sha256 /fd sha256 /a dist\CadCoreFramework.msi
```

(Requires a code-signing certificate in your user store.)

## Why MSI and not AutoCAD Autoloader Registry?

Autoloader uses `HKCU\...\ApplicationPlugins\...` registry keys or the
`%AppData%\...\ApplicationPlugins` folder. The folder approach is simpler
and MSI handles the folder+contents+upgrade+uninstall cleanly in one step.
Registry-based autoloading needs admin rights for machine-wide and has
flaky uninstall behavior when the plugin's product GUID collides with
another plugin.
