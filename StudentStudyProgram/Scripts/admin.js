// Admin Panel JavaScript
$(document).ready(function() {
    initializeAdminPanel();
});

function initializeAdminPanel() {
    // Initialize tooltips based on sidebar state
    function updateTooltips() {
        var isCollapsed = $('.admin-container').hasClass('collapsed');
        var $tooltipElements = $('.sidebar-nav [data-bs-toggle="tooltip"]');
        
        if (isCollapsed) {
            // Enable tooltips when collapsed
            $tooltipElements.each(function() {
                var tooltip = bootstrap.Tooltip.getInstance(this);
                if (!tooltip) {
                    new bootstrap.Tooltip(this);
                }
            });
        } else {
            // Disable and hide tooltips when expanded
            $tooltipElements.each(function() {
                var tooltip = bootstrap.Tooltip.getInstance(this);
                if (tooltip) {
                    tooltip.dispose();
                }
            });
        }
    }
    
    // Sidebar toggle: desktop collapses, mobile overlays
    $('.sidebar-toggle').on('click', function() {
        if ($(window).width() > 992) {
            $('.admin-container').toggleClass('collapsed');
            try { localStorage.setItem('sidebarCollapsed', $('.admin-container').hasClass('collapsed') ? '1' : '0'); } catch(e){}
            // Update tooltips after toggle
            setTimeout(updateTooltips, 350);
        } else {
            $('.admin-sidebar').toggleClass('show');
            $('.sidebar-overlay').toggleClass('show');
        }
    });

    // Apply persisted collapsed state on load (desktop)
    try {
        var persisted = localStorage.getItem('sidebarCollapsed');
        if (persisted === '1' && $(window).width() > 992) {
            $('.admin-container').addClass('collapsed');
        }
    } catch(e){}
    
    // Initialize tooltips on page load
    setTimeout(updateTooltips, 100);
    
    // Close sidebar when clicking outside on mobile
    $(document).on('click', function(e) {
        if ($(window).width() <= 768) {
            if (!$(e.target).closest('.admin-sidebar, .sidebar-toggle').length) {
                $('.admin-sidebar').removeClass('show');
                $('.sidebar-overlay').removeClass('show');
            }
        }
    });
    
    // Active navigation link
    var currentPath = window.location.pathname;
    $('.sidebar-nav .nav-link').each(function() {
        var linkPath = $(this).attr('href');
        if (linkPath && currentPath.indexOf(linkPath.split('/')[2]) !== -1) {
            $(this).addClass('active');
        }
    });
    
    // Auto-hide alerts after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
    
    // Confirm delete actions (deprecated - now handled by individual pages with modern modals)
    // This is kept for backward compatibility with older pages
    $(document).on('click', '.btn-delete[data-use-legacy-confirm="true"]', function(e) {
        e.preventDefault();
        var itemName = $(this).data('item-name') || 'bu öğeyi';
        
        if (confirm(itemName + ' silmek istediğinize emin misiniz?')) {
            // Proceed with delete action
            var deleteFunction = $(this).data('delete-function');
            if (deleteFunction && typeof window[deleteFunction] === 'function') {
                window[deleteFunction]($(this).data('item-id'));
            }
        }
    });
    
    // Loading state for buttons
    $(document).on('click', '.btn-loading', function() {
        var $btn = $(this);
        var originalText = $btn.html();
        
        $btn.html('<span class="spinner-border spinner-border-sm me-2" role="status"></span>İşlem yapılıyor...')
            .prop('disabled', true);
        
        // Re-enable button after 3 seconds (fallback)
        setTimeout(function() {
            $btn.html(originalText).prop('disabled', false);
        }, 3000);
    });
    
    // Form validation
    $(document).on('submit', 'form', function(e) {
        var $form = $(this);
        var isValid = true;
        
        $form.find('[required]').each(function() {
            var $field = $(this);
            if (!$field.val() || $field.val().trim() === '') {
                $field.addClass('is-invalid');
                isValid = false;
            } else {
                $field.removeClass('is-invalid').addClass('is-valid');
            }
        });
        
        if (!isValid) {
            e.preventDefault();
            showAlert('Lütfen tüm zorunlu alanları doldurun!', 'danger');
        }
    });
    
    // Remove validation classes on input
    $(document).on('input', '.is-invalid', function() {
        $(this).removeClass('is-invalid');
    });
}

// Show alert message
function showAlert(message, type = 'info') {
    var alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    // Remove existing alerts first
    $('.alert').remove();
    
    // Add new alert to the top of the main content
    $('main').prepend(alertHtml);
    
    // Auto-hide after 5 seconds
    setTimeout(function() {
        $('.alert').fadeOut('slow');
    }, 5000);
}

