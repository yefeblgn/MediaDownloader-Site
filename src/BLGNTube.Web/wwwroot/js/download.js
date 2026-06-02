// BLGNTube — indirme arayüzü akışı
// Adımlar: URL incele -> format/kalite seç -> job başlat -> ilerlemeyi izle -> dosyayı kaydet
(() => {
    'use strict';

    const $ = (id) => document.getElementById(id);

    const urlInput = $('urlInput');
    const fetchBtn = $('fetchBtn');
    const errorBox = $('errorBox');
    const previewPanel = $('previewPanel');
    const progressPanel = $('progressPanel');
    const readyPanel = $('readyPanel');
    const downloadBtn = $('downloadBtn');
    const qualityWrap = $('qualityWrap');
    const qualitySelect = $('qualitySelect');
    const progressBar = $('progressBar');
    const progressPct = $('progressPct');
    const progressLabel = $('progressLabel');
    const saveBtn = $('saveBtn');
    const resetBtn = $('resetBtn');
    const quotaRemaining = $('quotaRemaining');

    let selectedFormat = 'mp4';
    let pollTimer = null;

    // --- Yardımcılar ---
    function showError(msg) {
        errorBox.textContent = msg;
        errorBox.classList.remove('hidden');
    }
    function clearError() { errorBox.classList.add('hidden'); errorBox.textContent = ''; }

    function setBusy(btn, busy) {
        const label = btn.querySelector('.btn-label');
        const spin = btn.querySelector('.btn-spinner');
        btn.disabled = busy;
        if (label && spin) {
            label.classList.toggle('hidden', busy);
            spin.classList.toggle('hidden', !busy);
        }
    }

    function hide(...els) { els.forEach(e => e.classList.add('hidden')); }
    function show(...els) { els.forEach(e => e.classList.remove('hidden')); }

    async function postJson(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const data = await res.json().catch(() => ({}));
        return { ok: res.ok, status: res.status, data };
    }

    // --- 1. Medyayı incele ---
    async function fetchInfo() {
        const url = urlInput.value.trim();
        clearError();
        if (!url) { showError('Lütfen bir bağlantı yapıştır.'); return; }

        setBusy(fetchBtn, true);
        hide(previewPanel, progressPanel, readyPanel);

        const { ok, data } = await postJson('/api/download/info', { url });
        setBusy(fetchBtn, false);

        if (!ok) { showError(data.error || 'Bilgi alınamadı.'); return; }

        $('thumb').src = data.thumbnailUrl || '';
        $('thumb').style.display = data.thumbnailUrl ? '' : 'none';
        $('mediaTitle').textContent = data.title || 'Başlıksız';
        $('mediaSite').textContent = data.siteName || 'Medya';
        $('mediaUploader').textContent = data.uploader || '';
        $('mediaDuration').textContent = data.duration && data.duration !== '—' ? '⏱ ' + data.duration : '';

        // Kalite seçeneklerini doldur
        qualitySelect.innerHTML = '';
        (data.qualities || []).forEach(q => {
            const opt = document.createElement('option');
            opt.value = q.height;
            opt.textContent = q.label;
            qualitySelect.appendChild(opt);
        });

        show(previewPanel);
        previewPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    // --- Format seçimi ---
    document.querySelectorAll('.format-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            document.querySelectorAll('.format-btn').forEach(b => b.classList.remove('is-active'));
            btn.classList.add('is-active');
            selectedFormat = btn.dataset.format;
            qualityWrap.style.display = selectedFormat === 'mp4' ? '' : 'none';
        });
    });

    // --- 2. İndirmeyi başlat ---
    async function startDownload() {
        clearError();
        const url = urlInput.value.trim();
        const quality = selectedFormat === 'mp4' ? qualitySelect.value : null;

        hide(previewPanel, readyPanel);
        show(progressPanel);
        setProgress(0, 'İndirme başlatılıyor…');

        const { ok, status, data } = await postJson('/api/download/start', {
            url, format: selectedFormat, quality
        });

        if (!ok) {
            hide(progressPanel);
            show(previewPanel);
            showError(data.error || 'İndirme başlatılamadı.');
            if (status === 429 && quotaRemaining) quotaRemaining.textContent = '0';
            return;
        }

        if (typeof data.remaining === 'number' && quotaRemaining) {
            quotaRemaining.textContent = Math.max(0, data.remaining);
        }

        pollStatus(data.jobId);
    }

    function setProgress(pct, label) {
        progressBar.style.width = Math.max(2, pct) + '%';
        progressPct.textContent = Math.round(pct) + '%';
        if (label) progressLabel.textContent = label;
    }

    // --- 3. Durumu izle ---
    function pollStatus(jobId) {
        clearInterval(pollTimer);
        pollTimer = setInterval(async () => {
            const res = await fetch('/api/download/status/' + jobId);
            if (!res.ok) { clearInterval(pollTimer); showFailure('İşlem bulunamadı.'); return; }
            const job = await res.json();

            switch (job.state) {
                case 'Downloading':
                    setProgress(job.progress, 'İndiriliyor…');
                    break;
                case 'Processing':
                    setProgress(Math.max(job.progress, 99), 'Dönüştürülüyor…');
                    break;
                case 'Completed':
                    clearInterval(pollTimer);
                    setProgress(100, 'Tamamlandı');
                    setTimeout(() => onReady(jobId, job), 350);
                    break;
                case 'Failed':
                    clearInterval(pollTimer);
                    showFailure(job.error || 'İndirme başarısız oldu.');
                    break;
            }
        }, 800);
    }

    function showFailure(msg) {
        hide(progressPanel);
        show(previewPanel);
        showError(msg);
    }

    // --- 4. Hazır ---
    function onReady(jobId, job) {
        hide(progressPanel);
        const sizeMb = job.fileSizeBytes ? (job.fileSizeBytes / 1048576).toFixed(1) + ' MB' : '';
        $('readyMeta').textContent = [job.title, sizeMb].filter(Boolean).join(' · ');
        saveBtn.href = '/api/download/file/' + jobId;
        show(readyPanel);
        readyPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    // --- Sıfırla ---
    function reset() {
        clearError();
        hide(progressPanel, readyPanel, previewPanel);
        urlInput.value = '';
        urlInput.focus();
    }

    // --- Olaylar ---
    fetchBtn.addEventListener('click', fetchInfo);
    urlInput.addEventListener('keydown', (e) => { if (e.key === 'Enter') fetchInfo(); });
    downloadBtn.addEventListener('click', startDownload);
    resetBtn.addEventListener('click', reset);
})();
