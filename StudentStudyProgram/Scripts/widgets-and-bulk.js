/**
 * Dashboard Widgets and Bulk Operations System
 * Provides reusable components for modern dashboard interfaces
 */

(function() {
    'use strict';

    // ============= DASHBOARD WIDGETS =============

    /**
     * Create a dashboard widget
     * @param {object} config - Widget configuration
     */
    window.createWidget = function(config) {
        const {
            id,
            title,
            value,
            icon,
            type = 'primary',
            trend = null,
            trendValue = null,
            label = null,
            onClick = null
        } = config;

        const widgetHTML = `
            <div class="widget widget-${type}" id="${id}">
                <div class="widget-header">
                    <h3 class="widget-title">${title}</h3>
                    <div class="widget-icon">
                        <i class="${icon}"></i>
                    </div>
                </div>
                <div class="widget-body">
                    <p class="widget-value">${value}</p>
                </div>
                <div class="widget-footer">
                    ${trend ? `
                        <span class="widget-trend ${trend}">
                            <i class="fas fa-arrow-${trend === 'up' ? 'up' : 'down'}"></i>
                            ${trendValue}
                        </span>
                    ` : ''}
                    ${label ? `<span class="widget-label">${label}</span>` : ''}
                </div>
            </div>
        `;

        return widgetHTML;
    };

    /**
     * Update widget value
     */
    window.updateWidget = function(id, value, trend = null, trendValue = null) {
        const widget = document.getElementById(id);
        if (!widget) return;

        const valueEl = widget.querySelector('.widget-value');
        if (valueEl) {
            valueEl.textContent = value;
            valueEl.classList.add('widget-skeleton');
            setTimeout(() => valueEl.classList.remove('widget-skeleton'), 300);
        }

        if (trend && trendValue) {
            const footer = widget.querySelector('.widget-footer');
            const trendEl = footer.querySelector('.widget-trend');
            if (trendEl) {
                trendEl.className = `widget-trend ${trend}`;
                trendEl.innerHTML = `
                    <i class="fas fa-arrow-${trend === 'up' ? 'up' : 'down'}"></i>
                    ${trendValue}
                `;
            }
        }
    };

    // ============= BULK OPERATIONS =============

    class BulkOperationsManager {
        constructor(tableId, options = {}) {
            this.tableId = tableId;
            this.table = document.getElementById(tableId);
            if (!this.table) {
                console.error(`Table with id "${tableId}" not found`);
                return;
            }

            this.options = {
                onSelectionChange: options.onSelectionChange || null,
                checkboxClass: 'row-checkbox',
                selectAllClass: 'select-all-checkbox',
                ...options
            };

            this.selectedItems = new Set();
            this.init();
        }

        init() {
            this.createBulkActionsBar();
            this.addCheckboxes();
            this.attachEventListeners();
        }

        createBulkActionsBar() {
            if (document.getElementById('bulkActionsBar')) return;

            const bar = document.createElement('div');
            bar.id = 'bulkActionsBar';
            bar.className = 'bulk-actions-bar';
            bar.innerHTML = `
                <div class="bulk-actions-info">
                    <div class="bulk-selection-count">0</div>
                    <div class="bulk-selection-text">öğe seçildi</div>
                </div>
                <div class="bulk-actions-buttons">
                    <button class="bulk-action-btn secondary" onclick="bulkManager.clearSelection()">
                        <i class="fas fa-times"></i> Temizle
                    </button>
                    <button class="bulk-action-btn primary" onclick="bulkManager.exportSelected()">
                        <i class="fas fa-download"></i> Dışa Aktar
                    </button>
                    <button class="bulk-action-btn danger" onclick="bulkManager.deleteSelected()">
                        <i class="fas fa-trash"></i> Sil
                    </button>
                </div>
            `;
            document.body.appendChild(bar);
            this.bulkBar = bar;
        }

        addCheckboxes() {
            const thead = this.table.querySelector('thead tr');
            const tbody = this.table.querySelector('tbody');
            
            if (!thead || !tbody) return;

            // Add select-all checkbox to header
            const th = document.createElement('th');
            th.className = 'checkbox-cell';
            th.innerHTML = `
                <label class="custom-checkbox">
                    <input type="checkbox" class="${this.options.selectAllClass}">
                    <span class="checkbox-mark"></span>
                </label>
            `;
            thead.insertBefore(th, thead.firstChild);

            // Add checkboxes to each row
            const rows = tbody.querySelectorAll('tr');
            rows.forEach((row, index) => {
                const td = document.createElement('td');
                td.className = 'checkbox-cell';
                td.innerHTML = `
                    <label class="custom-checkbox">
                        <input type="checkbox" class="${this.options.checkboxClass}" data-row-index="${index}">
                        <span class="checkbox-mark"></span>
                    </label>
                `;
                row.insertBefore(td, row.firstChild);
            });
        }

        attachEventListeners() {
            // Select all checkbox
            const selectAllCheckbox = this.table.querySelector(`.${this.options.selectAllClass}`);
            if (selectAllCheckbox) {
                selectAllCheckbox.addEventListener('change', (e) => {
                    this.handleSelectAll(e.target.checked);
                });
            }

            // Individual checkboxes
            const checkboxes = this.table.querySelectorAll(`.${this.options.checkboxClass}`);
            checkboxes.forEach(checkbox => {
                checkbox.addEventListener('change', (e) => {
                    this.handleCheckboxChange(e.target);
                });
            });
        }

        handleSelectAll(checked) {
            const checkboxes = this.table.querySelectorAll(`.${this.options.checkboxClass}`);
            checkboxes.forEach(checkbox => {
                checkbox.checked = checked;
                this.updateRowSelection(checkbox);
            });
            this.updateSelection();
        }

        handleCheckboxChange(checkbox) {
            this.updateRowSelection(checkbox);
            this.updateSelection();
            this.updateSelectAllState();
        }

        updateRowSelection(checkbox) {
            const row = checkbox.closest('tr');
            if (checkbox.checked) {
                row.classList.add('selected');
                this.selectedItems.add(row);
            } else {
                row.classList.remove('selected');
                this.selectedItems.delete(row);
            }
        }

        updateSelection() {
            const count = this.selectedItems.size;
            const countEl = document.querySelector('.bulk-selection-count');
            const textEl = document.querySelector('.bulk-selection-text');
            
            if (countEl) countEl.textContent = count;
            if (textEl) textEl.textContent = `öğe seçildi`;

            if (count > 0) {
                this.bulkBar.classList.add('show');
            } else {
                this.bulkBar.classList.remove('show');
            }

            if (this.options.onSelectionChange) {
                this.options.onSelectionChange(Array.from(this.selectedItems));
            }
        }

        updateSelectAllState() {
            const selectAllCheckbox = this.table.querySelector(`.${this.options.selectAllClass}`);
            if (!selectAllCheckbox) return;

            const checkboxes = this.table.querySelectorAll(`.${this.options.checkboxClass}`);
            const checkedCount = Array.from(checkboxes).filter(cb => cb.checked).length;

            if (checkedCount === 0) {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = false;
            } else if (checkedCount === checkboxes.length) {
                selectAllCheckbox.checked = true;
                selectAllCheckbox.indeterminate = false;
            } else {
                selectAllCheckbox.checked = false;
                selectAllCheckbox.indeterminate = true;
            }
        }

        clearSelection() {
            const checkboxes = this.table.querySelectorAll(`.${this.options.checkboxClass}`);
            checkboxes.forEach(checkbox => {
                checkbox.checked = false;
                this.updateRowSelection(checkbox);
            });
            this.selectedItems.clear();
            this.updateSelection();
            this.updateSelectAllState();
        }

        getSelectedItems() {
            return Array.from(this.selectedItems);
        }

        getSelectedData(dataAttribute = 'id') {
            return this.getSelectedItems().map(row => {
                return row.getAttribute(`data-${dataAttribute}`) || row.cells[1]?.textContent;
            });
        }

        exportSelected() {
            const selectedData = this.getSelectedData();
            console.log('Exporting items:', selectedData);
            
            if (window.toast) {
                window.toast.info(`${selectedData.length} öğe dışa aktarılıyor...`, {
                    duration: 3000
                });
            }

            // Implement your export logic here
            // This is just a placeholder
            setTimeout(() => {
                if (window.toast) {
                    window.toast.success('Dışa aktarma tamamlandı!');
                }
            }, 1500);
        }

        deleteSelected() {
            const selectedData = this.getSelectedData();
            const count = selectedData.length;
            
            if (count === 0) return;

            if (confirm(`${count} öğeyi silmek istediğinizden emin misiniz?`)) {
                console.log('Deleting items:', selectedData);
                
                if (window.toast) {
                    window.toast.warning(`${count} öğe siliniyor...`);
                }

                // Implement your delete logic here
                // This is just a placeholder
                setTimeout(() => {
                    this.getSelectedItems().forEach(row => {
                        row.remove();
                    });
                    this.selectedItems.clear();
                    this.updateSelection();
                    
                    if (window.toast) {
                        window.toast.success(`${count} öğe silindi!`);
                    }
                }, 1000);
            }
        }
    }

    // Export to window
    window.BulkOperationsManager = BulkOperationsManager;

    /**
     * Initialize bulk operations on a table
     * @param {string} tableId - ID of the table
     * @param {object} options - Configuration options
     */
    window.initBulkOperations = function(tableId, options = {}) {
        return new BulkOperationsManager(tableId, options);
    };

    // Global instance for easy access
    window.bulkManager = null;

})();

// ============= USAGE EXAMPLES =============

/*
// Example 1: Create widgets
const widgetsContainer = document.getElementById('widgetsContainer');
widgetsContainer.innerHTML = `
    <div class="dashboard-widgets">
        ${createWidget({
            id: 'totalStudents',
            title: 'Toplam Öğrenci',
            value: '125',
            icon: 'fas fa-user-graduate',
            type: 'primary',
            trend: 'up',
            trendValue: '+12%',
            label: 'Geçen aya göre'
        })}
        ${createWidget({
            id: 'totalTeachers',
            title: 'Toplam Öğretmen',
            value: '24',
            icon: 'fas fa-chalkboard-teacher',
            type: 'success',
            trend: 'up',
            trendValue: '+5%',
            label: 'Geçen aya göre'
        })}
    </div>
`;

// Example 2: Update widget
updateWidget('totalStudents', '130', 'up', '+15%');

// Example 3: Initialize bulk operations
bulkManager = initBulkOperations('myTable', {
    onSelectionChange: (selectedRows) => {
        console.log('Selected rows:', selectedRows.length);
    }
});

// Example 4: Get selected items
const selectedData = bulkManager.getSelectedData('id');
console.log('Selected IDs:', selectedData);
*/
