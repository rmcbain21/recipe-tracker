# 08: Scale

**What to build:** On the recipe page I can Scale a Recipe by ×½, ×1, ×1½, ×2, ×3, or set a target Yield ("make 6 servings" from 4). Every quantity, Ingredient Use amounts included, is multiplied and shown as a cook-friendly fraction. Lines with no quantity stay as they are. The Scale isn't saved; leaving the page resets it.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] Core: Scale on a quantity (both ends of a range), and rounding to the nearest ⅛ below 1 and ¼ above 1; unit tests for edge cases (⅓ × 2, 1½ × ⅓, ranges).
- [ ] The recipe page has a Scale control showing the scaled Yield ("6 servings (×1½)").
- [ ] The Scale passes on to Cooking Mode when started from the page (ticket 09).

## Comments
