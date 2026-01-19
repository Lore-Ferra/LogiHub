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

            const header = offcanvasEl.querySelector('.offcanvas-title');
            if (header) header.textContent = title || '';

            // Mostra spinner
            const body = offcanvasEl.querySelector('[data-offcanvas-content]');
            if (!body) return;

            // Spinner
            body.innerHTML = `
                <div class="d-flex justify-content-center align-items-center h-100">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Caricamento...</span>
                    </div>
                </div>`;

            // Bootstrap Offcanvas: usa getInstance prima di crearne uno nuovo
            let bsInstance = bootstrap.Offcanvas.getInstance(offcanvasEl);
            if (!bsInstance) {
                bsInstance = new bootstrap.Offcanvas(offcanvasEl, { backdrop: true, scroll: false });
            }

            bsInstance.show();
            this.state[id] = { isOpen: true };

            this.fetchContent(url, body);
        }
        load(options: OffcanvasOptions) {
            const { id, url, title } = options;
            const offcanvasEl = document.getElementById(id);
            if (!offcanvasEl) return;

            const body = offcanvasEl.querySelector('[data-offcanvas-content]');
            const header = offcanvasEl.querySelector('.offcanvas-title');
            if (!body) return;

            if (header && title) header.textContent = title;
            body.classList.add('opacity-50');

            this.fetchContent(url, body, () => body.classList.remove('opacity-50'));
        }

        private fetchContent(url: string, container: Element, onComplete?: () => void) {
            fetch(url)
                .then(r => r.text())
                .then(html => {
                    container.innerHTML = html;
                    if (onComplete) onComplete();

                    // QUI STA LA MAGIA: Avviso il mondo che ho caricato del contenuto.
                    // Chi se ne frega di cosa c'è dentro.
                    container.dispatchEvent(new CustomEvent('offcanvas:content-loaded', {
                        bubbles: true,
                        detail: { url: url }
                    }));
                })
                .catch(() => {
                    container.innerHTML = '<p class="text-danger p-3">Errore nel caricamento.</p>';
                    if (onComplete) onComplete();
                });
        }


        close(id: string) {
            const offcanvasEl = document.getElementById(id);
            if (!offcanvasEl) return;
            const bsInstance = bootstrap.Offcanvas.getInstance(offcanvasEl);
            bsInstance?.hide();
            if (this.state[id]) this.state[id].isOpen = false;
        }    }

    export let Offcanvas = new OffcanvasComponent();
}

// Listener generico per apertura (click) - RIMANE QUI
document.addEventListener('click', (e: MouseEvent) => {
    // ... logica click identica a prima ...
    const target = e.target as HTMLElement | null;
    if (!target) return;
    if (target.closest('.btn-elimina')) return; // O qualsiasi altra eccezione generica

    // Ignora click su btn-elimina
    if (target.closest('[data-delete]')) return;
    if (target.closest('[data-confirm-trigger]')) return;

    const btn = target.closest('[data-offcanvas]') as HTMLElement | null;
    if (!btn) return;

    // ... recupero id, url, title e chiamo Offcanvas.open/load ...
    const id = btn.dataset.id;
    const url = btn.dataset.url;
    const title = btn.dataset.title;

    if (!id || !url) return;

    const offcanvasEl = document.getElementById(id);
    const isOpen = offcanvasEl?.classList.contains('show');
    if (isOpen) Example.Components.Offcanvas.load({ id, url, title });
    else Example.Components.Offcanvas.open({ id, url, title });
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
        } else {
            const html = await response.text();
            const container = form.closest('[data-offcanvas-content]');
            if (container) {
                container.innerHTML = html;
                // Rilancio l'evento anche dopo un errore di validazione!
                container.dispatchEvent(new CustomEvent('offcanvas:content-loaded', { bubbles: true }));
            }
        }
    } catch (err) {
        console.error(err);
        alert('Errore tecnico');
    }
}, true);