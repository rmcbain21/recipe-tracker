# 08: Scale

**What to build:** On the recipe page I can Scale a Recipe by ×½, ×1, ×1½, ×2, ×3, or set a target Yield ("make 6 servings" from 4). Every quantity, Ingredient Use amounts included, is multiplied and shown as a cook-friendly fraction. Lines with no quantity stay as they are. The Scale isn't saved; leaving the page resets it.

**Blocked by:** 06

**Status:** ready-for-human (phone check)

- [x] Core: Scale on a quantity (both ends of a range), and rounding to the nearest ⅛ below 1 and ¼ above 1; unit tests for edge cases (⅓ × 2, 1½ × ⅓, ranges).
- [x] The recipe page has a Scale control showing the scaled Yield ("6 servings (×1½)").
- [ ] The Scale passes on to Cooking Mode when started from the page (ticket 09).

## Comments

**2026-11-08 (agent):** Implemented on branch `08-scale`. 268 core tests pass (23 new in `ScaleTests`).

- **Core** (`Quantities/Scale.cs`): `Scale.By(factor)`, `Scale.ToYield(yield, target)` (needs a Yield with a number), `Scale.Presets` (×½, ×1, ×1½, ×2, ×3), and `Label` ("×1½"). `Format(quantity)` multiplies both ends of a range and rounds each. A range whose ends meet becomes one amount, and an amount that rounds to nothing shows as "a pinch". At ×1 quantities show exactly as written, so ⅓ isn't rounded to ⅜. `Format(yield)` gives "6 servings (×1½)".
- **Rounding, one deviation from the spec**: to the nearest ⅛ below 1 and ¼ above 1 as specified, and also to thirds (⅓, ⅔). Without thirds, ⅓ cup × 2 would show as ⅝ cup, which is nobody's measure. Tested: ⅓ × 2 = ⅔, 1½ × ⅓ = ½, 2–3 × 1½ = 3–4½, ¾ × 1½ = 1, 1⅛ × 3 = 3⅓, ⅛ × ¼ = a pinch.
- **View**: `RecipeView.AtScale(scale)` gives the same view with every Ingredient Line and Ingredient Use quantity scaled. Use amounts show "(of …)" at the scaled line amount. Lines with no quantity are unchanged. `YieldText` is the scaled Yield.
- **Recipe page**: a Scale row with the preset chips and, when the Yield has a number, "Make [4] servings", which works out the factor (6 from 4 highlights ×1½; 5 from 4 shows ×1¼). "Makes" shows the scaled Yield. The Scale lives only in the page and resets on leaving. It also survives adding Notes or changing Tags, which reload the Recipe. The row is hidden when no line has a quantity.

Checked in a headless browser at phone width: ×2, Make 6 and Make 5 scaled the lines, the Step uses and the Yield as expected, and leaving and reopening the page reset it to ×1. No console errors.

Not done: the third box, passing the Scale to Cooking Mode, needs ticket 09. Possible follow-ups seen while checking: units stay as written, so ×2 shows "2 cup sugar", and counted items can scale to fractions ("2½–3¾ eggs").
