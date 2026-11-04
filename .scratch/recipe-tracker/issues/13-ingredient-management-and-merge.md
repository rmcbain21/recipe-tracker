# 13: Ingredient list, Merge and delete

**What to build:** An Ingredients page (reached from Settings for now) lists every Ingredient with its aliases and how many Recipes use it. I can rename one, edit its aliases, Merge it into another (every Ingredient Line repointed, its name and aliases added to the other's aliases) and delete it only when no Recipe uses it. Merge follows ADR 0003: the merged Ingredient's tombstone records what it was merged into, and after every sync or JSON import, any line still pointing at it is repointed, following chains.

**Blocked by:** 02, 03

**Status:** ready-for-agent

- [ ] Core: rename, aliases, Merge with a "merged into" id on the tombstone, repointing after sync and import, and delete blocked while in use; tests cover a chain (A→B, then B→C) and a line added on another device before the sync.
- [ ] An Ingredient in an unsettled Sync Conflict can't be merged.
- [ ] Merge shows a preview ("12 lines in 9 Recipes will use *green onion*") before applying.

## Comments
