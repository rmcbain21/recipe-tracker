# 04: Source and Books

**What to build:** Every Recipe can credit where it came from. On the Recipe editor I pick a Source kind: Website (URL, site name), Book (picked from my Books, plus page), Video (URL, channel) or Other (free text). Books are their own records (title, author) that I add once, and I can add one from the Book picker. The recipe page shows the Source, with links opening in a new tab.

**Blocked by:** 03

**Status:** ready-for-human (phone check)

- [x] Core: Source on Recipe; Book records with add, edit and delete (delete blocked while any Recipe uses the Book).
- [x] The editor's Source fields change with the kind; the site name is filled in from the URL's host when left blank.
- [x] The recipe page shows "From *Salt Fat Acid Heat*, p. 212", "From seriouseats.com" with a link, and so on.
- [x] A Books page lists Books with how many Recipes each has (reached from Settings for now).

## Comments

**2026-11-08 (agent):** Implemented on branch `04-source-and-books`. 190 core tests pass (29 new in `SourceAndBookTests`).

- **Core**: `Books/Book.cs` and `BookCatalogue` (`AppCore.Books`): add, edit, delete (blocked while any Recipe or Draft has the Book as its Source), list by title, and `ListBookSummariesAsync` with Recipe counts. Titles are trimmed and must be unique, ignoring case.
- **Source**: the editor now holds an `EditedSource` (Kind null means no Source) that keeps every kind's fields while the Cook switches kinds. Saving keeps only the chosen kind's fields. A Website's blank site name is filled in from the URL's host without "www.". A URL with no scheme gets "https://", and anything that still isn't an http(s) address is refused. A Book Source needs a Book that exists. A Source with nothing filled in saves as none.
- **Recipe page**: `RecipeView.Source` (`ShownSource`) gives "From *Salt Fat Acid Heat*, p. 212" (also "pp. 12–13", or the page as typed), "From seriouseats.com" linked in a new tab, a Video's channel (or its host) linked, and Other's text.
- **UI**: `Components/SourceEditor.razor` in the Recipe editor and the capture review, with `Components/BookPicker.razor` and `Dialogs/BookDialog.razor` for adding a Book from the picker. `/books` lists Books with counts, edit and delete. There's a minimal `/settings` page behind a gear icon in the top bar that links to Books; issue 02 fills it in.

Checked in a headless browser at phone width: add a Book (and a duplicate title is refused in the dialog), a Recipe from the Book shows "From Salt Fat Acid Heat, p. 212", switching to Website with "www.seriouseats.com/caesar" shows "From seriouseats.com" linking to the page with `target=_blank`, the Books page counts 1 Recipe, and deleting the Book is refused with a message. No console errors.

Still to check by hand: the same on the phone.
