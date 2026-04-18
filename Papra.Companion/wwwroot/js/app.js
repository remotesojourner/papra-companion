window.clipboardCopy = async function (text) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(text);
        return;
    }
    // Fallback for non-secure contexts (HTTP on LAN)
    const el = document.createElement('textarea');
    el.value = text;
    el.style.position = 'fixed';
    el.style.opacity = '0';
    document.body.appendChild(el);
    el.focus();
    el.select();
    document.execCommand('copy');
    document.body.removeChild(el);
};

window.darkMode = {
    init: function () {
        const theme = localStorage.getItem('color-theme');
        if (theme === 'dark' || (!theme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    },
    toggle: function () {
        const html = document.documentElement;
        if (html.classList.contains('dark')) {
            html.classList.remove('dark');
            localStorage.setItem('color-theme', 'light');
            return false;
        } else {
            html.classList.add('dark');
            localStorage.setItem('color-theme', 'dark');
            return true;
        }
    },
    isDark: function () {
        return document.documentElement.classList.contains('dark');
    }
};
