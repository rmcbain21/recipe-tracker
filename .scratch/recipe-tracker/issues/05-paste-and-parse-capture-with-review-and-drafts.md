# 05: Paste-and-parse capture, review screen and Drafts

**What to build:** The main way Recipes get in. "Capture" opens a big text box. I paste recipe text, and the parser (`IRecipeParser`, rule-based, in the core) proposes a title, Ingredient Sections, Ingredient Lines (each split into quantity, unit, Ingredient, preparation and optional, with the original text kept) and Steps. The review screen shows each line next to its original text, with its Ingredient match (existing Ingredient, alias, or new), and everything can be edited there. I can save as a Draft at any point, or Finish once every line's Ingredient is confirmed. The app is also an Android **share target** for text, so "Share → Recipe Tracker" from Chrome or Google Lens opens Capture already filled in.

**Blocked by:** 03

**Status:** ready-for-agent

- [x] Parser: finds the ingredient block and the steps block from headings ("Ingredients", "Instructions", "Method", "Directions") or, with no headings, from line shape (lines starting with a quantity are ingredients). Strips step numbers and bullets. A line ending in ":" inside ingredients becomes an Ingredient Section.
- [x] Ingredient Line splitting handles unicode and typed fractions, ranges, unit words and abbreviations (cup(s), c, tbsp, T, tsp, t, oz, lb, g, kg, ml, l, clove(s), can(s), pinch, dash, stick(s)), "salt and pepper to taste", "(optional)", and preparation after a comma.
- [x] Ingredient matching as in the spec's behaviour rules, shown as Matched / Alias / New on each line.
- [x] Table-driven tests from at least four real samples: a website copy, a YouTube description, a Google Lens book page and a messy one.
- [x] Drafts: saving keeps the Draft flag; a Draft lists at the top of home with a "Draft" chip; Finish checks the spec's rules.
- [ ] The web manifest declares a `share_target` (GET, `title`/`text`/`url` params) to the Capture page, which pre-fills the text box (with any shared URL stored as a Website Source).

## Comments

**2026-11-06 (agent):** Implemented on branch `05-paste-and-parse`. 161 core tests pass.

- **Parser** (`Core/Parsing`): `IRecipeParser` with `RuleBasedRecipeParser`. Finds ingredients and steps from headings (also "INGREDIENTS:", "Method", "What you'll need"), stops at Notes/Nutrition/Equipment, and with no headings splits by line shape. Skips website junk (ratings, "Jump to Recipe", "US Customary Metric", "1x 2x 3x"), video timestamps, hashtags and social sign-offs. Reads Yield ("Serves 4", "Makes 24 cookies") and Stated Times, including label-then-value lines. Joins lines that Lens wrapped: ingredients after a comma or open bracket, and steps that continue in lower case. Takes a two-line all-caps book title as one title. Strips step numbers ("1.", "2)", "Step 3:", a bare "Step 1" line) and bullets.
- **Line splitting** (`IngredientLineParser`): the listed units and abbreviations plus quart/pint/jar/package/slice/sprig/bunch/handful and the like, a size word as the unit ("2 large eggs"), bracketed sizes into the unit ("1 (15-ounce) can", "1 cup (240 ml)"), leading preparation words ("finely chopped", "freshly ground"), "(optional)" / ", optional" / "Optional:", trailing "to taste" / "for serving" as preparation, and "salt and pepper to taste" as two lines.
- **Tests**: `IngredientLineParserTests` (table of 37 lines) and `RecipeParserTests` (a website copy, a YouTube description, a Google Lens book page, a messy note, plus small cases). The samples are written to look like real captures rather than copied from real sources.
- **Matching and review**: `RecipeCatalogue.CaptureAsync(text, title, url)` returns an unsaved editor where each line suggests an Ingredient (Matched / Alias / New) that is unconfirmed until the Cook taps Confirm, picks one, or uses Confirm all. The review screen shows each line's original text and its chip.
- **Drafts**: `SaveDraftAsync` keeps suggestions as suggestions (`IngredientLine.MatchUnconfirmed`, `SuggestedName`; `IngredientId` can be null on a Draft) and creates no Ingredients for them. A blank title becomes "Untitled Recipe". `SaveAsync` (Finish) needs a title, at least one line or Step, and every line's Ingredient picked and confirmed. Drafts list first on home with a Draft chip, and the recipe page shows a Draft note. `AddRecipeAsync(title)` now makes a Draft.
- **Share target**: the manifest's `share_target` (GET, title/text/url) points to `/capture`, which pre-fills the text box. A shared `url`, or a line in the text that is only a URL (as Chrome adds), becomes a Website Source. `Source` is added to the Recipe now (kind, URL, name, Book id, page, text). Only the Website URL is set, shown as "From <host>" on the recipe page. Picking and editing a Source is still issue 04.
- **UI**: home ＋ now opens Capture, which has "Type it in instead" for the manual editor. The editor fields are shared by `Components/RecipeEditorForm.razor` and the save buttons by `RecipeSaveActions.razor` (Cancel / Save as Draft / Save or Finish).

Still to check by hand: the capture → review → Draft / Finish flow on the phone, and Share → Recipe Tracker from Chrome and Google Lens (needs a deploy, and the installed app may need reinstalling to pick up the new manifest).

**2026-11-06 (human):** Capture, review, Drafts and Finish work locally. The share target is still to check on Android after a deploy.
