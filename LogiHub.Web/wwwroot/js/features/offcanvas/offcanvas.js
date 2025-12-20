function initOffcanvasContent(container) {

    if (window.$ && $.fn.select2) {
        container.querySelectorAll('.js-select-search').forEach(el => {

            if (!el.classList.contains('select2-hidden-accessible')) {

                const offcanvas = el.closest('.offcanvas');

                $(el).select2({
                    width: '100%',
                    placeholder: el.dataset.placeholder || 'Seleziona',
                    allowClear: true,
                    dropdownParent: offcanvas || document.body
                });
            }
        });
    }
}
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
                const header = offcanvasEl.querySelector('.offcanvas-title');
                if (header)
                    header.textContent = title || '';
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
                const bsInstance = new bootstrap.Offcanvas(offcanvasEl);
                bsInstance.show();
                this.state[id] = { isOpen: true };
                fetch(url)
                    .then(r => r.text())
                    .then(html => {
                        body.innerHTML = html;
                        initOffcanvasContent(body);
                    })
                    .catch(() => {
                        body.innerHTML = '<p class="text-danger text-center p-3">Errore nel caricamento.</p>';
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
    const btn = target.closest('[data-offcanvas]');
    if (!btn)
        return;
    const id = btn.dataset.id;
    const url = btn.dataset.url;
    const title = btn.dataset.title;
    if (!id || !url)
        return;
    Example.Components.Offcanvas.open({ id, url, title });
});
//# sourceMappingURL=offcanvas.js.map