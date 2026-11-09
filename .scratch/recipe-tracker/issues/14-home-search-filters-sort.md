# 14: Home: search, filters, sort and Surprise me

**What to build:** The home screen becomes the real Recipe list. It has a search box (titles, Ingredient names and aliases, Note text), a filter sheet with one row of chips per Tag Group, ungrouped Tags, Source kind or Book, total time (≤ 30 / 60 / 90 min) and cooked / never cooked. Sorting is by recently added, recently cooked or title (Rating once the Retro exists). Drafts stay pinned at the top. The active filters show as removable chips above the list. A 🎲 Surprise me button opens a random Recipe from the filtered list.

**Blocked by:** 04, 07, 11, 12

**Status:** ready-for-human (phone check)

- [x] Core: one query (search text, Tag selections, field filters, sort) returning the list; it follows the spec's filter logic (OR within a group, AND across groups) and is tested for that, plus search over aliases and Notes.
- [x] Each row shows title, Source, total Stated Time, Tags and "last cooked".
- [x] The filter state persists on this device across reloads (not synced).
- [x] Surprise me is disabled when the list is empty, and Drafts are excluded from it.

## Comments

**2026-11-09 (agent):** Implemented on branch `14-home-search` (off `11-cook-log`). 358 core tests pass (12 new in `RecipeFinderTests`).

- **Core** (`Core/Organizing`): `RecipeQuery` holds search text, Tag ids, Source kind or Book, max total minutes (30 / 60 / 90), cooked / never cooked, and sort (Title, which is the default, Recently added, Recently cooked). `RecipeFinder` (`AppCore.Finder`):
  - `FindAsync` returns rows with title, Draft, Source name, total time and Tags, plus last cooked. Drafts are pinned first. Tags are OR within a Tag Group and AND across groups. Each ungrouped Tag is its own AND. A Tag merged away counts as the Tag it went into, and a deleted one is ignored.
  - Search is a case-insensitive substring match on titles, Ingredient names and aliases (and a Draft's suggested names), and Note text.
  - Total time is the stated total, or prep plus cook when only those are given. Recipes with no time drop out when a time filter is on.
  - Recently added uses `CreatedAt` (falling back to `UpdatedAt` for older Recipes). Recently cooked puts never-cooked Recipes last. Ties go by title.
  - `GetFilterOptionsAsync` offers only the Tags and Books in use, plus any already picked. `DescribeAsync` gives the active filters as chips, each with the query left when it's removed.
  - `RecipeList.Surprise(Random)` picks from the listed Recipes that aren't Drafts. `CanSurprise` is false when there are none.
  - The query is saved in the device-only `homeQuery` table (noted on issue 02).
- **Decision:** search and filters apply to Drafts too, and a Draft that matches stays pinned at the top. Otherwise a Tag filter would still show every untagged Draft.
- **Home**: the search box (debounced) sits under the title, with a sort menu at the top right. Below it are Filters (with a badge counting the filters in use) and 🎲 Surprise me (disabled when nothing cookable is listed), then "2 of 4" when filtered. Active filters show as removable chips, with Clear all when there's more than one. Each row shows its title, Draft chip, "source · time · cooked 9 Nov" and its Tags. With no matches it shows "No Recipes match." and Clear filters.
- **Filter sheet** (`Components/FilterSheet.razor`): slides up from the bottom. It has a row of chips per Tag Group, then the other Tags, Source kinds then Books, Total time and Cooked. Each tap applies at once, and the button reads "Show 2 Recipes" (or "No Recipes match"). Clear keeps the search.

Checked in a headless browser at 412×900 with four Recipes. "coconut" found the curry by Ingredient name. "crockpot or instant pot, dinner, ≤ 1 hr" gave Chili and Curry, with four chips and a badge of 4, and stayed the same after a reload. Surprise me opened one of those two. Recently cooked put the cooked Recipe first. Never cooked + breakfast + instant pot gave no matches, with Surprise me disabled. No console errors.

Still to check by hand: the sheet and chips on the phone with real data.
