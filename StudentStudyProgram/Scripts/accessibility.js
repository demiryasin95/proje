// Accessibility (A11y) Enhancement System
(function() {
    'use strict';

    const Accessibility = {
        init: function() {
            this.enhanceKeyboardNavigation();
            this.addAriaLabels();
            this.improveFocusManagement();
            this.addSkipLinks();
            this.enhanceFormAccessibility();
            this.improveTableAccessibility();
            this.addLiveRegions();
            this.enhanceColorContrast();
            this.setupAccessibilityPanel();
        },

        // Enhanced keyboard navigation
        enhanceKeyboardNavigation: function() {
            // Escape key to close modals/dropdowns
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') {
                    // Close open modals
                    const openModal = document.querySelector('.modal.show');
                    if (openModal) {
                        const modalInstance = bootstrap.Modal.getInstance(openModal);
                        if (modalInstance) modalInstance.hide();
                        return;
                    }

                    // Close open dropdowns
                    const openDropdown = document.querySelector('.dropdown-menu.show');
                    if (openDropdown) {
                        const dropdownToggle = openDropdown.previousElementSibling;
                        if (dropdownToggle) dropdownToggle.click();
                    }
                }
            });

            // Tab trap in modals
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    const modal = document.querySelector('.modal.show');
                    if (modal) {
                        this.trapFocus(e, modal);
                    }
                }
            });

            // Arrow key navigation in dropdowns
            document.querySelectorAll('.dropdown-menu').forEach(menu => {
                menu.addEventListener('keydown', (e) => {
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                        e.preventDefault();
                        this.navigateDropdown(menu, e.key === 'ArrowDown');
                    }
                });
            });
        },

        trapFocus: function(e, element) {
            const focusableElements = element.querySelectorAll(
                'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
            );
            const firstFocusable = focusableElements[0];
            const lastFocusable = focusableElements[focusableElements.length - 1];

            if (e.shiftKey) {
                if (document.activeElement === firstFocusable) {
                    lastFocusable.focus();
                    e.preventDefault();
                }
            } else {
                if (document.activeElement === lastFocusable) {
                    firstFocusable.focus();
                    e.preventDefault();
                }
            }
        },

        navigateDropdown: function(menu, down) {
            const items = Array.from(menu.querySelectorAll('.dropdown-item:not(.disabled)'));
            const currentIndex = items.indexOf(document.activeElement);
            let newIndex;

            if (down) {
                newIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
            } else {
                newIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
            }

            items[newIndex].focus();
        },

        // Add ARIA labels to elements
        addAriaLabels: function() {
            // Add aria-label to buttons without text
            document.querySelectorAll('button:not([aria-label])').forEach(btn => {
                const icon = btn.querySelector('i');
                if (icon && !btn.textContent.trim()) {
                    const classes = Array.from(icon.classList);
                    const iconName = classes.find(c => c.startsWith('fa-'));
                    if (iconName) {
                        const label = this.getAriaLabelForIcon(iconName);
                        btn.setAttribute('aria-label', label);
                    }
                }
            });

            // Add aria-labels to navigation
            document.querySelectorAll('nav:not([aria-label])').forEach(nav => {
                if (nav.classList.contains('admin-sidebar')) {
                    nav.setAttribute('aria-label', 'Ana navigasyon');
                } else if (nav.classList.contains('admin-top-nav')) {
                    nav.setAttribute('aria-label', 'Üst navigasyon');
                }
            });

            // Add aria-labels to forms
            document.querySelectorAll('form:not([aria-label])').forEach(form => {
                const heading = form.querySelector('h1, h2, h3, h4, h5, h6');
                if (heading) {
                    form.setAttribute('aria-label', heading.textContent.trim());
                }
            });
        },

        getAriaLabelForIcon: function(iconClass) {
            const iconMap = {
                'fa-search': 'Ara',
                'fa-filter': 'Filtrele',
                'fa-times': 'Kapat',
                'fa-plus': 'Ekle',
                'fa-edit': 'Düzenle',
                'fa-trash': 'Sil',
                'fa-save': 'Kaydet',
                'fa-bars': 'Menü',
                'fa-user': 'Kullanıcı',
                'fa-cog': 'Ayarlar',
                'fa-sign-out-alt': 'Çıkış',
                'fa-moon': 'Karanlık mod',
                'fa-sun': 'Aydınlık mod',
                'fa-keyboard': 'Klavye kısayolları'
            };
            return iconMap[iconClass] || 'Buton';
        },

        // Improve focus management
        improveFocusManagement: function() {
            // Show focus outline for keyboard users only
            let mouseUser = false;

            document.addEventListener('mousedown', () => {
                mouseUser = true;
                document.body.classList.add('mouse-user');
            });

            document.addEventListener('keydown', (e) => {
                if (e.key === 'Tab') {
                    mouseUser = false;
                    document.body.classList.remove('mouse-user');
                }
            });

            // Return focus after modal closes
            document.querySelectorAll('.modal').forEach(modal => {
                let lastFocusedElement;
                
                modal.addEventListener('show.bs.modal', () => {
                    lastFocusedElement = document.activeElement;
                });

                modal.addEventListener('hidden.bs.modal', () => {
                    if (lastFocusedElement) {
                        lastFocusedElement.focus();
                    }
                });
            });

            // Focus first input in modals
            document.querySelectorAll('.modal').forEach(modal => {
                modal.addEventListener('shown.bs.modal', () => {
                    const firstInput = modal.querySelector('input:not([type="hidden"]), textarea, select');
                    if (firstInput) {
                        setTimeout(() => firstInput.focus(), 100);
                    }
                });
            });
        },

        // Add skip links
        addSkipLinks: function() {
            const skipLink = document.createElement('a');
            skipLink.href = '#main-content';
            skipLink.className = 'skip-link';
            skipLink.textContent = 'Ana içeriğe atla';
            document.body.insertBefore(skipLink, document.body.firstChild);

            // Add ID to main content if not exists
            const mainContent = document.querySelector('main');
            if (mainContent && !mainContent.id) {
                mainContent.id = 'main-content';
            }
        },

        // Enhance form accessibility
        enhanceFormAccessibility: function() {
            // Associate labels with inputs
            document.querySelectorAll('input:not([id]), select:not([id]), textarea:not([id])').forEach((input, index) => {
                if (!input.id && !input.getAttribute('aria-label')) {
                    const label = input.closest('.form-group, .mb-3, .mb-2')?.querySelector('label');
                    if (label && !label.getAttribute('for')) {
                        const id = 'input_' + index;
                        input.id = id;
                        label.setAttribute('for', id);
                    }
                }
            });

            // Add required indicators
            document.querySelectorAll('input[required], select[required], textarea[required]').forEach(input => {
                if (!input.getAttribute('aria-required')) {
                    input.setAttribute('aria-required', 'true');
                }
                
                const label = document.querySelector(`label[for="${input.id}"]`);
                if (label && !label.querySelector('.required-indicator')) {
                    const indicator = document.createElement('span');
                    indicator.className = 'required-indicator text-danger';
                    indicator.setAttribute('aria-hidden', 'true');
                    indicator.textContent = ' *';
                    label.appendChild(indicator);
                }
            });

            // Add error announcements
            document.querySelectorAll('.is-invalid').forEach(input => {
                if (!input.getAttribute('aria-invalid')) {
                    input.setAttribute('aria-invalid', 'true');
                }
                
                const feedback = input.nextElementSibling;
                if (feedback && feedback.classList.contains('invalid-feedback')) {
                    const id = 'error_' + input.id;
                    feedback.id = id;
                    input.setAttribute('aria-describedby', id);
                }
            });
        },

        // Improve table accessibility
        improveTableAccessibility: function() {
            document.querySelectorAll('table').forEach(table => {
                // Add scope to headers
                table.querySelectorAll('th').forEach(th => {
                    if (!th.getAttribute('scope')) {
                        const isColumnHeader = th.parentElement.parentElement.tagName === 'THEAD';
                        th.setAttribute('scope', isColumnHeader ? 'col' : 'row');
                    }
                });

                // Add caption if missing
                if (!table.querySelector('caption') && !table.getAttribute('aria-label')) {
                    const heading = table.previousElementSibling;
                    if (heading && heading.tagName.match(/H[1-6]/)) {
                        table.setAttribute('aria-label', heading.textContent.trim());
                    }
                }
            });
        },

        // Add ARIA live regions
        addLiveRegions: function() {
            // Create announcement region
            const liveRegion = document.createElement('div');
            liveRegion.id = 'a11y-announcer';
            liveRegion.className = 'visually-hidden';
            liveRegion.setAttribute('aria-live', 'polite');
            liveRegion.setAttribute('aria-atomic', 'true');
            document.body.appendChild(liveRegion);

            // Export announce function
            window.announce = function(message, priority = 'polite') {
                liveRegion.setAttribute('aria-live', priority);
                liveRegion.textContent = message;
                setTimeout(() => {
                    liveRegion.textContent = '';
                }, 1000);
            };
        },

        // Enhance color contrast (for accessibility)
        enhanceColorContrast: function() {
            // Add high contrast mode option
            const contrastModes = ['normal', 'high'];
            let currentMode = localStorage.getItem('contrastMode') || 'normal';

            if (currentMode === 'high') {
                document.documentElement.setAttribute('data-contrast', 'high');
            }

            // Export toggle function
            window.toggleContrast = function() {
                currentMode = currentMode === 'normal' ? 'high' : 'normal';
                localStorage.setItem('contrastMode', currentMode);
                
                if (currentMode === 'high') {
                    document.documentElement.setAttribute('data-contrast', 'high');
                    if (window.announce) window.announce('Yüksek kontrast modu aktif');
                } else {
                    document.documentElement.removeAttribute('data-contrast');
                    if (window.announce) window.announce('Normal kontrast modu aktif');
                }
            };
        },

        // Accessibility settings panel
        setupAccessibilityPanel: function() {
            // Register keyboard shortcut
            if (window.KeyboardShortcuts) {
                window.KeyboardShortcuts.register('Alt+KeyA', () => {
                    this.showAccessibilityPanel();
                }, 'Erişilebilirlik Ayarları');
            }
        },

        showAccessibilityPanel: function() {
            const modalId = 'accessibilityModal';
            let modal = document.getElementById(modalId);
            
            if (!modal) {
                modal = document.createElement('div');
                modal.id = modalId;
                modal.className = 'modal fade';
                modal.innerHTML = `
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">
                                    <i class="fas fa-universal-access me-2"></i>Erişilebilirlik Ayarları
                                </h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button>
                            </div>
                            <div class="modal-body">
                                <div class="form-check form-switch mb-3">
                                    <input class="form-check-input" type="checkbox" id="highContrastToggle"
                                           ${document.documentElement.getAttribute('data-contrast') === 'high' ? 'checked' : ''}>
                                    <label class="form-check-label" for="highContrastToggle">
                                        <strong>Yüksek Kontrast Modu</strong>
                                        <div class="small text-muted">Daha iyi görünürlük için kontrast artırılır</div>
                                    </label>
                                </div>
                                <div class="form-check form-switch mb-3">
                                    <input class="form-check-input" type="checkbox" id="reduceMotionToggle"
                                           ${document.documentElement.getAttribute('data-reduce-motion') === 'true' ? 'checked' : ''}>
                                    <label class="form-check-label" for="reduceMotionToggle">
                                        <strong>Animasyonları Azalt</strong>
                                        <div class="small text-muted">Hareketleri ve geçişleri azaltır</div>
                                    </label>
                                </div>
                                <div class="mb-3">
                                    <label for="fontSizeRange" class="form-label">
                                        <strong>Yazı Boyutu</strong>
                                    </label>
                                    <input type="range" class="form-range" id="fontSizeRange" 
                                           min="90" max="130" step="10" value="100">
                                    <div class="d-flex justify-content-between small text-muted">
                                        <span>Küçük</span>
                                        <span>Normal</span>
                                        <span>Büyük</span>
                                    </div>
                                </div>
                                <hr>
                                <div class="alert alert-info mb-0">
                                    <i class="fas fa-keyboard me-2"></i>
                                    <strong>Klavye Kısayolları:</strong><br>
                                    <kbd>Alt+H</kbd> Kısayolları göster<br>
                                    <kbd>Alt+A</kbd> Bu paneli aç<br>
                                    <kbd>Esc</kbd> Kapat
                                </div>
                            </div>
                        </div>
                    </div>
                `;
                document.body.appendChild(modal);

                // Add event listeners
                modal.querySelector('#highContrastToggle').addEventListener('change', (e) => {
                    if (window.toggleContrast) window.toggleContrast();
                });

                modal.querySelector('#reduceMotionToggle').addEventListener('change', (e) => {
                    if (e.target.checked) {
                        document.documentElement.setAttribute('data-reduce-motion', 'true');
                        if (window.announce) window.announce('Animasyonlar azaltıldı');
                    } else {
                        document.documentElement.removeAttribute('data-reduce-motion');
                        if (window.announce) window.announce('Animasyonlar normal');
                    }
                });

                modal.querySelector('#fontSizeRange').addEventListener('input', (e) => {
                    const size = e.target.value;
                    document.documentElement.style.fontSize = size + '%';
                    localStorage.setItem('fontSize', size);
                });

                // Load saved font size
                const savedSize = localStorage.getItem('fontSize');
                if (savedSize) {
                    document.documentElement.style.fontSize = savedSize + '%';
                    modal.querySelector('#fontSizeRange').value = savedSize;
                }
            }

            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        }
    };

    // Add accessibility CSS
    const style = document.createElement('style');
    style.textContent = `
        /* Skip link */
        .skip-link {
            position: absolute;
            top: -40px;
            left: 0;
            background: #000;
            color: #fff;
            padding: 8px 16px;
            text-decoration: none;
            z-index: 10000;
        }
        .skip-link:focus {
            top: 0;
        }

        /* Focus styles for keyboard users */
        body:not(.mouse-user) *:focus {
            outline: 3px solid #5e72e4;
            outline-offset: 2px;
        }

        body.mouse-user *:focus {
            outline: none;
        }

        /* High contrast mode */
        [data-contrast="high"] {
            --text-primary: #000000;
            --bg-primary: #ffffff;
            filter: contrast(1.2);
        }

        [data-theme="dark"][data-contrast="high"] {
            --text-primary: #ffffff;
            --bg-primary: #000000;
            filter: contrast(1.5);
        }

        /* Visually hidden but accessible */
        .visually-hidden {
            position: absolute;
            width: 1px;
            height: 1px;
            padding: 0;
            margin: -1px;
            overflow: hidden;
            clip: rect(0, 0, 0, 0);
            white-space: nowrap;
            border: 0;
        }

        /* Required indicator */
        .required-indicator {
            font-weight: bold;
        }

        /* Focus within improvements */
        .form-control:focus-within,
        .form-select:focus-within {
            box-shadow: 0 0 0 0.25rem rgba(94, 114, 228, 0.25);
        }
    `;
    document.head.appendChild(style);

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => Accessibility.init());
    } else {
        Accessibility.init();
    }

    // Export for global use
    window.Accessibility = Accessibility;

})();