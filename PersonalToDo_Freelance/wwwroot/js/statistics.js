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
        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{ label: 'Completed', data: counts, borderColor: 'rgba(75,192,192,1)', backgroundColor: 'rgba(75,192,192,0.2)', tension: 0.2, fill: true }]
            },
            options: { responsive: true, maintainAspectRatio: false }
        });
    }

    function buildBarChart(ctx, labels, counts, label) {
        return new Chart(ctx, {
            type: 'bar',
            data: { labels, datasets: [{ label, data: counts, backgroundColor: 'rgba(54,162,235,0.7)' }] },
            options: { responsive: true, maintainAspectRatio: false }
        });
    }

    function init() {
        const completedEl = document.getElementById('completedChart');
        const categoryEl = document.getElementById('categoryChart');
        const priorityEl = document.getElementById('priorityChart');

        if (completedEl && typeof completedPerDayData !== 'undefined') {
            // ensure canvas has height
            completedEl.style.height = '240px';
            buildCompletedChart(completedEl.getContext('2d'), completedPerDayData);
        }

        if (categoryEl && typeof tasksByCategoryData !== 'undefined') {
            categoryEl.style.height = '240px';
            const labels = tasksByCategoryData.map(x => x.category);
            const counts = tasksByCategoryData.map(x => x.count);
            buildBarChart(categoryEl.getContext('2d'), labels, counts, 'Tasks');
        }

        if (priorityEl && typeof tasksByPriorityData !== 'undefined') {
            priorityEl.style.height = '240px';
            const labels = tasksByPriorityData.map(x => x.priority);
            const counts = tasksByPriorityData.map(x => x.count);
            buildBarChart(priorityEl.getContext('2d'), labels, counts, 'Tasks');
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
                    start = new Date(today.setDate(diff));
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
