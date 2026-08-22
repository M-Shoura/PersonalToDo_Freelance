(() => {
    function formatDate(d) {
        const dt = new Date(d);
        const yyyy = dt.getFullYear();
        const mm = String(dt.getMonth() + 1).padStart(2, '0');
        const dd = String(dt.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    }

    function buildCompletedChart(ctx, data) {
        const labels = data.map(x => x.date);
        const counts = data.map(x => x.count);
        
        // Gradient fill
        const gradient = ctx.createLinearGradient(0, 0, 0, 240);
        gradient.addColorStop(0, 'rgba(16, 185, 129, 0.35)');
        gradient.addColorStop(1, 'rgba(16, 185, 129, 0.01)');

        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Completed Tasks',
                    data: counts,
                    borderColor: '#10b981',
                    borderWidth: 3,
                    backgroundColor: gradient,
                    tension: 0.35,
                    fill: true,
                    pointBackgroundColor: '#ffffff',
                    pointBorderColor: '#10b981',
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
                        backgroundColor: '#1e293b',
                        padding: 10,
                        cornerRadius: 8,
                        titleFont: { family: 'Plus Jakarta Sans', size: 12, weight: '600' },
                        bodyFont: { family: 'Plus Jakarta Sans', size: 12 }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Plus Jakarta Sans', size: 11 }, color: '#64748b' }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: '#f1f5f9' },
                        ticks: { precision: 0, font: { family: 'Plus Jakarta Sans', size: 11 }, color: '#64748b' }
                    }
                }
            }
        });
    }

    function buildCategoryChart(ctx, data) {
        const labels = data.map(x => x.category || 'Uncategorized');
        const counts = data.map(x => x.count);
        const colors = [
            '#4f46e5', '#0ea5e9', '#10b981', '#f59e0b', '#ec4899', '#8b5cf6', '#06b6d4', '#14b8a6'
        ];

        return new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data: counts,
                    backgroundColor: colors.slice(0, labels.length),
                    borderWidth: 2,
                    borderColor: '#ffffff',
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { font: { family: 'Plus Jakarta Sans', size: 12 }, padding: 16 }
                    },
                    tooltip: {
                        backgroundColor: '#1e293b',
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
        const labels = data.map(x => x.priority);
        const counts = data.map(x => x.count);
        
        const colorMap = {
            'Low': '#94a3b8',
            'Medium': '#0ea5e9',
            'High': '#f59e0b',
            'Critical': '#f43f5e'
        };

        const bgColors = labels.map(l => colorMap[l] || '#4f46e5');

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
                        backgroundColor: '#1e293b',
                        padding: 10,
                        cornerRadius: 8,
                        titleFont: { family: 'Plus Jakarta Sans', size: 12 },
                        bodyFont: { family: 'Plus Jakarta Sans', size: 12 }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Plus Jakarta Sans', size: 11, weight: '600' }, color: '#475569' }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: '#f1f5f9' },
                        ticks: { precision: 0, font: { family: 'Plus Jakarta Sans', size: 11 }, color: '#64748b' }
                    }
                }
            }
        });
    }

    function init() {
        const completedEl = document.getElementById('completedChart');
        const categoryEl = document.getElementById('categoryChart');
        const priorityEl = document.getElementById('priorityChart');

        if (completedEl && typeof completedPerDayData !== 'undefined') {
            buildCompletedChart(completedEl.getContext('2d'), completedPerDayData);
        }

        if (categoryEl && typeof tasksByCategoryData !== 'undefined' && tasksByCategoryData.length > 0) {
            buildCategoryChart(categoryEl.getContext('2d'), tasksByCategoryData);
        }

        if (priorityEl && typeof tasksByPriorityData !== 'undefined' && tasksByPriorityData.length > 0) {
            buildPriorityChart(priorityEl.getContext('2d'), tasksByPriorityData);
        }

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
                    const diff = today.getDate() - day + (day === 0 ? -6 : 1); // Monday as start
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

