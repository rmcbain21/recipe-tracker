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

**2026-11-08 (agent, from 09):** Cooking Sessions are stored in the same record store, in the `cookingSessions` table (`CookingCatalogue.Table`). The spec says they're device-local and never synced, so don't add that table to the sync engine's table list (`RecordTable.All` in Backpacking Tracker) or to the JSON backup. Tables to sync so far: `recipes`, `ingredients`, `books`, `tags`, `tagGroups`. The Settings page also exists already in a minimal form (`Pages/SettingsPage.razor`, linking Books and Tags), so build on it.

**2026-11-08 (agent):** Appearance and Text size are already built (outside this ticket, at the Cook's request), so this ticket's Settings part is done except for sync and backup:

- `Core/Preferences`: `Settings` (Palette, Appearance) in the **synced** `settings` table, one record with the fixed id `00000000-0000-0000-0000-000000000001`, as in Backpacking Tracker. Sync it, and give it a Sync Conflicts view if it needs one.
- `DeviceSettings` (TextSize) in the **device-only** `deviceSettings` table: the Cook chose to have text size set per device (phone and tablet are read from different distances). Leave it out of sync and backup, like `cookingSessions`.
- Ten food palettes (`Web/Layout/AppThemes.cs`): Paprika (default), Basil & Tomato, Saffron, Blueberry, Matcha, Espresso, Plum, Citrus, Cast Iron, Lavender & Honey. Five text sizes, including Extra large (125%). MainLayout applies them through `ThemeState`. After a sync, MainLayout should reload `Settings` and call `ThemeState.Set`, as Backpacking Tracker does.

**2026-11-09 (agent, from 11):** New synced table `cookLog` (`CookingCatalogue.CookLogTable`) holds Cook Log Entries. Add it to the sync table list and to the JSON backup. Deleting a Recipe tombstones its entries, so the tombstones need to sync too.
