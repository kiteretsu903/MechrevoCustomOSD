# Security model

## Runtime

- `requestedExecutionLevel="asInvoker"` and `uiAccess="false"`
- No process injection, UIPI bypass, firmware writes, or WMI writes
- Runtime business logic performs no network access
- Tray input is limited to five fixed command IDs
- Settings accept only `auto`, `zh`, or `en` and are capped at 4096 bytes
- Logs rotate at approximately 2 MB and retain one previous file

The `Local\` demo and stop events can be signaled by another process in the same user session. That can only request a preview or exit; it cannot carry text, paths, or executable content.

## Installer

The one-click wrapper is elevated and extracts an embedded ZIP into a new GUID-named directory directly under the system temporary directory. Every archive path is normalized and checked against that exact root before extraction. Cleanup validates the parent directory and fixed prefix before recursive deletion.

The elevated setup path limits changes to the fixed `BLDHotKeyService`, `%ProgramData%\MechrevoCustomOSD`, and the fixed scheduled task name `Mechrevo Custom OSD`. The installed task uses `RunLevel Limited`.

## Distribution

The v1.0.0 binaries are unsigned. Publish SHA256 hashes with every release and do not advise users to disable SmartScreen or antivirus protection globally. A future code-signing certificate would materially improve publisher verification.
