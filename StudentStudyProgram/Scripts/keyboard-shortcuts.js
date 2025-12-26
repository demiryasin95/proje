// Keyboard Shortcuts System
(function() {
    'use strict';

    const KeyboardShortcuts = {
        shortcuts: {},
        isModalOpen: false,
        
        init: function() {
            this.registerDefaultShortcuts();
            this.attachListeners();
            this.createShortcutsModal();
        },

        registerDefaultShortcuts: function() {
            // Alt+M: Toggle menu
            this.register('Alt+KeyM', () => {
                const toggleBtn = document.querySelector('.sidebar-toggle');
                if (toggleBtn) toggleBtn.click();
            }, 'Menüyü Aç/Kapat');

            // Alt+S: Focus search
            this.register('Alt+KeyS', () => {
                const searchInput = document.querySelector('input[type="search"], input[placeholder*="Ara"]');
                if (searchInput) {
                    searchInput.focus();
                    searchInput.select();
                }
            }, 'Aramaya Odaklan');

            // Escape: Close modals/dropdowns
            this.register('Escape', () => {
                // Close Bootstrap modals
                const openModal = document.querySelector('.modal.show');
                if (openModal) {
                    const modalInstance = bootstrap.Modal.getInstance(openModal);
                    if (modalInstance) modalInstance.hide();
                    return;
                }

                // Close dropdowns
                const openDropdown = document.querySelector('.dropdown-menu.show');
                if (openDropdown) {
                    const dropdownToggle = openDropdown.previousElementSibling;
                    if (dropdownToggle) dropdownToggle.click();
                    return;
                }

                // Close sidebar overlay on mobile
                const overlay = document.querySelector('.sidebar-overlay.show');
                if (overlay) {
                    overlay.click();
                }
            }, 'Kapat');

            // Ctrl+S: Save form (if in form context)
            this.register('Control+KeyS', (e) => {
                e.preventDefault();
                const activeElement = document.activeElement;
                const form = activeElement.closest('form');
                if (form) {
                    const submitBtn = form.querySelector('button[type="submit"], input[type="submit"], button.btn-primary');
                    if (submitBtn) {
                        submitBtn.click();
                        if (typeof showMelodyToast !== 'undefined') {
                            showMelodyToast('Form kaydediliyor...', 'info', 1500);
                        }
                    }
                }
            }, 'Formu Kaydet');

            // Alt+H: Show shortcuts help
            this.register('Alt+KeyH', () => {
                this.showModal();
            }, 'Kısayolları Göster');

            // Alt+D: Go to dashboard
            this.register('Alt+KeyD', () => {
                const dashboardLink = document.querySelector('a[href*="Admin/Index"], a[href*="Dashboard"]');
                if (dashboardLink) window.location.href = dashboardLink.href;
            }, 'Dashboard\'a Git');
        },

        register: function(key, callback, description) {
            this.shortcuts[key] = { callback, description };
        },

        attachListeners: function() {
            document.addEventListener('keydown', (e) => {
                // Don't trigger if user is typing in input/textarea
                if (e.target.matches('input, textarea, select') && !e.key === 'Escape') {
                    // Allow Ctrl+S in forms
                    if (!(e.ctrlKey && e.code === 'KeyS')) {
                        return;
                    }
                }

                const key = this.getKeyCombo(e);
                const shortcut = this.shortcuts[key];

                if (shortcut) {
                    e.preventDefault();
                    shortcut.callback(e);
                }
            });
        },

        getKeyCombo: function(e) {
            const parts = [];
            if (e.ctrlKey) parts.push('Control');
            if (e.altKey) parts.push('Alt');
            if (e.shiftKey) parts.push('Shift');
            if (e.metaKey) parts.push('Meta');
            
            if (e.code && !['ControlLeft', 'ControlRight', 'AltLeft', 'AltRight', 'ShiftLeft', 'ShiftRight', 'MetaLeft', 'MetaRight'].includes(e.code)) {
                parts.push(e.code);
            } else if (e.key && e.key.length === 1) {
                parts.push('Key' + e.key.toUpperCase());
            } else if (e.key) {
                parts.push(e.key);
            }

            return parts.join('+');
        },

        createShortcutsModal: function() {
            const modal = document.createElement('div');
            modal.id = 'keyboardShortcutsModal';
            modal.className = 'modal fade';
            modal.innerHTML = `
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title">
                                <i class="fas fa-keyboard me-2"></i>Klavye Kısayolları
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="table-responsive">
                                <table class="table table-hover">
                                    <thead>
                                        <tr>
                                            <th>Kısayol</th>
                                            <th>Açıklama</th>
                                        </tr>
                                    </thead>
                                    <tbody id="shortcutsTableBody">
                                        ${this.generateShortcutsTable()}
                                    </tbody>
                                </table>
                            </div>
                            <div class="alert alert-info mt-3 mb-0">
                                <i class="fas fa-info-circle me-2"></i>
                                <strong>İpucu:</strong> Bu pencereyi <kbd>Alt+H</kbd> ile açabilirsiniz.
                            </div>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        },

        generateShortcutsTable: function() {
            let html = '';
            for (const [key, data] of Object.entries(this.shortcuts)) {
                const displayKey = this.formatKeyDisplay(key);
                html += `
                    <tr>
                        <td><kbd class="px-2 py-1">${displayKey}</kbd></td>
                        <td>${data.description}</td>
                    </tr>
                `;
            }
            return html;
        },

        formatKeyDisplay: function(key) {
            return key
                .replace('Control+', 'Ctrl+')
                .replace('Key', '')
                .replace('Digit', '')
                .replace('Arrow', '→')
                .replace('Escape', 'Esc');
        },

        showModal: function() {
            const modal = document.getElementById('keyboardShortcutsModal');
            if (modal) {
                const bsModal = new bootstrap.Modal(modal);
                bsModal.show();
            }
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => KeyboardShortcuts.init());
    } else {
        KeyboardShortcuts.init();
    }

    // Export for global use
    window.KeyboardShortcuts = KeyboardShortcuts;

})();