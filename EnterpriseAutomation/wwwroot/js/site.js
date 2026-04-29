const themeStorageKey = 'enterprise-theme';

function setTheme(theme) {
    const nextTheme = theme === 'dark' ? 'dark' : 'light';
    document.documentElement.dataset.theme = nextTheme;
    localStorage.setItem(themeStorageKey, nextTheme);

    document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
        const icon = button.querySelector('[data-theme-toggle-icon]');
        const text = button.querySelector('[data-theme-toggle-text]');

        if (icon) {
            icon.textContent = nextTheme === 'dark' ? '☾' : '☀';
        }

        if (text) {
            text.textContent = nextTheme === 'dark' ? 'Dark' : 'Light';
        }
    });
}

function getCurrentTheme() {
    return localStorage.getItem(themeStorageKey) || document.documentElement.dataset.theme || 'light';
}

document.addEventListener('DOMContentLoaded', () => {
    setTheme(getCurrentTheme());

    document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
        button.addEventListener('click', () => {
            setTheme(getCurrentTheme() === 'dark' ? 'light' : 'dark');
        });
    });
});
