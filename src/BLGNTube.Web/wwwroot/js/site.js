// BLGNTube — genel arayüz davranışları
(() => {
    'use strict';

    // Sayfa kaydırıldıkça navbar'a hafif gölge/arka plan ekle.
    const nav = document.querySelector('header nav');
    if (nav) {
        const onScroll = () => {
            if (window.scrollY > 8) nav.classList.add('shadow-lg', 'shadow-black/20');
            else nav.classList.remove('shadow-lg', 'shadow-black/20');
        };
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }
})();
