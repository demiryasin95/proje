/**
 * Skeleton Loader Utility
 * Modern loading states and skeleton screens for better UX
 */

const SkeletonLoader = {
    /**
     * Generate skeleton rows for tables
     * @param {number} rows - Number of skeleton rows
     * @param {number} cols - Number of columns
     * @param {string} type - Type of skeleton (student, teacher, timeslot, classroom)
     * @returns {string} HTML string of skeleton rows
     */
    generateTableSkeleton: function(rows = 5, cols = 5, type = 'default') {
        let html = '';
        
        for (let i = 0; i < rows; i++) {
            html += '<tr class="skeleton-row">';
            
            if (type === 'student' || type === 'teacher') {
                // Avatar column
                html += '<td><div class="skeleton skeleton-avatar"></div></td>';
                // Name column
                html += '<td><div class="skeleton skeleton-text" style="width: 70%;"></div></td>';
                // Other columns
                for (let j = 2; j < cols; j++) {
                    html += '<td><div class="skeleton skeleton-text" style="width: 60%;"></div></td>';
                }
            } else {
                // Default skeleton for other types
                for (let j = 0; j < cols; j++) {
                    const width = 50 + Math.random() * 30; // Random width between 50-80%
                    html += `<td><div class="skeleton skeleton-text" style="width: ${width}%;"></div></td>`;
                }
            }
            
            html += '</tr>';
        }
        
        return html;
    },

    /**
     * Generate detailed skeleton for specific types
     */
    generateStudentSkeleton: function(count = 5) {
        let html = '';
        for (let i = 0; i < count; i++) {
            html += `
                <tr class="skeleton-student-row">
                    <td><div class="skeleton skeleton-avatar"></div></td>
                    <td>
                        <div class="skeleton skeleton-text" style="width: 60%; margin-bottom: 4px;"></div>
                        <div class="skeleton skeleton-text skeleton-text-sm" style="width: 40%;"></div>
                    </td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 70%;"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 50%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td class="skeleton-actions">
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                    </td>
                </tr>
            `;
        }
        return html;
    },

    generateTeacherSkeleton: function(count = 5) {
        let html = '';
        for (let i = 0; i < count; i++) {
            html += `
                <tr class="skeleton-teacher-row">
                    <td><div class="skeleton skeleton-avatar"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 60%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 70%;"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 50%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td class="skeleton-actions">
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                    </td>
                </tr>
            `;
        }
        return html;
    },

    generateTimeSlotSkeleton: function(count = 5) {
        let html = '';
        for (let i = 0; i < count; i++) {
            html += `
                <tr class="skeleton-timeslot-row">
                    <td><div class="skeleton skeleton-text" style="width: 50%;"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 40%;"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 40%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td class="skeleton-actions">
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                    </td>
                </tr>
            `;
        }
        return html;
    },

    generateClassroomSkeleton: function(count = 5) {
        let html = '';
        for (let i = 0; i < count; i++) {
            html += `
                <tr class="skeleton-classroom-row">
                    <td><div class="skeleton skeleton-text" style="width: 50%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td><div class="skeleton skeleton-text" style="width: 30%;"></div></td>
                    <td><div class="skeleton skeleton-badge"></div></td>
                    <td class="skeleton-actions">
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                        <div class="skeleton skeleton-button" style="width: 32px; height: 32px;"></div>
                    </td>
                </tr>
            `;
        }
        return html;
    },

    /**
     * Show skeleton in a table
     * @param {string|jQuery} tableBodySelector - Table tbody selector or jQuery object
     * @param {string} type - Type of skeleton
     * @param {number} count - Number of rows
     */
    showTableSkeleton: function(tableBodySelector, type = 'default', count = 5) {
        const $tbody = $(tableBodySelector);
        let skeletonHTML = '';

        switch(type) {
            case 'student':
                skeletonHTML = this.generateStudentSkeleton(count);
                break;
            case 'teacher':
                skeletonHTML = this.generateTeacherSkeleton(count);
                break;
            case 'timeslot':
                skeletonHTML = this.generateTimeSlotSkeleton(count);
                break;
            case 'classroom':
                skeletonHTML = this.generateClassroomSkeleton(count);
                break;
            default:
                skeletonHTML = this.generateTableSkeleton(count, 5);
        }

        $tbody.html(skeletonHTML);
    },

    /**
     * Hide skeleton and show real content with fade-in animation
     * @param {string|jQuery} tableBodySelector - Table tbody selector or jQuery object
     * @param {string} content - Real content HTML
     */
    hideTableSkeleton: function(tableBodySelector, content) {
        const $tbody = $(tableBodySelector);
        $tbody.fadeOut(200, function() {
            $tbody.html(content).addClass('fade-in').fadeIn(300);
            setTimeout(() => {
                $tbody.removeClass('fade-in');
            }, 300);
        });
    },

    /**
     * Show loading overlay on an element
     * @param {string|jQuery} selector - Element selector or jQuery object
     * @param {string} message - Loading message
     */
    showLoadingOverlay: function(selector, message = 'Yükleniyor...') {
        const $element = $(selector);
        const overlayHTML = `
            <div class="loading-container-overlay">
                <div class="text-center">
                    <div class="spinner-loader mb-3"></div>
                    <div class="text-muted">${message}</div>
                </div>
            </div>
        `;
        
        $element.css('position', 'relative').append(overlayHTML);
    },

    /**
     * Hide loading overlay
     * @param {string|jQuery} selector - Element selector or jQuery object
     */
    hideLoadingOverlay: function(selector) {
        $(selector).find('.loading-container-overlay').fadeOut(200, function() {
            $(this).remove();
        });
    },

    /**
     * Show progress bar loader
     * @param {string|jQuery} selector - Container selector
     */
    showProgressLoader: function(selector) {
        const $element = $(selector);
        const progressHTML = `
            <div class="progress-loader">
                <div class="progress-loader-bar"></div>
            </div>
        `;
        $element.prepend(progressHTML);
    },

    /**
     * Hide progress bar loader
     * @param {string|jQuery} selector - Container selector
     */
    hideProgressLoader: function(selector) {
        $(selector).find('.progress-loader').fadeOut(200, function() {
            $(this).remove();
        });
    },

    /**
     * Show dots loader inline
     * @param {string|jQuery} selector - Element selector
     */
    showDotsLoader: function(selector) {
        const $element = $(selector);
        const dotsHTML = `
            <div class="dots-loader">
                <span></span>
                <span></span>
                <span></span>
            </div>
        `;
        $element.html(dotsHTML);
    },

    /**
     * Add loading state to a button
     * @param {string|jQuery} buttonSelector - Button selector
     * @param {string} loadingText - Text to show while loading
     */
    showButtonLoading: function(buttonSelector, loadingText = 'İşleniyor...') {
        const $button = $(buttonSelector);
        const originalText = $button.html();
        
        $button.data('original-text', originalText)
               .prop('disabled', true)
               .html(`<span class="spinner-border spinner-border-sm me-2"></span>${loadingText}`);
    },

    /**
     * Remove loading state from a button
     * @param {string|jQuery} buttonSelector - Button selector
     */
    hideButtonLoading: function(buttonSelector) {
        const $button = $(buttonSelector);
        const originalText = $button.data('original-text');
        
        if (originalText) {
            $button.prop('disabled', false).html(originalText);
        }
    },

    /**
     * Show card loading state
     * @param {string|jQuery} cardSelector - Card selector
     */
    showCardLoading: function(cardSelector) {
        $(cardSelector).addClass('card-loading');
    },

    /**
     * Hide card loading state
     * @param {string|jQuery} cardSelector - Card selector
     */
    hideCardLoading: function(cardSelector) {
        $(cardSelector).removeClass('card-loading');
    },

    /**
     * Enhanced AJAX wrapper with automatic loading states
     * @param {Object} options - AJAX options with additional loading config
     */
    ajaxWithLoading: function(options) {
        const defaults = {
            showTableSkeleton: false,
            tableSelector: null,
            tableType: 'default',
            showOverlay: false,
            overlaySelector: null,
            overlayMessage: 'Yükleniyor...',
            showProgress: false,
            progressSelector: null,
            showButtonLoading: false,
            buttonSelector: null,
            buttonLoadingText: 'İşleniyor...'
        };

        const config = $.extend({}, defaults, options);
        const self = this;

        // Show loading states
        if (config.showTableSkeleton && config.tableSelector) {
            this.showTableSkeleton(config.tableSelector, config.tableType);
        }
        if (config.showOverlay && config.overlaySelector) {
            this.showLoadingOverlay(config.overlaySelector, config.overlayMessage);
        }
        if (config.showProgress && config.progressSelector) {
            this.showProgressLoader(config.progressSelector);
        }
        if (config.showButtonLoading && config.buttonSelector) {
            this.showButtonLoading(config.buttonSelector, config.buttonLoadingText);
        }

        // Original callbacks
        const originalSuccess = config.success;
        const originalError = config.error;
        const originalComplete = config.complete;

        // Wrap success callback
        config.success = function(data, textStatus, jqXHR) {
            if (originalSuccess) {
                originalSuccess(data, textStatus, jqXHR);
            }
        };

        // Wrap error callback
        config.error = function(jqXHR, textStatus, errorThrown) {
            if (originalError) {
                originalError(jqXHR, textStatus, errorThrown);
            }
        };

        // Wrap complete callback
        config.complete = function(jqXHR, textStatus) {
            // Hide loading states
            if (config.showOverlay && config.overlaySelector) {
                self.hideLoadingOverlay(config.overlaySelector);
            }
            if (config.showProgress && config.progressSelector) {
                self.hideProgressLoader(config.progressSelector);
            }
            if (config.showButtonLoading && config.buttonSelector) {
                self.hideButtonLoading(config.buttonSelector);
            }

            if (originalComplete) {
                originalComplete(jqXHR, textStatus);
            }
        };

        return $.ajax(config);
    }
};

// Export to window
window.SkeletonLoader = SkeletonLoader;

// jQuery plugin wrapper
$.fn.showSkeleton = function(type = 'default', count = 5) {
    SkeletonLoader.showTableSkeleton(this, type, count);
    return this;
};

$.fn.hideSkeleton = function(content) {
    SkeletonLoader.hideTableSkeleton(this, content);
    return this;
};

$.fn.showLoadingOverlay = function(message) {
    SkeletonLoader.showLoadingOverlay(this, message);
    return this;
};

$.fn.hideLoadingOverlay = function() {
    SkeletonLoader.hideLoadingOverlay(this);
    return this;
};

$.fn.showButtonLoading = function(text) {
    SkeletonLoader.showButtonLoading(this, text);
    return this;
};

$.fn.hideButtonLoading = function() {
    SkeletonLoader.hideButtonLoading(this);
    return this;
};
