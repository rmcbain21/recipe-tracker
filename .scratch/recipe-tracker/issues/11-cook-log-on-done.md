# 11: Cook Log Entry on Done

**What to build:** On the last Step, Next becomes **Done**. Tapping it records a Cook Log Entry (Recipe, today's date), ends the Cooking Session and returns to the recipe page, which shows "Cooked 3 times · last on 4 Nov 2026". This is the date-only first version; the Retro (Actual Time, Rating, "next time") later adds fields to the same record.

**Blocked by:** 09

**Status:** ready-for-human (phone check)

- [x] Core: Cook Log Entry as a top-level synced record; Recipe summary of times cooked and last cooked.
- [x] Deleting a Recipe tombstones its Cook Log Entries.
- [x] I can delete a mistaken Cook Log Entry from a small history list on the recipe page.

## Comments

**2026-11-09 (agent):** Implemented on branch `11-cook-log` (off `10-timers`). 346 core tests pass (5 new in `CookLogTests`).

- **Core**: `CookLogEntry` (Recipe id, `Date` as a `DateOnly` from `IClock.Today`) in the new **synced** `cookLog` table (`CookingCatalogue.CookLogTable`; noted on issue 02). `CookingCatalogue.FinishAsync` saves the entry and ends the Cooking Session in one `SaveAllAsync`, refusing Drafts and deleted Recipes. `ListCookLogAsync` returns entries newest first. `GetCookSummaryAsync` returns a `CookSummary`, shown as "Never cooked", "Cooked once · last on 4 Nov 2026" or "Cooked 3 times · last on 4 Nov 2026". `DeleteCookLogEntryAsync` leaves a tombstone. `RecipeCatalogue.DeleteRecipeAsync` now tombstones the Recipe's entries along with it, in one save.
- **Cooking Mode**: the last Step's button reads **Done**. It records the entry, ends the session and returns to the recipe page, so Start cooking doesn't offer Resume afterwards.
- **Recipe page**: under Start cooking, a "Cooked 2 times · last on 9 Nov 2026" line opens a short list of dates, each with a delete button and a confirmation. The Recipe's delete confirmation now says its Notes and cooking history go too.

Checked in a headless browser at 412×900: cooked a Recipe twice through Done, and the summary went from absent to "Cooked once" to "Cooked 2 times". There was no Resume offer after Done. Deleting one entry and reloading left "Cooked once". No console errors.
