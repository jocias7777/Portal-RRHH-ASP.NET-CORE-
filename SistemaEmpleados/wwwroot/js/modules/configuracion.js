/* ══════════════════════════════════════════════
   SICE — Módulo Configuración (Deptos & Puestos)
══════════════════════════════════════════════ */

window.Configuracion = (() => {

    let departamentos = [];

    function init() {
        cargarDeptos();
        cargarPuestos();
        initEvents();
        cargarDepartamentosSelect();
    }

    // ══════════════════════════════════════
    // CARGA DE DATOS → CARDS
    // ══════════════════════════════════════

    async function cargarDeptos() {
        try {
            const res = await Http.get('/Configuracion/GetDepartamentos');
            if (!res.success) return;
            // Mapear nombres de campo al formato que espera ConfigCards
            const data = res.data.map(d => ({
                id: d.id,
                codigo: d.codigo,
                nombre: d.nombre,
                descripcion: d.descripcion,
                activo: d.activo,
                totalPuestos: d.totalPuestos,
                totalEmpleados: d.totalEmpleados
            }));
            window.ConfigCards?.renderDeptos(data);
            // Atar clicks de editar/eliminar
            bindDeptoActions();
        } catch (_) { }
    }

    async function cargarPuestos(departamentoId) {
        try {
            const url = departamentoId
                ? `/Configuracion/GetPuestos?departamentoId=${departamentoId}`
                : '/Configuracion/GetPuestos';
            const res = await Http.get(url);
            if (!res.success) return;
            const data = res.data.map(p => ({
                id: p.id,
                codigo: p.codigo,
                nombre: p.nombre,
                departamento: p.departamento,
                departamentoId: p.departamentoId,
                activo: p.activo,
                nivel: p.nivelJerarquico,
                salMin: p.salarioMinimo,
                salMax: p.salarioMaximo,
                totalEmpleados: p.totalEmpleados
            }));
            window.ConfigCards?.renderPuestos(data);
            bindPuestoActions();
        } catch (_) { }
    }

    // ── Atar botones de cards (delegación) ──
    function bindDeptoActions() {
        const container = document.getElementById('cardsDeptos');
        if (!container) return;
        // Remover listener anterior para evitar duplicados
        container.replaceWith(container.cloneNode(true));
        const fresh = document.getElementById('cardsDeptos');
        fresh.addEventListener('click', e => {
            const btnEdit = e.target.closest('.btn-edit');
            const btnDel = e.target.closest('.btn-delete');
            if (btnEdit) editarDepto(parseInt(btnEdit.dataset.id));
            if (btnDel) eliminarDepto(parseInt(btnDel.dataset.id));
        });
    }

    function bindPuestoActions() {
        const container = document.getElementById('cardsPuestos');
        if (!container) return;
        container.replaceWith(container.cloneNode(true));
        const fresh = document.getElementById('cardsPuestos');
        fresh.addEventListener('click', e => {
            const btnEdit = e.target.closest('.btn-edit');
            const btnDel = e.target.closest('.btn-delete');
            if (btnEdit) editarPuesto(parseInt(btnEdit.dataset.id));
            if (btnDel) eliminarPuesto(parseInt(btnDel.dataset.id));
        });
    }

    // ══════════════════════════════════════
    // SELECT DE DEPARTAMENTOS
    // ══════════════════════════════════════

    async function cargarDepartamentosSelect() {
        try {
            const res = await Http.get('/Configuracion/GetDepartamentos');
            if (!res.success) return;
            departamentos = res.data;

            // Filtro en toolbar de puestos
            const filtro = document.getElementById('filtroDeptoP');
            if (filtro) {
                filtro.querySelectorAll('option:not([value=""])').forEach(o => o.remove());
                departamentos.forEach(d => {
                    const opt = document.createElement('option');
                    opt.value = d.id; opt.textContent = d.nombre;
                    filtro.appendChild(opt);
                });
            }

            poblarSelectDepto();
        } catch (_) { }
    }

    function poblarSelectDepto() {
        const sel = document.getElementById('puestoDepto');
        if (!sel) return;
        sel.querySelectorAll('option:not([value=""])').forEach(o => o.remove());
        departamentos
            .filter(d => d.activo)
            .forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.id; opt.textContent = d.nombre;
                sel.appendChild(opt);
            });
    }

    // ══════════════════════════════════════
    // EVENTOS
    // ══════════════════════════════════════

    function initEvents() {
        document.getElementById('btnNuevoDepto')?.addEventListener('click', () => {
            abrirModalDepto(null);
        });

        document.getElementById('btnNuevoPuesto')?.addEventListener('click', () => {
            abrirModalPuesto(null);
        });

        document.getElementById('filtroDeptoP')?.addEventListener('change', e => {
            cargarPuestos(e.target.value || null);
        });

        document.getElementById('formDepto')?.addEventListener('submit', async e => {
            e.preventDefault();
            await guardarDepto();
        });

        document.getElementById('formPuesto')?.addEventListener('submit', async e => {
            e.preventDefault();
            await guardarPuesto();
        });
    }

    // ══════════════════════════════════════
    // DEPARTAMENTOS — CRUD
    // ══════════════════════════════════════

    function abrirModalDepto(data) {
        document.getElementById('deptoId').value = data?.id ?? 0;
        document.getElementById('deptoCodigo').value = data?.codigo ?? '';
        document.getElementById('deptoNombre').value = data?.nombre ?? '';
        document.getElementById('deptoDescripcion').value = data?.descripcion ?? '';
        document.getElementById('deptoActivo').checked = data?.activo ?? true;
        document.getElementById('modalDeptoTitulo').textContent =
            data ? 'Editar Departamento' : 'Nuevo Departamento';

        new bootstrap.Modal(document.getElementById('modalDepto')).show();
    }

    async function editarDepto(id) {
        try {
            const res = await Http.get(`/Configuracion/GetDepartamento?id=${id}`);
            if (res.success) abrirModalDepto(res.data);
        } catch (_) { Notify.error('Error al cargar.'); }
    }

    async function guardarDepto() {
        const btn = document.getElementById('btnGuardarDepto');
        const id = parseInt(document.getElementById('deptoId').value) || 0;

        const vm = {
            id,
            codigo: document.getElementById('deptoCodigo').value,
            nombre: document.getElementById('deptoNombre').value,
            descripcion: document.getElementById('deptoDescripcion').value || null,
            activo: document.getElementById('deptoActivo').checked
        };

        if (!vm.codigo || !vm.nombre) {
            Notify.warning('Código y nombre son requeridos.');
            return;
        }

        btn.disabled = true;
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Configuracion/SaveDepartamento', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(vm)
            }).then(r => r.json());

            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('modalDepto'))?.hide();
                Notify.success(res.message);
                await cargarDeptos();
                await cargarDepartamentosSelect();
            } else {
                Notify.error(res.message);
            }
        } catch (_) {
            Notify.error('Error de conexión.');
        } finally {
            btn.disabled = false;
        }
    }

    async function eliminarDepto(id) {
        const ok = await Confirm.delete('este departamento');
        if (!ok) return;
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Configuracion/DeleteDepartamento?id=${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());
            if (res.success) {
                Notify.success(res.message);
                await cargarDeptos();
                await cargarDepartamentosSelect();
            } else {
                Notify.error(res.message);
            }
        } catch (_) { Notify.error('Error.'); }
    }

    // ══════════════════════════════════════
    // PUESTOS — CRUD
    // ══════════════════════════════════════

    function abrirModalPuesto(data) {
        poblarSelectDepto();
        document.getElementById('puestoId').value = data?.id ?? 0;
        document.getElementById('puestoCodigo').value = data?.codigo ?? '';
        document.getElementById('puestoNombre').value = data?.nombre ?? '';
        document.getElementById('puestoDepto').value = data?.departamentoId ?? '';
        document.getElementById('puestoSalMin').value = data?.salarioMinimo ?? '';
        document.getElementById('puestoSalMax').value = data?.salarioMaximo ?? '';
        document.getElementById('puestoNivel').value = data?.nivelJerarquico ?? 1;
        document.getElementById('puestoDescripcion').value = data?.descripcion ?? '';
        document.getElementById('puestoActivo').checked = data?.activo ?? true;
        document.getElementById('modalPuestoTitulo').textContent =
            data ? 'Editar Puesto' : 'Nuevo Puesto';

        new bootstrap.Modal(document.getElementById('modalPuesto')).show();
    }

    async function editarPuesto(id) {
        try {
            const res = await Http.get(`/Configuracion/GetPuesto?id=${id}`);
            if (res.success) abrirModalPuesto(res.data);
        } catch (_) { Notify.error('Error al cargar.'); }
    }

    async function guardarPuesto() {
        const btn = document.getElementById('btnGuardarPuesto');
        const id = parseInt(document.getElementById('puestoId').value) || 0;

        const vm = {
            id,
            codigo: document.getElementById('puestoCodigo').value,
            nombre: document.getElementById('puestoNombre').value,
            departamentoId: parseInt(document.getElementById('puestoDepto').value) || 0,
            salarioMinimo: parseFloat(document.getElementById('puestoSalMin').value) || 0,
            salarioMaximo: parseFloat(document.getElementById('puestoSalMax').value) || 0,
            nivelJerarquico: parseInt(document.getElementById('puestoNivel').value) || 1,
            descripcion: document.getElementById('puestoDescripcion').value || null,
            activo: document.getElementById('puestoActivo').checked
        };

        if (!vm.codigo || !vm.nombre || !vm.departamentoId) {
            Notify.warning('Código, nombre y departamento son requeridos.');
            return;
        }

        btn.disabled = true;
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Configuracion/SavePuesto', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(vm)
            }).then(r => r.json());

            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('modalPuesto'))?.hide();
                Notify.success(res.message);
                await cargarPuestos();
            } else {
                Notify.error(res.message);
            }
        } catch (_) {
            Notify.error('Error de conexión.');
        } finally {
            btn.disabled = false;
        }
    }

    async function eliminarPuesto(id) {
        const ok = await Confirm.delete('este puesto');
        if (!ok) return;
        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Configuracion/DeletePuesto?id=${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());
            if (res.success) {
                Notify.success(res.message);
                await cargarPuestos();
            } else {
                Notify.error(res.message);
            }
        } catch (_) { Notify.error('Error.'); }
    }

    return { init, editarDepto, eliminarDepto, editarPuesto, eliminarPuesto };
})();

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('cardsDeptos')) Configuracion.init();
});