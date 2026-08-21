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
            return { succeeded: false, error: e.message };
        }
    }

    function updateRowStatus(taskId, statusText) {
        const row = document.querySelector(`[data-task-id='${taskId}']`);
        if (!row) return;
        const statusEl = row.querySelector('.task-status');
        if (statusEl) statusEl.textContent = statusText;
        // simple visual for completed
        if (statusText && statusText.toLowerCase().includes('completed')) {
            row.classList.remove('table-danger');
            row.classList.add('table-success');
        } else {
            row.classList.remove('table-success');
        }
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
                if (r.succeeded) updateRowStatus(taskId, r.status || 'Completed');
                else alert(r.error || 'Failed to complete task');
            });
        } else if (action === 'reopen') {
            postChangeStatus(taskId, '0').then(r => {
                if (r.succeeded) updateRowStatus(taskId, r.status || 'NotStarted');
                else alert(r.error || 'Failed to reopen task');
            });
        }
    }

    document.addEventListener('click', handleActionClick);
})();
