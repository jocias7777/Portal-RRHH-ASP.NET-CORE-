/* ══════════════════════════════════════════════
   SICE — Módulo Vacaciones y Ausencias
══════════════════════════════════════════════ */

// ── Tabs ──
window.VacacionesTabs = {
    show(tab) {
        document.querySelectorAll('.tab-content-vac').forEach(el => el.classList.remove('active'));
        document.querySelectorAll('.module-tab').forEach(el => el.classList.remove('active'));
        document.getElementById(`tab-${tab}`)?.classList.add('active');
        document.querySelector(`[data-tab="${tab}"]`)?.classList.add('active');

        if (tab === 'ausencias' && !VacacionesAusencias._loaded) VacacionesAusencias.cargar();
        if (tab === 'saldos' && !VacacionesSaldos._loaded) VacacionesSaldos.cargar();
    }
};

// ══════════════════════════════════════════════
// MÓDULO PRINCIPAL — Solicitudes de Vacaciones
// ══════════════════════════════════════════════
window.Vacaciones = (() => {

    let table = null;

    function init() {
        limpiarModal();
        initDataTable();
        initEvents();
        loadDepartamentosFilter();
        cargarKPIs();
    }

    function limpiarModal() {
        document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('padding-right');
        document.body.style.removeProperty('overflow');
        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl) {
            const inst = bootstrap.Modal.getInstance(modalEl);
            if (inst) inst.dispose();
            modalEl.style.display = 'none';
            modalEl.classList.remove('show');
            modalEl.removeAttribute('aria-modal');
            modalEl.setAttribute('aria-hidden', 'true');
        }
    }

    function cerrarModal() {
        const modalEl = document.getElementById('modalEmpleado');
        if (!modalEl) return;
        document.activeElement?.blur();
        const inst = bootstrap.Modal.getInstance(modalEl);
        inst ? inst.hide() : limpiarModal();
    }

    function initDataTable() {
        if ($.fn.DataTable.isDataTable('#tablaVacaciones')) {
            $('#tablaVacaciones').DataTable().destroy();
        }

        table = $('#tablaVacaciones').DataTable({
            dom: 'rtip',
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Vacaciones/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify(buildRequest(d)),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            },
            columns: [
                {
                    data: null, orderable: false,
                    render: (_, __, ___, meta) => `<span class="row-num">${meta.row + 1}</span>`
                },
                {
                    data: null, name: 'empleado',
                    render: row => `
                    <div>
                        <div style="font-size:13px;font-weight:600;color:var(--color-dark)">${row.nombreEmpleado}</div>
                    </div>`
                },
                { data: 'departamento' },
                { data: 'fechaInicio' },
                { data: 'fechaFin' },
                { data: 'diasHabiles', render: v => `<span class="monospace fw-600">${v}</span>` },
                { data: 'fechaSolicitud' },
                { data: 'estado', render: v => `<span class="badge-vac-${v.toLowerCase()}">${v}</span>` },
                {
                    data: 'aprobadoPor',
                    render: v => v ? `<small>${v}</small>` : '<span class="text-muted">—</span>'
                },
                {
                    data: 'id', orderable: false, className: 'text-center',
                    render: (id, _, row) => {
                        const esPendiente = row.estado === 'Pendiente';
                        return `
                        <div style="display:inline-flex;align-items:center;gap:6px">
                            ${esPendiente ? `
                            <button class="btn-action btn-ver" title="Aprobar" onclick="Vacaciones.aprobar(${id})">
                                <i class="fa-solid fa-check"></i>
                            </button>
                            <button class="btn-action btn-eliminar" title="Rechazar" onclick="Vacaciones.rechazar(${id})" style="background:#FFF7ED;color:#C2410C">
                                <i class="fa-solid fa-xmark"></i>
                            </button>` : ''}
                            <button class="btn-action btn-editar" title="Editar" onclick="Vacaciones.openForm(${id})">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn-action btn-eliminar" title="Eliminar" onclick="Vacaciones.eliminar(${id})">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>`;
                    }
                }
            ],
            language: { url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json' },
            pageLength: 15,
            lengthMenu: [10, 15, 25, 50],
            order: [[6, 'desc']],
            drawCallback(settings) {
                const info = settings.json;
                const el = document.getElementById('totalVacaciones');
                if (el && info) el.textContent = `${info.recordsFiltered} solicitudes`;
            }
        });
    }

    function buildRequest(d) {
        return {
            draw: d.draw, start: d.start, length: d.length,
            searchValue: d.search?.value || '',
            orderColumn: d.columns?.[d.order?.[0]?.column]?.name || '',
            orderDir: d.order?.[0]?.dir || 'desc',
            departamentoId: document.getElementById('filtroDeptoVac')?.value || null,
            estado: document.getElementById('filtroEstadoVac')?.value || ''
        };
    }

    async function loadDepartamentosFilter() {
        try {
            const res = await Http.get('/api/departamentos');
            if (!res.success) return;
            const sel = document.getElementById('filtroDeptoVac');
            if (!sel) return;
            sel.querySelectorAll('option:not([value=""])').forEach(o => o.remove());
            res.data.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.id;
                opt.textContent = d.nombre;
                sel.appendChild(opt);
            });
        } catch (_) { }
    }

    async function cargarKPIs() {
        try {
            const res = await fetch('/Vacaciones/GetKPIs', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            if (!res.success) return;
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
            set('kpiTotalSolicitudes', res.data.totalSolicitudes);
            set('kpiPendientes', res.data.pendientes);
            set('kpiAprobadas', res.data.aprobadas);
            set('kpiEnVacaciones', res.data.enVacacionesHoy);
        } catch (_) { }
    }

    function initEvents() {
        const btnNuevo = document.getElementById('btnNuevaVacacion');
        if (btnNuevo) {
            const clone = btnNuevo.cloneNode(true);
            btnNuevo.parentNode.replaceChild(clone, btnNuevo);
            clone.addEventListener('click', () => openForm(null));
        }

        ['filtroDeptoVac', 'filtroEstadoVac'].forEach(id => {
            document.getElementById(id)?.addEventListener('change', () => table?.ajax.reload());
        });

        document.getElementById('btnLimpiarVac')?.addEventListener('click', () => {
            document.getElementById('filtroDeptoVac').value = '';
            document.getElementById('filtroEstadoVac').value = '';
            table?.ajax.reload();
        });

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.initialized) {
            modalEl.dataset.initialized = 'true';
            modalEl.addEventListener('submit', async e => {
                e.preventDefault();
                if (e.target.id === 'formVacacion') await guardar(e.target);
                if (e.target.id === 'formAusencia') await VacacionesAusencias.guardar(e.target);
            });
            modalEl.addEventListener('hidden.bs.modal', () => {
                document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('padding-right');
                document.body.style.removeProperty('overflow');
            });
        }
    }

    function getModal() {
        const modalEl = document.getElementById('modalEmpleado');
        if (!modalEl) return null;
        let inst = bootstrap.Modal.getInstance(modalEl);
        if (!inst) inst = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true });
        return inst;
    }

    async function openForm(id) {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border" style="color:var(--vac-color);width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        getModal()?.show();

        try {
            const url = id ? `/Vacaciones/Form?id=${id}` : '/Vacaciones/Form';
            const html = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.text());
            body.innerHTML = html;
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar el formulario</p>
            </div>`;
        }
    }

    async function guardar(form) {
        const btn = form.querySelector('#btnGuardarVacacion');
        FormHelper.clearErrors(form);
        FormHelper.setLoading(btn, true);

        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0 ? `/Vacaciones/Edit/${id}` : '/Vacaciones/Create';

        data.Id = id;
        data.EmpleadoId = parseInt(data.EmpleadoId) || 0;
        data.Estado = parseInt(data.Estado) || 0;

        try {
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(data)
            }).then(r => r.json());

            if (res.success) {
                cerrarModal();
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarKPIs();
            } else {
                Notify.error(res.message || 'Error al guardar');
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    async function aprobar(id) {
        const ok = await Confirm.custom(
            '¿Aprobar solicitud?',
            'Se aprobará la solicitud de vacaciones del empleado.',
            '<i class="fa-solid fa-check me-1"></i> Aprobar'
        );
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Vacaciones/Aprobar/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarKPIs();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    async function rechazar(id) {
        const { value: motivo } = await Swal.fire({
            title: 'Rechazar solicitud',
            input: 'textarea',
            inputLabel: 'Motivo del rechazo',
            inputPlaceholder: 'Escribe el motivo...',
            inputAttributes: { 'aria-label': 'Motivo' },
            showCancelButton: true,
            confirmButtonText: 'Rechazar',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'Cancelar',
            inputValidator: (value) => { if (!value) return 'El motivo es obligatorio'; }
        });
        if (!motivo) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Vacaciones/Rechazar/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(motivo)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarKPIs();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    async function eliminar(id) {
        const ok = await Confirm.delete('esta solicitud');
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Vacaciones/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarKPIs();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    return { init, openForm, aprobar, rechazar, eliminar, cerrarModal };

})();

// ══════════════════════════════════════════════
// SUBMÓDULO — Ausencias
// ══════════════════════════════════════════════
window.VacacionesAusencias = (() => {
    let _loaded = false;
    let _todos = [];

    async function cargar() {
        _loaded = true;
        try {
            const res = await fetch('/Vacaciones/GetAusencias', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            _todos = res.data || [];
            renderTabla(_todos);
            document.getElementById('totalAusencias').textContent = `${_todos.length} ausencias`;
        } catch { }
    }

    function renderTabla(datos) {
        const tbody = document.getElementById('bodyAusencias');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `<tr><td colspan="9" class="text-center text-muted py-4">No hay ausencias registradas</td></tr>`;
            return;
        }

        tbody.innerHTML = datos.map((a, i) => `
            <tr>
                <td><span class="row-num">${i + 1}</span></td>
                <td><span style="font-weight:600;font-size:13px">${a.nombreEmpleado}</span></td>
                <td><span class="badge" style="background:#EFF6FF;color:#1D4ED8;font-size:11px">${a.tipo}</span></td>
                <td>${a.fechaInicio}</td>
                <td>${a.fechaFin}</td>
                <td><span class="monospace fw-600">${a.totalDias}</span></td>
                <td><span class="${a.justificada ? 'badge-justificada' : 'badge-injustificada'}">${a.justificada ? 'Sí' : 'No'}</span></td>
                <td><small class="text-muted">${a.observacion || '—'}</small></td>
                <td class="text-center">
                    <button class="btn-action btn-eliminar" title="Eliminar" onclick="VacacionesAusencias.eliminar(${a.id})">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            </tr>`).join('');
    }

    function filtrar(tipo, justificada) {
        let datos = [..._todos];
        if (tipo) datos = datos.filter(a => a.tipo === tipo);
        if (justificada !== '') datos = datos.filter(a => String(a.justificada) === justificada);
        renderTabla(datos);
    }

    async function nuevo() {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border" style="color:var(--vac-color);width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        let inst = bootstrap.Modal.getInstance(modalEl);
        if (!inst) inst = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true });
        inst.show();

        try {
            const html = await fetch('/Vacaciones/FormAusencia', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
            body.innerHTML = html;
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger"><p>Error al cargar</p></div>`;
        }
    }

    async function guardar(form) {
        const btn = form.querySelector('#btnGuardarAusencia');
        if (btn) btn.disabled = true;

        const data = FormHelper.serialize(form);
        data.EmpleadoId = parseInt(data.EmpleadoId) || 0;
        data.Justificada = data.Justificada === 'true';

        try {
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Vacaciones/CreateAusencia', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(data)
            }).then(r => r.json());

            if (res.success) {
                const inst = bootstrap.Modal.getInstance(document.getElementById('modalEmpleado'));
                inst?.hide();
                Notify.success(res.message);
                cargar();
            } else {
                Notify.error(res.message || 'Error al guardar');
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            if (btn) btn.disabled = false;
        }
    }

    async function eliminar(id) {
        const ok = await Confirm.delete('esta ausencia');
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Vacaciones/DeleteAusencia/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) { Notify.success(res.message); cargar(); }
            else Notify.error(res.message);
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    return { _loaded: false, cargar, filtrar, nuevo, guardar, eliminar };
})();

