# 05: Paste-and-parse capture, review screen and Drafts

**What to build:** The main way Recipes get in. "Capture" opens a big text box. I paste recipe text, and the parser (`IRecipeParser`, rule-based, in the core) proposes a title, Ingredient Sections, Ingredient Lines (each split into quantity, unit, Ingredient, preparation and optional, with the original text kept) and Steps. The review screen shows each line next to its original text, with its Ingredient match (existing Ingredient, alias, or new), and everything can be edited there. I can save as a Draft at any point, or Finish once every line's Ingredient is confirmed. The app is also an Android **share target** for text, so "Share → Recipe Tracker" from Chrome or Google Lens opens Capture already filled in.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Parser: finds the ingredient block and the steps block from headings ("Ingredients", "Instructions", "Method", "Directions") or, with no headings, from line shape (lines starting with a quantity are ingredients). Strips step numbers and bullets. A line ending in ":" inside ingredients becomes an Ingredient Section.
- [ ] Ingredient Line splitting handles unicode and typed fractions, ranges, unit words and abbreviations (cup(s), c, tbsp, T, tsp, t, oz, lb, g, kg, ml, l, clove(s), can(s), pinch, dash, stick(s)), "salt and pepper to taste", "(optional)", and preparation after a comma.
- [ ] Ingredient matching as in the spec's behaviour rules, shown as Matched / Alias / New on each line.
- [ ] Table-driven tests from at least four real samples: a website copy, a YouTube description, a Google Lens book page and a messy one.
- [ ] Drafts: saving keeps the Draft flag; a Draft lists at the top of home with a "Draft" chip; Finish checks the spec's rules.
- [ ] The web manifest declares a `share_target` (GET, `title`/`text`/`url` params) to the Capture page, which pre-fills the text box (with any shared URL stored as a Website Source).

## Comments
