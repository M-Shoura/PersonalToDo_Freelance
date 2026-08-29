// ==========================================================================
// TaskPulse Theme Engine & Global Utilities
// ==========================================================================

const ThemeManager = (() => {
    const STORAGE_KEY = 'taskpulse-theme';

    function getSavedTheme() {
        return localStorage.getItem(STORAGE_KEY);
    }

    function getSystemTheme() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function getActiveTheme() {
        return getSavedTheme() || getSystemTheme();
    }

    function applyTheme(theme, persist = true) {
        const root = document.documentElement;
        root.setAttribute('data-bs-theme', theme);
        root.setAttribute('data-theme', theme);

        if (persist) {
            localStorage.setItem(STORAGE_KEY, theme);
        }

        updateToggleButtons(theme);

        // Dispatch custom event for charts and reactive components
        document.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { theme: theme }
        }));
    }

    function updateToggleButtons(theme) {
        const buttons = document.querySelectorAll('.theme-toggle-btn, [data-theme-toggle]');
        buttons.forEach(btn => {
            const nextTheme = theme === 'dark' ? 'light' : 'dark';
            const label = `Switch to ${nextTheme} mode`;
            btn.setAttribute('aria-label', label);
            btn.setAttribute('title', label);
        });
    }

    function toggleTheme() {
        const current = getActiveTheme();
        const next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next, true);
    }

    function init() {
        // Apply active theme immediately on DOM ready
        const currentTheme = getActiveTheme();
        applyTheme(currentTheme, false);

        // Bind click events on all theme toggle buttons
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('.theme-toggle-btn, [data-theme-toggle]');
            if (btn) {
                e.preventDefault();
                toggleTheme();
            }
        });

        // Listen for OS system theme changes
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                // Only update if user hasn't set an explicit preference
                if (!getSavedTheme()) {
                    applyTheme(e.matches ? 'dark' : 'light', false);
                }
            });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    return {
        getTheme: getActiveTheme,
        setTheme: applyTheme,
        toggle: toggleTheme
    };
})();

window.ThemeManager = ThemeManager;

