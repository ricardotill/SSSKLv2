// leaderboard.js - small helpers for the leaderboard page
// Provides a robust preloadImage(url) function that returns a Promise<boolean>

window.leaderboard = window.leaderboard || {};

window.leaderboard.preloadImage = function (url) {
    return new Promise(function (resolve) {
        if (!url) {
            resolve(false);
            return;
        }

        try {
            var img = new Image();
            var finished = false;

            var cleanup = function () {
                if (img) {
                    img.onload = null;
                    img.onerror = null;
                }
            };

            img.onload = function () {
                if (finished) return;
                finished = true;
                clearTimeout(timer);
                cleanup();
                resolve(true);
            };

            img.onerror = function () {
                if (finished) return;
                finished = true;
                clearTimeout(timer);
                cleanup();
                resolve(false);
            };

            // Start loading
            img.src = url;

            // If the image is in cache, some browsers mark it complete immediately.
            // Check for that to resolve synchronously when possible.
            try {
                if (img.complete && typeof img.naturalWidth !== 'undefined' && img.naturalWidth > 0) {
                    finished = true;
                    cleanup();
                    resolve(true);
                    return;
                }
            } catch (e) {
                // ignore and fall back to onload/onerror
            }

            // Provide a timeout fallback so caller doesn't wait forever for slow hosts.
            var timer = setTimeout(function () {
                if (finished) return;
                finished = true;
                cleanup();
                // If the image hasn't fired onload in time, treat as failure to avoid broken images
                resolve(false);
            }, 4000);
        }
        catch (e) {
            try { console.warn('leaderboard.preloadImage error', e); } catch (e2) { }
            resolve(false);
        }
    });
};
