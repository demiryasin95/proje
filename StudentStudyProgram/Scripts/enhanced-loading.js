// Enhanced Loading States System
(function() {
    'use strict';

    const EnhancedLoading = {
        activeLoaders: new Set(),
        
        init: function() {
            this.createGlobalLoader();
            this.enhanceAjaxCalls();
            this.enhanceForms();
            this.createSkeletonScreens();
        },

        // Global page loader
        createGlobalLoader: function() {
            const loader = document.createElement('div');
            loader.id = 'globalPageLoader';
            loader.className = 'global-page-loader';
            loader.innerHTML = `
                <div class="loader-content">
                    <div class="spinner-wrapper">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Yükleniyor...</span>
                        </div>
                    </div>
                    <p class="loader-text mt-3">Yükleniyor...</p>
                    <div class="progress-bar-wrapper mt-3">
                        <div class="progress" style="height: 4px;">
                            <div class="progress-bar progress-bar-striped progress-bar-animated" 
                                 role="progressbar" style="width: 0%"></div>
                        </div>
                    </div>
                </div>
            `;
            document.body.appendChild(loader);
        },

        // Show global loader with optional message and progress
        showGlobalLoader: function(message = 'Yükleniyor...', showProgress = false) {
            const loader = document.getElementById('globalPageLoader');
            if (!loader) return;

            loader.querySelector('.loader-text').textContent = message;
            loader.querySelector('.progress-bar-wrapper').style.display = showProgress ? 'block' : 'none';
            loader.classList.add('active');
            document.body.style.overflow = 'hidden';

            if (showProgress) {
                this.animateProgress(0, 90, 2000);
            }
        },

        hideGlobalLoader: function() {
            const loader = document.getElementById('globalPageLoader');
            if (!loader) return;

            const progressBar = loader.querySelector('.progress-bar');
            if (progressBar) {
                progressBar.style.width = '100%';
            }

            setTimeout(() => {
                loader.classList.remove('active');
                document.body.style.overflow = '';
                if (progressBar) {
                    progressBar.style.width = '0%';
                }
            }, 300);
        },

        animateProgress: function(start, end, duration) {
            const loader = document.getElementById('globalPageLoader');
            if (!loader) return;

            const progressBar = loader.querySelector('.progress-bar');
            const startTime = Date.now();

            const animate = () => {
                const elapsed = Date.now() - startTime;
                const progress = Math.min((elapsed / duration) * (end - start) + start, end);
                
                progressBar.style.width = progress + '%';

                if (progress < end) {
                    requestAnimationFrame(animate);
                }
            };

            animate();
        },

        // Inline loader for elements
        showInlineLoader: function(element, size = 'md', message = '') {
            if (typeof element === 'string') {
                element = document.querySelector(element);
            }
            if (!element) return;

            const loaderId = 'loader_' + Math.random().toString(36).substr(2, 9);
            this.activeLoaders.add(loaderId);

            const sizeClasses = {
                sm: 'spinner-border-sm',
                md: '',
                lg: 'spinner-border-lg'
            };

            const originalContent = element.innerHTML;
            element.setAttribute('data-original-content', originalContent);
            element.setAttribute('data-loader-id', loaderId);

            element.innerHTML = `
                <div class="inline-loader text-center py-3">
                    <div class="spinner-border text-primary ${sizeClasses[size]}" role="status">
                        <span class="visually-hidden">Yükleniyor...</span>
                    </div>
                    ${message ? `<p class="mt-2 text-muted small">${message}</p>` : ''}
                </div>
            `;

            return loaderId;
        },

        hideInlineLoader: function(element, delay = 0) {
            if (typeof element === 'string') {
                element = document.querySelector(element);
            }
            if (!element) return;

            setTimeout(() => {
                const loaderId = element.getAttribute('data-loader-id');
                if (loaderId) {
                    this.activeLoaders.delete(loaderId);
                }

                const originalContent = element.getAttribute('data-original-content');
                if (originalContent) {
                    element.innerHTML = originalContent;
                    element.removeAttribute('data-original-content');
                    element.removeAttribute('data-loader-id');
                }
            }, delay);
        },

        // Button loading state
        setButtonLoading: function(button, loading = true) {
            if (typeof button === 'string') {
                button = document.querySelector(button);
            }
            if (!button) return;

            if (loading) {
                button.setAttribute('data-original-html', button.innerHTML);
                button.disabled = true;
                
                const spinner = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>';
                const text = button.getAttribute('data-loading-text') || 'İşlem yapılıyor...';
                button.innerHTML = spinner + text;
            } else {
                const originalHtml = button.getAttribute('data-original-html');
                if (originalHtml) {
                    button.innerHTML = originalHtml;
                    button.removeAttribute('data-original-html');
                }
                button.disabled = false;
            }
        },

        // Skeleton screens for tables
        createSkeletonScreens: function() {
            // Add skeleton styles
            if (!document.getElementById('skeletonStyles')) {
                const style = document.createElement('style');
                style.id = 'skeletonStyles';
                style.textContent = `
                    .skeleton {
                        background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
                        background-size: 200% 100%;
                        animation: skeleton-loading 1.5s ease-in-out infinite;
                        border-radius: 4px;
                    }
                    [data-theme="dark"] .skeleton {
                        background: linear-gradient(90deg, #2a2d3a 25%, #3a3d4a 50%, #2a2d3a 75%);
                        background-size: 200% 100%;
                    }
                    @keyframes skeleton-loading {
                        0% { background-position: 200% 0; }
                        100% { background-position: -200% 0; }
                    }
                    .skeleton-text {
                        height: 1em;
                        margin: 0.5em 0;
                    }
                    .skeleton-text-short { width: 60%; }
                    .skeleton-text-medium { width: 80%; }
                    .skeleton-text-long { width: 100%; }
                `;
                document.head.appendChild(style);
            }
        },

        showTableSkeleton: function(table, rows = 5) {
            if (typeof table === 'string') {
                table = document.querySelector(table);
            }
            if (!table) return;

            const tbody = table.querySelector('tbody');
            if (!tbody) return;

            const colCount = table.querySelectorAll('thead th').length;
            
            let html = '';
            for (let i = 0; i < rows; i++) {
                html += '<tr>';
                for (let j = 0; j < colCount; j++) {
                    const widthClass = j === 0 ? 'skeleton-text-short' : 
                                      j === colCount - 1 ? 'skeleton-text-short' : 
                                      'skeleton-text-medium';
                    html += `<td><div class="skeleton skeleton-text ${widthClass}"></div></td>`;
                }
                html += '</tr>';
            }

            tbody.innerHTML = html;
        },

        // Enhance jQuery AJAX calls
        enhanceAjaxCalls: function() {
            if (typeof $ === 'undefined') return;

            $(document).ajaxStart(() => {
                // Show a subtle loading indicator in navbar
                this.showTopLoadingBar();
            });

            $(document).ajaxStop(() => {
                this.hideTopLoadingBar();
            });

            $(document).ajaxError((event, jqxhr, settings, thrownError) => {
                this.hideGlobalLoader();
                this.hideTopLoadingBar();
                console.error('AJAX Error:', thrownError);
            });
        },

        showTopLoadingBar: function() {
            let bar = document.getElementById('topLoadingBar');
            if (!bar) {
                bar = document.createElement('div');
                bar.id = 'topLoadingBar';
                bar.className = 'top-loading-bar';
                document.body.appendChild(bar);
            }
            bar.classList.add('loading');
        },

        hideTopLoadingBar: function() {
            const bar = document.getElementById('topLoadingBar');
            if (bar) {
                bar.classList.add('loaded');
                setTimeout(() => {
                    bar.classList.remove('loading', 'loaded');
                }, 400);
            }
        },

        // Enhance form submissions
        enhanceForms: function() {
            document.addEventListener('submit', (e) => {
                const form = e.target;
                if (form.classList.contains('no-loading')) return;

                const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
                if (submitBtn) {
                    this.setButtonLoading(submitBtn, true);
                    
                    // Auto-hide after 10 seconds as fallback
                    setTimeout(() => {
                        this.setButtonLoading(submitBtn, false);
                    }, 10000);
                }
            });
        },

        // Progress indicator for long operations
        createProgressModal: function(title = 'İşlem Yapılıyor') {
            const modalId = 'progressModal';
            let modal = document.getElementById(modalId);
            
            if (!modal) {
                modal = document.createElement('div');
                modal.id = modalId;
                modal.className = 'modal fade';
                modal.setAttribute('data-bs-backdrop', 'static');
                modal.setAttribute('data-bs-keyboard', 'false');
                modal.innerHTML = `
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header border-0">
                                <h5 class="modal-title">${title}</h5>
                            </div>
                            <div class="modal-body text-center py-4">
                                <div class="spinner-border text-primary mb-3" style="width: 3rem; height: 3rem;" role="status">
                                    <span class="visually-hidden">Yükleniyor...</span>
                                </div>
                                <p class="progress-message mb-3">Lütfen bekleyin...</p>
                                <div class="progress" style="height: 8px;">
                                    <div class="progress-bar progress-bar-striped progress-bar-animated" 
                                         role="progressbar" style="width: 0%"></div>
                                </div>
                                <small class="text-muted d-block mt-2 progress-detail"></small>
                            </div>
                        </div>
                    </div>
                `;
                document.body.appendChild(modal);
            }

            return new bootstrap.Modal(modal);
        },

        updateProgress: function(percent, message = '', detail = '') {
            const modal = document.getElementById('progressModal');
            if (!modal) return;

            const progressBar = modal.querySelector('.progress-bar');
            const progressMessage = modal.querySelector('.progress-message');
            const progressDetail = modal.querySelector('.progress-detail');

            if (progressBar) progressBar.style.width = percent + '%';
            if (progressMessage && message) progressMessage.textContent = message;
            if (progressDetail && detail) progressDetail.textContent = detail;
        }
    };

    // Create global CSS for loaders
    const style = document.createElement('style');
    style.textContent = `
        .global-page-loader {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.7);
            backdrop-filter: blur(5px);
            z-index: 9999;
            display: none;
            align-items: center;
            justify-content: center;
            pointer-events: none;
        }
        .global-page-loader.active {
            display: flex;
            pointer-events: all;
        }
        .global-page-loader.active .loader-content {
            pointer-events: all;
        }
        .global-page-loader .loader-content {
            background: var(--card-bg, white);
            padding: 2rem 3rem;
            border-radius: 12px;
            text-align: center;
            box-shadow: 0 10px 40px rgba(0,0,0,0.3);
            min-width: 300px;
        }
        .global-page-loader .spinner-wrapper {
            display: flex;
            justify-content: center;
        }
        .global-page-loader .progress-bar-wrapper {
            width: 100%;
            display: none;
        }

        .top-loading-bar {
            position: fixed;
            top: 0;
            left: 0;
            height: 3px;
            background: linear-gradient(90deg, #5e72e4, #825ee4);
            z-index: 10000;
            width: 0%;
            transition: width 0.3s ease;
        }
        .top-loading-bar.loading {
            animation: loading-bar 2s ease-in-out infinite;
        }
        .top-loading-bar.loaded {
            width: 100%;
            opacity: 0;
        }
        @keyframes loading-bar {
            0% { width: 0%; }
            50% { width: 70%; }
            100% { width: 100%; opacity: 0; }
        }

        .spinner-border-lg {
            width: 2rem;
            height: 2rem;
            border-width: 0.3em;
        }
    `;
    document.head.appendChild(style);

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => EnhancedLoading.init());
    } else {
        EnhancedLoading.init();
    }

    // Export for global use
    window.EnhancedLoading = EnhancedLoading;

})();