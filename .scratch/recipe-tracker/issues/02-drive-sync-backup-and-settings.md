# 02: Google Drive sync, JSON backup and Settings

**What to build:** Copy Backpacking Tracker's sync, backup and settings and rename them: the sync engine (merge per record, most recent edit wins, Sync Conflicts settled by the Cook), the Google Drive adapter (`googleDrive.js`, `appDataFolder`, `drive.appdata` scope), auto-sync, the sync status icon and banner, the Sync Conflicts page, JSON export/import, and a Settings page with Appearance (light, dark or match device) and Text size. Sync Conflicts need a side-by-side view for Recipe, Ingredient, Book, Tag and Tag Group; Recipe's can start as a summary (title, line count, step count, updated) with "keep this device / keep other device".

**Blocked by:** 01

**Status:** ready-for-human (one manual step first), then ready-for-agent

- [ ] **Human:** create a **new** Google Cloud OAuth client (web) for Recipe Tracker with authorised JavaScript origins `https://rmcbain21.github.io` and `http://localhost:5312`, and give its client id for `wwwroot/appsettings.json`. Don't reuse Backpacking Tracker's client, because Drive's `appDataFolder` belongs to the OAuth client.
- [ ] The sync engine and its tests come over with names changed; the sync file is `recipe-tracker.json`.
- [ ] Records added on the phone show up on the tablet after a sync, and deletions sync as tombstones.
- [ ] A conflicting edit shows on the Sync Conflicts page with a badge on Settings, and the Cook picks a version.
- [ ] Export downloads all records as JSON, and importing that file into an empty app restores them.
- [ ] Appearance and Text size sync through the Settings record (fixed id), and the top bar has a Settings icon.

## Comments
