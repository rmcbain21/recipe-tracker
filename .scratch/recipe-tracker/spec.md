# Spec: Recipe Tracker v1

Status: ready-for-agent

## Problem Statement

My recipes are scattered across websites, cookbooks and cooking videos. Recipe websites are cluttered, books stay in the kitchen, and videos only give a description. When I cook I'm working from my phone or tablet with messy hands, scrolling back and forth to find which ingredients a step needs and how much. I have nowhere to keep what I changed last time or how long a recipe really took me. Apps like Cookwell have good ideas, such as each step showing its own ingredients, but they're built around their own recipe library rather than mine.

## Solution

A personal app that installs from the browser on my Android phone and tablet and works offline. I capture Recipes by pasting text from anywhere (a website, a video description, or a book page copied with Google Lens). The app splits the text into Ingredient Lines and Steps, matches each line to a shared Ingredient and suggests which Ingredients each Step uses, and I confirm all of that on a review screen. Each Recipe credits its Source, such as a website or a Book and page. I organize Recipes with Tags in Tag Groups and find them with search and filters. Cooking Mode starts with a Gather checklist and then shows one Step per screen, with that Step's ingredients and amounts at the chosen Scale, my Notes, and timers. Finishing it records a Cook Log Entry. Data lives on each device and syncs between my phone and tablet through my own Google Drive, with no server.

Architecture: ADR 0001. Quantities as written: ADR 0002. Shared Ingredients: ADR 0003. Terms: `GLOSSARY.md`.

## User Stories

### Capture

1. As a Cook, I want to paste recipe text and have it split into a title, Ingredient Lines and Steps, so that I don't retype recipes.
2. As a Cook, I want each pasted ingredient line split into quantity, unit, Ingredient, preparation and optional, with the original text kept, so that a bad parse can be fixed by looking at what was there.
3. As a Cook, I want quantities like "1½", "1 1/2", "½", "2-3" and "2 to 3" understood, and lines like "salt to taste" left without a quantity, so that typical recipe text parses.
4. As a Cook, I want each Ingredient Line matched to an existing Ingredient by name or alias, or offered as a new Ingredient, and I confirm the match on the review screen, so that the same butter is the same butter in every Recipe.
5. As a Cook, I want ingredient headings such as "For the dough:" to become Ingredient Sections, so that a recipe's structure is kept.
6. As a Cook, I want a review screen before saving where I can fix any line, Step, match or Ingredient Use, so that what's saved is right.
7. As a Cook, I want to save a half-reviewed Recipe as a Draft and finish it later, so that a quick capture never blocks on tidying.
8. As a Cook, I want to type or edit any part of a Recipe by hand, so that I can capture without paste and fix anything later.
9. As a Cook, I want to share text from another Android app straight into Recipe Tracker, so that capturing from Chrome or Google Lens is one step.

### Recipe

10. As a Cook, I want a Recipe to have a title, short description, Yield ("4 servings", "24 cookies") and Stated Times (prep, cook, total), so that I know what it makes and how long it says it takes.
11. As a Cook, I want to record a Recipe's Source as a Website (URL, site name), a Book (picked from my Books, plus page), a Video (URL, channel) or Other (free text), so that I can always go back to the original.
12. As a Cook, I want to add a Book once (title, author) and pick it for every Recipe from it, so that I can find everything from one cookbook.
13. As a Cook, I want each Step to show the Ingredient Uses it needs, including partial amounts ("½ cup of the 1 cup sugar"), so that I never hunt through the ingredient list mid-step.
14. As a Cook, I want Ingredient Uses suggested from the words in each Step and confirmed by me, so that linking is quick but correct.
15. As a Cook, I want to add Notes to a Recipe, an Ingredient Line or a Step, without changing the Recipe itself, so that I keep a record of what I tried.
16. As a Cook, I want to Scale a Recipe by a multiplier or a target Yield while viewing or cooking it, with amounts rounded to cook-friendly fractions, without it being saved, so that I can make a double batch once.
17. As a Cook, I want to delete a Recipe after confirming, along with its Notes and Cook Log Entries, so that I can remove recipes I don't want.

### Ingredients

18. As a Cook, I want a list of my Ingredients with their aliases and how many Recipes use each, so that I can tidy them.
19. As a Cook, I want to Merge one Ingredient into another, with every line repointed and the old name kept as an alias, so that duplicates such as "scallion" and "green onion" become one.
20. As a Cook, I want deleting an Ingredient to be blocked while any Recipe uses it, so that no Recipe loses an ingredient.

