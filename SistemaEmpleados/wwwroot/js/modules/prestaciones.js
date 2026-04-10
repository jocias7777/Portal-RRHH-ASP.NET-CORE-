/* ══════════════════════════════════════════════
   SICE — Módulo Prestaciones Laborales
══════════════════════════════════════════════ */

// ── Tabs ──
window.PrestacionesTabs = {
    show(tab) {
        document.querySelectorAll('.tab-content-prest').forEach(el => el.classList.remove('active'));
        document.querySelectorAll('.module-tab').forEach(el => el.classList.remove('active'));
        document.getElementById(`tab-${tab}`)?.classList.add('active');
        document.querySelector(`[data-tab="${tab}"]`)?.classList.add('active');

        if (tab === 'finiquitos' && !PrestacionesFiniquitos._loaded)
            PrestacionesFiniquitos.cargar();
    }
};

function firePrestModal(options) {
    return Swal.fire({
        buttonsStyling: false,
        customClass: {
            popup: 'prest-swal-popup',
            actions: 'prest-swal-actions',
            confirmButton: 'prest-swal-btn prest-swal-btn-confirm',
            cancelButton: 'prest-swal-btn prest-swal-btn-cancel'
        },
        ...options
    });
}

// ══════════════════════════════════════════════
// MÓDULO PRINCIPAL — Prestaciones
// ══════════════════════════════════════════════
window.Prestaciones = (() => {
    let table = null;

    function init() {
        limpiarModal();
        initDataTable();
        initEvents();
        loadFiltros();
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
        if ($.fn.DataTable.isDataTable('#tablaPrestaciones'))
            $('#tablaPrestaciones').DataTable().destroy();

        table = $('#tablaPrestaciones').DataTable({
            dom: 'rtip',
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Prestaciones/GetData',
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
                    data: null,
                    render: row => `<div style="font-size:13px;font-weight:600;color:var(--color-dark)">${row.nombreEmpleado}</div>`
                },
                { data: 'departamento' },
                {
                    data: 'tipo',
                    render: v => `<span class="tipo-badge tipo-${v.toLowerCase()}">${v}</span>`
                },
                { data: 'periodo', render: v => `<span class="monospace">${v}</span>` },
                { data: 'mesesTrabajados', render: v => `<span class="monospace">${v}</span>` },
                {
                    data: 'salarioBase',
                    render: v => `<span class="monospace">Q ${Number(v).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span>`
                },
                {
                    data: 'monto',
                    render: v => `<span class="monospace fw-600" style="color:#065F46">Q ${Number(v).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span>`
                },
                {
                    data: 'estado',
                    render: v => `<span class="badge-prest-${v.toLowerCase()}">${v}</span>`
                },
                {
                    data: 'fechaPago',
                    render: v => v || '<span class="text-muted">—</span>'
                },
                {
                    data: 'id', orderable: false, className: 'text-center',
                    render: (id, _, row) => {
                        const esPagado = row.estado === 'Pagado';
                        return `
        <div style="display:inline-flex;align-items:center;gap:6px">
            <button class="btn-action" title="Ver detalle"
                onclick="Prestaciones.verDetalle(${id})">
                <i class="fa-solid fa-eye"></i>
            </button>
            ${!esPagado ? `
            <button class="btn-action" title="Marcar pagado"
                onclick="Prestaciones.marcarPagado(${id})">
                <i class="fa-solid fa-money-bill-wave"></i>
            </button>` : ''}
            <button class="btn-action" title="Editar"
                onclick="Prestaciones.openForm(${id})">
                <i class="fa-solid fa-pen"></i>
            </button>
            ${!esPagado ? `
            <button class="btn-action" title="Eliminar"
                onclick="Prestaciones.eliminar(${id})">
                <i class="fa-solid fa-trash"></i>
            </button>` : ''}

        </div>`;
                    }
                }
            ],
            language: { url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json' },
            pageLength: 15,
            lengthMenu: [10, 15, 25, 50],
            order: [[4, 'desc']],
            drawCallback(settings) {
                const info = settings.json;
                const el = document.getElementById('totalPrestaciones');
                if (el && info) el.textContent = `${info.recordsFiltered} registros`;
            }
        });
    }

    function buildRequest(d) {
        return {
            draw: d.draw, start: d.start, length: d.length,
            searchValue: d.search?.value || '',
            orderColumn: d.columns?.[d.order?.[0]?.column]?.name || '',
            orderDir: d.order?.[0]?.dir || 'desc',
            departamentoId: document.getElementById('filtroDepto')?.value || null,
            estado: document.getElementById('filtroEstado')?.value || '',
            tipoPrestacion: document.getElementById('filtroTipo')?.value || ''
        };
    }

    async function loadFiltros() {
        try {
            const res = await Http.get('/api/departamentos');
            if (!res.success) return;
            const sel = document.getElementById('filtroDepto');
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
            const res = await fetch('/Prestaciones/GetKPIs', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            if (!res.success) return;
            const set = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
            set('kpiTotalPrest', res.data.totalRegistros);
            set('kpiPendientesPrest', res.data.pendientes);
            set('kpiTotalPagadoPrest', `Q ${Number(res.data.totalPagado).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`);
            set('kpiMontoPendiente', `Q ${Number(res.data.montoPendiente).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`);
        } catch (_) { }
    }

    function initEvents() {
        const btnNuevo = document.getElementById('btnNuevaPrestacion');
        if (btnNuevo) {
            const clone = btnNuevo.cloneNode(true);
            btnNuevo.parentNode.replaceChild(clone, btnNuevo);
            clone.addEventListener('click', () => openForm(null));
        }

        const btnGenerar = document.getElementById('btnGenerarAnio');
        if (btnGenerar) {
            const clone = btnGenerar.cloneNode(true);
            btnGenerar.parentNode.replaceChild(clone, btnGenerar);
            clone.addEventListener('click', generarAnio);
        }

        const bindUniqueClick = (id, handler) => {
            const btn = document.getElementById(id);
            if (!btn) return;
            const clone = btn.cloneNode(true);
            btn.parentNode.replaceChild(clone, btn);
            clone.addEventListener('click', handler);
        };

        bindUniqueClick('btnCalcularFiniquito', () => PrestacionesFiniquitos.calcular());

        // ── Botón PDF ──
        bindUniqueClick('btnGenerarPDF', () => {
            const tipo = document.getElementById('filtroTipo')?.value || '';
            const estado = document.getElementById('filtroEstado')?.value || '';
            const url = `/Prestaciones/GenerarPDF?tipo=${tipo}&estado=${estado}`;
            window.open(url, '_blank');
        });

        // ── Botón Marcar todas pagadas ──
        bindUniqueClick('btnMarcarTodasPagadas', async () => {
            const tipo = document.getElementById('filtroTipo')?.value || '';

            const { value: fecha } = await firePrestModal({
                title: 'Marcar todas como pagadas',
                html: `
        <p style="font-size:13px;color:#64748B;margin-bottom:12px">
            Se marcarán como pagadas todas las prestaciones
            <strong>${tipo || 'de todos los tipos'}</strong>
            que aún no estén pagadas.
        </p>
        <input type="date" id="fechaPagoMasivo" class="swal2-input"
            value="${new Date().toISOString().split('T')[0]}">`,
                confirmButtonText: '<i class="fa-solid fa-check-double me-1"></i> Confirmar',
                confirmButtonColor: '#065F46',
                cancelButtonText: 'Cancelar',
                showCancelButton: true,
                preConfirm: () => document.getElementById('fechaPagoMasivo').value
            });

            if (!fecha) return;

            try {
                const token = document.querySelector(
                    'input[name="__RequestVerificationToken"]')?.value || '';
                const res = await fetch('/Prestaciones/MarcarTodasPagadas', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({
                        tipo,
                        periodo: null,
                        fechaPago: fecha
                    })
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
        });

        // ── Botón Pagar todas ──
        bindUniqueClick('btnPagarTodas', async () => {
            const tipo = document.getElementById('filtroTipo')?.value || '';

            const { value: formValues } = await firePrestModal({
                title: 'Pago masivo de prestaciones',
                html: `
        <p style="font-size:13px;color:#64748B;margin-bottom:16px">
            Se registrará el pago de <strong>todas las prestaciones calculadas</strong>
            ${tipo ? `de tipo <strong>${tipo}</strong>` : ''}.
        </p>
        <div style="text-align:left;margin-bottom:12px">
            <label style="font-size:12px;font-weight:600;color:#64748B;
                display:block;margin-bottom:4px">Fecha de pago</label>
            <input type="date" id="fechaPagoTotal" class="swal2-input"
                style="margin:0;width:100%"
                value="${new Date().toISOString().split('T')[0]}">
        </div>
        <div style="background:#FFFBEB;border:1px solid #FDE68A;border-radius:2px;
            padding:10px;font-size:12px;color:#92400E;text-align:left">
            <i class="fa-solid fa-circle-info me-1"></i>
            Esta acción no se puede revertir. Solo afecta prestaciones en estado Calculado.
        </div>`,
                confirmButtonText:
                    '<i class="fa-solid fa-money-bill-wave me-1"></i> Confirmar pago',
                confirmButtonColor: '#065F46',
                cancelButtonText: 'Cancelar',
                showCancelButton: true,
                width: 460,
                preConfirm: () => ({
                    tipo,
                    fechaPago: document.getElementById('fechaPagoTotal').value,
                    periodo: null
                })
            });

            if (!formValues) return;

            try {
                const token = document.querySelector(
                    'input[name="__RequestVerificationToken"]')?.value || '';
                const res = await fetch('/Prestaciones/MarcarTodasPagadas', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify(formValues)
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
        });




        ['filtroDepto', 'filtroEstado', 'filtroTipo'].forEach(id => {
            document.getElementById(id)?.addEventListener('change', () => table?.ajax.reload());
        });

        document.getElementById('btnLimpiar')?.addEventListener('click', () => {
            ['filtroDepto', 'filtroEstado', 'filtroTipo'].forEach(id => {
                const el = document.getElementById(id);
                if (el) el.value = '';
            });
            table?.ajax.reload();
        });

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.initialized) {
            modalEl.dataset.initialized = 'true';
            modalEl.addEventListener('submit', async e => {
                e.preventDefault();
                if (e.target.id === 'formPrestacion') await guardar(e.target);
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
            <div class="spinner-border" style="color:var(--prestcolor);width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;
        getModal()?.show();

        try {
            const url = id ? `/Prestaciones/Form?id=${id}` : '/Prestaciones/Form';
            const html = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } }).then(r => r.text());
            body.innerHTML = html;
            initPrestacionFormAutoCalc(body);
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar el formulario</p>
            </div>`;
        }
    }

    function initPrestacionFormAutoCalc(container) {
        const form = container.querySelector('#formPrestacion');
        if (!form) return;

        const selectEmp = form.querySelector('#selectEmpleadoPrest');
        const inputPeriodo = form.querySelector('[name="Periodo"]');
        const inputMeses = form.querySelector('#inputMeses');
        const inputSalario = form.querySelector('#inputSalario');
        const inputMonto = form.querySelector('#inputMonto');
        const selectTipo = form.querySelector('[name="Tipo"]');
        const btnCalcular = form.querySelector('#btnCalcularPrest');
        const divInfo = form.querySelector('#empleadoInfoBox');
        const calculoBox = form.querySelector('#calculoBox');

        let ultimoCalculo = null;

        if (calculoBox) calculoBox.style.display = 'none';
        if (divInfo) divInfo.style.display = 'none';

        const setMontoPorTipo = (c) => {
            if (!c || !inputMonto || !selectTipo) return;
            const tipo = parseInt(selectTipo.value);
            const montos = {
                0: c.aguinaldo,
                1: c.bono14,
                2: c.indemnizacion,
                3: c.totalFiniquito
            };
            if (montos[tipo] !== undefined) inputMonto.value = montos[tipo];
        };

        const pintar = (c) => {
            ultimoCalculo = c;
            if (inputMeses) inputMeses.value = c.mesesTrabajados ?? 0;
            if (inputSalario) inputSalario.value = c.salarioBase ?? 0;
            setMontoPorTipo(c);

            const n = id => form.querySelector(id);
            if (n('#calcNombre')) n('#calcNombre').textContent = c.nombreEmpleado || '';
            if (n('#calcSalario')) n('#calcSalario').textContent = `Q ${Number(c.salarioBase || 0).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`;
            if (n('#calcMeses')) n('#calcMeses').textContent = c.mesesTrabajados ?? 0;
            if (n('#calcAguinaldo')) n('#calcAguinaldo').textContent = `Q ${Number(c.aguinaldo || 0).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`;
            if (n('#calcBono14')) n('#calcBono14').textContent = `Q ${Number(c.bono14 || 0).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`;
            if (n('#calcIndem')) n('#calcIndem').textContent = `Q ${Number(c.indemnizacion || 0).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`;
            if (n('#calcTotal')) n('#calcTotal').textContent = `Q ${Number(c.totalFiniquito || 0).toLocaleString('es-GT', { minimumFractionDigits: 2 })}`;
            if (calculoBox) calculoBox.style.display = 'block';

            if (divInfo) {
                divInfo.style.display = 'block';
                divInfo.innerHTML = `
                    <div style="background:#F8FAFC;border:1px solid #E2E8F0;border-radius:2px;padding:8px 12px;font-size:12px;display:flex;gap:16px;flex-wrap:wrap">
                        <div><span style="color:#64748B">Meses en período:</span> <strong>${c.mesesTrabajados ?? 0}</strong></div>
                        <div><span style="color:#64748B">Fecha ingreso:</span> <strong>${c.fechaIngreso ? new Date(c.fechaIngreso).toLocaleDateString('es-GT') : '—'}</strong></div>
                        <div><span style="color:#64748B">Departamento:</span> <strong>${c.departamento || '—'}</strong></div>
                    </div>`;
            }
        };

        const pintarSoloMeses = (c) => {
            if (!c) return;
            if (inputMeses) inputMeses.value = c.mesesTrabajados ?? 0;
        };

        const fetchCalculo = async () => {
            const empleadoId = selectEmp?.value;
            const anio = parseInt(inputPeriodo?.value) || new Date().getFullYear();
            if (!empleadoId) return null;

            const res = await fetch(`/Prestaciones/Calcular?empleadoId=${empleadoId}&anio=${anio}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            if (!res.success || !res.data) return null;
            return res.data;
        };

        const recalcular = async (showError = false) => {
            if (!selectEmp?.value) {
                if (showError) Notify.warning('Selecciona un empleado primero.');
                return;
            }

            try {
                const data = await fetchCalculo();
                if (!data) {
                    if (showError) Notify.error('No se pudo calcular.');
                    return;
                }

                // Solo al presionar "Calcular" se muestra el detalle visual completo.
                pintar(data);
            } catch {
                if (showError) Notify.error('Error al calcular.');
            }
        };

        const autollenarMeses = async () => {
            if (!selectEmp?.value) {
                if (inputMeses) inputMeses.value = 0;
                return;
            }
            try {
                const data = await fetchCalculo();
                if (!data) return;
                pintarSoloMeses(data);
            } catch {
                // silencio en autollenado
            }
        };

        btnCalcular?.addEventListener('click', () => recalcular(true));
        selectEmp?.addEventListener('change', autollenarMeses);
        inputPeriodo?.addEventListener('change', autollenarMeses);
        inputPeriodo?.addEventListener('blur', autollenarMeses);
        selectTipo?.addEventListener('change', () => setMontoPorTipo(ultimoCalculo));

        // Al abrir el modal, solo autollenar meses (sin mostrar cards de cálculo)
        if (selectEmp?.value) autollenarMeses();
    }

    async function guardar(form) {
        const btn = form.querySelector('#btnGuardarPrest');
        FormHelper.clearErrors(form);
        FormHelper.setLoading(btn, true);

        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0 ? `/Prestaciones/Edit/${id}` : '/Prestaciones/Create';

        data.Id = id;
        data.EmpleadoId = parseInt(data.EmpleadoId) || 0;
        data.Tipo = parseInt(data.Tipo) || 0;
        data.Periodo = parseInt(data.Periodo) || new Date().getFullYear();
        data.MesesTrabajados = parseInt(data.MesesTrabajados) || 0;
        data.SalarioBase = parseFloat(data.SalarioBase) || 0;
        data.Monto = parseFloat(data.Monto) || 0;
        data.Estado = parseInt(data.Estado) || 0;
        data.FechaPago = data.FechaPago || null;
        data.Observacion = data.Observacion || null;

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
                if (Array.isArray(res.errors) && res.errors.length) {
                    Notify.warning(res.errors[0]);
                }
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    async function marcarPagado(id) {
        const { value: fecha } = await firePrestModal({
            title: 'Marcar como pagado',
            html: `<input type="date" id="fechaPagoInput" class="swal2-input"
                value="${new Date().toISOString().split('T')[0]}">`,
            confirmButtonText: '<i class="fa-solid fa-check me-1"></i> Confirmar pago',
            confirmButtonColor: '#16A34A',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            preConfirm: () => document.getElementById('fechaPagoInput').value
        });
        if (!fecha) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Prestaciones/MarcarPagado/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(new Date(fecha))
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

    async function generarAnio() {
        const anioActual = new Date().getFullYear();
        const filtroDepto = document.getElementById('filtroDepto');
        const opcionesDepto = filtroDepto
            ? Array.from(filtroDepto.options)
                .map(o => `<option value="${o.value}" ${o.selected ? 'selected' : ''}>${o.textContent}</option>`)
                .join('')
            : '<option value="">Todos los departamentos</option>';

        const { value: anio } = await firePrestModal({
            title: 'Generar prestaciones del año',
            html: `
            <p style="font-size:13px;color:#64748B;margin-bottom:16px">
                Se generarán <strong>Aguinaldo</strong> y <strong>Bono 14</strong>
                para los empleados activos del departamento seleccionado.
            </p>
            <div style="background:#FFFBEB;border:1px solid #FDE68A;border-radius:6px;
                padding:10px 14px;font-size:12px;color:#92400E;text-align:left;margin-bottom:12px">
                <i class="fa-solid fa-circle-info me-1"></i>
                Aguinaldo: diciembre | Bono 14: julio — Ley guatemalteca
            </div>
            <select id="deptoInput" class="swal2-input" style="margin:0 0 10px 0;width:100%">
                ${opcionesDepto}
            </select>
            <input type="number" id="anioInput" class="swal2-input"
                value="${anioActual}" min="2020" max="2099">`,
            confirmButtonText: '<i class="fa-solid fa-wand-magic-sparkles me-1"></i> Generar',
            confirmButtonColor: '#E05C2A',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 460,
            preConfirm: () => ({
                anio: parseInt(document.getElementById('anioInput').value),
                departamentoId: document.getElementById('deptoInput').value
                    ? parseInt(document.getElementById('deptoInput').value)
                    : null
            })
        });
        if (!anio) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Prestaciones/GenerarAnio', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(anio)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarKPIs();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al generar.');
        }
    }

    async function eliminar(id) {
        const ok = await Confirm.delete('esta prestación');
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Prestaciones/Delete/${id}`, {
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

    async function verDetalle(id) {
        try {
            const res = await fetch(`/Prestaciones/GetDetalle?id=${id}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            if (!res.success) { Notify.error(res.message); return; }
            const d = res.data;

            // Construir filas de vacaciones
            const filasVac = d.vacaciones.length
                ? d.vacaciones.map(v => `
                    <tr>
                        <td style="padding:5px 8px;font-size:12px">${v.fechaInicio}</td>
                        <td style="padding:5px 8px;font-size:12px">${v.fechaFin}</td>
                        <td style="padding:5px 8px;font-size:12px;text-align:center;color:#DC2626;font-weight:600">-${v.diasHabiles} días</td>
                        <td style="padding:5px 8px;font-size:12px;color:#64748B">${v.observacion || '—'}</td>
                    </tr>`).join('')
                : `<tr><td colspan="4" style="padding:8px;text-align:center;color:#94A3B8;font-size:12px">Sin vacaciones en este período</td></tr>`;

            // Construir filas de ausencias
            const filasAus = d.ausencias.length
                ? d.ausencias.map(a => `
                    <tr>
                        <td style="padding:5px 8px;font-size:12px">${a.fechaInicio}</td>
                        <td style="padding:5px 8px;font-size:12px">${a.fechaFin}</td>
                        <td style="padding:5px 8px;font-size:12px">${a.tipo}</td>
                        <td style="padding:5px 8px;font-size:12px;text-align:center;color:#DC2626;font-weight:600">-${a.totalDias} días</td>
                    </tr>`).join('')
                : `<tr><td colspan="4" style="padding:8px;text-align:center;color:#94A3B8;font-size:12px">Sin ausencias injustificadas en este período</td></tr>`;

            await firePrestModal({
                title: `<span style="font-size:16px">${d.tipo} ${d.periodo} — ${d.nombreEmpleado}</span>`,
                html: `
                <div style="text-align:left;font-size:13px">

                    <!-- Resumen período -->
                    <div style="background:#F8FAFC;border:1px solid #E2E8F0;border-radius:2px;
                        padding:12px 16px;margin-bottom:14px">
                        <div style="font-size:11px;font-weight:700;text-transform:uppercase;
                            letter-spacing:0.5px;color:#64748B;margin-bottom:10px">
                            Período de cálculo
                        </div>
                        <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
                            <div>
                                <div style="font-size:11px;color:#94A3B8">Inicio período</div>
                                <div style="font-weight:600">${d.inicioPeriodo}</div>
                            </div>
                            <div>
                                <div style="font-size:11px;color:#94A3B8">Fin período</div>
                                <div style="font-weight:600">${d.finPeriodo}</div>
                            </div>
                        </div>
                    </div>

                    <!-- Cálculo de días -->
                    <div style="background:#F8FAFC;border:1px solid #E2E8F0;border-radius:2px;
                        padding:12px 16px;margin-bottom:14px">
                        <div style="font-size:11px;font-weight:700;text-transform:uppercase;
                            letter-spacing:0.5px;color:#64748B;margin-bottom:10px">
                            Cálculo de días
                        </div>
                        <div style="display:flex;justify-content:space-between;padding:5px 0;
                            border-bottom:1px solid #F1F5F9">
                            <span style="color:#64748B">Días totales del período</span>
                            <span style="font-weight:600">${d.diasTotalesPeriodo} días</span>
                        </div>
                        <div style="display:flex;justify-content:space-between;padding:5px 0;
                            border-bottom:1px solid #F1F5F9">
                            <span style="color:#DC2626">(-) Días de vacaciones</span>
                            <span style="font-weight:600;color:#DC2626">-${d.totalVacaciones} días</span>
                        </div>
                        <div style="display:flex;justify-content:space-between;padding:5px 0;
                            border-bottom:1px solid #F1F5F9">
                            <span style="color:#DC2626">(-) Ausencias injustificadas</span>
                            <span style="font-weight:600;color:#DC2626">-${d.totalAusencias} días</span>
                        </div>
                        <div style="display:flex;justify-content:space-between;padding:8px 0 5px">
                            <span style="font-weight:700">Días efectivos pagados</span>
                            <span style="font-weight:700;color:#065F46;font-size:15px">
                                ${d.diasEfectivos} días
                            </span>
                        </div>
                    </div>

                    <!-- Vacaciones detalle -->
                    <div style="margin-bottom:14px">
                        <div style="font-size:11px;font-weight:700;text-transform:uppercase;
                            letter-spacing:0.5px;color:#64748B;margin-bottom:8px">
                            Detalle de vacaciones tomadas en el período
                        </div>
                        <table style="width:100%;border-collapse:collapse;
                            border:1px solid #E2E8F0;border-radius:2px">
                            <thead>
                                <tr style="background:#F8FAFC">
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Inicio</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Fin</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:center;
                                        color:#64748B;font-weight:600">Días</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Observación</th>
                                </tr>
                            </thead>
                            <tbody>${filasVac}</tbody>
                        </table>
                    </div>

                    <!-- Ausencias detalle -->
                    <div style="margin-bottom:14px">
                        <div style="font-size:11px;font-weight:700;text-transform:uppercase;
                            letter-spacing:0.5px;color:#64748B;margin-bottom:8px">
                            Detalle de ausencias injustificadas en el período
                        </div>
                        <table style="width:100%;border-collapse:collapse;
                            border:1px solid #E2E8F0;border-radius:2px">
                            <thead>
                                <tr style="background:#F8FAFC">
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Inicio</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Fin</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:left;
                                        color:#64748B;font-weight:600">Tipo</th>
                                    <th style="padding:6px 8px;font-size:11px;text-align:center;
                                        color:#64748B;font-weight:600">Días</th>
                                </tr>
                            </thead>
                            <tbody>${filasAus}</tbody>
                        </table>
                    </div>

                    <!-- Monto final -->
                    <div style="background:#F0FDF4;border:1px solid #BBF7D0;border-radius:2px;
                        padding:12px 16px;display:flex;justify-content:space-between;
                        align-items:center">
                        <div>
                            <div style="font-size:11px;color:#64748B">Salario base</div>
                            <div style="font-weight:600">Q ${Number(d.salarioBase).toLocaleString('es-GT',{minimumFractionDigits:2})}</div>
                        </div>
                        <div style="text-align:right">
                            <div style="font-size:11px;color:#64748B">Monto a pagar</div>
                            <div style="font-size:20px;font-weight:700;color:#065F46">
                                Q ${Number(d.monto).toLocaleString('es-GT',{minimumFractionDigits:2})}
                            </div>
                        </div>
                    </div>

                </div>`,
                confirmButtonColor: '#E05C2A',
                confirmButtonText: 'Cerrar',
                width: 580
            });
        } catch {
            Notify.error('Error al cargar el detalle.');
        }
    }

    return { init, openForm, verDetalle, marcarPagado, generarAnio, eliminar, cerrarModal };
})();

// ══════════════════════════════════════════════
// SUBMÓDULO — Finiquitos
// ══════════════════════════════════════════════
window.PrestacionesFiniquitos = (() => {
    let _loaded = false;
    let _todos = [];

    async function cargar() {
        _loaded = true;
        try {
            const res = await fetch('/Prestaciones/GetFiniquitos', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            _todos = res.data || [];
            renderTabla(_todos);
            document.getElementById('totalFiniquitos').textContent = `${_todos.length} finiquitos`;
        } catch { }
    }

    function renderTabla(datos) {
        const tbody = document.getElementById('bodyFiniquitos');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `<tr><td colspan="10" class="text-center text-muted py-4">No hay finiquitos calculados</td></tr>`;
            return;
        }

        tbody.innerHTML = datos.map((f, i) => `
            <tr>
                <td><span class="row-num">${i + 1}</span></td>
                <td><span style="font-weight:600;font-size:13px">${f.nombreEmpleado}</span></td>
                <td>${f.departamento}</td>
                <td class="text-center"><span class="monospace">${f.aniosTrabajados} año(s)</span></td>
                <td><span class="monospace">Q ${Number(f.salarioBase).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span></td>
                <td><span class="monospace">Q ${Number(f.aguinaldo).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span></td>
                <td><span class="monospace">Q ${Number(f.bono14).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span></td>
                <td><span class="monospace">Q ${Number(f.indemnizacion).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span></td>
                <td><span class="monospace fw-600" style="color:#065F46">Q ${Number(f.totalFiniquito).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</span></td>
                <td class="text-center">
                    <button class="btn-action btn-ver" title="Ver detalle"
                        onclick="PrestacionesFiniquitos.verDetalle(${f.empleadoId})">
                        <i class="fa-solid fa-eye"></i>
                    </button>
                </td>
            </tr>`).join('');
    }

    async function calcular() {
        // Cargar lista de empleados para seleccionar
        try {
            const res = await fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            const empleados = res.data || [];
            const opciones = empleados.map(e =>
                `<option value="${e.id}">${e.nombreCompleto}</option>`
            ).join('');

            const { value: empleadoId } = await firePrestModal({
                title: 'Calcular finiquito',
                html: `
                <p style="font-size:13px;color:#64748B;margin-bottom:12px">
                    Selecciona el empleado para calcular su finiquito completo.
                </p>
                <select id="selectEmpFiniquito" class="swal2-input" style="margin:0;width:100%">
                    <option value="">— Seleccionar empleado —</option>
                    ${opciones}
                </select>`,
                confirmButtonText: '<i class="fa-solid fa-calculator me-1"></i> Calcular',
                confirmButtonColor: '#E05C2A',
                cancelButtonText: 'Cancelar',
                showCancelButton: true,
                preConfirm: () => {
                    const v = document.getElementById('selectEmpFiniquito').value;
                    if (!v) return Swal.showValidationMessage('Selecciona un empleado');
                    return v;
                }
            });

            if (!empleadoId) return;
            await verDetalle(empleadoId);
        } catch {
            Notify.error('Error al cargar empleados.');
        }
    }

    async function verDetalle(empleadoId) {
        try {
            const res = await fetch(`/Prestaciones/Calcular?empleadoId=${empleadoId}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            if (!res.success) { Notify.error(res.message); return; }
            const f = res.data;

            await firePrestModal({
                title: `Finiquito — ${f.nombreEmpleado}`,
                html: `
                <table style="width:100%;font-size:13px;text-align:left;border-collapse:collapse">
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Fecha ingreso</td>
                        <td style="font-weight:600">${new Date(f.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Tiempo trabajado</td>
                        <td style="font-weight:600">${f.aniosTrabajados} año(s) / ${f.mesesTrabajados} meses</td>
                    </tr>
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Salario base</td>
                        <td class="monospace">Q ${Number(f.salarioBase).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Aguinaldo proporcional</td>
                        <td class="monospace">Q ${Number(f.aguinaldo).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Bono 14 proporcional</td>
                        <td class="monospace">Q ${Number(f.bono14).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</td>
                    </tr>
                    <tr style="border-bottom:1px solid #E2E8F0">
                        <td style="padding:8px 0;color:#64748B">Indemnización</td>
                        <td class="monospace">Q ${Number(f.indemnizacion).toLocaleString('es-GT', { minimumFractionDigits: 2 })}</td>
                    </tr>
                    <tr>
                        <td style="padding:8px 0;font-weight:700">TOTAL FINIQUITO</td>
                        <td class="monospace fw-600" style="color:#065F46;font-size:16px">
                            Q ${Number(f.totalFiniquito).toLocaleString('es-GT', { minimumFractionDigits: 2 })}
                        </td>
                    </tr>
                </table>`,
                confirmButtonColor: '#E05C2A',
                confirmButtonText: 'Cerrar'
            });
        } catch {
            Notify.error('Error al calcular finiquito.');
        }
    }


    return { _loaded: false, cargar, calcular, verDetalle };
})();

// ── Auto-init ──
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('tablaPrestaciones')) Prestaciones.init();
});

document.addEventListener('spaNavigated', () => {
    if (document.getElementById('tablaPrestaciones')) {
        Prestaciones.init();
        PrestacionesFiniquitos._loaded = false;
    }
});