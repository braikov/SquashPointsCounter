// wwwroot/js/alert-autoremove.js
(function () {
    // Quick exit if DOM not ready yet
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        // Auto-remove logic
        var alerts = document.querySelectorAll('.alert-auto-remove');
        if (alerts && alerts.length) {
            alerts.forEach(function (el) {
                var timeout = parseInt(el.getAttribute('data-timeout'), 10) || 4000;
                var timer = null;

                function start() {
                    stop();
                    timer = setTimeout(function () {
                        safeRemoveAlert(el);
                    }, timeout);
                }
                function stop() {
                    if (timer) {
                        clearTimeout(timer);
                        timer = null;
                    }
                }

                el.addEventListener('mouseenter', stop);
                el.addEventListener('mouseleave', start);
                el.addEventListener('focusin', stop);
                el.addEventListener('focusout', start);

                start();
            });
        }

        // Ensure close button works even if bootstrap/coreui event handling didn't bind
        document.addEventListener('click', function (ev) {
            var btn = ev.target.closest('.btn-close, [data-bs-dismiss="alert"], [data-coreui-dismiss="alert"]');
            if (!btn) return;

            // prevent accidental form submit if button inside a form
            if (btn.tagName.toLowerCase() === 'button' && !btn.getAttribute('type')) {
                btn.setAttribute('type', 'button');
            }

            var alertEl = btn.closest('.alert');
            if (alertEl) {
                safeRemoveAlert(alertEl);
            }
        }, { passive: true });

        // keyboard accessibility: Enter/Space on focused close control
        document.addEventListener('keydown', function (ev) {
            if (ev.key === 'Enter' || ev.key === ' ') {
                var el = document.activeElement;
                if (!el) return;
                if (el.classList.contains('btn-close') || el.getAttribute('data-bs-dismiss') === 'alert' || el.getAttribute('data-coreui-dismiss') === 'alert') {
                    ev.preventDefault();
                    el.click();
                }
            }
        });
    }

    function safeRemoveAlert(alertEl) {
        // Prefer Bootstrap/CoreUI API if available (gives CSS hide animation)
        try {
            if (typeof bootstrap !== 'undefined' && bootstrap.Alert && typeof bootstrap.Alert.getInstance === 'function') {
                // get existing instance or create one then call close
                var instance = bootstrap.Alert.getInstance(alertEl) || new bootstrap.Alert(alertEl);
                if (instance && typeof instance.close === 'function') {
                    instance.close();
                    return;
                }
            }
        } catch (e) {
            // ignore and fallback to direct removal
        }

        // Fallback: plain removal
        try { alertEl.remove(); } catch (e) { /* ignore */ }
    }
})();
