(() => {
    let completedChartInstance = null;
    let categoryChartInstance = null;
    let priorityChartInstance = null;

    function isDarkMode() {
        const theme = document.documentElement.getAttribute('data-bs-theme') || 
                      document.documentElement.getAttribute('data-theme');
        if (theme) return theme === 'dark';
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    function getThemeColors() {
        const dark = isDarkMode();
        return {
            dark,
            textColor: dark ? '#94a3b8' : '#64748b',
            titleColor: dark ? '#f8fafc' : '#0f172a',
            gridColor: dark ? 'rgba(255, 255, 255, 0.06)' : '#f1f5f9',
            tooltipBg: dark ? '#1e293b' : '#0f172a',
            doughnutBorder: dark ? '#131b2e' : '#ffffff'
        };
    }

    function formatDate(d) {
        const dt = new Date(d);
        const yyyy = dt.getFullYear();
        const mm = String(dt.getMonth() + 1).padStart(2, '0');
        const dd = String(dt.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    }

    function buildCompletedChart(ctx, data) {
        const theme = getThemeColors();
        const labels = data.map(x => x.date);
        const counts = data.map(x => x.count);
        
        const gradient = ctx.createLinearGradient(0, 0, 0, 240);
        gradient.addColorStop(0, theme.dark ? 'rgba(52, 211, 153, 0.3)' : 'rgba(16, 185, 129, 0.35)');
        gradient.addColorStop(1, 'rgba(16, 185, 129, 0.01)');

        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Completed Tasks',
                    data: counts,
                    borderColor: theme.dark ? '#34d399' : '#10b981',
                    borderWidth: 3,
                    backgroundColor: gradient,
                    tension: 0.35,
                    fill: true,
                    pointBackgroundColor: theme.dark ? '#131b2e' : '#ffffff',
                    pointBorderColor: theme.dark ? '#34d399' : '#10b981',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: theme.tooltipBg,
                        titleColor: '#ffffff',
                        bodyColor: '#cbd5e1',
                        padding: 10,
                        cornerRadius: 8,
                        titleFont: { family: 'Plus Jakarta Sans', size: 12, weight: '600' },
                        bodyFont: { family: 'Plus Jakarta Sans', size: 12 }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Plus Jakarta Sans', size: 11 }, color: theme.textColor }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: theme.gridColor },
                        ticks: { precision: 0, font: { family: 'Plus Jakarta Sans', size: 11 }, color: theme.textColor }
                    }
                }
            }
        });
    }

    function buildCategoryChart(ctx, data) {
        const theme = getThemeColors();
        const labels = data.map(x => x.category || 'Uncategorized');
        const counts = data.map(x => x.count);
        const colors = [
            '#6366f1', '#38bdf8', '#34d399', '#fbbf24', '#ec4899', '#a855f7', '#06b6d4', '#14b8a6'
        ];

        return new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data: counts,
                    backgroundColor: colors.slice(0, labels.length),
                    borderWidth: 2,
                    borderColor: theme.doughnutBorder,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { 
                            font: { family: 'Plus Jakarta Sans', size: 12 }, 
                            color: theme.textColor,
                            padding: 16 
                        }
                    },
                    tooltip: {
                        backgroundColor: theme.tooltipBg,
                        titleColor: '#ffffff',
                        bodyColor: '#cbd5e1',
                        padding: 10,
                        cornerRadius: 8,
                        titleFont: { family: 'Plus Jakarta Sans', size: 12 },
                        bodyFont: { family: 'Plus Jakarta Sans', size: 12 }
                    }
                },
                cutout: '65%'
            }
        });
    }

    function buildPriorityChart(ctx, data) {
        const theme = getThemeColors();
        const labels = data.map(x => x.priority);
        const counts = data.map(x => x.count);
        
        const colorMap = {
            'Low': theme.dark ? '#64748b' : '#94a3b8',
            'Medium': theme.dark ? '#38bdf8' : '#0ea5e9',
            'High': theme.dark ? '#fbbf24' : '#f59e0b',
            'Critical': theme.dark ? '#fb7185' : '#f43f5e'
        };

        const bgColors = labels.map(l => colorMap[l] || '#6366f1');

        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: 'Tasks',
                    data: counts,
                    backgroundColor: bgColors,
                    borderRadius: 8,
                    maxBarThickness: 45
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: theme.tooltipBg,
                        titleColor: '#ffffff',
                        bodyColor: '#cbd5e1',
                        padding: 10,
                        cornerRadius: 8,
                        titleFont: { family: 'Plus Jakarta Sans', size: 12 },
                        bodyFont: { family: 'Plus Jakarta Sans', size: 12 }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Plus Jakarta Sans', size: 11, weight: '600' }, color: theme.textColor }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: theme.gridColor },
                        ticks: { precision: 0, font: { family: 'Plus Jakarta Sans', size: 11 }, color: theme.textColor }
                    }
                }
            }
        });
    }

    function renderAllCharts() {
        const completedEl = document.getElementById('completedChart');
        const categoryEl = document.getElementById('categoryChart');
        const priorityEl = document.getElementById('priorityChart');

        if (completedChartInstance) {
            completedChartInstance.destroy();
            completedChartInstance = null;
        }
        if (categoryChartInstance) {
            categoryChartInstance.destroy();
            categoryChartInstance = null;
        }
        if (priorityChartInstance) {
            priorityChartInstance.destroy();
            priorityChartInstance = null;
        }

        if (completedEl && typeof completedPerDayData !== 'undefined') {
            completedChartInstance = buildCompletedChart(completedEl.getContext('2d'), completedPerDayData);
        }

        if (categoryEl && typeof tasksByCategoryData !== 'undefined' && tasksByCategoryData.length > 0) {
            categoryChartInstance = buildCategoryChart(categoryEl.getContext('2d'), tasksByCategoryData);
        }

        if (priorityEl && typeof tasksByPriorityData !== 'undefined' && tasksByPriorityData.length > 0) {
            priorityChartInstance = buildPriorityChart(priorityEl.getContext('2d'), tasksByPriorityData);
        }
    }

    function init() {
        renderAllCharts();

        // Listen for real-time theme switch events
        document.addEventListener('themeChanged', () => {
            renderAllCharts();
        });

        // Preset buttons
        document.querySelectorAll('[data-preset]').forEach(btn => {
            btn.addEventListener('click', () => {
                const preset = btn.getAttribute('data-preset');
                const startInput = document.getElementById('startDate');
                const endInput = document.getElementById('endDate');
                const today = new Date();
                let start, end = today;
                if (preset === 'today') {
                    start = today;
                    end = today;
                } else if (preset === 'week') {
                    const day = today.getDay();
                    const diff = today.getDate() - day + (day === 0 ? -6 : 1);
                    start = new Date(new Date().setDate(diff));
                    end = new Date();
                } else if (preset === 'month') {
                    start = new Date(today.getFullYear(), today.getMonth(), 1);
                    end = today;
                } else if (preset === 'year') {
                    start = new Date(today.getFullYear(), 0, 1);
                    end = today;
                }
                if (startInput && endInput && start) {
                    startInput.value = formatDate(start);
                    endInput.value = formatDate(end);
                    const form = document.getElementById('statsForm');
                    form.submit();
                }
            });
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();


