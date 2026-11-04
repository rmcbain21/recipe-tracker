# Recipe Tracker

A personal app for one cook to capture recipes from websites, books and videos, and to cook from them on a phone or tablet.

## Recipes

**Cook**:
The one person who uses the app: captures Recipes, cooks from them and keeps notes on them.
_Avoid_: User, chef

**Recipe**:
A dish the Cook has captured, made up of its ingredients and steps, and credited to the Source it came from.
_Avoid_: Dish, entry, card

**Source**:
Where a Recipe came from: a Website, a Book, a Video, or Other (such as a family member). A Recipe has one Source.
_Avoid_: Origin, reference, credit

**Book**:
A cookbook the Cook owns or borrowed, recorded once and picked as the Source of every Recipe taken from it, along with the page.
_Avoid_: Cookbook (as a separate term), publication

**Variation**:
A Recipe the Cook made from another Recipe, linked to the original, for when their version has really diverged. Small changes are Notes or edits, not Variations.
_Avoid_: Remix, fork, version

**Ingredient**:
A food the Cook buys or keeps, such as butter or all-purpose flour, shared by every Recipe that uses it. It can have aliases, so "unsalted butter" and "butter" can be the same Ingredient.
_Avoid_: Food, item, product

**Ingredient Line**:
One line of a Recipe's ingredients: a quantity (which may be a range or missing), a unit as written, the Ingredient, a preparation such as "diced", whether it is optional, and the original text it was captured from.
_Avoid_: Ingredient (for the line itself), row, item

**Ingredient Section**:
A heading that groups some of a Recipe's Ingredient Lines, such as "For the dough".
_Avoid_: Group, component, sub-recipe

**Step**:
One numbered instruction in a Recipe. A Recipe's Steps are one list, not split by Ingredient Section.
_Avoid_: Instruction, direction

**Ingredient Use**:
A Step's use of an Ingredient Line, with an optional amount when the Step uses only part of it, such as half the sugar. With no amount, the Step uses the whole line.
_Avoid_: Step ingredient, link

**Note**:
The Cook's comment on a Recipe, an Ingredient Line or a Step, such as "used half the sugar, still sweet enough". A Note never changes the Recipe; changing the Recipe is an edit.
_Avoid_: Comment, annotation, modification

**Scale**:
A temporary multiplier on a Recipe's quantities while viewing or cooking it, such as ×2 or "make 6 servings". It isn't saved and leaves lines with no quantity as they are.
_Avoid_: Resize, multiply

**Draft**:
A Recipe the Cook saved before finishing its review, such as a quick paste to tidy up later. A Draft can't be cooked in Cooking Mode until it is finished.
_Avoid_: Unfinished, pending, inbox

## Cooking

**Cooking Mode**:
The hands-busy way of following a Recipe: first the Gather checklist, then one Step per screen with that Step's Ingredient Uses, Notes and timers.
_Avoid_: Cook view, step-by-step, guided mode

**Gather**:
The first screen of Cooking Mode: every Ingredient Line at the chosen Scale as a checklist, for setting things out before the first Step.
_Avoid_: Mise en place, prep list

**Cook Log Entry**:
A record that the Cook made a Recipe on a given day, added when they finish Cooking Mode, optionally with an Actual Time, a Rating and a "next time" thought. A Recipe's Cook Log Entries are its cooking history.
_Avoid_: Cook (for the event), session, attempt

**Retro**:
The optional questions asked when the Cook finishes Cooking Mode: how long it really took, a Rating, and what to do differently next time. A "next time" answer becomes a Note on the Recipe.
_Avoid_: Review, feedback, survey

**Rating**:
The Cook's score from 1 to 5 for one Cook Log Entry. A Recipe shows its latest Rating and its average.
_Avoid_: Stars, score, favorite

**Yield**:
How much a Recipe makes: a number and what it counts, such as "4 servings", "24 cookies" or "one 9×13 pan". Scale works from it.
_Avoid_: Servings (for the field), portions, makes

**Stated Time**:
The prep, cook and total time a Recipe's Source gives. It is kept as stated and shown next to the Cook's average Actual Time.
_Avoid_: Estimated time, expected time

**Actual Time**:
How long the Cook really took to make a Recipe on one Cook Log Entry.
_Avoid_: Real time, duration

## Shopping

**Shopping List**:
The Cook's one list of things to buy, filled from Recipes at a Scale or by hand. Lines for the same Ingredient are combined when their units convert, and each line remembers which Recipes it is for.
_Avoid_: Grocery list, cart

**Staple**:
An Ingredient the Cook always has, such as salt or oil. Staples are left off the Shopping List by default and don't count as missing when searching by Ingredients on hand.
_Avoid_: Pantry item, basic

**Density**:
How much a set volume of an Ingredient weighs, such as a cup of flour weighing about 4¼ oz. Volume and weight convert only for Ingredients that have one.
_Avoid_: Conversion factor, weight per cup

**Store Section**:
The part of the store an Ingredient is found in, such as Produce, Dairy or Frozen. The Shopping List groups its lines by Store Section, in the order the Cook walks the store.
_Avoid_: Aisle, department, category

**Meal Plan**:
The Recipes the Cook plans to make on each day of a week, used to fill the Shopping List.
_Avoid_: Menu, schedule, calendar

**Merge**:
Folding a duplicate Ingredient into another, such as "scallion" into "green onion": every Ingredient Line that used it now uses the other, and its name becomes an alias.
_Avoid_: Combine, dedupe

## Organizing

**Tag**:
A label the Cook puts on Recipes to find them, such as "crockpot", "dinner" or "weeknight", for things only the Cook can judge. A Tag is shared by every Recipe that has it, so renaming it renames it everywhere. Facts the app already knows about a Recipe, such as its Source or time, are not Tags.
_Avoid_: Label, keyword, category

**Tag Group**:
A named set of Tags the Cook defines, such as Meal, Cuisine or Equipment, that the filters show as one row. A Tag is in one Tag Group or in none. Picking several Tags from one group finds Recipes with any of them; picking from different groups finds Recipes that match every group.
_Avoid_: Category, facet, type
