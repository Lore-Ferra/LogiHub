module Example.Components {
    export interface OffcanvasOptions {
        id: string;
        url: string;
        title?: string;
    }

    export class OffcanvasComponent {
        private state: Record<string, { isOpen: boolean }> = {};

        open(options: OffcanvasOptions) {
            const { id, url, title } = options;
            const offcanvasEl = document.getElementById(id);
            if (!offcanvasEl) return;

            // Aggiorna titolo
            const header = offcanvasEl.querySelector('.offcanvas-title');
            if (header) header.textContent = title || '';

            // Mostra spinner
            const body = offcanvasEl.querySelector('[data-offcanvas-content]');
            if (!body) return;
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
                .then(html => {body.innerHTML = html; initSemilavoratoForm(body);})
                .catch(() => body.innerHTML = '<p class="text-danger text-center p-3">Errore nel caricamento.</p>');
        }
        load(options: OffcanvasOptions) {
            const { id, url, title } = options;
            const offcanvasEl = document.getElementById(id);
            if (!offcanvasEl) return;

            const body = offcanvasEl.querySelector('[data-offcanvas-content]');
            const header = offcanvasEl.querySelector('.offcanvas-title');
            if (!body) return;

            // Aggiorna titolo se fornito
            if (header && title) header.textContent = title;

            // Opzionale: mostra un piccolo overlay o spinner sopra il contenuto attuale
            body.classList.add('opacity-50');

            fetch(url)
                .then(r => r.text())
                .then(html => {
                    body.innerHTML = html;
                    initSemilavoratoForm(body);
                    body.classList.remove('opacity-50');
                })
                .catch(() => {
                    body.innerHTML = '<p class="text-danger p-3">Errore nel caricamento.</p>';
                    body.classList.remove('opacity-50');
                });
        }
        

        close(id: string) {
            const offcanvasEl = document.getElementById(id);
            if (!offcanvasEl) return;
            const bsInstance = bootstrap.Offcanvas.getInstance(offcanvasEl);
            bsInstance?.hide();
            if (this.state[id]) this.state[id].isOpen = false;
        }
    }

    export let Offcanvas = new OffcanvasComponent();
}

document.addEventListener('click', (e: MouseEvent) => {
    const target = e.target as HTMLElement | null;
    if (!target) return;

    // Ignora click su btn-elimina
    if (target.closest('[data-delete]')) return;

    const btn = target.closest('[data-offcanvas]') as HTMLElement | null;
    if (!btn) return;

    const id = btn.dataset.id;
    const url = btn.dataset.url;
    const title = btn.dataset.title;

    if (!id || !url) return;

    const offcanvasEl = document.getElementById(id);
    const isOpen = offcanvasEl?.classList.contains('show');

    if (isOpen) {
        Example.Components.Offcanvas.load({ id, url, title });
    } else {
        Example.Components.Offcanvas.open({ id, url, title });
    }
});

document.addEventListener('submit', async (e) => {
    const form = e.target as HTMLFormElement;
    if (!form.classList.contains('modifica-form')) return;

    e.preventDefault();
    e.stopPropagation();

    if (!form.checkValidity()) {
        form.classList.add('was-validated');
        return;
    }

    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        if (response.redirected) {
            window.location.href = response.url;
        }
    } catch (err) {
        console.error(err);
        alert('Errore durante il salvataggio');
    }
}, true);

function initSemilavoratoForm(root: Element) {

    const uscita = root.querySelector('#Uscito') as HTMLInputElement;
    const rientro = root.querySelector('#Rientrato') as HTMLInputElement;
    const azienda = root.querySelector('[name="AziendaEsternaId"]') as HTMLSelectElement;
    const form = root.querySelector('form') as HTMLFormElement;

    if (!uscita || !rientro || !azienda || !form) return;

    function updateAziendaValidation() {
        if (uscita.checked) {
            azienda.setAttribute('required', 'true');
        } else {
            azienda.removeAttribute('required');
            azienda.classList.remove('is-invalid');
        }
    }

    azienda.addEventListener('change', () => {
        if (azienda.value) {
            uscita.checked = true;
            rientro.checked = false;
        }
        updateAziendaValidation();
    });

    uscita.addEventListener('change', () => {
        if (!uscita.checked) {
            azienda.value = '';
        }
        updateAziendaValidation();
    });

    rientro.addEventListener('change', () => {
        if (rientro.checked) {
            uscita.checked = false;
            azienda.value = '';
        }
        updateAziendaValidation();
    });

    updateAziendaValidation();
}


function setChecked(el: HTMLInputElement, value: boolean) {
    if (el.checked !== value) {
        el.checked = value;
        el.dispatchEvent(new Event('change', { bubbles: true }));
    }
}