### Organizing and finding

21. As a Cook, I want to put Tags on a Recipe and group Tags into Tag Groups (Meal, Cuisine, Equipment, Diet, Occasion to start), so that I can filter the way I think.
22. As a Cook, I want to rename, regroup, Merge and delete Tags and Tag Groups, with a rename showing on every Recipe, so that my Tags stay tidy.
23. As a Cook, I want the home screen to be my Recipe list with search across titles, Ingredients and Notes, so that I find a Recipe fast.
24. As a Cook, I want filters by Tag Group (any Tag within a group, every group across groups), Source or Book, total time, and cooked / never cooked, so that "crockpot or instant pot, dinner, under an hour" works.
25. As a Cook, I want to sort by recently added, recently cooked, Rating and title, so that I can browse different ways.
26. As a Cook, I want Drafts pinned at the top of the list until they're finished, so that I don't forget them.
27. As a Cook, I want a Surprise me button that picks a random Recipe from the current filters, so that I can break a decision deadlock.

### Cooking Mode

28. As a Cook, I want Cooking Mode to start with a Gather screen listing every Ingredient Line at the chosen Scale as a checklist, so that I set everything out first.
29. As a Cook, I want one Step per screen in large text, with that Step's Ingredient Uses and amounts and any Notes, so that I can follow along from a distance.
30. As a Cook, I want big Back and Next buttons in thumb reach, plus swipe, so that I can move with a knuckle.
31. As a Cook, I want the screen to stay awake in Cooking Mode, so that I don't have to unlock it with messy hands.
32. As a Cook, I want a Step that mentions a time ("simmer 20 minutes", "bake 25-30 min") to offer a tap-to-start timer, with several running at once and visible on every Step, so that I don't need a separate timer app.
33. As a Cook, I want to return to the same Step, Scale, Gather ticks and running timers if I leave Cooking Mode on this device, so that a phone call doesn't lose my place.
34. As a Cook, on a tablet in landscape, I want all the Ingredient Lines beside the Step with the current Step's highlighted, so that I see the whole picture.
35. As a Cook, I want tapping Done on the last Step to record a Cook Log Entry for today, so that I know when I last made it.

### Devices and sync

36. As a Cook, I want the app to install to my home screen and work fully offline, so that it works with no signal.
37. As a Cook, I want my Recipes synced between my phone and tablet through my Google Drive, with conflicts shown for me to settle, so that both devices agree.
38. As a Cook, I want to export everything to a JSON file and import it back, so that I have a backup I control.
39. As a Cook, I want light, dark or match-device appearance and a text size setting, so that it's readable in my kitchen.

## Implementation Decisions

### Modules

- **App core**: a plain C# class library (no Blazor or JavaScript) that holds the domain model and all behaviour, and exposes one application API used by the UI. Sub-areas:
  - **Recipes**: Recipe, Ingredient Sections, Ingredient Lines, Steps, Ingredient Uses, Notes, Source, Drafts, delete.
  - **Ingredients**: the shared Ingredient catalogue, aliases, matching, Merge, delete rules (ADR 0003).
  - **Books**.
  - **Quantities**: a parsed quantity (exact number or range, or none), formatting as cook-friendly fractions, and Scale. Unit conversion arrives with the Shopping List, after v1.
  - **Parsing**: text to a proposed Recipe (title, Ingredient Sections, Ingredient Lines, Steps), Ingredient Line splitting, Ingredient Use suggestions, and timer durations in Step text. It sits behind an `IRecipeParser` interface so that an LLM parser can be added later (ADR 0001). It's pure and heavily unit tested.
  - **Organizing**: Tags, Tag Groups, the filter/search/sort query, Surprise me.
  - **Cooking**: a Cooking Session's state (Scale, current Step, Gather ticks, timers) and finishing it into a Cook Log Entry.
  - **Sync engine, backup and Settings**: copied from Backpacking Tracker and renamed.
- **Ports** (copied from Backpacking Tracker): record store (IndexedDB via Dexie, one `records` table keyed by `[table+id]`), cloud file store (Google Drive `appDataFolder`, `drive.appdata` scope only), clock.
- **Web app**: the existing Blazor WebAssembly PWA with MudBlazor. Pages are thin; logic lives in the core.

### Records and schema

