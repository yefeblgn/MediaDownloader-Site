// BLGNTube — indirme arayüzü
(() => {
    'use strict';

    const T = Object.assign({
        starting: 'İndirme başlatılıyor…',
        downloading: 'İndiriliyor…',
        converting: 'Dönüştürülüyor…',
        completed: 'Tamamlandı',
        paste: 'Lütfen bir bağlantı yapıştır.',
        infoFailed: 'Bilgi alınamadı.',
        startFailed: 'İndirme başlatılamadı.',
        notFound: 'İşlem bulunamadı.',
        failed: 'İndirme başarısız oldu.'
    }, window.LOC || {});

    const $ = (id) => document.getElementById(id);

    const urlInput       = $('urlInput');
    const fetchBtn       = $('fetchBtn');
    const errorBox       = $('errorBox');
    const previewPanel   = $('previewPanel');
    const progressPanel  = $('progressPanel');
    const readyPanel     = $('readyPanel');
    const downloadBtn    = $('downloadBtn');
    const qualityWrap    = $('qualityWrap');
    const qualitySelect  = $('qualitySelect');
    const progressBar    = $('progressBar');
    const progressPct    = $('progressPct');
    const progressLabel  = $('progressLabel');
    const resetBtn       = $('resetBtn');
    const quotaRemaining = $('quotaRemaining');

    let selectedFormat      = 'mp4';
    let pollTimer           = null;
    let fakeTimer           = null;
    let fakePercent         = 0;
    let realProgressStarted = false;

    // --- Yardımcılar ---
    function showError(msg) {
        errorBox.textContent = msg;
        errorBox.style.display = '';
    }
    function clearError() { errorBox.style.display = 'none'; errorBox.textContent = ''; }

    function setBusy(btn, busy) {
        const label = btn.querySelector('.btn-label');
        const spin  = btn.querySelector('.btn-spinner');
        btn.disabled = busy;
        if (label && spin) {
            label.style.display = busy ? 'none' : '';
            spin.style.display  = busy ? '' : 'none';
        }
    }

    function hide(...els) { els.forEach(e => { if (e) e.style.display = 'none'; }); }
    function show(...els) { els.forEach(e => { if (e) e.style.display = ''; }); }

    async function postJson(url, body) {
        const res  = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const data = await res.json().catch(() => ({}));
        return { ok: res.ok, status: res.status, data };
    }

    // --- Reklam tetikleyici ---
    function triggerAd(slot) {
        if (window.ADS && typeof window.ADS.trigger === 'function') {
            window.ADS.trigger(slot);
        }
    }

    // --- Sahte ilerleme ---
    function startSimulatedProgress() {
        realProgressStarted = false;
        fakePercent = 2;
        progressBar.classList.add('is-indeterminate');
        setProgress(fakePercent, T.starting);
        fakeTimer = setInterval(() => {
            const gap = 82 - fakePercent;
            fakePercent += Math.max(0.15, gap * 0.04) * (0.7 + Math.random() * 0.6);
            fakePercent = Math.min(82, fakePercent);
            setProgress(fakePercent, null);
        }, 350);
    }

    function stopSimulatedProgress() {
        if (fakeTimer) { clearInterval(fakeTimer); fakeTimer = null; }
        progressBar.classList.remove('is-indeterminate');
    }

    // --- 1. Medyayı incele ---
    async function fetchInfo() {
        const url = urlInput.value.trim();
        clearError();
        if (!url) { showError(T.paste); return; }

        // Reklam — Bul tuşu
        triggerAd('fetch');

        setBusy(fetchBtn, true);
        hide(previewPanel, progressPanel, readyPanel);

        const { ok, data } = await postJson('/api/download/info', { url });
        setBusy(fetchBtn, false);

        if (!ok) { showError(data.error || T.infoFailed); return; }

        $('thumb').src = data.thumbnailUrl || '';
        $('thumb').style.display = data.thumbnailUrl ? '' : 'none';
        $('mediaTitle').textContent    = data.title    || 'Başlıksız';
        $('mediaSite').textContent     = data.siteName || 'Medya';
        $('mediaUploader').textContent = data.uploader || '';
        $('mediaDuration').textContent = data.duration && data.duration !== '—' ? '⏱ ' + data.duration : '';

        qualitySelect.innerHTML = '';
        (data.qualities || []).forEach(q => {
            const opt = document.createElement('option');
            opt.value       = q.height;
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
        const url     = urlInput.value.trim();
        const quality = selectedFormat === 'mp4' ? qualitySelect.value : null;

        // Reklam — İndir tuşu
        triggerAd('download');

        hide(previewPanel, readyPanel);
        show(progressPanel);
        startSimulatedProgress();

        const { ok, status, data } = await postJson('/api/download/start', {
            url, format: selectedFormat, quality
        });

        if (!ok) {
            stopSimulatedProgress();
            hide(progressPanel);
            show(previewPanel);
            showError(data.error || T.startFailed);
            if (status === 429 && quotaRemaining) quotaRemaining.textContent = '0';
            return;
        }

        if (typeof data.remaining === 'number' && quotaRemaining) {
            quotaRemaining.textContent = Math.max(0, data.remaining);
        }

        pollStatus(data.jobId);
    }

    // label null ise etiket güncellenmez
    function setProgress(pct, label) {
        progressBar.style.width = Math.max(2, pct) + '%';
        progressPct.textContent = Math.round(pct) + '%';
        if (label != null) progressLabel.textContent = label;
    }

    // --- 3. Durumu izle ---
    function pollStatus(jobId) {
        clearInterval(pollTimer);
        pollTimer = setInterval(async () => {
            const res = await fetch('/api/download/status/' + jobId);
            if (!res.ok) {
                clearInterval(pollTimer);
                stopSimulatedProgress();
                showFailure(T.notFound);
                return;
            }
            const job = await res.json();

            switch (job.state) {
                case 'Downloading':
                    if (job.progress > 1 && !realProgressStarted) {
                        realProgressStarted = true;
                        stopSimulatedProgress();
                    }
                    if (realProgressStarted) setProgress(job.progress, T.downloading);
                    break;

                case 'Processing':
                    stopSimulatedProgress();
                    setProgress(Math.max(job.progress, 99), T.converting);
                    break;

                case 'Completed':
                    clearInterval(pollTimer);
                    stopSimulatedProgress();
                    setProgress(100, T.completed);
                    setTimeout(() => onReady(jobId, job), 400);
                    break;

                case 'Failed':
                    clearInterval(pollTimer);
                    stopSimulatedProgress();
                    showFailure(job.error || T.failed);
                    break;
            }
        }, 800);
    }

    function showFailure(msg) {
        hide(progressPanel);
        show(previewPanel);
        showError(msg);
    }

    // --- 4. Hazır — otomatik indir ---
    function onReady(jobId, job) {
        // Tarayıcıya otomatik indir
        const a = document.createElement('a');
        a.href = '/api/download/file/' + jobId;
        a.download = '';
        a.style.display = 'none';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        // Progress paneli gizle, "tamamlandı + yeni indirme" göster
        hide(progressPanel);
        const sizeMb = job.fileSizeBytes ? (job.fileSizeBytes / 1048576).toFixed(1) + ' MB' : '';
        const readyMeta = $('readyMeta');
        if (readyMeta) readyMeta.textContent = [job.title, sizeMb].filter(Boolean).join(' · ');
        show(readyPanel);
        readyPanel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    // --- Sıfırla ---
    function reset() {
        clearError();
        stopSimulatedProgress();
        hide(progressPanel, readyPanel, previewPanel);
        urlInput.value = '';
        urlInput.focus();
    }

    // --- Olaylar ---
    fetchBtn.addEventListener('click', fetchInfo);
    urlInput.addEventListener('keydown', (e) => { if (e.key === 'Enter') fetchInfo(); });
    downloadBtn.addEventListener('click', startDownload);
    if (resetBtn) resetBtn.addEventListener('click', reset);
})();
