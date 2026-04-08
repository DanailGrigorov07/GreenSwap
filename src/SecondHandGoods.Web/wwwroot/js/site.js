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

(function () {
    var header = document.querySelector('header.site-header');
    if (!header) return;

    document.body.classList.add('has-site-header-fixed');

    var mqDesktop = window.matchMedia('(min-width: 992px)');

    function setHeaderHeight() {
        var h = header.offsetHeight;
        document.documentElement.style.setProperty('--site-header-height', h + 'px');
    }

    var lastY = window.scrollY;
    var ticking = false;
    var threshold = 8;
    var hideAfter = 56;

    function onScroll() {
        if (!mqDesktop.matches) {
            header.classList.remove('site-header--hidden');
            lastY = window.scrollY;
            ticking = false;
            return;
        }
        var y = window.scrollY;
        if (y <= threshold) {
            header.classList.remove('site-header--hidden');
        } else if (y > lastY && y > hideAfter) {
            header.classList.add('site-header--hidden');
        } else if (y < lastY) {
            header.classList.remove('site-header--hidden');
        }
        lastY = y;
        ticking = false;
    }

    window.addEventListener('scroll', function () {
        if (!ticking) {
            ticking = true;
            requestAnimationFrame(onScroll);
        }
    }, { passive: true });

    window.addEventListener('resize', setHeaderHeight);
    function onMqChange() {
        if (!mqDesktop.matches) {
            header.classList.remove('site-header--hidden');
        }
        setHeaderHeight();
    }
    if (mqDesktop.addEventListener) {
        mqDesktop.addEventListener('change', onMqChange);
    } else if (mqDesktop.addListener) {
        mqDesktop.addListener(onMqChange);
    }

    if (typeof ResizeObserver !== 'undefined') {
        new ResizeObserver(setHeaderHeight).observe(header);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setHeaderHeight);
    } else {
        setHeaderHeight();
    }

    var collapseEl = document.getElementById('mainNavbarCollapse');
    if (collapseEl) {
        collapseEl.addEventListener('shown.bs.collapse', setHeaderHeight);
        collapseEl.addEventListener('hidden.bs.collapse', setHeaderHeight);
    }
})();
