# 03: Recipe structure, shared Ingredients and manual entry

**What to build:** The real Recipe, entered by hand. I can create and edit a Recipe with a description, Yield, Stated Times, Ingredient Sections, Ingredient Lines (quantity, unit, Ingredient, preparation, optional) and Steps, and view it as a recipe page. Each Ingredient Line points to a shared Ingredient: typing a name picks an existing Ingredient by name or alias, or creates one (ADR 0003). Quantities are stored as written (ADR 0002) and shown as fractions. I can delete a Recipe after confirming.

**Blocked by:** 01

**Status:** ready-for-agent

- [x] Core: Recipe with nested Ingredient Sections, Ingredient Lines and Steps as in the spec's schema; Ingredient as its own record with aliases.
- [x] Quantities: parse and format exact numbers, fractions ("1½", "1 1/2", "½") and ranges ("2-3", "2 to 3"), with "no quantity" allowed. Unit tests cover the formats.
- [x] Editing: add, edit, reorder and remove Ingredient Lines, Sections and Steps; the Ingredient picker searches names and aliases and offers "New Ingredient".
- [x] The recipe page shows title, description, Yield, Stated Times, ingredients grouped by Section, and numbered Steps.
- [x] Deleting a Recipe tombstones it, after a confirmation.
- [ ] Tapping a Recipe on the home page opens its recipe page (`/recipes/{id}`); deep links work.

## Comments

**2026-11-06 (agent):** Implemented on branch `03-recipe-structure`. Core: `Recipe` now holds Description, Yield, Stated Times, CreatedAt, Sections, Lines and Steps; `Ingredient` is its own record (name, aliases, merged-into) in `IngredientCatalogue` (`ListIngredientsAsync`, `FindByNameAsync`, `SearchAsync`), matching names and aliases ignoring case, spacing and a trailing "s"/"es". `Quantity.TryParse` reads exact numbers, unicode and slash fractions, mixed numbers and ranges ("2-3", "2 to 3", "1-1/2" as 1½), and formats them with fraction glyphs. Editing goes through a core `RecipeEditor` (add, reorder, remove lines, sections and Steps; removing a heading keeps its lines), and `RecipeCatalogue.SaveAsync` checks it and saves the Recipe plus any new Ingredients in one `SaveAllAsync`. A new Ingredient whose name matches an existing name or alias uses that one. `DeleteRecipeAsync` tombstones. 92 core tests pass.

Deviation from the spec's schema: Sections, Lines and Steps have no `order` field; the list order inside the Recipe record is the order, since the whole Recipe syncs as one record.

UI: `/recipes/{id}` recipe page (Edit, and Delete behind a confirmation), `/recipes/new` and `/recipes/{id}/edit` editor with an Ingredient picker (`Components/IngredientPicker.razor`) that offers "New Ingredient". Home rows open the recipe page; ＋ opens the new-Recipe editor.

Still to check by hand: the editor and picker on a phone, the recipe page, and opening a `/recipes/{id}` deep link. Once those pass, tick the Editing, recipe page and deep link boxes.

**2026-11-06 (human):** The editor (sections, lines, new and existing Ingredients, reordering) and the recipe page work on the phone. The deep link check is still to do.
