class ConfirmModal {
    constructor(modalEl) {
        this.modalEl = modalEl;
        this.currentConfig = null;
        this.modal = new bootstrap.Modal(modalEl);
        this.confirmBtn = modalEl.querySelector('[data-confirm-btn]');
        this.messageEl = modalEl.querySelector('[data-confirm-message]');
        this.labelEl = modalEl.querySelector('[data-dynamic-label]');
        this.bind();
    }
    bind() {
        document.addEventListener('click', e => {
            const trigger = e.target.closest('[data-confirm-trigger]');
            if (!trigger)
                return;
            e.preventDefault();
            e.stopImmediatePropagation();
            this.currentConfig = {
                url: trigger.getAttribute('data-url'),
                method: trigger.getAttribute('data-method') || 'POST',
                type: trigger.getAttribute('data-type') || 'ajax',
                body: trigger.getAttribute('data-body'),
                successAction: trigger.getAttribute('data-success-action') || 'none',
                trigger: trigger
            };
            const customMessage = trigger.getAttribute('data-message');
            const dynamicLabel = trigger.getAttribute('data-label');
            if (customMessage) {
                this.messageEl.innerHTML = customMessage;
            }
            else if (dynamicLabel && this.labelEl) {
                this.labelEl.textContent = dynamicLabel;
            }
            this.modal.show();
        });
        this.confirmBtn.addEventListener('click', () => this.confirm());
    }
    async confirm() {
        if (!this.currentConfig)
            return;
        this.confirmBtn.disabled = true;
        try {
            if (this.currentConfig.type === 'form') {
                this.submitForm();
            }
            else {
                await this.submitAjax();
            }
        }
        catch (error) {
            console.error(error);
            alert('Si è verificato un errore durante l\'operazione.');
            this.confirmBtn.disabled = false;
        }
    }
    submitForm() {
        const form = document.createElement('form');
        form.method = this.currentConfig.method;
        form.action = this.currentConfig.url;
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = '__RequestVerificationToken';
            input.value = token.value;
            form.appendChild(input);
        }
        document.body.appendChild(form);
        form.submit();
    }
    async submitAjax() {
        var _a;
        const response = await fetch(this.currentConfig.url, {
            method: this.currentConfig.method,
            headers: { 'Content-Type': 'application/json' },
            body: this.currentConfig.body
        });
        if (response.ok) {
            this.modal.hide();
            if (this.currentConfig.successAction === 'remove') {
                (_a = this.currentConfig.trigger.closest('tr, .card')) === null || _a === void 0 ? void 0 : _a.remove();
            }
            else if (this.currentConfig.successAction === 'reload') {
                window.location.reload();
            }
        }
        else {
            throw new Error('Richiesta fallita');
        }
        this.confirmBtn.disabled = false;
    }
}
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-confirm-modal]')
        .forEach(el => new ConfirmModal(el));
});
//# sourceMappingURL=ConfirmModal.js.map