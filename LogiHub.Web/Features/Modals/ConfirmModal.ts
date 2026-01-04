class ConfirmModal {
    private modal: any;
    private confirmBtn: HTMLButtonElement;
    private messageEl: HTMLElement;
    private labelEl: HTMLElement;

    private currentConfig: {
        url: string;
        method: string;
        type: 'ajax' | 'form';
        body: string | null;
        successAction: 'remove' | 'reload' | 'none';
        trigger: HTMLElement;
    } | null = null;

    constructor(private modalEl: HTMLElement) {
        this.modal = new bootstrap.Modal(modalEl);
        this.confirmBtn = modalEl.querySelector('[data-confirm-btn]') as HTMLButtonElement;
        this.messageEl = modalEl.querySelector('[data-confirm-message]') as HTMLElement;
        this.labelEl = modalEl.querySelector('[data-dynamic-label]') as HTMLElement;

        this.bind();
    }

    private bind() {
        document.addEventListener('click', e => {
            const trigger = (e.target as HTMLElement).closest('[data-confirm-trigger]');
            if (!trigger) return;

            e.preventDefault();
            e.stopImmediatePropagation();

            this.currentConfig = {
                url: trigger.getAttribute('data-url')!,
                method: trigger.getAttribute('data-method') || 'POST',
                type: (trigger.getAttribute('data-type') as 'ajax' | 'form') || 'ajax',
                body: trigger.getAttribute('data-body'),
                successAction: (trigger.getAttribute('data-success-action') as any) || 'none',
                trigger: trigger as HTMLElement
            };

            const customMessage = trigger.getAttribute('data-message');
            const dynamicLabel = trigger.getAttribute('data-label');

            if (customMessage) {
                this.messageEl.innerHTML = customMessage; 
            } else if (dynamicLabel && this.labelEl) {
                this.labelEl.textContent = dynamicLabel;
            }

            this.modal.show();
        });

        this.confirmBtn.addEventListener('click', () => this.confirm());
    }

    private async confirm() {
        if (!this.currentConfig) return;

        this.confirmBtn.disabled = true;

        try {
            if (this.currentConfig.type === 'form') {
                this.submitForm();
            } else {
                await this.submitAjax();
            }
        } catch (error) {
            console.error(error);
            alert('Si è verificato un errore durante l\'operazione.');
            this.confirmBtn.disabled = false;
        }
    }

    private submitForm() {
        const form = document.createElement('form');
        form.method = this.currentConfig!.method;
        form.action = this.currentConfig!.url;

        const token = document.querySelector('input[name="__RequestVerificationToken"]') as HTMLInputElement;
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

    private async submitAjax() {
        const response = await fetch(this.currentConfig!.url, {
            method: this.currentConfig!.method,
            headers: { 'Content-Type': 'application/json' },
            body: this.currentConfig!.body
        });

        if (response.ok) {
            this.modal.hide();
            if (this.currentConfig!.successAction === 'remove') {
                this.currentConfig!.trigger.closest('tr, .card')?.remove();
            } else if (this.currentConfig!.successAction === 'reload') {
                window.location.reload();
            }
        } else {
            throw new Error('Richiesta fallita');
        }
        
        this.confirmBtn.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll<HTMLElement>('[data-confirm-modal]')
        .forEach(el => new ConfirmModal(el));
});