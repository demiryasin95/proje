/**
 * Modern Toast Notification System
 * Enhanced with animations, icons, and action buttons
 */

(function() {
    'use strict';

    // Toast configuration
    const TOAST_CONFIG = {
        duration: 5000,
        position: 'top-right',
        maxToasts: 5,
        pauseOnHover: true
    };

    // Initialize toast container
    let toastContainer = null;
    let toastQueue = [];
    let activeToasts = [];

    function initToastContainer() {
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container';
            document.body.appendChild(toastContainer);
        }
        return toastContainer;
    }

    // Icon mapping for different types
    const ICONS = {
        success: '<i class="fas fa-check-circle"></i>',
        error: '<i class="fas fa-times-circle"></i>',
        warning: '<i class="fas fa-exclamation-triangle"></i>',
        info: '<i class="fas fa-info-circle"></i>'
    };

    // Title mapping for different types
    const TITLES = {
        success: 'Başarılı',
        error: 'Hata',
        warning: 'Uyarı',
        info: 'Bilgi'
    };

    /**
     * Show a toast notification
     * @param {string} message - The message to display
     * @param {string} type - Type of toast (success, error, warning, info)
     * @param {object} options - Additional options
     */
    function showToast(message, type = 'info', options = {}) {
        const container = initToastContainer();
        
        // Create toast element
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        
        const title = options.title || TITLES[type];
        const icon = ICONS[type] || ICONS.info;
        const duration = options.duration || TOAST_CONFIG.duration;
        const actionText = options.actionText;
        const actionCallback = options.onAction;

        // Build toast HTML
        let toastHTML = `
            <div class="toast-icon">${icon}</div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
                ${actionText ? `<div class="toast-action"><button class="toast-action-btn">${actionText}</button></div>` : ''}
            </div>
            <button class="toast-close" aria-label="Close">
                <i class="fas fa-times"></i>
            </button>
        `;

        if (options.showProgress !== false && duration > 0) {
            toastHTML += `<div class="toast-progress"></div>`;
        }

        toast.innerHTML = toastHTML;

        // Add to container
        container.appendChild(toast);

        // Trigger animation
        setTimeout(() => toast.classList.add('show'), 10);

        // Track active toast
        const toastObj = {
            element: toast,
            timeout: null,
            progressInterval: null,
            isPaused: false
        };
        activeToasts.push(toastObj);

        // Remove old toasts if exceeding max
        if (activeToasts.length > TOAST_CONFIG.maxToasts) {
            const oldestToast = activeToasts.shift();
            removeToast(oldestToast.element);
        }

        // Setup close button
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => removeToast(toast));

        // Setup action button
        if (actionText && actionCallback) {
            const actionBtn = toast.querySelector('.toast-action-btn');
            actionBtn.addEventListener('click', () => {
                actionCallback();
                removeToast(toast);
            });
        }

        // Auto-dismiss logic
        if (duration > 0) {
            let remainingTime = duration;
            const startTime = Date.now();

            // Progress bar animation
            const progressBar = toast.querySelector('.toast-progress');
            if (progressBar) {
                progressBar.style.width = '100%';
                progressBar.style.transition = `width ${duration}ms linear`;
                setTimeout(() => {
                    progressBar.style.width = '0%';
                }, 10);
            }

            // Pause on hover
            if (TOAST_CONFIG.pauseOnHover) {
                toast.addEventListener('mouseenter', () => {
                    toastObj.isPaused = true;
                    if (toastObj.timeout) {
                        clearTimeout(toastObj.timeout);
                        toastObj.timeout = null;
                    }
                    if (progressBar) {
                        const elapsed = Date.now() - startTime;
                        remainingTime = duration - elapsed;
                        progressBar.style.transition = 'none';
                        const currentWidth = (remainingTime / duration) * 100;
                        progressBar.style.width = currentWidth + '%';
                    }
                });

                toast.addEventListener('mouseleave', () => {
                    toastObj.isPaused = false;
                    if (progressBar) {
                        progressBar.style.transition = `width ${remainingTime}ms linear`;
                        progressBar.style.width = '0%';
                    }
                    toastObj.timeout = setTimeout(() => removeToast(toast), remainingTime);
                });
            }

            // Set initial timeout
            toastObj.timeout = setTimeout(() => removeToast(toast), duration);
        }

        return toast;
    }

    /**
     * Remove a toast notification
     */
    function removeToast(toast) {
        if (!toast || !toast.parentElement) return;

        // Remove from active toasts
        const index = activeToasts.findIndex(t => t.element === toast);
        if (index > -1) {
            const toastObj = activeToasts[index];
            if (toastObj.timeout) clearTimeout(toastObj.timeout);
            if (toastObj.progressInterval) clearInterval(toastObj.progressInterval);
            activeToasts.splice(index, 1);
        }

        // Animate out
        toast.classList.remove('show');
        toast.classList.add('hide');

        setTimeout(() => {
            if (toast.parentElement) {
                toast.parentElement.removeChild(toast);
            }
        }, 300);
    }

    /**
     * Clear all toasts
     */
    function clearAllToasts() {
        activeToasts.forEach(toastObj => {
            removeToast(toastObj.element);
        });
        activeToasts = [];
    }

    // Convenience methods
    const toast = {
        success: (message, options) => showToast(message, 'success', options),
        error: (message, options) => showToast(message, 'error', options),
        warning: (message, options) => showToast(message, 'warning', options),
        info: (message, options) => showToast(message, 'info', options),
        show: showToast,
        clear: clearAllToasts
    };

    // Export to window
    window.toast = toast;

    // Backward compatibility with old showAlert function
    window.showMelodyToast = function(message, type) {
        toast.show(message, type);
    };

    // Also integrate with existing showAlert if it exists
    const originalShowAlert = window.showAlert;
    window.showAlert = function(message, type) {
        // Map Bootstrap alert types to toast types
        const typeMap = {
            'success': 'success',
            'danger': 'error',
            'warning': 'warning',
            'info': 'info',
            'error': 'error'
        };
        toast.show(message, typeMap[type] || 'info');
        
        // Also call original if it exists
        if (originalShowAlert && typeof originalShowAlert === 'function') {
            originalShowAlert(message, type);
        }
    };

})();

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        console.log('Toast notification system initialized');
    });
} else {
    console.log('Toast notification system initialized');
}
