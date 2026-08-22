(() => {
    function getAntiForgeryToken() {
        const form = document.getElementById('__antiForgeryForm');
        if (!form) return null;
        const input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    async function postChangeStatus(taskId, status) {
        const token = getAntiForgeryToken();
        const url = `/Task/ChangeStatus?id=${encodeURIComponent(taskId)}`;
        const body = new URLSearchParams();
        body.append('status', status);
        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'fetch',
                    'RequestVerificationToken': token
                },
                body: body,
                credentials: 'same-origin'
            });
            if (!res.ok) throw new Error('Network error');
            const json = await res.json();
            return json;
        } catch (e) {
            window.showToast(e.message || 'Network error', 'danger');
            return { succeeded: false, error: e.message };
        }
    }

    function updateRowStatus(taskId, statusText) {
        const items = document.querySelectorAll(`[data-task-id='${taskId}']`);
        items.forEach(el => {
            const statusEl = el.querySelector('.task-status');
            if (statusEl) {
                statusEl.textContent = statusText;
                // update badge classes if badge-status
                if (statusEl.classList.contains('badge-status')) {
                    statusEl.className = 'badge-status ' + (
                        statusText.toLowerCase() === 'completed' ? 'badge-status-completed' :
                        statusText.toLowerCase() === 'inprogress' ? 'badge-status-inprogress' :
                        statusText.toLowerCase() === 'cancelled' ? 'badge-status-cancelled' :
                        'badge-status-notstarted'
                    );
                }
            }

            const titleEl = el.querySelector('.task-title-text');
            if (statusText && statusText.toLowerCase().includes('completed')) {
                el.classList.remove('table-danger', 'border-danger');
                el.classList.add('table-success');
                if (titleEl) titleEl.classList.add('text-decoration-line-through', 'text-muted');
            } else {
                el.classList.remove('table-success');
                if (titleEl) titleEl.classList.remove('text-decoration-line-through', 'text-muted');
            }
        });
    }

    function handleActionClick(e) {
        const btn = e.target.closest('button[data-action]');
        if (!btn) return;
        const action = btn.getAttribute('data-action');
        const taskId = btn.getAttribute('data-task-id');
        if (!action || !taskId) return;
        e.preventDefault();
        if (action === 'complete') {
            postChangeStatus(taskId, '2').then(r => {
                if (r.succeeded) {
                    updateRowStatus(taskId, r.status || 'Completed');
                    window.showToast('Task marked as completed! 🎉', 'success');
                } else {
                    window.showToast(r.error || 'Failed to complete task', 'danger');
                }
            });
        } else if (action === 'reopen') {
            postChangeStatus(taskId, '0').then(r => {
                if (r.succeeded) {
                    updateRowStatus(taskId, r.status || 'NotStarted');
                    window.showToast('Task reopened', 'primary');
                } else {
                    window.showToast(r.error || 'Failed to reopen task', 'danger');
                }
            });
        }
    }

    document.addEventListener('click', handleActionClick);
})();

