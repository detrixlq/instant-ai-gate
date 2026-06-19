// wwwroot/js/pages/aimodels-config.js

let isDrawerLoading = false;

document.addEventListener('DOMContentLoaded', () => {
    initializeAccordionTriggers();
    initializeGpuLogicListeners();
    initializeDrawerFormControls();
});

/**
 * Initializes accordion animation triggers without inline onclick markup
 */
function initializeAccordionTriggers() {
    document.querySelectorAll('.accordion-trigger').forEach(trigger => {
        trigger.addEventListener('click', () => {
            const contentId = trigger.getAttribute('data-target');
            const chevronId = trigger.getAttribute('data-chevron');

            const content = document.getElementById(contentId);
            const chevron = document.getElementById(chevronId);

            if (!content || !chevron) return;

            if (content.classList.contains('hidden')) {
                content.classList.remove('hidden');
                chevron.classList.remove('rotate-180');
            } else {
                content.classList.add('hidden');
                chevron.classList.add('rotate-180');
            }
        });
    });
}

/**
 * Binds the CPU Only toggle and GPU layers slider UI
 */
function initializeGpuLogicListeners() {
    const cpuOnlyToggle = document.getElementById('cpu-only-toggle');
    const gpuLayers = document.getElementById('gpu-layers');
    const gpuLayersVal = document.getElementById('gpu-layers-val');

    if (!cpuOnlyToggle || !gpuLayers || !gpuLayersVal) return;

    function updateGpuUI(val) {
        if (val == -1) {
            gpuLayersVal.innerText = '-1 (Full Allocation)';
        } else if (val == 0) {
            gpuLayersVal.innerText = '0 (CPU Only)';
        } else {
            gpuLayersVal.innerText = val;
        }
    }

    // CPU Only checkbox change
    cpuOnlyToggle.addEventListener('change', (e) => {
        gpuLayers.value = e.target.checked ? 0 : -1;
        updateGpuUI(gpuLayers.value);
    });

    // GPU layers slider change
    gpuLayers.addEventListener('input', (e) => {
        const val = parseInt(e.target.value);
        updateGpuUI(val);
        cpuOnlyToggle.checked = (val === 0);
    });
}

/**
 * Initializes drawer close and submit triggers
 */
function initializeDrawerFormControls() {
    const closeBtn = document.getElementById('drawer-close-btn');
    const cancelBtn = document.getElementById('drawer-cancel-btn');
    const overlay = document.getElementById('drawer-overlay');
    const form = document.getElementById('drawer-form');

    if (closeBtn) closeBtn.addEventListener('click', closeConfigDrawer);
    if (cancelBtn) cancelBtn.addEventListener('click', closeConfigDrawer);

    if (overlay) {
        overlay.addEventListener('click', () => {
            if (!isDrawerLoading) closeConfigDrawer();
        });
    }

    if (form) {
        form.addEventListener('submit', (e) => {
            showDrawerLoading();
        });
    }
}

/**
 * Public global functions to control drawer state (called from aimodels.js)
 */
function openConfigDrawer(repoId, displayName) {
    if (isDrawerLoading) return;

    const drawer = document.getElementById('config-drawer');
    const overlay = document.getElementById('drawer-overlay');
    const titleDisplay = document.getElementById('drawer-model-display-name');
    const inputPath = document.getElementById('drawer-input-modelpath');

    if (titleDisplay) titleDisplay.innerText = displayName;
    if (inputPath) inputPath.value = repoId;

    if (drawer && overlay) {
        // Remove old invisible marker if present in HTML
        drawer.classList.remove('invisible');
        overlay.classList.remove('hidden');

        // Microtimeout to start the smooth CSS slide animation
        setTimeout(() => {
            drawer.classList.add('drawer-open');
            overlay.classList.remove('opacity-0');
        }, 10);
    }
}

function closeConfigDrawer() {
    if (isDrawerLoading) return;

    const drawer = document.getElementById('config-drawer');
    const overlay = document.getElementById('drawer-overlay');

    if (drawer && overlay) {
        drawer.classList.remove('drawer-open');
        overlay.classList.add('opacity-0');

        setTimeout(() => {
            overlay.classList.add('hidden');
        }, 300);
    }
}

function showDrawerLoading() {
    isDrawerLoading = true;

    const loadingOverlay = document.getElementById('drawer-loading-overlay');
    const submitBtn = document.getElementById('drawer-submit-btn');
    const cancelBtn = document.getElementById('drawer-cancel-btn');
    const closeBtn = document.getElementById('drawer-close-btn');
    const submitIcon = document.getElementById('submit-icon');

    if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.classList.add('opacity-75', 'cursor-not-allowed');
    }
    if (cancelBtn) cancelBtn.disabled = true;
    if (closeBtn) closeBtn.disabled = true;

    if (submitIcon) {
        submitIcon.className = 'bi bi-arrow-repeat animate-spin';
    }

    if (loadingOverlay) {
        loadingOverlay.classList.remove('hidden');
        setTimeout(() => {
            loadingOverlay.classList.remove('opacity-0');
        }, 10);
    }
}