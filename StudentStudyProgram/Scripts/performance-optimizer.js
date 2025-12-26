// Performance Optimization System
(function() {
    'use strict';

    const PerformanceOptimizer = {
        imageObserver: null,
        scrollThrottle: null,
        resizeThrottle: null,
        
        init: function() {
            this.optimizeImages();
            this.implementLazyLoading();
            this.optimizeScrollEvents();
            this.optimizeResizeEvents();
            this.cacheStaticContent();
            this.prefetchLinks();
            this.optimizeAnimations();
            this.monitorPerformance();
        },

        // Lazy load images
        implementLazyLoading: function() {
            if ('IntersectionObserver' in window) {
                this.imageObserver = new IntersectionObserver((entries, observer) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            const img = entry.target;
                            if (img.dataset.src) {
                                img.src = img.dataset.src;
                                img.removeAttribute('data-src');
                            }
                            if (img.dataset.srcset) {
                                img.srcset = img.dataset.srcset;
                                img.removeAttribute('data-srcset');
                            }
                            img.classList.add('loaded');
                            observer.unobserve(img);
                        }
                    });
                }, {
                    rootMargin: '50px 0px',
                    threshold: 0.01
                });

                // Observe all images with data-src
                document.querySelectorAll('img[data-src]').forEach(img => {
                    this.imageObserver.observe(img);
                });
            } else {
                // Fallback for older browsers
                document.querySelectorAll('img[data-src]').forEach(img => {
                    img.src = img.dataset.src;
                    img.removeAttribute('data-src');
                });
            }
        },

        // Optimize images
        optimizeImages: function() {
            // Add loading="lazy" to images
            document.querySelectorAll('img:not([loading])').forEach(img => {
                if (!img.closest('.no-lazy')) {
                    img.loading = 'lazy';
                }
            });

            // Compress large images on upload
            this.setupImageCompression();
        },

        setupImageCompression: function() {
            document.addEventListener('change', (e) => {
                if (e.target.type === 'file' && e.target.accept && e.target.accept.includes('image')) {
                    const files = Array.from(e.target.files);
                    const maxSize = 1024 * 1024; // 1MB

                    files.forEach(file => {
                        if (file.size > maxSize && file.type.startsWith('image/')) {
                            this.compressImage(file, e.target);
                        }
                    });
                }
            });
        },

        compressImage: function(file, input) {
            const reader = new FileReader();
            reader.onload = (e) => {
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement('canvas');
                    let width = img.width;
                    let height = img.height;
                    const maxDimension = 1920;

                    if (width > maxDimension || height > maxDimension) {
                        if (width > height) {
                            height = (height / width) * maxDimension;
                            width = maxDimension;
                        } else {
                            width = (width / height) * maxDimension;
                            height = maxDimension;
                        }
                    }

                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, width, height);

                    canvas.toBlob((blob) => {
                        const compressedFile = new File([blob], file.name, {
                            type: 'image/jpeg',
                            lastModified: Date.now()
                        });
                        
                        // Replace file in input
                        const dataTransfer = new DataTransfer();
                        dataTransfer.items.add(compressedFile);
                        input.files = dataTransfer.files;

                        console.log(`Image compressed: ${file.size} → ${blob.size} bytes`);
                    }, 'image/jpeg', 0.85);
                };
                img.src = e.target.result;
            };
            reader.readAsDataURL(file);
        },

        // Throttle scroll events
        optimizeScrollEvents: function() {
            let ticking = false;
            const scrollCallbacks = [];

            window.addEventListener('scroll', () => {
                if (!ticking) {
                    window.requestAnimationFrame(() => {
                        scrollCallbacks.forEach(callback => callback());
                        ticking = false;
                    });
                    ticking = true;
                }
            }, { passive: true });

            // Export method to register scroll callbacks
            window.onOptimizedScroll = function(callback) {
                scrollCallbacks.push(callback);
            };
        },

        // Throttle resize events
        optimizeResizeEvents: function() {
            let resizeTimer;
            const resizeCallbacks = [];

            window.addEventListener('resize', () => {
                clearTimeout(resizeTimer);
                resizeTimer = setTimeout(() => {
                    resizeCallbacks.forEach(callback => callback());
                }, 250);
            });

            // Export method to register resize callbacks
            window.onOptimizedResize = function(callback) {
                resizeCallbacks.push(callback);
            };
        },

        // Cache static content
        cacheStaticContent: function() {
            if ('caches' in window) {
                caches.open('static-v1').then(cache => {
                    // Cache CSS and JS files
                    cache.addAll([
                        '/Content/admin-theme.v3.css',
                        '/Content/widgets-and-bulk.css',
                        '/Content/skeleton-loader.css',
                        '/Content/toast-notifications.css',
                        '/Scripts/admin.js',
                        '/Scripts/melody-theme.js'
                    ]).catch(err => {
                        console.log('Cache failed:', err);
                    });
                });
            }
        },

        // Prefetch likely next pages
        prefetchLinks: function() {
            if ('IntersectionObserver' in window) {
                const linkObserver = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            const link = entry.target;
                            this.prefetchLink(link.href);
                            linkObserver.unobserve(link);
                        }
                    });
                });

                // Observe navigation links
                document.querySelectorAll('a[href^="/"]').forEach(link => {
                    if (!link.hasAttribute('data-no-prefetch')) {
                        linkObserver.observe(link);
                    }
                });
            }
        },

        prefetchLink: function(url) {
            if (document.querySelector(`link[rel="prefetch"][href="${url}"]`)) {
                return; // Already prefetched
            }

            const link = document.createElement('link');
            link.rel = 'prefetch';
            link.href = url;
            document.head.appendChild(link);
        },

        // Optimize CSS animations
        optimizeAnimations: function() {
            // Disable animations when user prefers reduced motion
            if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                document.documentElement.style.setProperty('--animation-duration', '0.01ms');
                document.documentElement.setAttribute('data-reduce-motion', 'true');
            }

            // Pause animations on hidden tabs
            document.addEventListener('visibilitychange', () => {
                if (document.hidden) {
                    document.documentElement.setAttribute('data-animations-paused', 'true');
                } else {
                    document.documentElement.removeAttribute('data-animations-paused');
                }
            });
        },

        // Monitor performance
        monitorPerformance: function() {
            if ('performance' in window && 'PerformanceObserver' in window) {
                // Monitor long tasks
                try {
                    const observer = new PerformanceObserver((list) => {
                        for (const entry of list.getEntries()) {
                            if (entry.duration > 50) {
                                console.warn('Long task detected:', entry.duration.toFixed(2) + 'ms');
                            }
                        }
                    });
                    observer.observe({ entryTypes: ['longtask'] });
                } catch (e) {
                    // Long task API not supported
                }

                // Log page load metrics
                window.addEventListener('load', () => {
                    setTimeout(() => {
                        const perfData = performance.getEntriesByType('navigation')[0];
                        if (perfData) {
                            console.log('Performance Metrics:', {
                                'DNS Lookup': Math.round(perfData.domainLookupEnd - perfData.domainLookupStart) + 'ms',
                                'TCP Connection': Math.round(perfData.connectEnd - perfData.connectStart) + 'ms',
                                'Response Time': Math.round(perfData.responseEnd - perfData.requestStart) + 'ms',
                                'DOM Processing': Math.round(perfData.domComplete - perfData.domLoading) + 'ms',
                                'Total Load Time': Math.round(perfData.loadEventEnd - perfData.fetchStart) + 'ms'
                            });
                        }
                    }, 0);
                });
            }
        },

        // Debounce function
        debounce: function(func, wait) {
            let timeout;
            return function executedFunction(...args) {
                const later = () => {
                    clearTimeout(timeout);
                    func(...args);
                };
                clearTimeout(timeout);
                timeout = setTimeout(later, wait);
            };
        },

        // Throttle function
        throttle: function(func, limit) {
            let inThrottle;
            return function(...args) {
                if (!inThrottle) {
                    func.apply(this, args);
                    inThrottle = true;
                    setTimeout(() => inThrottle = false, limit);
                }
            };
        },

        // Optimize table rendering for large datasets
        virtualizeTable: function(table, rowHeight = 50) {
            if (typeof table === 'string') {
                table = document.querySelector(table);
            }
            if (!table) return;

            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.querySelectorAll('tr'));
            
            if (rows.length < 50) return; // Not worth virtualizing small tables

            const container = table.parentElement;
            const visibleRows = Math.ceil(container.clientHeight / rowHeight) + 2;
            let scrollTop = 0;

            const render = () => {
                const startIndex = Math.floor(scrollTop / rowHeight);
                const endIndex = Math.min(startIndex + visibleRows, rows.length);

                tbody.innerHTML = '';
                tbody.style.height = (rows.length * rowHeight) + 'px';
                tbody.style.paddingTop = (startIndex * rowHeight) + 'px';

                for (let i = startIndex; i < endIndex; i++) {
                    tbody.appendChild(rows[i].cloneNode(true));
                }
            };

            container.addEventListener('scroll', this.throttle(() => {
                scrollTop = container.scrollTop;
                render();
            }, 16), { passive: true });

            render();
        }
    };

    // Add performance-related CSS
    const style = document.createElement('style');
    style.textContent = `
        [data-reduce-motion="true"] * {
            animation-duration: 0.01ms !important;
            transition-duration: 0.01ms !important;
        }
        
        [data-animations-paused="true"] * {
            animation-play-state: paused !important;
        }

        img {
            transition: opacity 0.3s ease;
        }

        img:not(.loaded) {
            opacity: 0;
        }

        img.loaded {
            opacity: 1;
        }

        /* Will-change hints for better performance */
        .card:hover,
        .btn:hover,
        .nav-link:hover {
            will-change: transform;
        }

        /* GPU acceleration for animations */
        .modal.fade,
        .dropdown-menu,
        .tooltip {
            transform: translateZ(0);
            backface-visibility: hidden;
            perspective: 1000px;
        }
    `;
    document.head.appendChild(style);

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => PerformanceOptimizer.init());
    } else {
        PerformanceOptimizer.init();
    }

    // Export for global use
    window.PerformanceOptimizer = PerformanceOptimizer;

})();
