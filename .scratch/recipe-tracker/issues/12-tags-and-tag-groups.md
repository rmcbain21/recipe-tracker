# 12: Tags and Tag Groups

**What to build:** I can tag Recipes from the editor and the recipe page, picking from my Tags grouped by Tag Group or typing a new one. A Tags page manages Tag Groups (add, rename, reorder, delete, after which their Tags become ungrouped) and Tags (rename, move to a group, Merge, delete, which removes it from Recipes after a confirmation). First run creates the starter groups: Meal (breakfast, lunch, dinner, side, dessert, snack), Cuisine (none to start), Equipment (sheet pan, crockpot, instant pot, air fryer, grill, dutch oven), Diet (vegetarian, vegan, gluten-free, dairy-free) and Occasion (weeknight, meal prep, holiday, want to try).

**Blocked by:** 03

**Status:** ready-for-human (phone check)

- [x] Core: Tag and Tag Group records; a Tag is in at most one group; names are unique, ignoring case, with whitespace trimmed (the same clean-up as Backpacking Tracker's TagRules).
- [x] Starters have fixed ids and an `updatedAt` of the Unix epoch, so two devices don't create duplicates (spec, Records and schema).
- [x] Renaming a Tag shows on every Recipe; Merge repoints Recipes and tombstones the merged Tag.
- [x] The recipe page shows its Tags as chips grouped in Tag Group order, with ungrouped last.

## Comments

**2026-11-08 (agent):** Implemented on branch `12-tags`. 216 core tests pass (15 new in `TagTests`).

- **Core** (`Core/Organizing`): `Tag` (name, group id, and on a Merge tombstone `MergedInto`) and `TagGroup` (name, sort order) records, with `TagCatalogue` (`AppCore.Tags`). Names are trimmed with inner spaces collapsed, and unique ignoring case (`TagNames`): Tags among Tags, Tag Groups among Tag Groups. Tag Groups can be added at the end, renamed, moved up or down, and deleted, which leaves their Tags ungrouped. Tags can be added (in a group or not), renamed, moved to a group or out of one, merged (Recipes repointed, the Tag tombstoned with `MergedInto`, in one `SaveAllAsync`), and deleted (taken off every Recipe, after the UI confirms). `ListTagUsesAsync` gives Recipe counts, Drafts included. A Tag whose group was deleted on another device counts as ungrouped.
- **Starters** (`StarterTags`): created on the first Tag call when the store has no Tags or Tag Groups at all, tombstones included, so deleting them doesn't bring them back. They have fixed ids (`…-0002-…` for groups, `…-0003-…` for Tags) and an `UpdatedAt` of the Unix epoch, as in Backpacking Tracker's Default Categories.
- **Recipes**: `Recipe.TagIds`; the editor's `TagIds` list; `RecipeCatalogue.SetTagsAsync` for the recipe page. Both drop ids of deleted Tags. `RecipeView.Tags` groups the Recipe's Tags in Tag Group order, by name within a group, ungrouped last.
- **UI**: `Dialogs/TagPickerDialog.razor` shows every Tag as chips grouped by Tag Group with a find box. Enter picks the only match, or adds the typed name as a new ungrouped Tag. It's used from the editor (`Components/TagField.razor`, with removable chips) and the recipe page ("Add Tags" / "Edit" beside the chips). `Components/TagChips.razor` shows each group's name before its chips. `/tags`, from Settings, has a card per Tag Group with Add Tag and a ⋯ menu (Rename, Move up, Move down, Delete), then Ungrouped, and each Tag row has its Recipe count and a ⋯ menu (Rename, Move to group, Merge into…, Delete). Merge shows a preview ("1 Recipe tagged "weeknight" will be tagged "meal prep" instead…"), and deleting asks first, saying how many Recipes lose the Tag.

Checked in a headless browser at phone width: tagged a new Recipe in the editor (two starters plus a typed new Tag), changed its Tags from the recipe page, then on the Tags page renamed, moved to a group, merged, moved a group up, deleted a group (its Tags went to Ungrouped) and deleted a Tag. The Recipe showed each change. No console errors.

Still to check by hand: the same on the phone.