// Show loading state
function showLoading(element, message = 'Yükleniyor...') {
    var loadingHtml = `
        <div class="text-center py-4">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">${message}</span>
            </div>
            <p class="mt-2 text-muted">${message}</p>
        </div>
    `;
    
    $(element).html(loadingHtml);
}

// Hide loading state
function hideLoading(element) {
    $(element).find('.spinner-border').parent().remove();
}

// AJAX error handler
function handleAjaxError(xhr, status, error) {
    console.error('AJAX Error:', error);
    
    var message = 'Bir hata oluştu. Lütfen tekrar deneyin.';
    
    if (xhr.responseJSON && xhr.responseJSON.message) {
        message = xhr.responseJSON.message;
    } else if (xhr.status === 0) {
        message = 'Sunucuya bağlanılamıyor. İnternet bağlantınızı kontrol edin.';
    } else if (xhr.status === 404) {
        message = 'İstenen kaynak bulunamadı.';
    } else if (xhr.status === 500) {
        message = 'Sunucu hatası oluştu.';
    }
    
    showAlert(message, 'danger');
}

// AJAX success handler
function handleAjaxSuccess(response, successMessage = 'İşlem başarıyla tamamlandı.') {
    if (response.success) {
        showAlert(successMessage, 'success');
        return true;
    } else {
        showAlert(response.message || 'İşlem tamamlanamadı.', 'warning');
        return false;
    }
}

// File upload validation
function validateFileUpload(file, allowedTypes = ['image/jpeg', 'image/png', 'image/gif'], maxSize = 2 * 1024 * 1024) {
    if (!file) {
        return { valid: false, message: 'Lütfen bir dosya seçin.' };
    }
    
    if (!allowedTypes.includes(file.type)) {
        return { valid: false, message: 'Geçersiz dosya türü. Sadece JPEG, PNG ve GIF dosyaları kabul edilir.' };
    }
    
    if (file.size > maxSize) {
        return { valid: false, message: 'Dosya boyutu çok büyük. Maksimum 2MB olmalıdır.' };
    }
    
    return { valid: true };
}

// Preview image before upload
function previewImage(input, previewElement) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        
        reader.onload = function(e) {
            $(previewElement).attr('src', e.target.result);
        };
        
        reader.readAsDataURL(input.files[0]);
    }
}

// Format date for display
function formatDate(dateString) {
    var date = new Date(dateString);
    return date.toLocaleDateString('tr-TR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    });
}

// Format time for display
function formatTime(timeString) {
    var time = new Date('2000-01-01T' + timeString);
    return time.toLocaleTimeString('tr-TR', {
        hour: '2-digit',
        minute: '2-digit'
    });
}

// Get attendance status badge
function getAttendanceStatusBadge(status) {
    var badges = {
        'Present': '<span class="badge badge-present">Katıldı</span>',
        'Absent': '<span class="badge badge-absent">Katılmadı</span>',
        'Pending': '<span class="badge badge-pending">Bekliyor</span>'
    };
    
    return badges[status] || '<span class="badge bg-secondary">Bilinmiyor</span>';
}

// Get classroom type badge
function getClassroomTypeBadge(type) {
    var badges = {
        'Science': '<span class="badge bg-primary">Sayısal</span>',
        'Literature': '<span class="badge bg-success">Sözel</span>',
        'EqualWeight': '<span class="badge bg-warning">Eşit Ağırlık</span>',
        'Language': '<span class="badge bg-info">Dil</span>'
    };
    
    return badges[type] || '<span class="badge bg-secondary">Belirtilmemiş</span>';
}

// Generate unique ID
function generateId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
}

// Debounce function for search inputs
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Initialize tooltips
function initializeTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Initialize popovers
function initializePopovers() {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
}

// Export functions for global use
window.showAlert = showAlert;
window.showLoading = showLoading;
window.hideLoading = hideLoading;
window.handleAjaxError = handleAjaxError;
window.handleAjaxSuccess = handleAjaxSuccess;
window.validateFileUpload = validateFileUpload;
window.previewImage = previewImage;
window.formatDate = formatDate;
window.formatTime = formatTime;
window.getAttendanceStatusBadge = getAttendanceStatusBadge;
window.getClassroomTypeBadge = getClassroomTypeBadge;
window.generateId = generateId;
window.debounce = debounce;
window.initializeTooltips = initializeTooltips;
window.initializePopovers = initializePopovers;
