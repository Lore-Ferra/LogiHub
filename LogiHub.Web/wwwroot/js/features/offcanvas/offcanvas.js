var Example;
(function (Example) {
    var Components;
    (function (Components) {
        class OffcanvasComponent {
            constructor() {
                this.state = {};
            }
            open(options) {
                const { id, url, title } = options;
                const offcanvasEl = document.getElementById(id);
                if (!offcanvasEl)
                    return;
                // Aggiorna titolo
                const header = offcanvasEl.querySelector('.offcanvas-title');
                if (header)
                    header.textContent = title || '';
                // Mostra spinner
                const body = offcanvasEl.querySelector('[data-offcanvas-content]');
                if (!body)
                    return;
                body.innerHTML = `
        <div class="d-flex justify-content-center align-items-center h-100">
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Caricamento...</span>
            </div>
        </div>
    `;
                // Bootstrap Offcanvas: usa getInstance prima di crearne uno nuovo
                let bsInstance = bootstrap.Offcanvas.getInstance(offcanvasEl);
                if (!bsInstance) {
                    bsInstance = new bootstrap.Offcanvas(offcanvasEl, {
                        backdrop: true,
                        scroll: false
                    });
                }
                bsInstance.show();
                this.state[id] = { isOpen: true };
                fetch(url)
                    .then(r => r.text())
                    .then(html => body.innerHTML = html)
                    .catch(() => body.innerHTML = '<p class="text-danger text-center p-3">Errore nel caricamento.</p>');
            }
            load(options) {
                const { id, url, title } = options;
                const offcanvasEl = document.getElementById(id);
                if (!offcanvasEl)
                    return;
                const body = offcanvasEl.querySelector('[data-offcanvas-content]');
                const header = offcanvasEl.querySelector('.offcanvas-title');
                if (!body)
                    return;
                // Aggiorna titolo se fornito
                if (header && title)
                    header.textContent = title;
                // Opzionale: mostra un piccolo overlay o spinner sopra il contenuto attuale
                body.classList.add('opacity-50');
                fetch(url)
                    .then(r => r.text())
                    .then(html => {
                    body.innerHTML = html;
                    body.classList.remove('opacity-50');
                })
                    .catch(() => {
                    body.innerHTML = '<p class="text-danger p-3">Errore nel caricamento.</p>';
                    body.classList.remove('opacity-50');
                });
            }
            close(id) {
                const offcanvasEl = document.getElementById(id);
                if (!offcanvasEl)
                    return;
                const bsInstance = bootstrap.Offcanvas.getInstance(offcanvasEl);
                bsInstance === null || bsInstance === void 0 ? void 0 : bsInstance.hide();
                if (this.state[id])
                    this.state[id].isOpen = false;
            }
        }
        Components.OffcanvasComponent = OffcanvasComponent;
        Components.Offcanvas = new OffcanvasComponent();
    })(Components = Example.Components || (Example.Components = {}));
})(Example || (Example = {}));
document.addEventListener('click', (e) => {
    const target = e.target;
    if (!target)
        return;
    // Ignora click su btn-elimina
    if (target.closest('.btn-elimina'))
        return;
    const btn = target.closest('[data-offcanvas]');
    if (!btn)
        return;
    const id = btn.dataset.id;
    const url = btn.dataset.url;
    const title = btn.dataset.title;
    if (!id || !url)
        return;
    const offcanvasEl = document.getElementById(id);
    const isOpen = offcanvasEl === null || offcanvasEl === void 0 ? void 0 : offcanvasEl.classList.contains('show');
    if (isOpen) {
        Example.Components.Offcanvas.load({ id, url, title });
    }
    else {
        Example.Components.Offcanvas.open({ id, url, title });
    }
});
//# sourceMappingURL=offcanvas.js.map