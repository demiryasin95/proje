/*------------------------------------------------------------------
 [Melody Premium Bootstrap Admin Template JavaScript Integration]
 Project: Melody Admin - Enhanced for Student Study Program
 Version: 2.0.0 - Integrated Version
-------------------------------------------------------------------*/

// Melody Theme JavaScript
class MelodyTheme {
    constructor() {
        this.init();
    }

    init() {
        this.initSidebar();
        this.initCards();
        this.initButtons();
        this.initTables();
        this.initForms();
        this.initModals();
        this.initToasts();
        this.initCharts();
        this.initTooltips();
        this.initAnimations();
    }

    // Enhanced Sidebar
    initSidebar() {
        // Check if overlay already exists, if not create it
        let overlay = document.querySelector('.sidebar-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'sidebar-overlay';
            document.body.appendChild(overlay);
        }
        
        // Ensure overlay is hidden on page load
        overlay.classList.remove('show');
        
        // Close sidebar overlay when clicking on it (for mobile)
        overlay.addEventListener('click', () => {
            const sidebar = document.querySelector('.admin-sidebar');
            if (sidebar) {
                sidebar.classList.remove('show');
            }
            overlay.classList.remove('show');
        });

        // Active navigation link
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');
        navLinks.forEach(link => {
            const linkPath = link.getAttribute('href');
            if (linkPath && currentPath.indexOf(linkPath.split('/')[2]) !== -1) {
                link.classList.add('active');
            }
        });
    }

    // Enhanced Cards
    initCards() {
        // Avoid hover re-animations on cards to prevent flicker
        const cards = document.querySelectorAll('.card');
        cards.forEach(card => {
            card.classList.add('melody-fade-in');
        });
    }

    // Enhanced Buttons - Ripple effect DISABLED
    initButtons() {
        // Ripple effect disabled to prevent button growth issue
        return;
    }

    // Enhanced Tables
    initTables() {
        // Disable table animations to prevent flicker
        return;
    }

    // Enhanced Forms
    initForms() {
        const inputs = document.querySelectorAll('.form-control, .form-select');
        inputs.forEach(input => {
            input.addEventListener('focus', () => {
                input.parentElement.classList.add('focused');
            });
            
            input.addEventListener('blur', () => {
                if (!input.value) {
                    input.parentElement.classList.remove('focused');
                }
            });
        });
    }

    // Enhanced Modals
    initModals() {
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => {
            modal.addEventListener('show.bs.modal', () => {
                modal.classList.add('melody-fade-in');
            });
        });
    }

    // Toast Notifications
    initToasts() {
        window.showMelodyToast = (message, type = 'info', duration = 3000) => {
            const toast = document.createElement('div');
            toast.className = `melody-toast melody-toast-${type}`;
            toast.innerHTML = `
                <div class="toast-content">
                    <div class="toast-message">${message}</div>
                    <button type="button" class="toast-close">&times;</button>
                </div>
            `;
            
            document.body.appendChild(toast);
            
            // Show toast
            setTimeout(() => {
                toast.classList.add('show');
            }, 100);
            
            // Hide toast
            const hideToast = () => {
                toast.classList.remove('show');
                setTimeout(() => {
                    toast.remove();
                }, 300);
            };
            
            // Auto hide
            setTimeout(hideToast, duration);
            
            // Manual close
            toast.querySelector('.toast-close').addEventListener('click', hideToast);
        };
    }

    // Charts Initialization
    initCharts() {
        // Chart.js global configuration
        if (typeof Chart !== 'undefined') {
            Chart.defaults.font.family = 'PT Sans';
            Chart.defaults.font.size = 12;
            Chart.defaults.color = '#525F7F';
            
            Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(0, 0, 0, 0.8)';
            Chart.defaults.plugins.tooltip.padding = 12;
            Chart.defaults.plugins.tooltip.cornerRadius = 8;
            Chart.defaults.plugins.tooltip.titleFont = { weight: 'bold' };
            
            Chart.defaults.plugins.legend.labels.padding = 20;
            Chart.defaults.plugins.legend.labels.usePointStyle = true;
        }
    }

    // Tooltips - Handled by admin.js for sidebar
    initTooltips() {
        // Tooltip initialization is now handled in admin.js for better control
        // This prevents tooltips from showing when sidebar is expanded
        return;
    }

    // Animations
    initAnimations() {
        // Intersection Observer for scroll animations
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('melody-fade-in');
                }
            });
        });

        // Observe elements
        const animatedElements = document.querySelectorAll('.chart-container');
        animatedElements.forEach(el => {
            observer.observe(el);
        });
    }

    // Loading States
    showLoading(element, message = 'Yükleniyor...') {
        const loadingHtml = `
            <div class="melody-loading text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">${message}</span>
                </div>
                <p class="mt-2 text-muted">${message}</p>
            </div>
        `;
        
        if (typeof element === 'string') {
            document.querySelector(element).innerHTML = loadingHtml;
        } else {
            element.innerHTML = loadingHtml;
        }
    }

    hideLoading(element) {
        const loadingElement = typeof element === 'string' 
            ? document.querySelector(element).querySelector('.melody-loading')
            : element.querySelector('.melody-loading');
            
        if (loadingElement) {
            loadingElement.remove();
        }
    }

    // Enhanced AJAX
    melodyAjax(options) {
        const defaultOptions = {
            beforeSend: () => {
                if (options.loadingElement) {
                    this.showLoading(options.loadingElement, options.loadingMessage);
                }
            },
            complete: () => {
                if (options.loadingElement) {
                    this.hideLoading(options.loadingElement);
                }
            },
            success: (response) => {
                if (response.success) {
                    showMelodyToast(options.successMessage || 'İşlem başarıyla tamamlandı.', 'success');
                } else {
                    showMelodyToast(response.message || 'İşlem tamamlanamadı.', 'warning');
                }
            },
            error: (xhr) => {
                let message = 'Bir hata oluştu. Lütfen tekrar deneyin.';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                } else if (xhr.status === 0) {
                    message = 'Sunucuya bağlanılamıyor. İnternet bağlantınızı kontrol edin.';
                } else if (xhr.status === 404) {
                    message = 'İstenen kaynak bulunamadı.';
                } else if (xhr.status === 500) {
                    message = 'Sunucu hatası oluştu.';
                }
                
                showMelodyToast(message, 'error');
            }
        };

        return $.ajax({
            ...defaultOptions,
            ...options,
            beforeSend: function() {
                defaultOptions.beforeSend();
                if (options.beforeSend) options.beforeSend();
            },
            complete: function() {
                defaultOptions.complete();
                if (options.complete) options.complete();
            },
            success: function(response) {
                defaultOptions.success(response);
                if (options.success) options.success(response);
            },
            error: function(xhr) {
                defaultOptions.error(xhr);
                if (options.error) options.error(xhr);
            }
        });
    }
}

