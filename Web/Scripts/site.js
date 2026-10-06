/* 01 / Overlay navigation: keyboard focus, Escape, and anchor navigation. */
(function () {
    'use strict';
    var toggle = document.querySelector('.menu-toggle');
    var overlay = document.getElementById('menu-overlay');
    var main = document.querySelector('main');
    var footer = document.querySelector('footer');
    var previousFocus;
    function setMenu(open) {
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
        overlay.hidden = !open;
        document.body.classList.toggle('menu-open', open);
        main.inert = open; footer.inert = open;
        document.querySelector('.desktop-nav').inert = open;
        if (open) { previousFocus = document.activeElement; overlay.querySelector('a').focus(); }
        else if (previousFocus) previousFocus.focus();
    }
    toggle.addEventListener('click', function () { setMenu(overlay.hidden); });
    overlay.querySelectorAll('a').forEach(function (link) { link.addEventListener('click', function () { setMenu(false); }); });
    document.addEventListener('keydown', function (event) {
        if (overlay.hidden) return;
        if (event.key === 'Escape') { setMenu(false); return; }
        if (event.key === 'Tab') {
            var items = [toggle].concat(Array.from(overlay.querySelectorAll('a,button')));
            var first = items[0], last = items[items.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    /* 02 / Password reveal updates the accessible name and state. */
    document.querySelectorAll('.password-toggle').forEach(function (button) {
        button.addEventListener('click', function () {
            var input = button.parentElement.querySelector('input');
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.textContent = show ? 'Ocultar' : 'Ver';
            button.setAttribute('aria-label', show ? 'Ocultar contraseña' : 'Mostrar contraseña');
            button.setAttribute('aria-pressed', String(show));
        });
    });
}());
