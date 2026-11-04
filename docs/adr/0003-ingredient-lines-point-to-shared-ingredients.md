# Ingredient Lines point to shared Ingredients

Each Ingredient Line names a shared Ingredient ("butter") instead of keeping its own free-text name. The Shopping List combining butter from two Recipes, converting cups of flour into ounces to buy (Density belongs to the Ingredient), Staples, Store Sections and "what can I make" all need every Recipe's butter to be the same thing. When text is captured, the parser suggests a matching Ingredient for each line, and the Cook confirms it or creates a new one on the review screen.

## Considered Options

- **Free-text ingredient names on each line, matched by name when needed**: rejected; "unsalted butter", "butter, softened" and "Butter" would each need fuzzy matching in every feature, with no single place to keep a Density or Store Section.
- **Shared Ingredients added later**: rejected; turning existing free text into Ingredients would mean reviewing every Recipe again.

## Consequences

- Capture has one extra step: confirming Ingredient matches. Aliases ("scallion" → green onion) make matches better over time.
- Duplicates will happen, especially when the same Ingredient is created on two devices before syncing. Merge folds one into another and keeps its name as an alias. Like Backpacking Tracker's gear Merge (its ADR 0009), the merged Ingredient's tombstone records what it was merged into, so lines added on another device are repointed after sync.
- An Ingredient that any Recipe uses can't be deleted.
