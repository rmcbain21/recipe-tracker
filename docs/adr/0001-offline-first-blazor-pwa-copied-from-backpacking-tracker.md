# Offline-first Blazor PWA with no server, copied from Backpacking Tracker

Recipe Tracker is built the same way as Backpacking Tracker (its ADR 0001). It is a Blazor WebAssembly PWA served as static files from GitHub Pages, keeps each device's data in IndexedDB, and syncs the Cook's phone and tablet through one data file in their Google Drive app-data folder. It is a separate app with its own data, not a section of Backpacking Tracker: a backpacking Meal is about weight and calories on trail, while a Recipe is about Steps and cooking at home, and sharing words such as "Meal" or "Food Item" between the two would blur both. Code and patterns are copied, not shared.

## Considered Options

- **A recipe section inside Backpacking Tracker**: rejected; the two domains use the same words for different things.
- **A small backend (such as a Cloudflare Worker) for URL import or LLM parsing**: deferred. A static PWA can't fetch recipe sites because of CORS, so URL import waits. Text is pasted or shared into the app instead (including text copied from a book page with Google Lens), and a rule-based parser runs on the device.

## Consequences

- Getting a Recipe from a URL means copying the page's text until a proxy is added. Adding one later would be the app's only server part and should revise this ADR.
- The parser sits behind an interface, so an LLM parser can replace or add to the rule-based one when a proxy exists.
- Photos stay out of the sync file. When they arrive, they need their own files in Drive.
