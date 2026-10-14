// Wires every on-screen keypad to its target input, and resets the kiosk to
// the idle screen after a period of inactivity. Vanilla JS - the kiosk shell
// has no other client-side framework, so one small reusable script covers
// both concerns rather than pulling in a dependency for this.
(function () {
    'use strict';

    function wireKeypads() {
        document.querySelectorAll('[data-kiosk-keypad]').forEach(function (keypad) {
            var target = document.querySelector(keypad.dataset.kioskKeypad);
            if (!target) {
                return;
            }

            keypad.addEventListener('click', function (event) {
                var button = event.target.closest('[data-key]');
                if (!button) {
                    return;
                }

                switch (button.dataset.key) {
                    case 'backspace':
                        target.value = target.value.slice(0, -1);
                        break;
                    case 'clear':
                        target.value = '';
                        break;
                    case 'space':
                        target.value += ' ';
                        break;
                    default:
                        target.value += button.dataset.key;
                        break;
                }

                target.focus();
            });
        });
    }

    function wireInactivityReset() {
        var body = document.body;
        var resetUrl = body.dataset.kioskResetUrl;
        if (!resetUrl) {
            return;
        }

        var seconds = parseInt(body.dataset.kioskResetSeconds, 10) || 60;
        var timer = null;

        var resetTimer = function () {
            if (timer) {
                clearTimeout(timer);
            }

            timer = setTimeout(function () {
                window.location.href = resetUrl;
            }, seconds * 1000);
        };

        ['pointerdown', 'touchstart', 'keydown'].forEach(function (eventName) {
            document.addEventListener(eventName, resetTimer, { passive: true });
        });

        resetTimer();
    }

    document.addEventListener('DOMContentLoaded', function () {
        wireKeypads();
        wireInactivityReset();
    });
})();
