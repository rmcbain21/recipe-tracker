# Store Ingredient Line quantities and units as written

Backpacking Tracker stores every weight in grams and converts only for display (its ADR 0002). Recipe Tracker deliberately does the opposite: an Ingredient Line keeps the quantity and unit its Source gave ("1½ cups", "2 cloves", "1 can"), along with the original captured text. A recipe is a copy of someone else's instructions, so it should read the way the Source wrote it, and many units ("clove", "pinch", "can") have no metric equivalent to convert to. Conversions (tsp ↔ cup, oz ↔ g, and volume ↔ weight for Ingredients with a Density) happen only when showing a line or the Shopping List.

## Considered Options

- **Normalise to metric (grams and millilitres), as Backpacking Tracker does**: rejected; volume-to-weight needs a Density most Ingredients won't have, count units can't convert, and the Recipe would no longer match its Source.
- **Keep only the original text, no structured quantity**: rejected; Scale, the Shopping List and conversions all need a number and a unit.

## Consequences

- Combining Shopping List lines works only when their units convert; otherwise they're listed side by side for the same Ingredient.
- Scale works on the stored quantity and rounds to cook-friendly fractions when shown.
- Don't "fix" stored quantities to metric.
