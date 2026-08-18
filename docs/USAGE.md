# Usage

## Portable preview

```console
.\app\MechrevoCustomOSD.exe --demo
.\app\MechrevoCustomOSD.exe --stop
```

Portable execution does not modify services, scheduled tasks, or ProgramData.

## Tray menu

- Automatic language selection from the Windows user UI language
- Simplified Chinese
- English
- Show preview
- Exit

Settings and two rotating logs are stored under `%LOCALAPPDATA%\MechrevoCustomOSD`.

## Install and uninstall

The release one-click installer embeds everything it needs, requests elevation once, records and disables `BLDHotKeyService` to prevent duplicate OSDs from conflicting, and creates a `RunLevel Limited` logon task. It does not delete OEM files.

To uninstall and restore the recorded OEM service state, run the same one-click installer again and select the uninstall option.

OEM program files are never deleted. LocalAppData settings and logs remain until the user removes them manually.
