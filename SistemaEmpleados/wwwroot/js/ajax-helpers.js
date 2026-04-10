/* ══════════════════════════════════════════════
   SICE — Fetch helpers reutilizables
══════════════════════════════════════════════ */

const Http = {
    /**
     * GET JSON
     */
    async get(url) {
        const response = await fetch(url, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            }
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },

    /**
     * POST con JSON body + antiforgery token
     */
    async post(url, data = {}) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            || document.querySelector('meta[name="csrf-token"]')?.getAttribute('content')
            || '';
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },

    /**
     * POST con FormData (para formularios con archivos)
     */
    async postForm(url, formData) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            },
            body: formData
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
    },

    /**
     * DELETE
     */
    async delete(url) {
        return this.post(url, {});
    }
};

/* ══════ Notificaciones ══════ */
const Notify = {
    success(msg) {
        toastr.success(msg, '✓ Éxito', { timeOut: 3000, positionClass: 'toast-top-right' });
    },
    error(msg) {
        toastr.error(msg, '✗ Error', { timeOut: 4000, positionClass: 'toast-top-right' });
    },
    warning(msg) {
        toastr.warning(msg, '⚠ Atención', { timeOut: 3500, positionClass: 'toast-top-right' });
    },
    info(msg) {
        toastr.info(msg, 'ℹ Info', { timeOut: 3000, positionClass: 'toast-top-right' });
    }
};

/* ══════ Confirmación SweetAlert2 ══════ */
const Confirm = {
    async delete(nombre = 'este registro') {
        const result = await Swal.fire({
            title: '¿Eliminar registro?',
            html: `Vas a eliminar <strong>${nombre}</strong>.<br>Esta acción no se puede deshacer.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: '<i class="fa-solid fa-trash me-1"></i> Sí, eliminar',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#DC2626',
            cancelButtonColor: '#6B7280',
            buttonsStyling: false,
            customClass: {
                popup: 'sice-delete-popup',
                actions: 'sice-delete-actions',
                confirmButton: 'sice-delete-btn sice-delete-btn-confirm',
                cancelButton: 'sice-delete-btn sice-delete-btn-cancel'
            },
            reverseButtons: true,
            focusCancel: true
        });
        return result.isConfirmed;
    },

    async custom(title, html, confirmText = 'Confirmar', icon = 'question') {
        const result = await Swal.fire({
            title, html, icon,
            showCancelButton: true,
            confirmButtonText: confirmText,
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#2563EB',
            cancelButtonColor: '#6B7280',
            reverseButtons: true
        });
        return result.isConfirmed;
    }
};

/* ══════ Validación de formularios ══════ */
const FormHelper = {
    /**
     * Serializa un form a objeto JS
     */
    serialize(form) {
        const data = {};
        new FormData(form).forEach((v, k) => {
            if (data[k] !== undefined) {
                if (!Array.isArray(data[k])) data[k] = [data[k]];
                data[k].push(v);
            } else {
                data[k] = v;
            }
        });
        return data;
    },

    /**
     * Muestra errores de validación en el formulario
     */
    showErrors(form, errors) {
        FormHelper.clearErrors(form);
        Object.entries(errors).forEach(([field, messages]) => {
            const input = form.querySelector(`[name="${field}"]`);
            if (input) {
                input.classList.add('is-invalid');
                const feedback = document.createElement('div');
                feedback.className = 'invalid-feedback';
                feedback.textContent = Array.isArray(messages) ? messages[0] : messages;
                input.parentNode.appendChild(feedback);
            }
        });
    },

    clearErrors(form) {
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        form.querySelectorAll('.invalid-feedback').forEach(el => el.remove());
    },

    setLoading(btn, loading) {
        if (loading) {
            btn.dataset.originalText = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Guardando...';
            btn.disabled = true;
        } else {
            btn.innerHTML = btn.dataset.originalText || 'Guardar';
            btn.disabled = false;
        }
    }
};