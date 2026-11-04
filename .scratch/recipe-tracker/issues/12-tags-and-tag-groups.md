# 12: Tags and Tag Groups

**What to build:** I can tag Recipes from the editor and the recipe page, picking from my Tags grouped by Tag Group or typing a new one. A Tags page manages Tag Groups (add, rename, reorder, delete, after which their Tags become ungrouped) and Tags (rename, move to a group, Merge, delete, which removes it from Recipes after a confirmation). First run creates the starter groups: Meal (breakfast, lunch, dinner, side, dessert, snack), Cuisine (none to start), Equipment (sheet pan, crockpot, instant pot, air fryer, grill, dutch oven), Diet (vegetarian, vegan, gluten-free, dairy-free) and Occasion (weeknight, meal prep, holiday, want to try).

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Core: Tag and Tag Group records; a Tag is in at most one group; names are unique, ignoring case, with whitespace trimmed (the same clean-up as Backpacking Tracker's TagRules).
- [ ] Starters have fixed ids and an `updatedAt` of the Unix epoch, so two devices don't create duplicates (spec, Records and schema).
- [ ] Renaming a Tag shows on every Recipe; Merge repoints Recipes and tombstones the merged Tag.
- [ ] The recipe page shows its Tags as chips grouped in Tag Group order, with ungrouped last.

## Comments
