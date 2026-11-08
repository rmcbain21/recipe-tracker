# 07: Notes on Recipes, Ingredient Lines and Steps

**What to build:** I can add, edit and delete Notes on a Recipe, an Ingredient Line or a Step. A Note never changes the Recipe. On the recipe page, a Recipe's Notes sit under its header and line and Step Notes show as a small note icon that expands in place, with the newest first.

**Blocked by:** 03

**Status:** ready-for-human (phone check)

- [x] Core: Notes nested in the Recipe with a target (Recipe, line id or step id), text and created-at; deleting a line or Step deletes its Notes.
- [x] Adding a Note from the recipe page with a long-press or a ⋯ menu on the line or Step.
- [ ] Notes show on the recipe page as described, and are searchable later (ticket 14).

## Comments

**2026-11-08 (agent):** Implemented on branch `07-notes`. 201 core tests pass (11 new in `NoteTests`).

- **Core**: `Note(Id, TargetId, Text, CreatedAt)` nested in `Recipe.Notes`; `TargetId` is a line or Step id, or null for the whole Recipe. `RecipeCatalogue.AddNoteAsync` / `EditNoteAsync` / `DeleteNoteAsync` change only the Notes, and bump the Recipe's `UpdatedAt` so the Note syncs. Blank Notes, and Notes on a line or Step not in the Recipe, are refused. Editing keeps a Note's date and place. Drafts can have Notes. `RecipeView.RecipeNotes` and `NotesFor(id)` give them newest first.
- **Deleting lines and Steps**: saving from the editor drops the Notes on lines and Steps that are gone. It takes the Notes from the Recipe as stored at save time, so a Note added while the editor was open isn't lost.
- **Recipe page**: the Recipe's Notes sit under the header in a soft panel with their date and an Edit / Delete menu. Each line and Step has a ⋯ menu with "Add a Note", and a long-press (the context menu) adds one too. A line or Step with Notes shows a note icon with the count that expands them in place, and it opens after adding one. The header ⋯ menu has "Add a Note" for the whole Recipe.

Checked in a headless browser at phone width: Notes added to the Recipe, a line (⋯ menu) and a Step (long-press, twice), shown newest first; collapse and expand; edit and delete; they're still there after a reload; removing a Step in the editor deletes its Note and keeps the others. No console errors.

Still to check by hand: long-press on the phone (Android Chrome fires `contextmenu` on a long-press, but text selection may also start). The third box stays open: Notes show as described, and search over them is ticket 14.