- Every record has a GUID `id`, a UTC `updatedAt` and a `deleted` tombstone flag from slice 1, so that sync needs no migration.
- Top-level record types: Recipe, Ingredient, Book, Tag, Tag Group, Cook Log Entry, Settings.
- **Recipe**: title, description, Draft flag, Yield (number, label), Stated Times (prep, cook, total in minutes, each optional), Source (kind: Website, Book, Video or Other; URL; site or channel name; Book id and page; free text), Tag ids, created-at, and nested inside it: Ingredient Sections (id, heading, order), Ingredient Lines (id, section id or none, order, quantity, unit text, Ingredient id, preparation, optional flag, original text), Steps (id, order, text, Ingredient Uses: Ingredient Line id plus an optional quantity and unit), and Notes (id, target: the Recipe, a line id or a step id, text, created-at).
- **Ingredient**: name, aliases, and on a tombstone an optional "merged into" Ingredient id (ADR 0003). Density, Store Section and Staple come later, with the Shopping List.
- **Book**: title, author.
- **Tag**: name, Tag Group id or none. **Tag Group**: name, order. The starter Tag Groups and Tags have fixed ids and an `updatedAt` of the Unix epoch, so every device creates identical starters and sync can tell an untouched starter from an edited one (the same as Backpacking Tracker's Default Categories).
- **Cook Log Entry**: Recipe id, date. Actual Time, Rating and the "next time" text come later with the Retro.
- **Cooking Session**: device-local, never synced, at most one per Recipe.
- Quantities are stored as written (ADR 0002): a quantity is a decimal low value plus an optional decimal high value (for ranges), and the unit is the text as written.

### Behaviour rules

- **Ingredient matching**: normalize (lower case, trim, drop a trailing plural "s"/"es") and match the name or an alias. With no match, offer "new Ingredient" with the parsed name. Preparation words after a comma, or a leading word such as "chopped", go to preparation.
- **Ingredient Use suggestions**: a Step suggests every Ingredient Line whose Ingredient name or alias appears in its text. Suggestions are shown ticked on the review screen and only saved once confirmed.
- **Partial use**: an Ingredient Use with no quantity means the whole line. Cooking Mode shows a partial use as "sugar: ½ cup (of 1 cup)".
- **Scale**: multiplies every quantity, high and low; lines with no quantity are unchanged. Display rounds to the nearest ⅛ below 1 and to ¼ above 1, and shows "0" as "a pinch". It is never saved.
- **Drafts**: any Recipe can be saved as a Draft. A Draft can't open in Cooking Mode. Finishing a Draft needs a title, at least one Ingredient Line or Step, and every line's Ingredient confirmed.
- **Filters**: OR within a Tag Group, AND across groups, AND with ungrouped Tags and with field filters (Source, Book, total time ≤ N, cooked / never cooked).
- **Search**: case-insensitive substring match on title, Ingredient names and aliases, and Note text.
- **Timers**: a Step's durations are found by pattern ("20 minutes", "1 hour", "25-30 min", "1½ hours"). A range starts at its low end, and a running timer offers "+5 min". Timers keep a target time (not a countdown), so that they survive the app reloading.
- **Wake Lock**: Cooking Mode requests a screen Wake Lock and asks for it again when the page becomes visible.

### Build order

Slices are in `issues/`, each one usable on its own. 01–02 lay the foundation, 03–08 make a Recipe, 09–11 cover cooking, and 12–14 cover organizing.

## Testing Decisions

- Test external behaviour through the core's application API with an in-memory record store and a controllable clock, the same as Backpacking Tracker. Don't test private helpers.
- The parser gets table-driven tests from real recipe text: a website copy, a video description, a Google Lens book page, and odd formats (unicode fractions, ranges, "to taste", headings, numbered and unnumbered steps).
- The sync engine and its tests come over from Backpacking Tracker unchanged apart from names.
- UI is checked by hand, plus a headless browser smoke test for offline install as in Backpacking Tracker ticket 01.

## Out of Scope

Not in v1, in rough order of what comes next: the Shopping List (Staples, Store Sections in a set order, Density and unit conversions), the Retro (Actual Time, Rating, "next time" Notes), "what can I make", Meal Plan, Variations, voice commands, photos. Deferred with no plan yet: URL import and its proxy, in-app OCR (Google Lens covers it), LLM parsing, nutrition, cooking stats, AI chat, print and share, more than one Store layout, and equipment lists on Recipes (equipment is Tags only).

## Further Notes

- Use a new Google OAuth client for Drive, not Backpacking Tracker's. Both apps are served from `rmcbain21.github.io`, and Drive's `appDataFolder` belongs to the OAuth client, so sharing one client would mean sharing one folder.
- Cookwell research (2026-11-04): its step-by-step mode showing each step's ingredients inspired user stories 13 and 29.
