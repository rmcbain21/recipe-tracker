# 14: Home: search, filters, sort and Surprise me

**What to build:** The home screen becomes the real Recipe list. It has a search box (titles, Ingredient names and aliases, Note text), a filter sheet with one row of chips per Tag Group, ungrouped Tags, Source kind or Book, total time (≤ 30 / 60 / 90 min) and cooked / never cooked. Sorting is by recently added, recently cooked or title (Rating once the Retro exists). Drafts stay pinned at the top. The active filters show as removable chips above the list. A 🎲 Surprise me button opens a random Recipe from the filtered list.

**Blocked by:** 04, 07, 11, 12

**Status:** ready-for-agent

- [ ] Core: one query (search text, Tag selections, field filters, sort) returning the list; it follows the spec's filter logic (OR within a group, AND across groups) and is tested for that, plus search over aliases and Notes.
- [ ] Each row shows title, Source, total Stated Time, Tags and "last cooked".
- [ ] The filter state persists on this device across reloads (not synced).
- [ ] Surprise me is disabled when the list is empty, and Drafts are excluded from it.

## Comments
