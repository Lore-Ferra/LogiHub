window.OffcanvasManager = (function () {

    function open(offcanvasId, url) {

        const offcanvasEl = document.getElementById(offcanvasId);
        if (!offcanvasEl) return;

        const offcanvas = window.bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl);
        offcanvas.show();

        const contentDiv = offcanvasEl.querySelector('[data-offcanvas-content]');
        if (!contentDiv) return;

        contentDiv.innerHTML = `
            <div class="d-flex flex-column justify-content-center align-items-center h-100 mt-5">
                <div class="spinner-border text-primary"></div>
                <p class="mt-2 text-muted">Caricamento dati...</p>
            </div>`;

        fetch(url)
            .then(r => {
                if (!r.ok) throw new Error("Errore nel caricamento");
                return r.text();
            })
            .then(html => contentDiv.innerHTML = html)
            .catch(err => {
                contentDiv.innerHTML = `
                    <div class="alert alert-danger m-3">
                        Errore: ${err.message}
                    </div>`;
            });
    }

    function wireMobileBehavior() {
        document.querySelectorAll('.offcanvas').forEach(offcanvasEl => {

            offcanvasEl.addEventListener('shown.bs.offcanvas', () => {
                if (window.innerWidth < 768) {
                    document.body.classList.add('offcanvas-open-mobile');
                }
            });

            offcanvasEl.addEventListener('hidden.bs.offcanvas', () => {
                document.body.classList.remove('offcanvas-open-mobile');
            });

        });
    }

    document.addEventListener('DOMContentLoaded', wireMobileBehavior);

    return {
        open
    };

})();
