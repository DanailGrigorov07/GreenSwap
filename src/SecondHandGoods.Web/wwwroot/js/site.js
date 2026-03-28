// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function () {
    /**
     * Mobile browsers (and some desktop emulators) often lay out the first paint with a stale
     * layout width; media queries + Bootstrap then match the wrong breakpoint until a real resize.
     * Navigation loads a new document each time, so the bug repeats on every page.
     * Synthetic resize + delayed retries nudges the same reflow path without user interaction.
     */
    function resetHorizontalScroll() {
        if (window.scrollX) {
            window.scrollTo(0, window.scrollY);
        }
        document.documentElement.scrollLeft = 0;
        document.body.scrollLeft = 0;
    }

    function notifyLayout() {
        resetHorizontalScroll();
        window.dispatchEvent(new Event('resize'));
        void document.documentElement.clientWidth;
    }

    function scheduleLayoutFixes() {
        notifyLayout();
        requestAnimationFrame(function () {
            notifyLayout();
            requestAnimationFrame(notifyLayout);
        });
        [0, 50, 150, 300].forEach(function (ms) {
            setTimeout(notifyLayout, ms);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scheduleLayoutFixes);
    } else {
        scheduleLayoutFixes();
    }
    window.addEventListener('load', scheduleLayoutFixes);
    window.addEventListener('pageshow', function () {
        scheduleLayoutFixes();
    });
})();
