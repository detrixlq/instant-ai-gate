// wwwroot/js/pages/aimodels.js
document.addEventListener("DOMContentLoaded", () => {
    initializeLiveDownloadTelemetry();
});

function initializeLiveDownloadTelemetry() {
    if (!window.PageConfig || !window.PageConfig.apiUrl) {
        console.error("Page configuration context missing.");
        return;
    }

    const sseUrl = `${window.PageConfig.apiUrl}/api/admin/fetch/progress-stream`;
    const eventSource = new EventSource(sseUrl);

    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    const rvt = tokenElement ? tokenElement.value : '';

    const trackedDownloads = new Map();
    const displayNames = new Map();

    // 1. Collect DisplayNames from the DOM on page load to use later in button callbacks
    // Mapping of repoId -> displayName collected at DOM ready to avoid recomputing later
    document.querySelectorAll('.data-table tbody tr').forEach(tr => {
        const titleEl = tr.querySelector('.model-title');
        const repoEl = tr.querySelector('.model-repo');
        if (titleEl && repoEl) {
            displayNames.set(repoEl.textContent.trim(), titleEl.textContent.trim());
        }
    });

    // 2. Initialize existing download cancellation forms with a special class to prevent duplication
    document.querySelectorAll('form[action*="handler=CancelDownload"]').forEach(form => {
        form.classList.add('cancel-download-form');
    });

    // Ensure the above forms are marked so UI updates won't add duplicate cancel forms when streaming updates arrive.
    eventSource.onmessage = (event) => {
        const activeDownloads = JSON.parse(event.data);
        const currentSafeIds = new Set();

        activeDownloads.forEach(item => {
            const safeId = item.repoId.replace(/\//g, "-").replace(/\./g, "-");
            currentSafeIds.add(safeId);

            const displayName = item.displayName || displayNames.get(item.repoId) || item.repoId;
            trackedDownloads.set(safeId, { repoId: item.repoId, displayName: displayName });

            const statusCell = document.getElementById(`status-cell-${safeId}`);
            const actionsCell = document.getElementById(`actions-cell-${safeId}`);

            const isCompleted = item.progress >= 100 || item.status === 'completed' || item.status === 'success';
            const isFailed = item.status === 'failed' || item.status === 'error' || item.status === 'canceled';
            const isDownloading = !isCompleted && !isFailed;

            // --- Dynamic Status Cell Update ---
            if (statusCell) {
                if (isCompleted) {
                    statusCell.innerHTML = `<span class="badge-status info">Ready for Allocation</span>`;
                } else if (isFailed) {
                    statusCell.innerHTML = `
                        <span class="badge-status">
                            <i class="bi bi-exclamation-circle text-danger"></i> Failed / Canceled
                        </span>
                    `;
                } else {
                    statusCell.innerHTML = `
                        <div class="downloading-status-container">
                            <span class="badge-status warning">
                                <i class="bi bi-arrow-repeat animate-spin"></i> Downloading ${item.progress.toFixed(1)}%
                            </span>
                            <span class="download-file-subtext" title="${item.currentFile}">${item.currentFile}</span>
                        </div>
                    `;
                }
            }

            // --- Dynamic Action Buttons Update ---
            if (actionsCell) {
                // GUARD: Do not overwrite the actions cell if the model is currently active or unloading from VRAM
                if (actionsCell.querySelector('.unload-btn')) {
                    return;
                }

                if (isCompleted) {
                    // Download successful -> Provide the configuration drawer trigger
                    actionsCell.innerHTML = `
                        <button onclick="openConfigDrawer('${item.repoId}', '${displayName.replace(/'/g, "\\'")}')" class="btn-secondary">
                            <i class="bi bi-power"></i> Configure & Load
                        </button>
                    `;
                } else if (isFailed) {
                    // Error/Cancellation -> Restore the default download button
                    actionsCell.innerHTML = `
                        <form action="?handler=StartDownload&repoId=${encodeURIComponent(item.repoId)}" method="post" class="inline-form">
                            <input type="hidden" name="__RequestVerificationToken" value="${rvt}" />
                            <button type="submit" class="btn-outline-primary">
                                <i class="bi bi-cloud-download"></i> Download
                            </button>
                        </form>
                    `;
                } else if (isDownloading) {
                    // In progress -> Show the Cancel form
                    if (!actionsCell.querySelector('.cancel-download-form')) {
                        actionsCell.innerHTML = `
                            <form action="?handler=CancelDownload&repoId=${encodeURIComponent(item.repoId)}" method="post" class="inline-form cancel-download-form">
                                <input type="hidden" name="__RequestVerificationToken" value="${rvt}" />
                                <button type="submit" class="btn-danger-secondary">
                                    <i class="bi bi-slash-circle"></i> Cancel
                                </button>
                            </form>
                        `;
                    }
                }
            }
        });

        // 3. Handle tasks that have disappeared from the server's active downloads stream
        for (const [safeId, data] of trackedDownloads.entries()) {
            if (!currentSafeIds.has(safeId)) {
                const statusCell = document.getElementById(`status-cell-${safeId}`);
                const actionsCell = document.getElementById(`actions-cell-${safeId}`);

                if (statusCell) {
                    statusCell.innerHTML = `<span class="badge-status info">Ready for Allocation</span>`;
                }

                if (actionsCell && !actionsCell.querySelector('.unload-btn')) {
                    actionsCell.innerHTML = `
                        <button onclick="openConfigDrawer('${data.repoId}', '${data.displayName.replace(/'/g, "\\'")}')" class="btn-secondary">
                            <i class="bi bi-power"></i> Configure & Load
                        </button>
                    `;
                }

                trackedDownloads.delete(safeId);
            }
        }
    };

    eventSource.onerror = (err) => {
        console.warn("SSE stream channel dropped. Reconnecting automatically...", err);
    };
}

function showUnloadLoading(formElement) {

    const button = formElement.querySelector('.unload-btn');
    const icon = formElement.querySelector('.animate-icon');
    const text = formElement.querySelector('.btn-text');

    const actionLink = formElement.closest('td').querySelector('.console-link, .embedding-lab-link');

    if (button) {
        button.disabled = true;
        button.classList.add('opacity-75', 'cursor-not-allowed');
        if (text) text.innerText = 'Clearing VRAM...';
        if (icon) {
            icon.className = 'bi bi-arrow-repeat animate-spin';
        }
    }

    if (actionLink) {
        actionLink.classList.add('pointer-events-none', 'opacity-40');
    }

    // Guard the sliding side panel's global context (if utilized)
    if (typeof isDrawerLoading !== 'undefined') {
        isDrawerLoading = true;
    }
}