// Keeps the screen on in Cooking Mode. The browser drops a screen Wake Lock whenever the page is
// hidden, so it's asked for again each time the page becomes visible. Where Wake Lock isn't
// supported (or is refused), nothing happens and nothing breaks.

let lock = null;
let wanted = false;

async function request() {
    if (!wanted || lock || !('wakeLock' in navigator) || document.visibilityState !== 'visible') {
        return;
    }

    try {
        lock = await navigator.wakeLock.request('screen');
        lock.addEventListener('release', () => { lock = null; });
    } catch {
        lock = null;
    }
}

function onVisibilityChange() {
    if (document.visibilityState === 'visible') {
        request();
    }
}

/** Starts keeping the screen on. Returns whether this browser supports Wake Lock at all. */
export async function keepAwake() {
    wanted = true;
    document.addEventListener('visibilitychange', onVisibilityChange);
    await request();
    return 'wakeLock' in navigator;
}

/** Lets the screen sleep again. */
export async function release() {
    wanted = false;
    document.removeEventListener('visibilitychange', onVisibilityChange);
    try {
        await lock?.release();
    } catch {
        // Already released.
    }
    lock = null;
}

/** For checks: whether a lock is held right now. */
export function isHeld() {
    return lock !== null;
}
