// Admin Dashboard JavaScript
(function () {
    'use strict';

    // Global state
    let realTimeEnabled = false;
    let realTimeInterval = null;
    let charts = {};

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        console.log('Admin Dashboard initializing...');
        initializeDashboard();
    });

    function initializeDashboard() {
        try {
            loadDashboardData();
            initializeCharts();
            setupEventListeners();
            console.log('Dashboard initialized successfully');
        } catch (error) {
            console.error('Error initializing dashboard:', error);
        }
    }

    // Toggle real-time updates
    function toggleRealTimeUpdates() {
        realTimeEnabled = !realTimeEnabled;
        const btn = document.getElementById('realTimeToggle');
        const text = document.getElementById('realTimeText');

        if (realTimeEnabled) {
            btn.classList.remove('btn-outline-primary');
            btn.classList.add('btn-primary');
            text.textContent = 'Canlı: Açık';

            // Start real-time updates every 10 seconds (for testing, can increase later)
            realTimeInterval = setInterval(function () {
                console.log('Real-time update triggered at ' + new Date().toLocaleTimeString());
                // Flash cards to show update is happening
                flashUpdateIndicator();
                loadDashboardData();
                loadChartData();
            }, 10000);

            console.log('Real-time updates enabled - will refresh every 10 seconds');
        } else {
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-outline-primary');
            text.textContent = 'Canlı';

            // Stop real-time updates
            if (realTimeInterval) {
                clearInterval(realTimeInterval);
                realTimeInterval = null;
            }

            console.log('Real-time updates disabled');
        }
    }

    // Make toggleRealTimeUpdates globally accessible
    window.toggleRealTimeUpdates = toggleRealTimeUpdates;

    function setupEventListeners() {
        // Note: realTimeToggle uses onclick in HTML, so no addEventListener needed here

        // Export buttons
        const excelBtn = document.querySelector('[onclick*="exportToExcel"]');
        if (excelBtn) {
            excelBtn.onclick = exportToExcel;
        }

        const pdfBtn = document.querySelector('[onclick*="showReportModal"]');
        if (pdfBtn) {
            pdfBtn.onclick = showReportModal;
        }
    }

    // Flash all stat cards to show update is happening
    function flashUpdateIndicator() {
        const cards = document.querySelectorAll('.dash-stats .card');
        cards.forEach(card => {
            card.classList.add('updating-flash');
            setTimeout(() => card.classList.remove('updating-flash'), 800);
        });
    }

    function loadDashboardData() {
        loadStatistics();
        loadRecentSessions();
        loadWeeklySummary();
    }

    // Animate number update with count-up effect
    function animateNumber(element, newValue, duration = 500) {
        const $el = $(element);
        const oldValue = parseInt($el.text()) || 0;
        const diff = newValue - oldValue;

        if (diff === 0) return; // No change, skip animation

        // Add pulse animation class
        $el.addClass('pulse-update');

        // Animate the number
        $({ value: oldValue }).animate({ value: newValue }, {
            duration: duration,
            easing: 'swing',
            step: function () {
                $el.text(Math.round(this.value));
            },
            complete: function () {
                $el.text(newValue);
                // Remove pulse class after animation
                setTimeout(() => $el.removeClass('pulse-update'), 300);
            }
        });
    }

    function loadStatistics() {
        // Load total students
        $.get('/Admin/GetStudents', function (data) {
            if (data.success && data.students) {
                animateNumber('#totalStudents', data.students.length);
            }
        }).fail(function () {
            $('#totalStudents').text('0');
        });

        // Load total teachers
        $.get('/Admin/GetTeachers', function (data) {
            if (data.success && data.teachers) {
                animateNumber('#totalTeachers', data.teachers.length);
            }
        }).fail(function () {
            $('#totalTeachers').text('0');
        });

        // Load today's sessions
        $.get('/Admin/GetTodaySessions', function (data) {
            if (data.success) {
                animateNumber('#todaySessions', data.count || 0);
            }
        }).fail(function () {
            $('#todaySessions').text('0');
        });

        // Load total classrooms
        $.get('/Admin/GetClassrooms', function (data) {
            if (data.success && data.classrooms) {
                animateNumber('#totalClassrooms', data.classrooms.length);
            }
        }).fail(function () {
            $('#totalClassrooms').text('0');
        });
    }

    function loadRecentSessions() {
        console.log('Loading recent sessions...');
        $.ajax({
            url: '/Admin/GetRecentSessions',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                console.log('GetRecentSessions response:', data);
                const container = $('#recentSessions');

                if (!data.success) {
                    console.error('GetRecentSessions failed:', data.message);
                    container.html('<p class="text-warning text-center py-4">Veri yüklenemedi: ' + (data.message || 'Bilinmeyen hata') + '</p>');
                    return;
                }

                if (!data.sessions || data.sessions.length === 0) {
                    container.html('<p class="text-muted text-center py-4">Henüz etüt kaydı yok</p>');
                    return;
                }

                let html = '<div class="list-group">';
                data.sessions.slice(0, 5).forEach(function (session) {
                    html += `
                        <div class="list-group-item melody-hover mb-2">
                            <div class="d-flex w-100 justify-content-between">
                                <h6 class="mb-1">${session.studentName || 'N/A'}</h6>
                                <small class="text-muted">${session.date || 'N/A'}</small>
                            </div>
                            <p class="mb-1">Öğretmen: ${session.teacherName || 'N/A'}</p>
                            <small class="text-muted">Derslik: ${session.classroomName || 'N/A'}</small>
                        </div>
                    `;
                });
                html += '</div>';
                container.html(html);
            },
            error: function (xhr, status, error) {
                console.error('GetRecentSessions AJAX error:', status, error);
                console.error('Response:', xhr.responseText);
                $('#recentSessions').html('<p class="text-danger text-center py-4">Veriler yüklenirken hata oluştu</p>');
            }
        });
    }

    function loadWeeklySummary() {
        console.log('Loading weekly summary...');
        $.ajax({
            url: '/Admin/GetWeeklySummary',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                console.log('GetWeeklySummary response:', data);
                if (data.success) {
                    $('#thisWeekSessions').text(data.thisWeek || 0);
                    $('#thisMonthSessions').text(data.thisMonth || 0);
                    $('#totalSessions').text(data.total || 0);
                    $('#averageAttendanceRate').text((data.attendanceRate || 0) + '%');
                    $('#popularClassroom').text(data.popularClassroom || '-');
                    $('#activeTeacher').text(data.activeTeacher || '-');
                    $('#averageDuration').text(data.averageDuration || '45 dk');
                } else {
                    console.error('GetWeeklySummary failed:', data.message);
                    // Set default values on failure
                    setDefaultSummaryValues();
                }
            },
            error: function (xhr, status, error) {
                console.error('GetWeeklySummary AJAX error:', status, error);
                console.error('Response:', xhr.responseText);
                // Set default values on error
                setDefaultSummaryValues();
            }
        });
    }

    function setDefaultSummaryValues() {
        $('#thisWeekSessions').text('0');
        $('#thisMonthSessions').text('0');
        $('#totalSessions').text('0');
        $('#averageAttendanceRate').text('0%');
        $('#popularClassroom').text('-');
        $('#activeTeacher').text('-');
        $('#averageDuration').text('45 dk');
    }

    function initializeCharts() {
        console.log('Initializing charts...');

        // Weekly pie chart
        const weeklyCtx = document.getElementById('weeklyChart');
        if (weeklyCtx) {
            charts.weekly = new Chart(weeklyCtx, {
                type: 'doughnut',
                data: {
                    labels: ['Bu Hafta', 'Geçen Hafta', 'Önceki Hafta'],
                    datasets: [{
                        data: [1, 1, 1], // Placeholder data to show chart
                        backgroundColor: ['#3b82f6', '#22c55e', '#f59e0b']
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    }
                }
            });
            console.log('Weekly chart created');
        } else {
            console.warn('weeklyChart element not found');
        }

        // Weekly activity chart
        const activityCtx = document.getElementById('weeklyActivityChart');
        if (activityCtx) {
            charts.activity = new Chart(activityCtx, {
                type: 'line',
                data: {
                    labels: ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'],
                    datasets: [{
                        label: 'Etüt Sayısı',
                        data: [2, 3, 1, 4, 2, 0, 0], // Placeholder data
                        borderColor: '#3b82f6',
                        backgroundColor: 'rgba(59, 130, 246, 0.1)',
                        fill: true,
                        tension: 0.4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                stepSize: 1
                            }
                        }
                    }
                }
            });
            console.log('Activity chart created');
        } else {
            console.warn('weeklyActivityChart element not found');
        }

        // Load real chart data
        loadChartData();
    }

    function loadChartData() {
        $.ajax({
            url: '/Admin/GetChartData',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                console.log('Chart data loaded:', data);
                if (data.success) {
                    // Update weekly doughnut chart
                    if (charts.weekly && data.weeklyData) {
                        charts.weekly.data.datasets[0].data = [
                            data.weeklyData.thisWeek || 1,
                            data.weeklyData.lastWeek || 1,
                            data.weeklyData.twoWeeksAgo || 1
                        ];
                        charts.weekly.update();
                    }

                    // Update activity line chart
                    if (charts.activity && data.dailyData) {
                        charts.activity.data.datasets[0].data = data.dailyData;
                        charts.activity.update();
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Failed to load chart data:', error);
            }
        });
    }

    // Global functions (exposed to window for onclick handlers)
    window.toggleRealTimeUpdates = function () {
        realTimeEnabled = !realTimeEnabled;
        const btn = document.getElementById('realTimeToggle');
        const text = document.getElementById('realTimeText');

        if (realTimeEnabled) {
            btn.classList.add('active');
            text.textContent = 'Durdur';
            realTimeInterval = setInterval(loadDashboardData, 30000); // Update every 30 seconds
        } else {
            btn.classList.remove('active');
            text.textContent = 'Canlı';
            if (realTimeInterval) {
                clearInterval(realTimeInterval);
                realTimeInterval = null;
            }
        }
    };

    window.exportToExcel = function () {
        window.location.href = '/Admin/ExportToExcel';
    };

    window.showReportModal = function () {
        const modal = document.getElementById('reportModal');
        if (modal) {
            // Set default dates
            const today = new Date().toISOString().split('T')[0];
            const lastMonth = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

            document.getElementById('reportStartDate').value = lastMonth;
            document.getElementById('reportEndDate').value = today;

            // Show modal (Bootstrap 5)
            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();
        }
    };

    window.generateReport = function () {
        const startDate = document.getElementById('reportStartDate').value;
        const endDate = document.getElementById('reportEndDate').value;

        if (!startDate || !endDate) {
            alert('Lütfen başlangıç ve bitiş tarihlerini seçin!');
            return false;
        }

        window.location.href = `/Admin/GeneratePDFReport?startDate=${startDate}&endDate=${endDate}`;

        // Close modal
        const modal = document.getElementById('reportModal');
        if (modal) {
            const bsModal = bootstrap.Modal.getInstance(modal);
            if (bsModal) {
                bsModal.hide();
            }
        }

        return false;
    };

    window.updateChartView = function (view) {
        console.log('Updating chart view to:', view);
        // TODO: Implement chart view update based on selection
    };

})();
