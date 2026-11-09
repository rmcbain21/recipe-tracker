# 09: Cooking Mode

**What to build:** "Start cooking" on a finished Recipe opens Cooking Mode full screen. First comes Gather: every Ingredient Line at the current Scale as a checklist with big rows, and the Scale control. Then one Step per screen: the Step in large text with the previous and next ones faded above and below, its Ingredient Uses with scaled amounts, and Notes on the Step and on its lines. Big Back and Next buttons sit in the bottom half, and swipe works too. The screen stays awake. On a tablet in landscape, all the Ingredient Lines sit on the left with the current Step's uses highlighted, and the Step on the right. If I leave and come back on this device, I return to the same place.

**Blocked by:** 06, 07, 08

**Status:** ready-for-human (phone and tablet check)

- [x] Core: a Cooking Session (Recipe id, Scale, current position: Gather or Step n, Gather ticks) stored on this device only and never synced; starting cooking again offers "Resume" or "Start over".
- [x] Drafts can't start Cooking Mode.
- [x] Wake Lock is requested on entering and requested again when the page becomes visible; nothing breaks where Wake Lock isn't supported.
- [x] The phone layout is as described; Back and Next are at least 64 px tall and placed in the bottom third.
- [x] Tablet landscape (≥ 900 px wide, landscape) uses the two-pane layout.
- [x] Exit (✕) returns to the recipe page and keeps the session.

## Comments

**2026-11-08 (agent):** Implemented on branch `09-cooking-mode`. 291 core tests pass (10 new in `CookingTests`).

- **Core** (`Core/Cooking`): `CookingSession` (id = Recipe id, so at most one per Recipe; `ScaleFactor`; `Position`, where 0 is Gather and n is Step n; `GatherTicks`; `StartedAt`). It's kept in the `cookingSessions` table, which sync must leave out (noted on issue 02). `CookingCatalogue` (`AppCore.Cooking`): `StartAsync(recipeId, scale)` starts at Gather and replaces any session ("Start over"), refusing Drafts. `GetSessionAsync` returns null for a Recipe since deleted. `NextAsync` / `BackAsync` stop at Gather and at the last Step. `TickAsync`, `SetScaleAsync`, and `EndAsync`. Cooking never changes the Recipe record. `RecipeView.NotesForStep` gives the Step's Notes, then Notes on the lines it uses, labelled with the Ingredient.
- **Recipe page**: "Start cooking" (with the Scale, e.g. "Start cooking ×2"), not shown on Drafts. If this device has a session it asks "Resume cooking? You were on Step 2." with Resume / Start over / Cancel. Resume keeps the session's own place and Scale; Start over uses the page's Scale. The Scale control is now `Components/ScaleControl.razor`, shared with Gather.
- **Cooking Mode** (`/recipes/{id}/cook`): full screen over the app bar. The top shows ✕, the title and "Step 2 of 3 · ×2". Gather has the Scale control (larger chips) and a checklist of every line at the Scale in big rows, with ticked lines struck through. Each Step screen shows the Step in large text with the previous and next Steps faded above and below (tap them to move), its Ingredient Uses at the Scale, and its Notes. Back and Next are 72 px tall at the very bottom. Next reads "Start" on Gather and "Finish" on the last Step, which ends the session and returns to the recipe page (ticket 11 adds the Cook Log Entry there). A sideways swipe of 60 px or more moves, as do the arrow keys and Page Up/Down, and Esc exits. ✕ keeps the session. Opening `/cook` directly resumes, or starts at ×1. A Draft's `/cook` shows a message instead.
- **Wake Lock** (`wwwroot/js/wakeLock.js`): requested on entering, requested again on `visibilitychange` to visible, released on leaving. If it's unsupported or refused, nothing happens.
- **Tablet landscape** (≥ 900 px wide and landscape): every line in a left pane with the current Step's uses highlighted, and the Step on the right.

Checked in a headless browser: at phone size, started at ×2 from the page; Gather showed "2 cup sugar", "4 eggs", "⅔ cup cocoa"; the Wake Lock was held; a tick stuck; Step 1 showed its uses and both Notes; Back and Next were 72 px tall at y 816 of 900; swipe left and right and ArrowRight moved; Exit then Resume came back to Step 2 with the tick kept; Start over cleared it; Finish ended the session; a Draft had no Start cooking and its `/cook` was refused. At 1280×800 the two-pane layout showed with "⅔ cup cocoa" highlighted on Step 2. No console errors.

Still to check by hand: on the phone (swipe with a real finger, and the screen staying on with the phone untouched) and on the tablet in landscape.
