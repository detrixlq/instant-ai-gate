// wwwroot/js/pages/index.js
document.addEventListener('DOMContentLoaded', function () {
    const conn = window.HubConnection;
    if (!conn) {
        console.error('SignalR connection not found');
        return;
    }

    conn.on("ReceiveTelemetry", (data) => {
        updateHardwareMetrics(data.gpu, data.system);
        updateModelsTable(data.models);
    });

    function subscribeToMetrics() {
        conn.invoke("SubscribeToMetrics")
            .then(() => console.log('Subscribed to telemetry metrics'))
            .catch(err => console.error('Subscription error:', err));
    }

  
    if (typeof signalR !== 'undefined' && conn.state === 'Connected') {
        subscribeToMetrics();
    } else {
        window.addEventListener('signalr-connected', () => {
            subscribeToMetrics();
        }, { once: true });
    }

    conn.onreconnected(() => {
        console.log('SignalR reconnected, resubscribing...');
        subscribeToMetrics();
    });
});

function updateHardwareMetrics(gpu, system) {

    const vramPercent = gpu.totalMemoryGb > 0 ? (gpu.usedMemoryGb / gpu.totalMemoryGb) * 100 : 0;
    document.getElementById('vram-val').innerText = `${gpu.usedMemoryGb.toFixed(1)}/${gpu.totalMemoryGb.toFixed(0)} GB`;

    const vramBar = document.getElementById('vram-progress');
    vramBar.style.width = `${vramPercent}%`;


    vramBar.classList.remove('bg-warn', 'bg-crit');
    if (vramPercent > 85) vramBar.classList.add('bg-crit');
    else if (vramPercent > 70) vramBar.classList.add('bg-warn');


    document.getElementById('gpu-load-val').innerText = `${gpu.utilizationPercent}%`;
    const loadBar = document.getElementById('gpu-load-progress');
    loadBar.style.width = `${gpu.utilizationPercent}%`;

    loadBar.classList.remove('bg-warn', 'bg-crit');
    if (gpu.utilizationPercent > 90) loadBar.classList.add('bg-crit');
    else if (gpu.utilizationPercent > 70) loadBar.classList.add('bg-warn');


    document.getElementById('temp-val').innerText = `${gpu.temperatureCelsius} °C`;
    const tempStatus = document.getElementById('temp-status');

    tempStatus.classList.remove('text-warn', 'text-crit', 'text-emerald');
    if (gpu.temperatureCelsius > 80) {
        tempStatus.innerText = 'Critical';
        tempStatus.classList.add('text-crit');
    } else if (gpu.temperatureCelsius > 65) {
        tempStatus.innerText = 'Warm';
        tempStatus.classList.add('text-warn');
    } else {
        tempStatus.innerText = 'Normal';
        tempStatus.classList.add('text-emerald');
    }


    if (system) {
        // RAM
        const ramPercent = system.totalRamGb > 0 ? (system.usedRamGb / system.totalRamGb) * 100 : 0;
        document.getElementById('ram-val').innerText = `${system.usedRamGb.toFixed(1)}/${system.totalRamGb.toFixed(0)} GB`;

        const ramBar = document.getElementById('ram-progress');
        ramBar.style.width = `${ramPercent}%`;

        ramBar.classList.remove('bg-warn', 'bg-crit');
        if (ramPercent > 90) ramBar.classList.add('bg-crit');
        else if (ramPercent > 75) ramBar.classList.add('bg-warn');

        // CPU
        document.getElementById('cpu-load-val').innerText = `${system.cpuUtilizationPercent}%`;
        const cpuBar = document.getElementById('cpu-load-progress');
        cpuBar.style.width = `${system.cpuUtilizationPercent}%`;

        cpuBar.classList.remove('bg-warn', 'bg-crit');
        if (system.cpuUtilizationPercent > 85) cpuBar.classList.add('bg-crit');
        else if (system.cpuUtilizationPercent > 60) cpuBar.classList.add('bg-warn');
    }
}

function updateModelsTable(models) {
    document.getElementById('models-count').innerText = models.length;
    const tbody = document.getElementById('models-body');

    let totalActiveSlots = 0;

    if (models.length === 0) {
        document.getElementById('active-slots-val').innerText = "0";
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="px-6 py-12 text-center" style="color: var(--text-muted); padding: 3rem 0;">
                    <i class="bi bi-cpu" style="font-size: 2.5rem; display: block; margin-bottom: 0.75rem; opacity: 0.3;"></i>
                    <p>No models loaded in active pool</p>
                </td>
            </tr>
        `;
        return;
    }

    tbody.innerHTML = models.map(m => {
        totalActiveSlots += m.activeUsersCount;

        const contextPercent = m.maxParallelUsers > 0 ? (m.activeUsersCount / m.maxParallelUsers) * 100 : 0;
        const contextClass = contextPercent > 90 ? 'bg-crit' : contextPercent > 70 ? 'bg-warn' : 'bg-emerald';

      
        const queueIcon = m.isQueueWaiting ?
            '<i class="bi bi-exclamation-triangle text-crit animate-pulse" style="font-size: 1.1rem;"></i>' :
            '<i class="bi bi-check-circle text-emerald" style="font-size: 1.1rem;"></i>';

   
        let gpuLayersText;
        if (m.gpuLayers === -1) {
            gpuLayersText = '<span class="tag-auto">auto</span>';
        } else if (m.gpuLayers === 0) {
            gpuLayersText = '<span class="tag-cpu">CPU only</span>';
        } else {
            gpuLayersText = `<span>${m.gpuLayers}</span>`;
        }

        return `
            <tr>
                <td>
                    <div class="model-title">${m.repoId}</div>
                    <div class="model-subtitle">
                        ${m.flashAttention ? '<span style="color: var(--primary); font-weight: 500;">⚡ FlashAttention</span>' : '<span style="opacity: 0.5;">Standard KV</span>'}
                    </div>
                </td>
                <td>
                    ${m.isLoaded ?
                '<span class="badge-status active">Active</span>' :
                '<span class="badge-status offline">Offline</span>'
            }
                </td>
                <td>
                    <div class="context-wrapper">
                        <div class="context-info">
                            <div class="context-title">
                                ${m.activeUsersCount} / ${m.maxParallelUsers} Slots Used
                            </div>
                            <div class="progress-bar-container">
                                <div class="progress-bar ${contextClass}" style="width: ${contextPercent}%"></div>
                            </div>
                        </div>
                        <div class="context-idle">
                            ${m.idleContextsCount} idle
                        </div>
                    </div>
                </td>
                <td class="text-center">
                    ${queueIcon}
                </td>
                <td>
                    <div class="config-list">
                        <div>Ctx Size: <span>${m.contextSize}</span></div>
                        <div>GPU Layers: ${gpuLayersText}</div>
                        <div>CPU Threads: <span>${m.threads}</span></div>
                    </div>
                </td>
            </tr>
        `;
    }).join('');

    document.getElementById('active-slots-val').innerText = totalActiveSlots;
}