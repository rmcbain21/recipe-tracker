# 03: Recipe structure, shared Ingredients and manual entry

**What to build:** The real Recipe, entered by hand. I can create and edit a Recipe with a description, Yield, Stated Times, Ingredient Sections, Ingredient Lines (quantity, unit, Ingredient, preparation, optional) and Steps, and view it as a recipe page. Each Ingredient Line points to a shared Ingredient: typing a name picks an existing Ingredient by name or alias, or creates one (ADR 0003). Quantities are stored as written (ADR 0002) and shown as fractions. I can delete a Recipe after confirming.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Core: Recipe with nested Ingredient Sections, Ingredient Lines and Steps as in the spec's schema; Ingredient as its own record with aliases.
- [ ] Quantities: parse and format exact numbers, fractions ("1½", "1 1/2", "½") and ranges ("2-3", "2 to 3"), with "no quantity" allowed. Unit tests cover the formats.
- [ ] Editing: add, edit, reorder and remove Ingredient Lines, Sections and Steps; the Ingredient picker searches names and aliases and offers "New Ingredient".
- [ ] The recipe page shows title, description, Yield, Stated Times, ingredients grouped by Section, and numbered Steps.
- [ ] Deleting a Recipe tombstones it, after a confirmation.
- [ ] Tapping a Recipe on the home page opens its recipe page (`/recipes/{id}`); deep links work.

## Comments