// Enhanced DataTables
function initMelodyDataTables() {
    if (typeof $.fn.DataTable !== 'undefined') {
        $.extend(true, $.fn.DataTable.defaults, {
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.6/i18n/tr.json'
            },
            pageLength: 10,
            responsive: true,
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            columnDefs: [
                { 
                    targets: 'no-sort', 
                    orderable: false 
                }
            ],
            initComplete: function() {
                // Add Melody styling to DataTables elements
                $('.dataTables_length select').addClass('form-select form-select-sm melody-rounded');
                $('.dataTables_filter input').addClass('form-control form-control-sm melody-rounded');
                $('.dataTables_paginate .paginate_button').addClass('btn btn-sm');
                $('.dataTables_paginate .paginate_button.current').addClass('btn-primary');
            }
        });
    }
}

// Enhanced Charts
function createMelodyChart(ctx, config) {
    const melodyDefaults = {
        plugins: {
            legend: {
                labels: {
                    font: {
                        family: 'PT Sans',
                        size: 12
                    },
                    color: '#525F7F'
                }
            }
        },
        scales: {
            x: {
                ticks: {
                    font: {
                        family: 'PT Sans',
                        size: 11
                    },
                    color: '#525F7F'
                },
                grid: {
                    color: 'rgba(0,0,0,0.1)'
                }
            },
            y: {
                ticks: {
                    font: {
                        family: 'PT Sans',
                        size: 11
                    },
                    color: '#525F7F'
                },
                grid: {
                    color: 'rgba(0,0,0,0.1)'
                }
            }
        }
    };

    return new Chart(ctx, {
        ...config,
        options: {
            ...melodyDefaults,
            ...config.options
        }
    });
}

// Utility Functions
const melodyUtils = {
    formatDate: (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    },

    formatTime: (timeString) => {
        const time = new Date('2000-01-01T' + timeString);
        return time.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    formatPhone: (phone) => {
        const cleaned = phone.replace(/\D/g, '');
        if (cleaned.length === 0) return '';
        if (!cleaned.startsWith('0')) return '0' + cleaned;
        return cleaned.replace(/(\d{1})(\d{3})(\d{3})(\d{2})(\d{2}).*/, '$1$2 $3 $4 $5').trim();
    },

    getAttendanceBadge: (status) => {
        const badges = {
            'Present': '<span class="melody-badge melody-badge-success">Katıldı</span>',
            'Absent': '<span class="melody-badge melody-badge-danger">Katılmadı</span>',
            'Pending': '<span class="melody-badge melody-badge-warning">Bekliyor</span>'
        };
        return badges[status] || '<span class="melody-badge melody-badge-info">Bilinmiyor</span>';
    },

    getClassroomTypeBadge: (type) => {
        const badges = {
            'Science': '<span class="melody-badge melody-badge-success">Sayısal</span>',
            'Literature': '<span class="melody-badge melody-badge-primary">Sözel</span>',
            'EqualWeight': '<span class="melody-badge melody-badge-warning">Eşit Ağırlık</span>',
            'Language': '<span class="melody-badge melody-badge-info">Dil</span>'
        };
        return badges[type] || '<span class="melody-badge melody-badge-secondary">Belirtilmemiş</span>';
    },

    loadingStates: {
        show: (element, message = 'Yükleniyor...') => {
            const loadingHtml = `
                <div class="melody-loading text-center py-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">${message}</span>
                    </div>
                    <p class="mt-2 text-muted">${message}</p>
                </div>
            `;
            
            if (typeof element === 'string') {
                document.querySelector(element).innerHTML = loadingHtml;
            } else {
                element.innerHTML = loadingHtml;
            }
        },

        hide: (element) => {
            const loadingElement = typeof element === 'string' 
                ? document.querySelector(element).querySelector('.melody-loading')
                : element.querySelector('.melody-loading');
                
            if (loadingElement) {
                loadingElement.remove();
            }
        }
    }
};

// Initialize Melody Theme when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Melody Theme
    window.melodyTheme = new MelodyTheme();
    
    // Initialize DataTables
    initMelodyDataTables();
    
    // Add Melody CSS classes to body
    document.body.classList.add('melody-theme');
    
    // Show welcome message
    if (typeof showMelodyToast !== 'undefined') {
        showMelodyToast('Melody tema başarıyla yüklendi!', 'success', 2000);
    }
});

// Export for global use
window.MelodyTheme = MelodyTheme;
window.melodyUtils = melodyUtils;
window.createMelodyChart = createMelodyChart;
window.initMelodyDataTables = initMelodyDataTables;
