# 01: Walking skeleton: add and list Recipes

**What to build:** The first end-to-end path, with the shape every later ticket builds on. I can open the app on my phone, add a Recipe with just a title, and see it on the home screen, and it's still there after closing and reopening the app with no signal. Copy the plumbing from Backpacking Tracker and rename it: the app core's public API (`AppCore`), the record store and clock ports, the Dexie record store adapter (vendored `wwwroot/lib/dexie`, `recordStore.js`), and the test project's in-memory fakes. See the spec's Implementation Decisions and ADR 0001.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] The core depends only on the record store and clock ports; tests use an in-memory record store and a controllable clock.
- [x] Every record has a GUID `id`, a UTC `updatedAt` from the clock and a `deleted` tombstone flag.
- [x] Through the core API I can add a Recipe (title only for now) and list Recipes; tests cover adding, listing and persistence.
- [x] The Dexie adapter stores records in one `records` table keyed by `[table+id]`; data survives a reload.
- [x] The home page lists Recipes by title and has a "＋" button that adds one.
- [ ] A Release publish installs from Chrome on Android and works offline after the first visit (the service worker caches Dexie too; `.mjs` is in its cache list).

## Comments

**2026-11-05 (agent):** Implemented. `AppCore` exposes `Recipes` (`RecipeCatalogue`: `AddRecipeAsync(title)`, `ListRecipesAsync()`), which depends only on `IRecordStore` and `IClock`. 9 core tests pass (`RecipeTests`). Ports, `StoredRecord`, `RuleViolationException`, the fakes, `DexieRecordStore`, `SystemClock`, `recordStore.js` (database `recipe-tracker`) and vendored Dexie were copied from Backpacking Tracker and renamed. The home page lists Recipes and its ＋ opens a title dialog. A Release publish puts `lib/dexie/dexie.min.mjs` and `js/recordStore.js` in the service worker's asset manifest.

Still to check by hand: data surviving a reload in the browser, and installing from Chrome on Android and working offline after the first visit. Once those pass, tick the last two boxes.

**2026-11-06 (human):** Ran the app: a Recipe I added was still there after a reload. The Android install and offline check is still to do.
