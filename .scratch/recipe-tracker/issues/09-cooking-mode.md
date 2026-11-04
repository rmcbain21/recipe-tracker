# 09: Cooking Mode

**What to build:** "Start cooking" on a finished Recipe opens Cooking Mode full screen. First comes Gather: every Ingredient Line at the current Scale as a checklist with big rows, and the Scale control. Then one Step per screen: the Step in large text with the previous and next ones faded above and below, its Ingredient Uses with scaled amounts, and Notes on the Step and on its lines. Big Back and Next buttons sit in the bottom half, and swipe works too. The screen stays awake. On a tablet in landscape, all the Ingredient Lines sit on the left with the current Step's uses highlighted, and the Step on the right. If I leave and come back on this device, I return to the same place.

**Blocked by:** 06, 07, 08

**Status:** ready-for-agent

- [ ] Core: a Cooking Session (Recipe id, Scale, current position: Gather or Step n, Gather ticks) stored on this device only and never synced; starting cooking again offers "Resume" or "Start over".
- [ ] Drafts can't start Cooking Mode.
- [ ] Wake Lock is requested on entering and requested again when the page becomes visible; nothing breaks where Wake Lock isn't supported.
- [ ] The phone layout is as described; Back and Next are at least 64 px tall and placed in the bottom third.
- [ ] Tablet landscape (≥ 900 px wide, landscape) uses the two-pane layout.
- [ ] Exit (✕) returns to the recipe page and keeps the session.

## Comments
