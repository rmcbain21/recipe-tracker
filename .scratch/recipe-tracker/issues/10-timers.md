# 10: Timers from Step text

**What to build:** In Cooking Mode, each duration in a Step's text ("simmer 20 minutes", "bake 25-30 min", "1½ hours") shows a timer chip. Tapping it starts a timer. Running timers show in a strip on every Step and on Gather, with time left, "+5 min" and cancel. When a timer finishes, it alarms (sound and vibration) and flashes until dismissed. Timers keep a target time, so they survive the app reloading. Known limit: a PWA can't reliably alarm while the phone is locked or the app is in the background; Wake Lock keeps the app on screen.

**Blocked by:** 09

**Status:** ready-for-human (phone check)

- [x] Core: finding durations in Step text (minutes, hours, seconds, ranges, fractions, "an hour"), tested; a range starts at its low end.
- [x] Timers are part of the device-local Cooking Session (label, target time); several run at once.
- [x] The finish alarm uses the Web Audio API and `navigator.vibrate`, needing no sound file.
- [x] Timers that finished while the app was closed show as finished on return.

## Comments

**2026-11-09 (agent):** Implemented on branch `10-timers`. 341 core tests pass (37 new in `StepDurationTests`, 7 in `CookingTests`).

- **Finding times** (`Core/Parsing/StepDurations.cs`): hours / hr / h, minutes / min, seconds / sec; numbers, decimals, fractions ("1½", "1 1/2", "1-1/2"), ranges ("25-30", "10 to 15", "2 or 3"), words ("an hour", "ten minutes", "half an hour", "a couple of minutes"), "and a half" before or after the unit, "a 20-minute rest", and parts joined into one time ("1 hour 15 minutes", "1 hr, 20 min"). It skips "a second egg", words inside other words ("minced", "often"), and anything over 72 hours. `StepDuration.Label` gives "20 min", "25–30 min", "1 hr 30 min".
- **Timers** (`Core/Cooking/CookingTimer.cs`): part of the device-local Cooking Session (label "Step 2 · 25 min", Step id, where the time sits in the text, `EndsAt`). `CookingCatalogue.StartTimerAsync` starts at the low end of a range; tapping a time that's already running doesn't add a second timer. `AddTimeAsync` (+5 min) adds to a running timer and restarts a finished one from now. `RemoveTimerAsync` cancels or dismisses. Start over clears timers. Because a timer keeps its end time, one that ended while the app was closed shows as finished on return.
- **Cooking Mode**: each time in the Step text is a chip; while its timer runs, the chip fills and shows the time left. A strip under the top bar, on Gather and every Step, lists each timer with its countdown, "+5 min" and ✕. A finished timer turns red, flashes (not with reduced motion) and shows Done, +5 min and Dismiss.
- **Alarm** (`wwwroot/js/timerAlarm.js`): three Web Audio beeps plus `navigator.vibrate`, every 4 seconds until every finished timer is dismissed. Sound is unlocked by the tap that starts a timer. After a reload, sound may stay silent until the next tap, because browsers block audio before one; the timer still flashes.

Checked in a headless browser at 412×900 and 1280×800. Chips showed on both Steps. A 2-second timer finished, flashed and vibrated. Tapping its chip again added no second timer. Both timers survived a reload. Dismiss, +5 min (4:55 → 9:54) and cancel worked, and the strip showed on Gather. No console errors.

Still to check by hand on the phone: hearing the alarm, the vibration, and what happens with the screen locked (a known PWA limit).
