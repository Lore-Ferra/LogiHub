class ConfirmDeleteModal {
    constructor(modalEl) {
        this.modalEl = modalEl;
        this.currentId = null;
        this.currentTrigger = null;
        this.endpoint = modalEl.dataset.endpoint;
        this.modal = new bootstrap.Modal(modalEl);
        this.confirmBtn = modalEl.querySelector('[data-confirm]');
        this.labelEl = modalEl.querySelector('[data-delete-label]');
        this.bind();
    }
    bind() {
        document.addEventListener('click', e => {
            var _a;
            const btn = e.target.closest('[data-delete]');
            if (!btn)
                return;
            e.preventDefault();
            e.stopPropagation();
            this.currentId = btn.getAttribute('data-id');
            this.currentTrigger = btn;
            this.labelEl.textContent = (_a = btn.getAttribute('data-label')) !== null && _a !== void 0 ? _a : '';
            this.modal.show();
        });
        this.confirmBtn.addEventListener('click', () => this.confirm());
    }
    async confirm() {
        var _a, _b;
        if (!this.currentId)
            return;
        this.confirmBtn.disabled = true;
        try {
            const response = await fetch(this.endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ semiLavoratoId: this.currentId })
            });
            if (response.ok) {
                this.modal.hide();
                (_b = (_a = this.currentTrigger) === null || _a === void 0 ? void 0 : _a.closest('tr, .card')) === null || _b === void 0 ? void 0 : _b.remove();
            }
        }
        finally {
            this.confirmBtn.disabled = false;
            this.currentId = null;
            this.currentTrigger = null;
        }
    }
}
document.addEventListener('DOMContentLoaded', () => {
    document
        .querySelectorAll('[data-confirm-delete-modal]')
        .forEach(m => new ConfirmDeleteModal(m));
});
//# sourceMappingURL=ConfirmDeleteModal.js.map