# 06: Ingredient Uses on Steps

**What to build:** Each Step shows the ingredients it uses. On the review screen (and in the editor), each Step lists suggested Ingredient Uses: every Ingredient Line whose Ingredient name or alias appears in the Step's text, ticked for me to confirm or untick, plus "add an ingredient" for ones the text doesn't name. A use can have its own amount for partial use ("½ cup" of the 1 cup sugar). The recipe page shows each Step's ingredients and amounts under it.

**Blocked by:** 05

**Status:** ready-for-human (phone check)

- [x] Core: Ingredient Uses nested in Steps (line id, optional quantity and unit); removing an Ingredient Line removes its uses.
- [x] Suggestion is a pure function tested with plurals, aliases and multi-word names ("brown sugar" mustn't also match "sugar" when both are lines).
- [x] Review and editor: ticks per suggested use; an optional amount per use.
- [x] The recipe page shows under each Step: "butter, 2 tbsp · sugar, ½ cup (of 1 cup)".
- [x] Finishing a Draft keeps confirmed uses only.

## Comments

**2026-11-08 (agent):** Implemented on branch `06-ingredient-uses`. 05's only open item is the share-target check on Android, which doesn't touch this code, so the Cook agreed to treat 05 as done for blocking. 245 core tests pass (29 new in `IngredientUseSuggesterTests` and `IngredientUseTests`).

- **Core**: `Step.Uses` holds `IngredientUse(LineId, Quantity?, Unit)`, where no amount means the whole line. Saving drops uses of lines that weren't saved (removed or blank). Use amounts are checked like line quantities.
- **Suggestion** (`Parsing/IngredientUseSuggester.Suggest`, pure): matches the Ingredient's name, aliases, and the parser's name for a new Ingredient, as whole words, ignoring case and spacing, with plurals both ways (egg/eggs, tomato/tomatoes, blueberry/blueberries). Where matches overlap the longer one wins, so "brown sugar" doesn't also suggest the sugar line, though a separate "sugar" later in the Step still does. Two lines of the same Ingredient are both suggested. `IngredientChoice` now carries an existing Ingredient's aliases (not part of its equality).
- **Ticks and when suggestions come back** (my reading of the issue): `RecipeEditor.RefreshUses()` runs after capture, before every save, and on every render of the editor form. New suggestions are ticked, except on an unchanged Step of a finished Recipe, where they're offered unticked so a use the Cook took off isn't put back by an unrelated edit. A new or changed Step gets ticked suggestions. A Draft stores unticked suggestions as `IngredientUse.Unticked` so they aren't suggested again, and Finish keeps only ticked uses. Unticked suggestions the text no longer names are dropped.
- **UI**: in the editor and on the review screen, each Step lists its uses as tick boxes ("1 cup sugar"), each ticked one with an optional amount and unit (placeholder "all"), plus an "Add an ingredient" menu of the lines not used yet. The recipe page shows the uses under each Step: "sugar, ½ cup (of 1 cup) · butter, 2 tbsp". A unit-only amount reads mid-sentence ("salt, pinch").

Checked in a headless browser at phone width: capture suggested the right ticked uses, a ½ cup amount, an untick and an added ingredient all showed correctly on the recipe page, editing kept the untick unticked, and changing a Step's text ticked the newly named butter. No console errors.

Still to check by hand: the same on the phone.
