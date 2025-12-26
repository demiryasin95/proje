// Dark Mode System
(function() {
    'use strict';

    const DarkMode = {
        STORAGE_KEY: 'darkMode',
        SYSTEM_PREFERENCE: 'system',
        
        init: function() {
            this.createToggleButton();
            this.loadPreference();
            this.attachListeners();
            this.watchSystemPreference();
        },

        createToggleButton: function() {
            // Add toggle button to navbar
            const navbar = document.querySelector('.admin-top-nav .container-fluid .ms-auto');
            if (!navbar) return;

            const toggleBtn = document.createElement('button');
            toggleBtn.id = 'darkModeToggle';
            toggleBtn.className = 'btn btn-outline-secondary admin-top-action';
            toggleBtn.type = 'button';
            toggleBtn.innerHTML = '<i class="fas fa-moon"></i>';
            toggleBtn.title = 'Tema Değiştir (Alt+T)';
            toggleBtn.setAttribute('data-bs-toggle', 'tooltip');
            
            navbar.insertBefore(toggleBtn, navbar.firstChild);

            toggleBtn.addEventListener('click', () => this.toggle());

            // Register keyboard shortcut
            if (window.KeyboardShortcuts) {
                window.KeyboardShortcuts.register('Alt+KeyT', () => this.toggle(), 'Dark/Light Mode');
            }
        },

        loadPreference: function() {
            let mode = localStorage.getItem(this.STORAGE_KEY);
            
            // If no preference, use system
            if (!mode) {
                mode = this.SYSTEM_PREFERENCE;
            }

            this.applyMode(mode);
        },

        toggle: function() {
            const currentMode = document.documentElement.getAttribute('data-theme');
            const newMode = currentMode === 'dark' ? 'light' : 'dark';
            
            this.applyMode(newMode);
            localStorage.setItem(this.STORAGE_KEY, newMode);

            // Dispatch theme change event
            window.dispatchEvent(new CustomEvent('themeChanged', {
                detail: { theme: newMode }
            }));

            // Show toast
            if (typeof showMelodyToast !== 'undefined') {
                showMelodyToast(
                    newMode === 'dark' ? '🌙 Karanlık mod aktif' : '☀️ Aydınlık mod aktif',
                    'success',
                    2000
                );
            }
        },

        applyMode: function(mode) {
            let actualMode = mode;

            // Handle system preference
            if (mode === this.SYSTEM_PREFERENCE) {
                actualMode = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }

            document.documentElement.setAttribute('data-theme', actualMode);
            this.updateToggleButton(actualMode);
        },

        updateToggleButton: function(mode) {
            const btn = document.getElementById('darkModeToggle');
            if (!btn) return;

            const icon = btn.querySelector('i');
            if (mode === 'dark') {
                icon.className = 'fas fa-sun';
                btn.classList.remove('btn-outline-secondary');
                btn.classList.add('btn-outline-warning');
            } else {
                icon.className = 'fas fa-moon';
                btn.classList.remove('btn-outline-warning');
                btn.classList.add('btn-outline-secondary');
            }
        },

        watchSystemPreference: function() {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                const storedMode = localStorage.getItem(this.STORAGE_KEY);
                if (storedMode === this.SYSTEM_PREFERENCE || !storedMode) {
                    this.applyMode(this.SYSTEM_PREFERENCE);
                }
            });
        },

        attachListeners: function() {
            // Add smooth transition when toggling
            document.documentElement.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => DarkMode.init());
    } else {
        DarkMode.init();
    }

    // Export for global use
    window.DarkMode = DarkMode;

})();