// ══════════════════════════════════════════════
// SUBMÓDULO — Saldos de Vacaciones
// ══════════════════════════════════════════════
window.VacacionesSaldos = (() => {
    let _loaded = false;
    let _todos = [];

    async function cargar() {
        _loaded = true;
        try {
            const res = await fetch('/Vacaciones/GetSaldos', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            _todos = res.data || [];
            renderTabla(_todos);
            document.getElementById('totalSaldos').textContent = `${_todos.length} empleados`;
        } catch { }
    }

    function renderTabla(datos) {
        const tbody = document.getElementById('bodySaldos');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted py-4">No hay datos de saldos</td></tr>`;
            return;
        }

        tbody.innerHTML = datos.map((s, i) => {
            const claseDisp = s.diasDisponibles > 5 ? 'dias-ok' : s.diasDisponibles > 0 ? 'dias-alerta' : 'dias-cero';
            return `
            <tr>
                <td><span class="row-num">${i + 1}</span></td>
                <td><span style="font-weight:600;font-size:13px">${s.nombreEmpleado}</span></td>
                <td>${s.departamento}</td>
                <td><small>${s.antiguedad}</small></td>
                <td class="text-center"><span class="monospace">${s.diasCorresponden}</span></td>
                <td class="text-center"><span class="monospace">${s.diasTomados}</span></td>
                <td class="text-center"><span class="monospace">${s.diasPendientes}</span></td>
                <td class="text-center"><span class="${claseDisp}">${s.diasDisponibles}</span></td>
            </tr>`;
        }).join('');
    }

    function filtrar(texto, depto) {
        let datos = [..._todos];
        if (depto) datos = datos.filter(s => s.departamento === depto);
        if (texto) datos = datos.filter(s => s.nombreEmpleado.toLowerCase().includes(texto.toLowerCase()));
        renderTabla(datos);
    }

    return { _loaded: false, cargar, filtrar };
})();

// ── Eventos de filtros ausencias y saldos ──
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('tablaVacaciones')) Vacaciones.init();

    document.getElementById('filtroTipoAusencia')?.addEventListener('change', function () {
        VacacionesAusencias.filtrar(this.value, document.getElementById('filtroJustificada')?.value ?? '');
    });
    document.getElementById('filtroJustificada')?.addEventListener('change', function () {
        VacacionesAusencias.filtrar(document.getElementById('filtroTipoAusencia')?.value ?? '', this.value);
    });
    document.getElementById('btnNuevaAusencia')?.addEventListener('click', () => VacacionesAusencias.nuevo());

    document.getElementById('buscarEmpleadoSaldo')?.addEventListener('input', function () {
        VacacionesSaldos.filtrar(this.value, document.getElementById('filtroDeptoSaldos')?.value ?? '');
    });
});

document.addEventListener('spaNavigated', () => {
    if (document.getElementById('tablaVacaciones')) {
        Vacaciones.init();
        VacacionesAusencias._loaded = false;
        VacacionesSaldos._loaded = false;
    }
});