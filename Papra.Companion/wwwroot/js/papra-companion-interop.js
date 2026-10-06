window.papraCompanionInterop = {
    getLocalStorage: function (key) {
        return localStorage.getItem(key);
    },

    setLocalStorage: function (key, value) {
        localStorage.setItem(key, value);
    },

    copyText: async function (text) {
        if (window.isSecureContext && navigator.clipboard) {
            await navigator.clipboard.writeText(text);
            return;
        }

        const field = document.createElement('textarea');
        field.value = text;
        field.style.position = 'fixed';
        field.style.opacity = '0';
        document.body.appendChild(field);
        field.focus();
        field.select();
        const copied = document.execCommand('copy');
        document.body.removeChild(field);
        if (!copied) throw new Error('The browser refused to copy');
    }
};
