# 06: Ingredient Uses on Steps

**What to build:** Each Step shows the ingredients it uses. On the review screen (and in the editor), each Step lists suggested Ingredient Uses: every Ingredient Line whose Ingredient name or alias appears in the Step's text, ticked for me to confirm or untick, plus "add an ingredient" for ones the text doesn't name. A use can have its own amount for partial use ("½ cup" of the 1 cup sugar). The recipe page shows each Step's ingredients and amounts under it.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Core: Ingredient Uses nested in Steps (line id, optional quantity and unit); removing an Ingredient Line removes its uses.
- [ ] Suggestion is a pure function tested with plurals, aliases and multi-word names ("brown sugar" mustn't also match "sugar" when both are lines).
- [ ] Review and editor: ticks per suggested use; an optional amount per use.
- [ ] The recipe page shows under each Step: "butter, 2 tbsp · sugar, ½ cup (of 1 cup)".
- [ ] Finishing a Draft keeps confirmed uses only.

## Comments
