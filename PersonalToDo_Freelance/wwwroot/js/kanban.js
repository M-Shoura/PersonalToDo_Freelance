/**
 * TaskPulse Kanban Board Engine
 * HTML5 Drag & Drop + Touch & Fallback Move Actions + Optimistic Real-Time AJAX Status Updates
 */
(() => {
    'use strict';

    let draggedCard = null;
    let originalList = null;
    let originalStatus = null;

    function getAntiForgeryToken() {
        const form = document.getElementById('__antiForgeryForm');
        if (!form) return null;
        const input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : null;
    }

    function getStatusName(status) {
        switch (String(status)) {
            case '0': return 'To Do';
            case '1': return 'In Progress';
            case '2': return 'Done';
            case '3': return 'Cancelled';
            default: return 'Updated';
        }
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
                    'RequestVerificationToken': token || ''
                },
                body: body,
                credentials: 'same-origin'
            });

            if (!res.ok) throw new Error(`Server error: ${res.statusText}`);
            const json = await res.json();
            return json;
        } catch (err) {
            return { succeeded: false, error: err.message || 'Network error' };
        }
    }

    function updateColumnCounts() {
        const columns = document.querySelectorAll('.kanban-card-list');
        columns.forEach(col => {
            const status = col.getAttribute('data-status');
            const cards = col.querySelectorAll('.kanban-card');
            const countBadge = document.getElementById(`count-${status}`);
            if (countBadge) {
                countBadge.textContent = cards.length;
            }

            // Check empty state
            let emptyState = col.querySelector('.kanban-empty-state');
            if (cards.length === 0) {
                if (!emptyState) {
                    emptyState = document.createElement('div');
                    emptyState.className = 'kanban-empty-state';
                    emptyState.innerHTML = `
                        <i class="bi bi-inbox text-muted fs-3 mb-1"></i>
                        <p class="small text-muted mb-0">No tasks in this column</p>
                    `;
                    col.appendChild(emptyState);
                }
            } else if (emptyState) {
                emptyState.remove();
            }
        });
    }

    function updateCardUI(card, newStatus) {
        card.setAttribute('data-status', newStatus);
        const titleLink = card.querySelector('.kanban-card-title a');
        if (titleLink) {
            if (newStatus === '2') { // Completed
                titleLink.classList.add('text-decoration-line-through', 'text-muted');
                card.classList.add('kanban-card-completed');
            } else {
                titleLink.classList.remove('text-decoration-line-through', 'text-muted');
                card.classList.remove('kanban-card-completed');
            }
        }
    }

    async function moveCardToStatus(card, targetList, targetStatus, sourceList, sourceStatus) {
        if (targetStatus === sourceStatus && targetList === sourceList) return;

        // Optimistic DOM Update
        targetList.appendChild(card);
        updateCardUI(card, targetStatus);
        updateColumnCounts();

        const taskId = card.getAttribute('data-task-id');
        const result = await postChangeStatus(taskId, targetStatus);

        if (result && result.succeeded) {
            if (window.showToast) {
                window.showToast(`Task moved to ${getStatusName(targetStatus)}! ✨`, 'success');
            }
        } else {
            // Revert DOM on error
            sourceList.appendChild(card);
            updateCardUI(card, sourceStatus);
            updateColumnCounts();
            if (window.showToast) {
                window.showToast(result?.error || 'Failed to update task status', 'danger');
            }
        }
    }

    function initDragAndDrop() {
        const board = document.querySelector('.kanban-board-container');
        if (!board) return;

        // Drag Start on Cards
        board.addEventListener('dragstart', (e) => {
            const card = e.target.closest('.kanban-card');
            if (!card) return;

            draggedCard = card;
            originalList = card.closest('.kanban-card-list');
            originalStatus = originalList ? originalList.getAttribute('data-status') : null;

            e.dataTransfer.setData('text/plain', card.getAttribute('data-task-id'));
            e.dataTransfer.effectAllowed = 'move';

            setTimeout(() => {
                card.classList.add('is-dragging');
            }, 0);
        });

        // Drag End
        board.addEventListener('dragend', (e) => {
            const card = e.target.closest('.kanban-card');
            if (card) {
                card.classList.remove('is-dragging');
            }
            document.querySelectorAll('.kanban-card-list').forEach(list => {
                list.classList.remove('drag-over');
            });
            draggedCard = null;
        });

        // Drag Over Column
        board.addEventListener('dragover', (e) => {
            const list = e.target.closest('.kanban-card-list');
            if (!list) return;

            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';

            document.querySelectorAll('.kanban-card-list').forEach(l => {
                if (l !== list) l.classList.remove('drag-over');
            });
            list.classList.add('drag-over');
        });

        // Drag Leave Column
        board.addEventListener('dragleave', (e) => {
            const list = e.target.closest('.kanban-card-list');
            if (list && !list.contains(e.relatedTarget)) {
                list.classList.remove('drag-over');
            }
        });

        // Drop on Column
        board.addEventListener('drop', (e) => {
            const list = e.target.closest('.kanban-card-list');
            if (!list || !draggedCard) return;

            e.preventDefault();
            list.classList.remove('drag-over');

            const targetStatus = list.getAttribute('data-status');
            const card = draggedCard;
            const srcList = originalList;
            const srcStatus = originalStatus;

            moveCardToStatus(card, list, targetStatus, srcList, srcStatus);
        });

        // Fallback / Mobile Quick Move Actions
        board.addEventListener('click', (e) => {
            const moveBtn = e.target.closest('[data-action="move-status"]');
            if (!moveBtn) return;

            e.preventDefault();
            const taskId = moveBtn.getAttribute('data-task-id');
            const targetStatus = moveBtn.getAttribute('data-target-status');
            const card = document.querySelector(`.kanban-card[data-task-id="${taskId}"]`);
            const targetList = document.querySelector(`.kanban-card-list[data-status="${targetStatus}"]`);

            if (card && targetList) {
                const srcList = card.closest('.kanban-card-list');
                const srcStatus = srcList ? srcList.getAttribute('data-status') : null;
                moveCardToStatus(card, targetList, targetStatus, srcList, srcStatus);
            }
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        initDragAndDrop();
        updateColumnCounts();
    });
})();
