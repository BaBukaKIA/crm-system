const themeStorageKey = 'enterprise-theme';
let themeTransitionTimer;
const themeIcons = {
    light: `
        <svg viewBox="0 0 24 24" focusable="false" aria-hidden="true">
            <circle cx="12" cy="12" r="4"></circle>
            <path d="M12 2v2"></path>
            <path d="M12 20v2"></path>
            <path d="m4.93 4.93 1.41 1.41"></path>
            <path d="m17.66 17.66 1.41 1.41"></path>
            <path d="M2 12h2"></path>
            <path d="M20 12h2"></path>
            <path d="m6.34 17.66-1.41 1.41"></path>
            <path d="m19.07 4.93-1.41 1.41"></path>
        </svg>`,
    dark: `
        <svg viewBox="0 0 24 24" focusable="false" aria-hidden="true">
            <path d="M21 14.2A8.2 8.2 0 0 1 9.8 3 8.7 8.7 0 1 0 21 14.2Z"></path>
        </svg>`
};

function setTheme(theme, options = {}) {
    const nextTheme = theme === 'dark' ? 'dark' : 'light';
    const root = document.documentElement;
    const shouldAnimate = options.animate === true && root.dataset.theme !== nextTheme;
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const canRevealTheme = shouldAnimate &&
        options.source instanceof Element &&
        typeof document.startViewTransition === 'function' &&
        !reduceMotion;

    if (canRevealTheme) {
        const currentTheme = root.dataset.theme === 'dark' ? 'dark' : 'light';
        const direction = currentTheme === 'dark' && nextTheme === 'light' ? 'collapse' : 'reveal';
        const cleanupReveal = prepareThemeReveal(options.source, direction);
        const transition = document.startViewTransition(() => {
            setTheme(nextTheme);
        });

        transition.finished.finally(cleanupReveal);
        return;
    }

    if (shouldAnimate) {
        clearTimeout(themeTransitionTimer);
        root.dataset.themeChanging = 'true';
    }

    document.documentElement.dataset.theme = nextTheme;
    localStorage.setItem(themeStorageKey, nextTheme);

    document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
        const icon = button.querySelector('[data-theme-toggle-icon]');
        const text = button.querySelector('[data-theme-toggle-text]');

        button.dataset.theme = nextTheme;
        button.setAttribute('aria-pressed', nextTheme === 'dark' ? 'true' : 'false');

        if (icon) {
            icon.innerHTML = themeIcons[nextTheme];
        }

        if (text) {
            text.textContent = nextTheme === 'dark' ? 'Темная' : 'Светлая';
        }
    });

    if (shouldAnimate) {
        themeTransitionTimer = window.setTimeout(() => {
            delete root.dataset.themeChanging;
        }, 520);
    }
}

function getCurrentTheme() {
    return localStorage.getItem(themeStorageKey) || document.documentElement.dataset.theme || 'light';
}

function prepareThemeReveal(source, direction) {
    const root = document.documentElement;
    const rect = source.getBoundingClientRect();
    const x = rect.left + rect.width / 2;
    const y = rect.top + rect.height / 2;
    const endRadius = Math.hypot(
        Math.max(x, window.innerWidth - x),
        Math.max(y, window.innerHeight - y));

    root.style.setProperty('--theme-transition-x', `${x}px`);
    root.style.setProperty('--theme-transition-y', `${y}px`);
    root.style.setProperty('--theme-transition-radius', `${Math.ceil(endRadius)}px`);
    root.dataset.themeTransition = direction === 'collapse' ? 'collapse' : 'reveal';

    return () => {
        delete root.dataset.themeTransition;
        root.style.removeProperty('--theme-transition-x');
        root.style.removeProperty('--theme-transition-y');
        root.style.removeProperty('--theme-transition-radius');
    };
}

document.addEventListener('DOMContentLoaded', () => {
    setTheme(getCurrentTheme());

    document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
        button.addEventListener('click', () => {
            setTheme(getCurrentTheme() === 'dark' ? 'light' : 'dark', { animate: true, source: button });
        });
    });
});
