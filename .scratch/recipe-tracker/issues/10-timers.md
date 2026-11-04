# 10: Timers from Step text

**What to build:** In Cooking Mode, each duration in a Step's text ("simmer 20 minutes", "bake 25-30 min", "1½ hours") shows a timer chip. Tapping it starts a timer. Running timers show in a strip on every Step and on Gather, with time left, "+5 min" and cancel. When a timer finishes, it alarms (sound and vibration) and flashes until dismissed. Timers keep a target time, so they survive the app reloading. Known limit: a PWA can't reliably alarm while the phone is locked or the app is in the background; Wake Lock keeps the app on screen.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Core: finding durations in Step text (minutes, hours, seconds, ranges, fractions, "an hour"), tested; a range starts at its low end.
- [ ] Timers are part of the device-local Cooking Session (label, target time); several run at once.
- [ ] The finish alarm uses the Web Audio API and `navigator.vibrate`, needing no sound file.
- [ ] Timers that finished while the app was closed show as finished on return.

## Comments
