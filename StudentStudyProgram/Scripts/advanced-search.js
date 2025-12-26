// Advanced Search and Filter System
(function() {
    'use strict';

    const AdvancedSearch = {
        STORAGE_KEY: 'recentSearches',
        MAX_RECENT: 5,
        tables: {},
        
        init: function() {
            this.enhanceTables();
            // Disabled: createSearchUI adds duplicate search bars
            // this.createSearchUI();
            this.attachListeners();
        },

        enhanceTables: function() {
            const tables = document.querySelectorAll('table.table');
            tables.forEach((table, index) => {
                if (!table.id) {
                    table.id = 'enhancedTable' + index;
                }
                this.tables[table.id] = {
                    element: table,
                    originalRows: Array.from(table.querySelectorAll('tbody tr')),
                    filters: {}
                };
            });
        },

        createSearchUI: function() {
            const tables = document.querySelectorAll('table.table');
            tables.forEach(table => {
                const container = table.closest('.card, .table-responsive') || table.parentElement;
                if (!container) return;

                // Check if search already exists
                if (container.querySelector('.advanced-search-bar')) return;

                const searchBar = document.createElement('div');
                searchBar.className = 'advanced-search-bar mb-3';
                searchBar.innerHTML = `
                    <div class="row g-2">
                        <div class="col-md-6">
                            <div class="input-group">
                                <span class="input-group-text"><i class="fas fa-search"></i></span>
                                <input type="text" 
                                       class="form-control instant-search" 
                                       placeholder="Ara... (Alt+S)" 
                                       data-table="${table.id}">
                                <button class="btn btn-outline-secondary dropdown-toggle" 
                                        type="button" 
                                        data-bs-toggle="dropdown">
                                    <i class="fas fa-filter"></i> Filtrele
                                </button>
                                <ul class="dropdown-menu dropdown-menu-end filter-dropdown">
                                    ${this.generateFilterOptions(table)}
                                </ul>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="d-flex gap-2 justify-content-end">
                                <button class="btn btn-outline-secondary btn-sm clear-filters" data-table="${table.id}">
                                    <i class="fas fa-times"></i> Filtreleri Temizle
                                </button>
                                <div class="dropdown">
                                    <button class="btn btn-outline-secondary btn-sm dropdown-toggle" 
                                            type="button" 
                                            data-bs-toggle="dropdown">
                                        <i class="fas fa-clock"></i> Son Aramalar
                                    </button>
                                    <ul class="dropdown-menu recent-searches-dropdown" data-table="${table.id}">
                                        ${this.generateRecentSearches()}
                                    </ul>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="active-filters mt-2" data-table="${table.id}"></div>
                `;

                container.insertBefore(searchBar, table);
            });
        },

        generateFilterOptions: function(table) {
            const headers = Array.from(table.querySelectorAll('thead th'));
            let html = '<li><h6 class="dropdown-header">Sütuna Göre Filtrele</h6></li>';
            
            headers.forEach((header, index) => {
                const headerText = header.textContent.trim();
                if (headerText && !headerText.includes('İşlem')) {
                    html += `
                        <li>
                            <a class="dropdown-item filter-column" href="#" data-table="${table.id}" data-column="${index}">
                                <i class="fas fa-filter me-2"></i>${headerText}
                            </a>
                        </li>
                    `;
                }
            });

            return html;
        },

        generateRecentSearches: function() {
            const recent = this.getRecentSearches();
            if (recent.length === 0) {
                return '<li><span class="dropdown-item text-muted">Son arama yok</span></li>';
            }

            let html = '';
            recent.forEach(search => {
                html += `
                    <li>
                        <a class="dropdown-item recent-search-item" href="#" data-search="${search}">
                            <i class="fas fa-history me-2"></i>${search}
                        </a>
                    </li>
                `;
            });
            html += '<li><hr class="dropdown-divider"></li>';
            html += '<li><a class="dropdown-item text-danger clear-recent" href="#"><i class="fas fa-trash me-2"></i>Geçmişi Temizle</a></li>';
            
            return html;
        },

        attachListeners: function() {
            // Instant search
            document.addEventListener('input', (e) => {
                if (e.target.matches('.instant-search')) {
                    const tableId = e.target.getAttribute('data-table');
                    const searchTerm = e.target.value.toLowerCase();
                    this.performSearch(tableId, searchTerm);
                    
                    // Save to recent searches
                    if (searchTerm.length > 2) {
                        this.saveRecentSearch(searchTerm);
                    }
                }
            });

            // Clear filters
            document.addEventListener('click', (e) => {
                if (e.target.closest('.clear-filters')) {
                    e.preventDefault();
                    const tableId = e.target.closest('.clear-filters').getAttribute('data-table');
                    this.clearFilters(tableId);
                }
            });

            // Column filter
            document.addEventListener('click', (e) => {
                if (e.target.closest('.filter-column')) {
                    e.preventDefault();
                    const link = e.target.closest('.filter-column');
                    const tableId = link.getAttribute('data-table');
                    const column = parseInt(link.getAttribute('data-column'));
                    this.showColumnFilterDialog(tableId, column);
                }
            });

            // Recent search click
            document.addEventListener('click', (e) => {
                if (e.target.closest('.recent-search-item')) {
                    e.preventDefault();
                    const search = e.target.closest('.recent-search-item').getAttribute('data-search');
                    const input = e.target.closest('.advanced-search-bar').querySelector('.instant-search');
                    input.value = search;
                    input.dispatchEvent(new Event('input'));
                }
            });

            // Clear recent searches
            document.addEventListener('click', (e) => {
                if (e.target.closest('.clear-recent')) {
                    e.preventDefault();
                    localStorage.removeItem(this.STORAGE_KEY);
                    this.updateRecentSearchesDropdowns();
                }
            });
        },

        performSearch: function(tableId, searchTerm) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            const rows = tableData.originalRows;
            let visibleCount = 0;

            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                const matches = text.includes(searchTerm);
                row.style.display = matches ? '' : 'none';
                if (matches) visibleCount++;
            });

            this.updateResultCount(tableId, visibleCount, rows.length);
        },

        showColumnFilterDialog: function(tableId, columnIndex) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            // Get unique values from column
            const values = new Set();
            tableData.originalRows.forEach(row => {
                const cell = row.querySelectorAll('td')[columnIndex];
                if (cell) {
                    values.add(cell.textContent.trim());
                }
            });

            // Create filter modal
            const modalId = 'columnFilterModal';
            let modal = document.getElementById(modalId);
            if (!modal) {
                modal = document.createElement('div');
                modal.id = modalId;
                modal.className = 'modal fade';
                document.body.appendChild(modal);
            }

            const header = tableData.element.querySelectorAll('thead th')[columnIndex].textContent;
            
            modal.innerHTML = `
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${header} - Filtre</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" id="selectAll" checked>
                                <label class="form-check-label fw-bold" for="selectAll">
                                    Tümünü Seç
                                </label>
                            </div>
                            <hr>
                            <div class="filter-options" style="max-height: 300px; overflow-y: auto;">
                                ${Array.from(values).sort().map(value => `
                                    <div class="form-check">
                                        <input class="form-check-input filter-value" type="checkbox" 
                                               value="${value}" id="filter_${value.replace(/\s/g, '_')}" checked>
                                        <label class="form-check-label" for="filter_${value.replace(/\s/g, '_')}">
                                            ${value}
                                        </label>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">İptal</button>
                            <button type="button" class="btn btn-primary apply-column-filter" 
                                    data-table="${tableId}" data-column="${columnIndex}">
                                Uygula
                            </button>
                        </div>
                    </div>
                </div>
            `;

            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();

            // Select all checkbox
            modal.querySelector('#selectAll').addEventListener('change', (e) => {
                modal.querySelectorAll('.filter-value').forEach(cb => {
                    cb.checked = e.target.checked;
                });
            });

            // Apply filter
            modal.querySelector('.apply-column-filter').addEventListener('click', () => {
                const selectedValues = Array.from(modal.querySelectorAll('.filter-value:checked'))
                    .map(cb => cb.value);
                this.applyColumnFilter(tableId, columnIndex, selectedValues);
                bsModal.hide();
            });
        },

        applyColumnFilter: function(tableId, columnIndex, selectedValues) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            tableData.filters[columnIndex] = selectedValues;
            this.applyAllFilters(tableId);
            this.updateActiveFilters(tableId);
        },

        applyAllFilters: function(tableId) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            let visibleCount = 0;
            tableData.originalRows.forEach(row => {
                let show = true;
                
                // Check all active filters
                for (const [column, values] of Object.entries(tableData.filters)) {
                    const cell = row.querySelectorAll('td')[column];
                    if (cell && !values.includes(cell.textContent.trim())) {
                        show = false;
                        break;
                    }
                }

                row.style.display = show ? '' : 'none';
                if (show) visibleCount++;
            });

            this.updateResultCount(tableId, visibleCount, tableData.originalRows.length);
        },

        updateActiveFilters: function(tableId) {
            const container = document.querySelector(`.active-filters[data-table="${tableId}"]`);
            if (!container) return;

            const tableData = this.tables[tableId];
            const headers = Array.from(tableData.element.querySelectorAll('thead th'));
            
            let html = '';
            for (const [column, values] of Object.entries(tableData.filters)) {
                const headerText = headers[column].textContent.trim();
                html += `
                    <span class="badge bg-primary me-2 mb-2">
                        ${headerText}: ${values.length} seçili
                        <button type="button" class="btn-close btn-close-white btn-sm ms-2" 
                                onclick="window.AdvancedSearch.removeFilter('${tableId}', ${column})"></button>
                    </span>
                `;
            }

            container.innerHTML = html;
            container.style.display = html ? 'block' : 'none';
        },

        removeFilter: function(tableId, column) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            delete tableData.filters[column];
            this.applyAllFilters(tableId);
            this.updateActiveFilters(tableId);
        },

        clearFilters: function(tableId) {
            const tableData = this.tables[tableId];
            if (!tableData) return;

            tableData.filters = {};
            tableData.originalRows.forEach(row => row.style.display = '');
            
            const searchInput = document.querySelector(`.instant-search[data-table="${tableId}"]`);
            if (searchInput) searchInput.value = '';
            
            this.updateActiveFilters(tableId);
            this.updateResultCount(tableId, tableData.originalRows.length, tableData.originalRows.length);
        },

        updateResultCount: function(tableId, visible, total) {
            const table = document.getElementById(tableId);
            if (!table) return;

            let countDiv = table.parentElement.querySelector('.search-result-count');
            if (!countDiv) {
                countDiv = document.createElement('div');
                countDiv.className = 'search-result-count text-muted small mt-2';
                table.parentElement.appendChild(countDiv);
            }

            if (visible === total) {
                countDiv.textContent = `${total} kayıt`;
            } else {
                countDiv.textContent = `${visible} / ${total} kayıt gösteriliyor`;
            }
        },

        saveRecentSearch: function(search) {
            const recent = this.getRecentSearches();
            const index = recent.indexOf(search);
            
            if (index > -1) {
                recent.splice(index, 1);
            }
            
            recent.unshift(search);
            recent.splice(this.MAX_RECENT);
            
            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(recent));
            this.updateRecentSearchesDropdowns();
        },

        getRecentSearches: function() {
            try {
                return JSON.parse(localStorage.getItem(this.STORAGE_KEY) || '[]');
            } catch {
                return [];
            }
        },

        updateRecentSearchesDropdowns: function() {
            document.querySelectorAll('.recent-searches-dropdown').forEach(dropdown => {
                dropdown.innerHTML = this.generateRecentSearches();
            });
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            // Delay init to ensure tables are rendered
            setTimeout(() => AdvancedSearch.init(), 500);
        });
    } else {
        setTimeout(() => AdvancedSearch.init(), 500);
    }

    // Export for global use
    window.AdvancedSearch = AdvancedSearch;

})();