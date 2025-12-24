class ConfirmDeleteModal {
    private modal: { show(): void; hide(): void };
    private confirmBtn: HTMLButtonElement;
    private labelEl: HTMLElement;
    private endpoint: string;

    private currentId: string | null = null;
    private currentTrigger: HTMLElement | null = null;

    constructor(private modalEl: HTMLElement) {
        this.endpoint = modalEl.dataset.endpoint!;
        this.modal = new bootstrap.Modal(modalEl);

        this.confirmBtn = modalEl.querySelector('[data-confirm]')!;
        this.labelEl = modalEl.querySelector('[data-delete-label]')!;

        this.bind();
    }

    private bind() {
        document.addEventListener('click', e => {
            const btn = (e.target as HTMLElement).closest('[data-delete]');
            if (!btn) return;

            e.preventDefault();
            e.stopPropagation();

            this.currentId = btn.getAttribute('data-id');
            this.currentTrigger = btn as HTMLElement;

            this.labelEl.textContent = btn.getAttribute('data-label') ?? '';
            this.modal.show();
        });

        this.confirmBtn.addEventListener('click', () => this.confirm());
    }

    private async confirm() {
        if (!this.currentId) return;

        this.confirmBtn.disabled = true;

        try {
            const response = await fetch(this.endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ semiLavoratoId: this.currentId })
            });

            if (response.ok) {
                this.modal.hide();
                this.currentTrigger?.closest('tr, .card')?.remove();
            }
        } finally {
            this.confirmBtn.disabled = false;
            this.currentId = null;
            this.currentTrigger = null;
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    document
        .querySelectorAll<HTMLElement>('[data-confirm-delete-modal]')
        .forEach(m => new ConfirmDeleteModal(m));
});
