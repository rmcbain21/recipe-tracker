// The alarm for a finished Cooking Mode timer: beeps made with the Web Audio API (no sound file)
// and a vibration, repeated every few seconds until every finished timer is dismissed.
// Browsers only let a page make sound after a tap, so prime() is called when a timer is started.
// A PWA can't reliably ring while the phone is locked or the app is in the background.

let context = null;
let ringing = null;

function audio() {
    if (!context) {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) {
            return null;
        }
        context = new AudioContext();
    }
    if (context.state === 'suspended') {
        context.resume().catch(() => {});
    }
    return context;
}

function beep(ctx, at, frequency) {
    const oscillator = ctx.createOscillator();
    const gain = ctx.createGain();
    oscillator.type = 'square';
    oscillator.frequency.value = frequency;
    gain.gain.setValueAtTime(0.0001, at);
    gain.gain.exponentialRampToValueAtTime(0.25, at + 0.02);
    gain.gain.exponentialRampToValueAtTime(0.0001, at + 0.22);
    oscillator.connect(gain).connect(ctx.destination);
    oscillator.start(at);
    oscillator.stop(at + 0.25);
}

function ringOnce() {
    try {
        const ctx = audio();
        if (ctx) {
            const now = ctx.currentTime;
            [0, 0.3, 0.6].forEach((offset, i) => beep(ctx, now + offset, i === 2 ? 1320 : 880));
        }
    } catch {
        // No sound here; the timer still flashes.
    }
    try {
        navigator.vibrate?.([300, 150, 300, 150, 600]);
    } catch {
        // No vibration here.
    }
}

/** Unlocks sound for later. Call from a tap. */
export function prime() {
    try {
        audio();
    } catch {
        // No Web Audio.
    }
}

/** Starts ringing, if it isn't already. */
export function start() {
    if (ringing) {
        return;
    }
    ringOnce();
    ringing = setInterval(ringOnce, 4000);
}

/** Stops ringing. */
export function stop() {
    if (ringing) {
        clearInterval(ringing);
        ringing = null;
    }
    try {
        navigator.vibrate?.(0);
    } catch {
        // No vibration here.
    }
}
