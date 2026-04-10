/* ══════════════════════════════════════════════
   SICE — Módulo Nómina completo
   Tabs: Planillas | Salarios | Préstamos | Conceptos
══════════════════════════════════════════════ */

const BsModal = (() => {
    let _resolve = null;
    let _bsInst = null;

    function _build() {
        if (document.getElementById('bsModalGlobal')) return;
        const tpl = document.createElement('div');

        tpl.innerHTML = `
        <div class="modal fade" id="bsModalGlobal" tabindex="-1" aria-hidden="true">
          <div class="modal-dialog mt-5" id="bsModalDialog"> <!-- mt-5 para que salga arriba -->
            <div class="modal-content"
                 style="border-radius: 2px; border: none; box-shadow: 0 4px 20px rgba(0,0,0,0.15);">
                 
              <!-- HEADER BLANCO LIMPIO -->
              <div class="modal-header"
                   style="border-bottom: 1px solid #E2E8F0; padding: 16px 20px; background:#fff;">
                <h5 class="modal-title" id="bsModalTitle"
                    style="font-size: 17px; font-weight: 600; color: #1E293B; margin: 0;"></h5>
                <button type="button" id="bsModalXBtn"
                        class="btn-close" aria-label="Cerrar" style="font-size: 12px;"></button>
              </div>
              
              <div class="modal-body" id="bsModalBody" style="padding: 20px;"></div>
              
              <div class="modal-footer" id="bsModalFooter"
                   style="border-top: 1px solid #E2E8F0; padding: 14px 20px; gap: 8px; justify-content: flex-end; background: #fff;">
              </div>
            </div>
          </div>
        </div>`;
        document.body.appendChild(tpl.firstElementChild);

        const el = document.getElementById('bsModalGlobal');

        document.getElementById('bsModalXBtn').addEventListener('click', () => {
            if (_resolve) {
                const r = _resolve;
                _resolve = null;
                _bsInst?.hide();
                r({ isConfirmed: false, isDismissed: true, value: undefined });
            }
        });

        el.addEventListener('hidden.bs.modal', () => {
            if (_resolve) {
                const r = _resolve;
                _resolve = null;
                r({ isConfirmed: false, isDismissed: true, value: undefined });
            }
        });
    }

    function showValidationMessage(msg) {
        let el = document.getElementById('bsValidationMsg');
        if (!el) {
            el = document.createElement('div');
            el.id = 'bsValidationMsg';
            el.style.cssText =
                'color: #DC2626; font-size: 13px; margin-top: 16px; padding: 10px 14px;' +
                'background: #FEF2F2; border-radius: 2px; border: 1px solid #FECACA; font-weight: 500;';
            document.getElementById('bsModalBody')?.appendChild(el);
        }
        el.innerHTML = `<i class="fa-solid fa-circle-exclamation me-1"></i> ${msg}`;
        el.style.display = 'block';
        return false;
    }

    function _clearValidation() {
        const el = document.getElementById('bsValidationMsg');
        if (el) el.style.display = 'none';
    }

    async function fire(options = {}) {
        const {
            title = '',
            html = '',
            text = '',
            icon,
            confirmButtonText = 'Guardar',
            confirmButtonColor = '#0d9488',
            cancelButtonText = 'Cancelar',
            showCancelButton = false,
            width,
            allowOutsideClick = true,
            didOpen,
            preConfirm
        } = options;

        _build();
        const el = document.getElementById('bsModalGlobal');
        const dialog = document.getElementById('bsModalDialog');
        const titleEl = document.getElementById('bsModalTitle');
        const bodyEl = document.getElementById('bsModalBody');
        const footEl = document.getElementById('bsModalFooter');

        const prev = bootstrap.Modal.getInstance(el);
        if (prev) prev.dispose();

        if (confirmButtonColor === '#DC2626') {
            dialog.classList.remove('mt-5');
            dialog.classList.add('modal-dialog-centered');
        } else {
            dialog.classList.add('mt-5');
            dialog.classList.remove('modal-dialog-centered');
        }

        dialog.style.maxWidth = width ? (typeof width === 'number' ? width + 'px' : width) : '600px';
        titleEl.innerHTML = title;

        const iconMap = {
            warning: `<div style="text-align:center;margin-bottom:14px"><i class="fa-solid fa-triangle-exclamation fa-2x" style="color:#F59E0B"></i></div>`,
            success: `<div style="text-align:center;margin-bottom:14px"><i class="fa-solid fa-circle-check fa-2x" style="color:#16A34A"></i></div>`,
            error: `<div style="text-align:center;margin-bottom:14px"><i class="fa-solid fa-circle-xmark fa-2x" style="color:#DC2626"></i></div>`,
            info: `<div style="text-align:center;margin-bottom:14px"><i class="fa-solid fa-circle-info fa-2x" style="color:#3B82F6"></i></div>`
        };

        bodyEl.innerHTML = (icon ? (iconMap[icon] || '') : '') +
            (html || (text ? `<p style="font-size:14px;color:#475569;text-align:center;margin:0">${text}</p>` : ''));

        footEl.innerHTML =
            (showCancelButton
                ? `<button type="button" id="bsModalCancelBtn"
                       style="font-size: 13px; padding: 9px 18px; border-radius: 2px;
                              background: #6b7280; color: #fff; border: none; font-weight: 500;
                              display: inline-flex; align-items: center; justify-content: center; gap: 6px; cursor: pointer; min-width: 100px;">
                       <i class="fa-solid fa-arrow-left"></i> ${cancelButtonText}
                   </button>`
                : '') +
            `<button type="button" id="bsModalConfirmBtn"
                 style="font-size: 13px; padding: 9px 18px; border-radius: 2px;
                        background: ${confirmButtonColor}; color: #fff; border: none; font-weight: 500;
                        display: inline-flex; align-items: center; justify-content: center; gap: 6px; cursor: pointer; min-width: 100px;">
                 ${icon && confirmButtonColor !== '#0d9488' ? '' : '<i class="fa-solid fa-check"></i> '} ${confirmButtonText}
             </button>`;

        return new Promise(resolve => {
            _resolve = resolve;
            _bsInst = new bootstrap.Modal(el, { backdrop: allowOutsideClick ? true : 'static', keyboard: allowOutsideClick });

            document.getElementById('bsModalCancelBtn')?.addEventListener('click', () => {
                _resolve = null;
                _bsInst.hide();
                resolve({ isConfirmed: false, isDismissed: true, value: undefined });
            });

            document.getElementById('bsModalConfirmBtn').addEventListener('click', async () => {
                _clearValidation();
                let value;
                if (preConfirm) {
                    const btn = document.getElementById('bsModalConfirmBtn');
                    const txtOriginal = btn.innerHTML;
                    btn.innerHTML = `<span class="spinner-border spinner-border-sm"></span> Procesando...`;
                    btn.disabled = true;

                    value = preConfirm();
                    if (value instanceof Promise) value = await value;

                    if (value === false || value === undefined) {
                        btn.innerHTML = txtOriginal;
                        btn.disabled = false;
                        return;
                    }
                } else {
                    value = true;
                }
                _resolve = null;
                _bsInst.hide();
                resolve({ isConfirmed: true, value });
            });

            _bsInst.show();
            if (didOpen) el.addEventListener('shown.bs.modal', () => didOpen(), { once: true });
        });
    }

    function loadingShow(title = 'Procesando...', msg = 'Por favor espera...') {
        let overlay = document.getElementById('bsLoadingOverlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'bsLoadingOverlay';
            overlay.style.cssText =
                'position:fixed;inset:0;background:rgba(0,0,0,.45);' +
                'z-index:9999;display:flex;align-items:center;' +
                'justify-content:center';
            overlay.innerHTML = `
                <div style="background:#fff;border-radius:2px;
                            padding:32px 40px;text-align:center;
                            box-shadow:0 8px 32px rgba(0,0,0,.15);
                            min-width:280px">
                    <div class="spinner-border"
                         style="color:#0d9488;width:2.2rem;height:2.2rem">
                    </div>
                    <h5 id="bsLoadingTitle"
                        style="margin-top:16px;font-size:15px;
                               font-weight:600;color:#1E293B"></h5>
                    <p id="bsLoadingMsg"
                       style="font-size:13px;color:#64748B;margin:0"></p>
                </div>`;
            document.body.appendChild(overlay);
        }
        document.getElementById('bsLoadingTitle').textContent = title;
        document.getElementById('bsLoadingMsg').textContent = msg;
        overlay.style.display = 'flex';
    }

    function loadingHide() {
        const overlay = document.getElementById('bsLoadingOverlay');
        if (overlay) overlay.style.display = 'none';
    }

    return { fire, showValidationMessage, loadingShow, loadingHide };
})();

// ════════════════════════════════════════════
// EXPORTAR
// ════════════════════════════════════════════
window.NominaExport = {
    excel(tablaId, nombreArchivo) {
        const tabla = document.getElementById(tablaId);
        if (!tabla) { Notify.error('No hay datos para exportar.'); return; }

        const headers = [];
        tabla.querySelectorAll('thead th').forEach(th => {
            const txt = th.innerText.trim();
            if (txt && txt !== '#') headers.push(txt);
        });

        const filas = [];
        tabla.querySelectorAll('tbody tr').forEach(tr => {
            if (tr.querySelector('td[colspan]')) return;
            const fila = [];
            tr.querySelectorAll('td').forEach((td, i) => {
                if (i === 0) return;
                fila.push(td.innerText.trim()
                    .replace(/\n/g, ' ').replace(/\s+/g, ' '));
            });
            if (fila.length) filas.push(fila);
        });

        if (!filas.length) { Notify.error('No hay datos para exportar.'); return; }

        const bom = '\uFEFF';
        const sep = ',';
        const lineas = [
            headers.join(sep),
            ...filas.map(f =>
                f.map(c => `"${c.replace(/"/g, '""')}"`).join(sep))
        ];
        const blob = new Blob([bom + lineas.join('\n')],
            { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${nombreArchivo}_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        Notify.success('Archivo Excel generado correctamente.');
    },

    pdf(titulo, tablaId) {
        const tabla = document.getElementById(tablaId);
        if (!tabla) { Notify.error('No hay datos para exportar.'); return; }

        const clon = tabla.cloneNode(true);
        clon.querySelectorAll('td:last-child, th:last-child')
            .forEach(el => el.remove());
        clon.querySelectorAll('.btn-action, button')
            .forEach(el => el.remove());

        const estilos = `
            <style>
                body { font-family:Arial,sans-serif;font-size:12px;color:#1E293B; }
                h2   { font-size:16px;margin-bottom:4px;color:#0d9488; }
                p    { font-size:11px;color:#64748B;margin-bottom:16px; }
                table { width:100%;border-collapse:collapse; }
                th { background:#F8FAFC;border-bottom:2px solid #E2E8F0;
                     padding:8px 10px;text-align:left;font-size:11px;
                     color:#475569;text-transform:uppercase; }
                td { border-bottom:1px solid #F1F5F9;padding:7px 10px; }
                tr:last-child td { border-bottom:none; }
                .text-danger { color:#DC2626; }
                .monospace   { font-family:monospace; }
                @media print { @page { margin:1.5cm;size:landscape; } }
            </style>`;

        const fecha = new Date().toLocaleDateString('es-GT',
            { day: '2-digit', month: 'long', year: 'numeric' });
        const ventana = window.open('', '_blank');
        ventana.document.write(`
            <!DOCTYPE html><html>
            <head><meta charset="utf-8"><title>${titulo}</title>${estilos}</head>
            <body>
                <h2>${titulo}</h2>
                <p>Generado el ${fecha} — Portal RRHH</p>
                ${clon.outerHTML}
            </body></html>`);
        ventana.document.close();
        ventana.focus();
        setTimeout(() => { ventana.print(); ventana.close(); }, 500);
    },

    excelPrestamos(datos, nombreArchivo) {
        if (!datos || !datos.length) {
            Notify.error('No hay préstamos para exportar.'); return;
        }
        const headers = ['Empleado', 'Departamento', 'Monto Total',
            'Cuota Mensual', 'Cuotas', 'Saldo Pendiente', 'Estado',
            'Fecha Inicio', 'Motivo'];
        const filas = datos.map(p => [
            p.nombreEmpleado, p.departamento,
            `Q ${Number(p.montoTotal).toFixed(2)}`,
            `Q ${Number(p.cuotaMensual).toFixed(2)}`,
            `${p.cuotasPagadas}/${p.numeroCuotas}`,
            `Q ${Number(p.saldoPendiente).toFixed(2)}`,
            p.estado, p.fechaInicio, p.motivo || ''
        ]);
        const bom = '\uFEFF', sep = ',';
        const lineas = [
            headers.join(sep),
            ...filas.map(f =>
                f.map(c => `"${String(c).replace(/"/g, '""')}"`).join(sep))
        ];
        const blob = new Blob([bom + lineas.join('\n')],
            { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${nombreArchivo}_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
        Notify.success('Archivo Excel generado correctamente.');
    },

    pdfPrestamos(datos) {
        if (!datos || !datos.length) {
            Notify.error('No hay préstamos para exportar.'); return;
        }
        const filas = datos.map(p => `
            <tr>
                <td>${p.nombreEmpleado}</td>
                <td>${p.departamento}</td>
                <td>Q ${Number(p.montoTotal).toLocaleString('es-GT',
            { minimumFractionDigits: 2 })}</td>
                <td>Q ${Number(p.cuotaMensual).toLocaleString('es-GT',
                { minimumFractionDigits: 2 })}</td>
                <td>${p.cuotasPagadas}/${p.numeroCuotas}</td>
                <td>Q ${Number(p.saldoPendiente).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}</td>
                <td>${p.estado}</td>
                <td>${p.fechaInicio}</td>
            </tr>`).join('');
        const fecha = new Date().toLocaleDateString('es-GT',
            { day: '2-digit', month: 'long', year: 'numeric' });
        const ventana = window.open('', '_blank');
        ventana.document.write(`
            <!DOCTYPE html><html>
            <head>
                <meta charset="utf-8">
                <title>Reporte de Préstamos</title>
                <style>
                    body { font-family:Arial,sans-serif;font-size:12px; }
                    h2 { color:#0d9488; }
                    p  { color:#64748B;font-size:11px; }
                    table { width:100%;border-collapse:collapse; }
                    th { background:#F8FAFC;border-bottom:2px solid #E2E8F0;
                         padding:8px;font-size:11px;text-align:left; }
                    td { border-bottom:1px solid #F1F5F9;padding:7px 8px; }
                    @media print { @page { size:landscape;margin:1.5cm; } }
                </style>
            </head>
            <body>
                <h2>Reporte de Préstamos</h2>
                <p>Generado el ${fecha} — Portal RRHH / SICE</p>
                <table>
                    <thead>
                        <tr>
                            <th>Empleado</th><th>Departamento</th>
                            <th>Monto Total</th><th>Cuota</th>
                            <th>Cuotas</th><th>Saldo</th>
                            <th>Estado</th><th>Fecha Inicio</th>
                        </tr>
                    </thead>
                    <tbody>${filas}</tbody>
                </table>
            </body></html>`);
        ventana.document.close();
        ventana.focus();
        setTimeout(() => { ventana.print(); ventana.close(); }, 500);
    }
};

// ════════════════════════════════════════════
// TABS
// ════════════════════════════════════════════
window.NominaTabs = {
    show(tab) {
        document.querySelectorAll('.tab-content-nomina')
            .forEach(el => el.classList.remove('active'));
        document.querySelectorAll('.module-tab')
            .forEach(el => el.classList.remove('active'));

        document.getElementById(`tab-${tab}`)?.classList.add('active');
        document.querySelector(`[data-tab="${tab}"]`)?.classList.add('active');

        if (tab === 'salarios' && !NominaSalarios._loaded) NominaSalarios.cargar();
        if (tab === 'prestamos' && !NominaPrestamos._loaded) NominaPrestamos.cargar();
        if (tab === 'conceptos' && !NominaConceptos._loaded) NominaConceptos.cargar();
    }
};

// ════════════════════════════════════════════
// MÓDULO PRINCIPAL — Planillas
// ════════════════════════════════════════════
window.Nomina = (() => {
    let table = null;

    function init() {
        limpiarModal();
        initDataTable();
        initEvents();
        cargarResumenAnio();
        cargarDepartamentos();
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

    function getModal() {
        const modalEl = document.getElementById('modalEmpleado');
        if (!modalEl) return null;
        let inst = bootstrap.Modal.getInstance(modalEl);
        if (!inst) inst = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true });
        return inst;
    }

    function initDataTable() {
        if ($.fn.DataTable.isDataTable('#tablaNomina'))
            $('#tablaNomina').DataTable().destroy();

        table = $('#tablaNomina').DataTable({
            dom: 'rtip',
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Nomina/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify(buildRequest(d)),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            },
            columns: [
                {
                    data: null, orderable: false,
                    render: (_, __, ___, meta) =>
                        `<span class="row-num">${meta.row + 1}</span>`
                },
                {
                    data: 'periodo',
                    render: (v, _, row) =>
                        `<div class="fw-600">${v}</div>
                         <small class="text-muted">${row.generadoPor || ''}</small>`
                },
                {
                    data: 'totalEmpleados',
                    render: v => `<span class="monospace">${v} emp.</span>`
                },
                {
                    data: 'totalDevengado',
                    render: v =>
                        `<span class="monospace">Q ${Number(v).toLocaleString('es-GT',
                            { minimumFractionDigits: 2 })}</span>`
                },
                {
                    data: 'totalDeducciones',
                    render: v =>
                        `<span class="monospace text-danger">Q ${Number(v).toLocaleString('es-GT',
                            { minimumFractionDigits: 2 })}</span>`
                },
                {
                    data: 'totalNeto',
                    render: v =>
                        `<span class="monospace fw-600" style="color:#16A34A">Q ${Number(v).toLocaleString('es-GT',
                            { minimumFractionDigits: 2 })}</span>`
                },
                {
                    data: 'estado',
                    render: v => `<span class="badge-nomina-${v.toLowerCase()}">${v}</span>`
                },
                { data: 'fechaGeneracion' },
                {
                    data: 'fechaPago',
                    render: v => v
                        ? `<span style="color:#16A34A;font-weight:600">${v}</span>`
                        : '<span class="text-muted">Pendiente</span>'
                },
                {
                    data: 'id', orderable: false, className: 'text-center',
                    render: id => `
                        <div style="display:inline-flex;align-items:center;gap:6px">
                            <button class="btn-action btn-ver" title="Ver detalle"
                                onclick="Nomina.verDetalle(${id})">
                                <i class="fa-solid fa-eye"></i>
                            </button>
                            <button class="btn-action btn-editar" title="Marcar pagada"
                                onclick="Nomina.marcarPagada(${id})">
                                <i class="fa-solid fa-money-bill-wave"></i>
                            </button>
                            <button class="btn-action btn-eliminar" title="Anular"
                                onclick="Nomina.anular(${id})">
                                <i class="fa-solid fa-ban"></i>
                            </button>
                        </div>`
                }
            ],
            language: {
                url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json'
            },
            pageLength: 15,
            order: [[1, 'desc']],
            drawCallback(settings) {
                const info = settings.json;
                const el = document.getElementById('totalPlanillas');
                if (el && info) el.textContent = `${info.recordsFiltered} planillas`;
            }
        });
    }

    function buildRequest(d) {
        return {
            draw: d.draw, start: d.start, length: d.length,
            searchValue: d.search?.value || '',
            orderColumn: d.columns?.[d.order?.[0]?.column]?.name || '',
            orderDir: d.order?.[0]?.dir || 'desc',
            estado: document.getElementById('filtroEstadoNomina')?.value || ''
        };
    }

    function initEvents() {
        const btnGenerar = document.getElementById('btnGenerarPlanilla');
        if (btnGenerar) {
            const clone = btnGenerar.cloneNode(true);
            btnGenerar.parentNode.replaceChild(clone, btnGenerar);
            clone.addEventListener('click', generarPlanilla);
        }

        document.getElementById('filtroEstadoNomina')
            ?.addEventListener('change', () => table?.ajax.reload());

        document.getElementById('btnLimpiarNomina')
            ?.addEventListener('click', () => {
                document.getElementById('filtroEstadoNomina').value = '';
                table?.ajax.reload();
            });

        document.getElementById('selectAnioResumen')
            ?.addEventListener('change', function () {
                cargarResumenAnio(parseInt(this.value));
            });

        document.getElementById('buscarEmpleadoSalario')
            ?.addEventListener('input', function () {
                NominaSalarios.filtrar(this.value);
            });

        document.getElementById('filtroDeptoSalarios')
            ?.addEventListener('change', function () {
                NominaSalarios.filtrarDepto(this.value);
            });

        document.getElementById('filtroEstadoPrestamo')
            ?.addEventListener('change', function () {
                NominaPrestamos.filtrar(this.value);
            });

        document.getElementById('filtroTipoConcepto')
            ?.addEventListener('change', function () {
                NominaConceptos.filtrar(this.value);
            });

        document.getElementById('btnExcelPlanillas')
            ?.addEventListener('click', () =>
                NominaExport.pdf('Planillas de Nómina', 'tablaNomina'));
        document.getElementById('btnPdfPlanillas')
            ?.addEventListener('click', () =>
                NominaExport.pdf('Planillas de Nómina', 'tablaNomina'));

        document.getElementById('btnExcelSalarios')
            ?.addEventListener('click', () =>
                NominaExport.excel('tablaSalarios', 'Salarios'));
        document.getElementById('btnPdfSalarios')
            ?.addEventListener('click', () =>
                NominaExport.pdf('Salarios por Empleado', 'tablaSalarios'));

        document.getElementById('btnExcelPrestamos')
            ?.addEventListener('click', () =>
                NominaExport.excelPrestamos(NominaPrestamos._datos, 'Prestamos'));
        document.getElementById('btnPdfPrestamos')
            ?.addEventListener('click', () =>
                NominaExport.pdfPrestamos(NominaPrestamos._datos));

        document.getElementById('btnExcelConceptos')
            ?.addEventListener('click', () =>
                NominaExport.excel('tablaConceptos', 'Conceptos_Nomina'));
        document.getElementById('btnPdfConceptos')
            ?.addEventListener('click', () =>
                NominaExport.pdf('Conceptos de Nómina', 'tablaConceptos'));

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.nominaInit) {
            modalEl.dataset.nominaInit = 'true';
            modalEl.addEventListener('hidden.bs.modal', () => {
                document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('padding-right');
                document.body.style.removeProperty('overflow');
            });
        }
    }

    async function cargarDepartamentos() {
        try {
            const res = await fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            const deptos = [...new Map(
                (res.data || []).map(e => [e.departamento, e.departamento])
            ).entries()].map(([k]) => k);

            const sel = document.getElementById('filtroDeptoSalarios');
            if (!sel) return;
            while (sel.options.length > 1) sel.remove(1);
            deptos.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d; opt.textContent = d;
                sel.appendChild(opt);
            });
        } catch { }
    }

    async function cargarResumenAnio(anio = new Date().getFullYear()) {
        try {
            const res = await fetch(`/Nomina/ResumenAnio?anio=${anio}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            const setEl = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val;
            };
            setEl('kpiTotalPlanillas', res.totalPlanillas);
            setEl('kpiTotalPagado',
                `Q ${Number(res.totalPagado).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}`);
            setEl('kpiTotalDevengado',
                `Q ${Number(res.totalDevengadoAnio).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}`);
            setEl('kpiTotalDeducciones',
                `Q ${Number(res.totalDeduccionesAnio).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}`);
        } catch { }
    }

    async function generarPlanilla() {
        const hoy = new Date();
        const meses = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
            'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

        const { value: formValues } = await BsModal.fire({
            title: 'Generar planilla del mes',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 600,
            html: `
                <div style="text-align: left;">
                    <p style="font-size: 14px; color: #64748B; margin-bottom: 20px;">
                        El sistema calculará automáticamente salarios, horas extra, IGSS e ISR.
                    </p>
                    
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 24px;">
                        <!-- Columna Mes -->
                        <div>
                            <label style="font-size: 13px; color: #475569; display: block; margin-bottom: 6px;">
                                Mes <span style="color: #dc2626;">*</span>
                            </label>
                            <select id="inputMes" class="form-select" style="font-size: 13px; border-color: #E2E8F0; padding: 8px 12px; box-shadow: none;">
                                ${meses.map((m, i) =>
                `<option value="${i + 1}" ${i + 1 === hoy.getMonth() + 1 ? 'selected' : ''}>
                                        ${m}
                                    </option>`
            ).join('')}
                            </select>
                        </div>
                        
                        <!-- Columna Año -->
                        <div>
                            <label style="font-size: 13px; color: #475569; display: block; margin-bottom: 6px;">
                                Año <span style="color: #dc2626;">*</span>
                            </label>
                            <input id="inputAnio" type="number" class="form-control" 
                                   style="font-size: 13px; border-color: #E2E8F0; padding: 8px 12px; box-shadow: none;"
                                   value="${hoy.getFullYear()}" min="2020" max="2099">
                        </div>
                    </div>

                    <!-- Mensaje informativo -->
                    <div style="background: #FFFBEB; border: 1px solid #FDE68A; border-radius: 6px; padding: 12px 16px; display: flex; align-items: flex-start; gap: 10px;">
                        <i class="fa-solid fa-circle-info" style="color: #D97706; margin-top: 3px;"></i>
                        <p style="font-size: 13px; color: #92400E; margin: 0; line-height: 1.5;">
                            Las horas extra y atrasos se leerán del módulo de Asistencia automáticamente.
                        </p>
                    </div>
                </div>`,
            preConfirm: () => {
                const mes = document.getElementById('inputMes').value;
                const anio = document.getElementById('inputAnio').value;

                if (!mes || !anio) {
                    BsModal.showValidationMessage('Todos los campos son obligatorios');
                    return false;
                }

                return {
                    mes: parseInt(mes),
                    anio: parseInt(anio)
                };
            }
        });

        if (!formValues) return;

        BsModal.loadingShow('Generando planilla...', 'Calculando salarios, IGSS e ISR...');

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Nomina/Generar', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(formValues)
            }).then(r => r.json());

            BsModal.loadingHide();

            if (res.success) {
                await BsModal.fire({
                    icon: 'success',
                    title: 'Planilla generada',
                    text: res.message,
                    confirmButtonText: 'Ver planilla'
                });
                table?.ajax.reload(null, false);
                cargarResumenAnio();
                if (res.data?.id) verDetalle(res.data.id);
            } else {
                BsModal.fire({
                    icon: 'warning',
                    title: 'No se pudo generar',
                    text: res.message
                });
            }
        } catch {
            BsModal.loadingHide();
            Notify.error('Error al generar la planilla.');
        }
    }

    async function verDetalle(id) {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--nomina-color);width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;font-size:13px">
                    Cargando planilla...
                </p>
            </div>`;
        getModal()?.show();

        try {
            const html = await fetch(`/Nomina/Detalle/${id}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
            body.innerHTML = html;
        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                    <p>Error al cargar el detalle</p>
                </div>`;
        }
    }

    async function verBoleta(detalleId) {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--nomina-color);width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;font-size:13px">
                    Cargando boleta...
                </p>
            </div>`;
        getModal()?.show();

        try {
            const html = await fetch(`/Nomina/Boleta/${detalleId}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
            body.innerHTML = html;
        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                    <p>Error al cargar la boleta</p>
                </div>`;
        }
    }

    async function editarDetalle(id, otrosBonos, otrasDed, obs) {
        const { value: form } = await BsModal.fire({
            title: 'Editar línea de planilla',
            html: `
                <div style="text-align:left">
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">
                            Otros bonos (Q)
                        </label>
                        <input id="editOtrosBonos" type="number"
                               class="form-control form-control-sm"
                               value="${otrosBonos}" min="0" step="0.01">
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">
                            Otras deducciones (Q)
                        </label>
                        <input id="editOtrasDeducciones" type="number"
                               class="form-control form-control-sm"
                               value="${otrasDed}" min="0" step="0.01">
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">Observación</label>
                        <textarea id="editObservacion"
                                  class="form-control form-control-sm"
                                  style="height:70px"
                                  placeholder="Ej: Préstamo cuota 3/12..."
                                  >${obs || ''}</textarea>
                    </div>
                </div>`,
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 420,
            preConfirm: () => ({
                otrosBonos: parseFloat(
                    document.getElementById('editOtrosBonos').value) || 0,
                otrasDeducciones: parseFloat(
                    document.getElementById('editOtrasDeducciones').value) || 0,
                observacion: document.getElementById('editObservacion').value
            })
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/ActualizarDetalle/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(form)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al actualizar.');
        }
    }

    async function marcarPagada(id) {
        const { value: fecha } = await BsModal.fire({
            title: 'Registrar pago de planilla',
            html: `
                <p style="font-size:13px;color:#64748B;margin-bottom:12px">
                    Ingresa la fecha en que se realizó el pago.
                </p>
                <input type="date" id="fechaPagoNomina"
                       class="form-control form-control-sm"
                       value="${new Date().toISOString().split('T')[0]}">`,
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            preConfirm: () =>
                document.getElementById('fechaPagoNomina').value
        });

        if (!fecha) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/MarcarPagada/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(fecha)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                table?.ajax.reload(null, false);
                cargarResumenAnio();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    async function anular(id) {
        const ok = await BsModal.fire({
            title: '¿Anular esta planilla?',
            html: `<p style="font-size:14px;color:#64748B;text-align:center;margin:0">
                      Esta acción <strong>no se puede revertir</strong>.
                   </p>`,
            icon: 'warning',
            confirmButtonText: 'Sí, anular',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);
        if (!ok) return;
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/Anular/${id}`, {
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
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al anular.');
        }
    }

    async function eliminar(id) {
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/Eliminar/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());
            if (res.success) {
                cerrarModal();
                table?.ajax.reload(null, false);
                Notify.success(res.message);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al eliminar.');
        }
    }

    return { init, verDetalle, verBoleta, editarDetalle, marcarPagada, anular, eliminar, cerrarModal };
})();

// ════════════════════════════════════════════
// SUBMÓDULO — Salarios
// ════════════════════════════════════════════
window.NominaSalarios = {
    _loaded: false,
    _datos: [],

    async cargar() {
        this._loaded = true;
        try {
            const res = await fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());
            this._datos = res.data || [];
            this.renderizar(this._datos);
            const el = document.getElementById('totalSalarios');
            if (el) el.textContent = `${this._datos.length} empleados`;
        } catch {
            Notify.error('Error al cargar salarios.');
        }
    },

    renderizar(datos) {
        const tbody = document.getElementById('bodySalarios');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center text-muted py-4">
                        No hay empleados para mostrar
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = datos.map((e, i) => {
            const salMin = Number(e.salarioMinimo || 0);
            const salMax = Number(e.salarioMaximo || 0);
            const salBase = Number(e.salarioBase || 0);

            const rangoTexto = salMin > 0 && salMax > 0
                ? `Q ${salMin.toLocaleString('es-GT', { minimumFractionDigits: 0 })}
                   — Q ${salMax.toLocaleString('es-GT', { minimumFractionDigits: 0 })}`
                : '<span class="text-muted" style="font-size:11px">Sin rango</span>';

            const salarioTexto = salBase > 0
                ? `Q ${salBase.toLocaleString('es-GT', { minimumFractionDigits: 2 })}`
                : '<span class="text-muted">No asignado</span>';

            return `
                <tr>
                    <td><span class="row-num">${i + 1}</span></td>
                    <td>
                        <div class="fw-600">${e.nombreCompleto}</div>
                        <small class="text-muted">${e.codigo}</small>
                    </td>
                    <td class="text-muted">${e.departamento}</td>
                    <td class="text-muted">${e.puesto}</td>
                    <td>
                        <span class="monospace fw-600" style="color:#065F46">
                            ${salarioTexto}
                        </span>
                    </td>
                    <td>
                        <span class="monospace" style="font-size:11px;color:#64748B">
                            ${rangoTexto}
                        </span>
                    </td>
                    <td class="text-muted" style="font-size:12px">
                        ${e.fechaIngreso || '—'}
                    </td>
                    <td class="text-center">
                        <button class="btn-action btn-editar"
                            title="Actualizar salario"
                            onclick="NominaSalarios.actualizarSalario(
                                ${e.id},
                                '${(e.nombreCompleto || '').replace(/'/g, "\\'")}',
                                ${salBase},
                                ${salMin},
                                ${salMax})">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                    </td>
                </tr>`;
        }).join('');
    },

    filtrar(texto) {
        const t = (texto || '').toLowerCase();
        const filtrados = this._datos.filter(e =>
            (e.nombreCompleto || '').toLowerCase().includes(t) ||
            (e.codigo || '').toLowerCase().includes(t)
        );
        this.renderizar(filtrados);
    },

    filtrarDepto(depto) {
        const filtrados = depto
            ? this._datos.filter(e => e.departamento === depto)
            : this._datos;
        this.renderizar(filtrados);
    },

    async actualizarSalario(id, nombre, salarioActual, min, max) {
        const tieneRango = min > 0 && max > 0;
        const rangoInfo = tieneRango
            ? `Rango del puesto: Q${Number(min).toLocaleString('es-GT')}
               — Q${Number(max).toLocaleString('es-GT')}`
            : `Sin rango configurado. Ve a
               <strong>Personal → Departamentos → Puestos</strong>.`;

        const { value: form } = await BsModal.fire({
            title: 'Actualizar salario',
            html: `
                <div style="text-align:left">
                    <p style="font-size:13px;color:#1E293B;
                              font-weight:600;margin-bottom:16px">
                        ${nombre}
                    </p>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Salario actual
                        </label>
                        <div style="font-size:15px;font-weight:700;
                                    color:#065F46;margin-bottom:4px">
                            Q ${Number(salarioActual).toLocaleString(
                'es-GT', { minimumFractionDigits: 2 })}
                        </div>
                    </div>
                    <div style="margin-bottom:8px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Nuevo salario (Q)
                            <span style="color:#DC2626">*</span>
                        </label>
                        <input id="nuevoSalario" type="number"
                               class="form-control form-control-sm"
                               value="${salarioActual}"
                               step="0.01">
                        <small style="color:${tieneRango ? '#64748B' : '#F59E0B'};
                            font-size:11px;display:block;
                            margin-top:4px">
                            ${rangoInfo}
                        </small>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Motivo del cambio
                        </label>
                        <select id="motivoSalario"
                                class="form-select form-select-sm">
                            <option>Aumento por mérito</option>
                            <option>Ajuste salarial</option>
                            <option>Promoción</option>
                            <option>Corrección</option>
                            <option>Incremento salario mínimo</option>
                        </select>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Observación (opcional)
                        </label>
                        <textarea id="obsSalario"
                                  class="form-control form-control-sm"
                                  style="height:60px"
                                  placeholder="Detalle adicional...">
                        </textarea>
                    </div>
                </div>`,
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 480,
            preConfirm: () => {
                const val = parseFloat(
                    document.getElementById('nuevoSalario').value);
                if (!val || val <= 0)
                    return BsModal.showValidationMessage(
                        'Ingresa un salario válido mayor a Q0');
                if (tieneRango && (val < min || val > max))
                    return BsModal.showValidationMessage(
                        `El salario debe estar entre ` +
                        `Q${Number(min).toLocaleString('es-GT')} y ` +
                        `Q${Number(max).toLocaleString('es-GT')}`);
                return {
                    salarioBase: val,
                    motivo: document.getElementById('motivoSalario').value,
                    observacion: document.getElementById('obsSalario').value
                };
            }
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/ActualizarSalario/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(form)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al actualizar salario.');
        }
    }
};

// ════════════════════════════════════════════
// SUBMÓDULO — Préstamos
// ════════════════════════════════════════════
window.NominaPrestamos = {
    _loaded: false,
    _datos: [],

    async cargar() {
        this._loaded = true;
        try {
            const res = await fetch('/Nomina/GetPrestamos', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());
            this._datos = res.data || [];
            this.renderizar(this._datos);
            const el = document.getElementById('totalPrestamos');
            if (el) el.textContent = `${this._datos.length} préstamos`;
        } catch (err) {
            console.error('Error cargando préstamos:', err);
        }
    },

    // ── RENDERIZAR (versión con menú desplegable de 3 puntos) ──
    renderizar(datos) {
        const tbody = document.getElementById('bodyPrestamos');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9"
                        class="text-center text-muted py-4">
                        No hay préstamos registrados
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = datos.map((p, i) => {
            const pct = p.numeroCuotas > 0
                ? Math.round((p.cuotasPagadas / p.numeroCuotas) * 100)
                : 0;

            // ── Acciones según estado ──
            let acciones = '';

            if (p.estado === 'Activo') {
                acciones = `
                    <div class="prestamo-menu-wrapper">
                        <button class="btn-action btn-tres-puntos"
                            title="Opciones"
                            onclick="NominaPrestamos.toggleMenu(
                                event, ${p.id})">
                            <i class="fa-solid fa-ellipsis-vertical">
                            </i>
                        </button>
                        <div class="prestamo-dropdown"
                             id="menu-prestamo-${p.id}">
                            <button onclick="NominaPrestamos
                                .abonarCuota(${p.id},
                                ${p.cuotaMensual},
                                ${p.saldoPendiente})">
                                <i class="fa-solid fa-coins me-2"
                                   style="color:#C4A35A"></i>
                                Abonar cuota mensual
                            </button>
                            <button onclick="NominaPrestamos
                                .abonarPersonalizado(${p.id},
                                ${p.saldoPendiente})">
                                <i class="fa-solid fa-pen-to-square me-2"
                                   style="color:#1D4ED8"></i>
                                Abonar cantidad
                            </button>
                            <button onclick="NominaPrestamos
                                .pagarCompleto(${p.id},
                                ${p.saldoPendiente})">
                                <i class="fa-solid fa-circle-check me-2"
                                   style="color:#16A34A"></i>
                                Pagar deuda completa
                            </button>
                            <div style="border-top:1px solid #F1F5F9;
                                        margin:4px 0"></div>
                            <button style="color:#DC2626"
                                onclick="NominaPrestamos.cancelar(${p.id})">
                                <i class="fa-solid fa-ban me-2"></i>
                                Cancelar préstamo
                            </button>
                        </div>
                    </div>`;
            } else if (p.estado === 'Cancelado') {
                acciones = `
                    <button class="btn-action btn-eliminar"
                        title="Eliminar"
                        onclick="NominaPrestamos.eliminar(${p.id})">
                        <i class="fa-solid fa-trash"></i>
                    </button>`;
            } else {
                // Completado — mostrar eliminar
                acciones = `
                    <button class="btn-action btn-eliminar"
                        title="Eliminar préstamo liquidado"
                        onclick="NominaPrestamos.eliminar(${p.id})">
                        <i class="fa-solid fa-trash"></i>
                    </button>`;
            }

            return `
                <tr>
                    <td><span class="row-num">${i + 1}</span></td>
                    <td>
                        <div class="fw-600">${p.nombreEmpleado}</div>
                        <small class="text-muted">
                            ${p.departamento}
                        </small>
                    </td>
                    <td class="monospace">
                        Q ${Number(p.montoTotal).toLocaleString('es-GT',
                { minimumFractionDigits: 2 })}
                    </td>
                    <td class="monospace">
                        Q ${Number(p.cuotaMensual).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}
                    </td>
                    <td>
                        <div style="font-size:12px;margin-bottom:3px">
                            ${p.cuotasPagadas}/${p.numeroCuotas}
                            <span style="color:#94A3B8;font-size:11px">
                                (${pct}%)
                            </span>
                        </div>
                        <div class="prestamo-progress">
                            <div class="prestamo-progress-bar"
                                 style="width:${pct}%;
                                 background:${pct >= 100
                    ? '#16A34A'
                    : pct >= 50
                        ? '#C4A35A'
                        : '#3B82F6'}">
                            </div>
                        </div>
                    </td>
                    <td class="monospace fw-600"
                        style="color:${p.saldoPendiente > 0
                    ? '#DC2626' : '#16A34A'}">
                        Q ${Number(p.saldoPendiente).toLocaleString('es-GT',
                        { minimumFractionDigits: 2 })}
                    </td>
                    <td>
                        <span class="badge-prestamo-${p.estado.toLowerCase()}">
                            ${p.estado}
                        </span>
                    </td>
                    <td style="font-size:12px">${p.fechaInicio}</td>
                    <td class="text-center">
                        <div style="display:inline-flex;
                                    align-items:center;gap:4px">
                            <button class="btn-action btn-ver"
                                title="Ver detalle"
                                onclick="NominaPrestamos
                                    .verDetalle(${p.id})">
                                <i class="fa-solid fa-eye"></i>
                            </button>
                            ${acciones}
                        </div>
                    </td>
                </tr>`;
        }).join('');

        this._initCerrarMenus();
    },

    toggleMenu(event, id) {
        event.stopPropagation();
        document.querySelectorAll('.prestamo-dropdown')
            .forEach(m => {
                if (m.id !== `menu-prestamo-${id}`)
                    m.classList.remove('open');
            });
        document.getElementById(`menu-prestamo-${id}`)
            ?.classList.toggle('open');
    },

    _initCerrarMenus() {
        document.removeEventListener('click',
            this._cerrarMenusHandler);
        this._cerrarMenusHandler = () => {
            document.querySelectorAll('.prestamo-dropdown')
                .forEach(m => m.classList.remove('open'));
        };
        document.addEventListener('click',
            this._cerrarMenusHandler);
    },

    async abonarCuota(id, cuotaMensual, saldoPendiente) {
        document.querySelectorAll('.prestamo-dropdown')
            .forEach(m => m.classList.remove('open'));

        const montoReal = Math.min(cuotaMensual, saldoPendiente);

        const ok = await BsModal.fire({
            title: 'Abonar cuota mensual',
            html: `
                <p style="font-size:13px;color:#64748B;margin-bottom:16px">
                    Se registrará el siguiente abono:
                </p>
                <div style="background:#F8FAFC;border-radius:8px;
                            padding:16px;text-align:center">
                    <div style="font-size:28px;font-weight:700;
                                color:#C4A35A">
                        Q ${Number(montoReal).toLocaleString('es-GT',
                { minimumFractionDigits: 2 })}
                    </div>
                    <div style="font-size:12px;color:#64748B;
                                margin-top:4px">
                        Cuota mensual
                    </div>
                </div>
                <p style="font-size:12px;color:#94A3B8;margin-top:12px">
                    Saldo actual: Q ${Number(saldoPendiente)
                    .toLocaleString('es-GT',
                        { minimumFractionDigits: 2 })}
                </p>`,
            confirmButtonText:
                '<i class="fa-solid fa-check me-1"></i> Confirmar abono',
            confirmButtonColor: '#C4A35A',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;
        await this._enviarAbono(id, montoReal);
    },

    async abonarPersonalizado(id, saldoPendiente) {
        document.querySelectorAll('.prestamo-dropdown')
            .forEach(m => m.classList.remove('open'));

        const { value: monto } = await BsModal.fire({
            title: 'Abonar cantidad',
            html: `
                <div style="text-align:left">
                    <p style="font-size:13px;color:#64748B;
                              margin-bottom:16px">
                        Saldo actual:
                        <strong style="color:#DC2626">
                            Q ${Number(saldoPendiente).toLocaleString(
                'es-GT', { minimumFractionDigits: 2 })}
                        </strong>
                    </p>
                    <label style="font-size:12px;color:#64748B;
                           font-weight:600;display:block;
                           margin-bottom:4px">
                        Monto a abonar (Q)
                        <span style="color:#DC2626">*</span>
                    </label>
                    <input id="montoAbono" type="number"
                           class="form-control form-control-sm"
                           placeholder="0.00" step="0.01">
                    <div id="previewNuevoSaldo"
                         style="margin-top:10px;background:#F8FAFC;
                                border-radius:6px;padding:10px 14px;
                                font-size:12px;display:none">
                        Nuevo saldo:
                        <strong id="nuevoSaldoVal"
                                style="color:#DC2626">—</strong>
                    </div>
                </div>`,
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 400,
            didOpen: () => {
                document.getElementById('montoAbono')
                    ?.addEventListener('input', function () {
                        const m = parseFloat(this.value);
                        const prev = document.getElementById(
                            'previewNuevoSaldo');
                        const val = document.getElementById(
                            'nuevoSaldoVal');
                        if (m > 0 && m <= saldoPendiente) {
                            const nuevo = saldoPendiente - m;
                            val.textContent = `Q ${nuevo
                                .toLocaleString('es-GT',
                                    { minimumFractionDigits: 2 })}`;
                            val.style.color = nuevo <= 0
                                ? '#16A34A' : '#DC2626';
                            prev.style.display = 'block';
                        } else {
                            prev.style.display = 'none';
                        }
                    });
            },
            preConfirm: () => {
                const m = parseFloat(
                    document.getElementById('montoAbono').value);
                if (!m || m <= 0)
                    return BsModal.showValidationMessage(
                        'Ingresa un monto válido mayor a Q0');
                if (m > saldoPendiente)
                    return BsModal.showValidationMessage(
                        `No puede superar el saldo de Q${Number(saldoPendiente)
                            .toLocaleString('es-GT')}`);
                return m;
            }
        });

        if (!monto) return;
        await this._enviarAbono(id, monto);
    },

    async pagarCompleto(id, saldoPendiente) {
        document.querySelectorAll('.prestamo-dropdown')
            .forEach(m => m.classList.remove('open'));

        const ok = await BsModal.fire({
            title: 'Liquidar deuda completa',
            html: `
                <p style="font-size:13px;color:#64748B;margin-bottom:16px">
                    Se pagará el saldo total pendiente:
                </p>
                <div style="background:#ECFDF5;border-radius:8px;
                            padding:16px;text-align:center;
                            border:1px solid #A7F3D0">
                    <div style="font-size:28px;font-weight:700;
                                color:#065F46">
                        Q ${Number(saldoPendiente).toLocaleString('es-GT',
                { minimumFractionDigits: 2 })}
                    </div>
                    <div style="font-size:12px;color:#065F46;
                                margin-top:4px">
                        Saldo total a liquidar
                    </div>
                </div>
                <p style="font-size:12px;color:#94A3B8;margin-top:12px">
                    El préstamo quedará marcado como
                    <strong>Completado</strong>.
                </p>`,
            confirmButtonText:
                '<i class="fa-solid fa-circle-check me-1"></i>'
                + ' Liquidar ahora',
            confirmButtonColor: '#16A34A',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(
                `/Nomina/PagarDeudaCompleta/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                await this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    },

    async _enviarAbono(id, monto) {
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/AbonarCuota/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ monto })
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                await this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al registrar abono.');
        }
    },

    // ── Métodos existentes (sin cambios) ──

    filtrar(estado) {
        const filtrados = estado
            ? this._datos.filter(p => p.estado === estado)
            : this._datos;
        this.renderizar(filtrados);
    },

    async nuevo() {
        const empRes = await fetch('/Personal/GetAll', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).then(r => r.json());

        const empleados = (empRes.data || []).filter(e => e.estado === 'Activo');
        const optsEmp = empleados.map(e =>
            `<option value="${e.id}">${e.nombreCompleto} — ${e.departamento}</option>`
        ).join('');

        const { value: form } = await BsModal.fire({
            title: 'Registrar préstamo',
            html: `
                <div style="text-align:left">
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">
                            Empleado <span style="color:#DC2626">*</span>
                        </label>
                        <select id="prestEmp" class="form-select form-select-sm">
                            <option value="">— Seleccionar —</option>
                            ${optsEmp}
                        </select>
                    </div>
                    <div style="display:grid;grid-template-columns:1fr 1fr;
                                gap:10px;margin-bottom:12px">
                        <div>
                            <label style="font-size:12px;color:#64748B;font-weight:600;
                                   display:block;margin-bottom:4px">
                                Monto total (Q) <span style="color:#DC2626">*</span>
                            </label>
                            <input id="prestMonto" type="number"
                                   class="form-control form-control-sm"
                                   step="0.01" placeholder="0.00">
                        </div>
                        <div>
                            <label style="font-size:12px;color:#64748B;font-weight:600;
                                   display:block;margin-bottom:4px">
                                Número de cuotas
                            </label>
                            <input id="prestCuotas" type="number"
                                   class="form-control form-control-sm"
                                   value="12">
                        </div>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">Motivo</label>
                        <input id="prestMotivo" type="text"
                               class="form-control form-control-sm"
                               placeholder="Ej: Emergencia familiar...">
                    </div>
                    <div id="prestPreview"
                         style="background:#F8FAFC;border-radius:6px;
                                padding:10px 14px;font-size:12px;
                                color:#475569;display:none">
                        Cuota mensual estimada:
                        <strong id="prestCuotaVal">—</strong>
                    </div>
                </div>`,
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 480,
            didOpen: () => {
                const calcular = () => {
                    const m = parseFloat(document.getElementById('prestMonto').value);
                    const c = parseInt(document.getElementById('prestCuotas').value);
                    const prev = document.getElementById('prestPreview');
                    const val = document.getElementById('prestCuotaVal');
                    if (m > 0 && c > 0) {
                        val.textContent = `Q ${(m / c).toLocaleString('es-GT',
                            { minimumFractionDigits: 2 })}`;
                        prev.style.display = 'block';
                    }
                };
                document.getElementById('prestMonto')
                    ?.addEventListener('input', calcular);
                document.getElementById('prestCuotas')
                    ?.addEventListener('input', calcular);
            },
            preConfirm: () => {
                const empId = document.getElementById('prestEmp').value;
                const monto = parseFloat(document.getElementById('prestMonto').value);
                const cuotas = parseInt(document.getElementById('prestCuotas').value);
                if (!empId)
                    return BsModal.showValidationMessage('Selecciona un empleado');
                if (!monto || monto < 100)
                    return BsModal.showValidationMessage('El monto mínimo es Q100');
                if (!cuotas || cuotas < 1)
                    return BsModal.showValidationMessage('Ingresa el número de cuotas');
                return {
                    empleadoId: parseInt(empId),
                    montoTotal: monto,
                    numeroCuotas: cuotas,
                    cuotaMensual: Math.round((monto / cuotas) * 100) / 100,
                    motivo: document.getElementById('prestMotivo').value,
                    fechaInicio: new Date().toISOString().split('T')[0]
                };
            }
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Nomina/CrearPrestamo', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(form)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                await this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al registrar préstamo.');
        }
    },

    async cancelar(id) {
        const ok = await BsModal.fire({
            title: '¿Cancelar este préstamo?',
            html: `<p style="font-size:14px;color:#64748B;text-align:center;margin:0">
                      El saldo pendiente se marcará como cancelado.<br>
                      Podrás eliminarlo después si lo deseas.
                   </p>`,
            icon: 'warning',
            confirmButtonText: 'Sí, cancelar',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'No',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/CancelarPrestamo/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                await this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al cancelar.');
        }
    },

    async eliminar(id) {
        const ok = await BsModal.fire({
            title: '¿Eliminar este préstamo?',
            html: `<p style="font-size:14px;color:#64748B;text-align:center;margin:0">
                      Se eliminará permanentemente el registro.<br>
                      <strong>Esta acción no se puede deshacer.</strong>
                   </p>`,
            icon: 'warning',
            confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/EliminarPrestamo/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                await this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al eliminar.');
        }
    },

    verDetalle(id) {
        const p = this._datos.find(x => x.id === id);
        if (!p) return;
        const pct = p.numeroCuotas > 0
            ? Math.round((p.cuotasPagadas / p.numeroCuotas) * 100) : 0;

        BsModal.fire({
            title: `Préstamo — ${p.nombreEmpleado}`,
            html: `
                <table style="width:100%;font-size:13px;text-align:left">
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Monto total</td>
                        <td class="monospace fw-600">
                            Q ${Number(p.montoTotal).toLocaleString('es-GT',
                { minimumFractionDigits: 2 })}
                        </td>
                    </tr>
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Cuota mensual</td>
                        <td class="monospace">
                            Q ${Number(p.cuotaMensual).toLocaleString('es-GT',
                    { minimumFractionDigits: 2 })}
                        </td>
                    </tr>
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Progreso</td>
                        <td>${p.cuotasPagadas}/${p.numeroCuotas} cuotas (${pct}%)</td>
                    </tr>
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Saldo pendiente</td>
                        <td class="monospace" style="color:#DC2626;font-weight:700">
                            Q ${Number(p.saldoPendiente).toLocaleString('es-GT',
                        { minimumFractionDigits: 2 })}
                        </td>
                    </tr>
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Estado</td>
                        <td>
                            <span class="badge-prestamo-${p.estado.toLowerCase()}">
                                ${p.estado}
                            </span>
                        </td>
                    </tr>
                    <tr>
                        <td style="color:#64748B;padding:6px 0">Motivo</td>
                        <td>${p.motivo || '—'}</td>
                    </tr>
                </table>`,
            confirmButtonText: 'Cerrar'
        });
    }
};

// ════════════════════════════════════════════
// SUBMÓDULO — Conceptos
// ════════════════════════════════════════════
window.NominaConceptos = {
    _loaded: false,
    _datos: [],

    async cargar() {
        this._loaded = true;
        try {
            const res = await fetch('/Nomina/GetConceptos', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());
            this._datos = res.data || [];
            this.renderizar(this._datos);
            const el = document.getElementById('totalConceptos');
            if (el) el.textContent = `${this._datos.length} conceptos`;
        } catch {
            Notify.error('Error al cargar conceptos.');
        }
    },

    renderizar(datos) {
        const tbody = document.getElementById('bodyConceptos');
        if (!tbody) return;

        if (!datos.length) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-muted py-4">
                        No hay conceptos registrados
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = datos.map((c, i) => `
            <tr>
                <td><span class="row-num">${i + 1}</span></td>
                <td>
                    <code style="background:#F1F5F9;padding:2px 6px;
                                 border-radius:4px;font-size:11px">
                        ${c.codigo}
                    </code>
                </td>
                <td>
                    <div class="fw-600">${c.nombre}</div>
                    ${c.descripcion
                ? `<small class="text-muted">${c.descripcion}</small>`
                : ''}
                </td>
                <td>
                    <span class="badge-concepto-${c.tipo.toLowerCase()}">
                        ${c.tipo}
                    </span>
                </td>
                <td class="text-muted" style="font-size:12px">${c.aplicacion}</td>
                <td class="monospace">
                    ${c.aplicacion === 'Porcentaje'
                ? `${c.valor}%`
                : c.aplicacion === 'MontoFijo'
                    ? `Q ${Number(c.valor).toFixed(2)}`
                    : `×${c.valor}`}
                </td>
                <td class="text-center">
                    ${c.esObligatorio
                ? '<span class="badge-sistema">Sí</span>'
                : '<span style="color:#94A3B8;font-size:12px">No</span>'}
                </td>
                <td>
                    <span class="badge-nomina-${c.activo ? 'procesada' : 'anulada'}">
                        ${c.activo ? 'Activo' : 'Inactivo'}
                    </span>
                </td>
                <td class="text-center">
                    ${!c.esSistema ? `
                    <div style="display:inline-flex;gap:4px">
                        <button class="btn-action btn-editar" title="Editar"
                            onclick="NominaConceptos.editar(${c.id})">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button class="btn-action btn-eliminar" title="Eliminar"
                            onclick="NominaConceptos.eliminar(${c.id})">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                    </div>` : `<span class="badge-sistema">Sistema</span>`}
                </td>
            </tr>`
        ).join('');
    },

    filtrar(tipo) {
        const filtrados = tipo
            ? this._datos.filter(c => c.tipo === tipo)
            : this._datos;
        this.renderizar(filtrados);
    },

    async nuevo() {
        const { value: form } = await BsModal.fire({
            title: 'Nuevo concepto de nómina',
            html: this._formHtml(),
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 460,
            preConfirm: () => this._leerForm()
        });
        if (!form) return;
        await this._guardar('/Nomina/CrearConcepto', form);
    },

    async editar(id) {
        const c = this._datos.find(x => x.id === id);
        if (!c) return;
        const { value: form } = await BsModal.fire({
            title: 'Editar concepto',
            html: this._formHtml(c),
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 460,
            preConfirm: () => this._leerForm()
        });
        if (!form) return;
        await this._guardar(`/Nomina/EditarConcepto/${id}`, form);
    },

    _formHtml(c = {}) {
        return `
            <div style="text-align:left">
                <div style="display:grid;grid-template-columns:1fr 1fr;
                            gap:10px;margin-bottom:12px">
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">
                            Código <span style="color:#DC2626">*</span>
                        </label>
                        <input id="cCodigo" class="form-control form-control-sm"
                               value="${c.codigo || ''}"
                               placeholder="Ej: BONO-PROD"
                               ${c.esSistema ? 'disabled' : ''}>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">Tipo</label>
                        <select id="cTipo" class="form-select form-select-sm">
                            <option value="Devengado"
                                ${c.tipo === 'Devengado' ? 'selected' : ''}>
                                Devengado
                            </option>
                            <option value="Deduccion"
                                ${c.tipo === 'Deduccion' ? 'selected' : ''}>
                                Deducción
                            </option>
                        </select>
                    </div>
                </div>
                <div style="margin-bottom:12px">
                    <label style="font-size:12px;color:#64748B;font-weight:600;
                           display:block;margin-bottom:4px">
                        Nombre <span style="color:#DC2626">*</span>
                    </label>
                    <input id="cNombre" class="form-control form-control-sm"
                           value="${c.nombre || ''}"
                           placeholder="Nombre del concepto">
                </div>
                <div style="display:grid;grid-template-columns:1fr 1fr;
                            gap:10px;margin-bottom:12px">
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">Aplicación</label>
                        <select id="cAplicacion" class="form-select form-select-sm">
                            <option value="MontoFijo"
                                ${c.aplicacion === 'MontoFijo' ? 'selected' : ''}>
                                Monto fijo
                            </option>
                            <option value="Porcentaje"
                                ${c.aplicacion === 'Porcentaje' ? 'selected' : ''}>
                                Porcentaje
                            </option>
                        </select>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;
                               display:block;margin-bottom:4px">Valor</label>
                        <input id="cValor" type="number"
                               class="form-control form-control-sm"
                               value="${c.valor || 0}" min="0" step="0.01">
                    </div>
                </div>
                <div>
                    <label style="font-size:12px;color:#64748B;font-weight:600;
                           display:block;margin-bottom:4px">
                        Descripción (opcional)
                    </label>
                    <textarea id="cDesc" class="form-control form-control-sm"
                              style="height:60px"
                              placeholder="Descripción..."
                              >${c.descripcion || ''}</textarea>
                </div>
            </div>`;
    },

    _leerForm() {
        const codigo = document.getElementById('cCodigo')?.value?.trim();
        const nombre = document.getElementById('cNombre')?.value?.trim();
        if (!codigo)
            return BsModal.showValidationMessage('El código es requerido');
        if (!nombre)
            return BsModal.showValidationMessage('El nombre es requerido');
        return {
            codigo,
            nombre,
            tipo: document.getElementById('cTipo').value,
            aplicacion: document.getElementById('cAplicacion').value,
            valor: parseFloat(document.getElementById('cValor').value) || 0,
            descripcion: document.getElementById('cDesc').value,
            activo: true,
            esObligatorio: false,
            esSistema: false
        };
    },

    async _guardar(url, form) {
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(form)
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al guardar concepto.');
        }
    },

    async eliminar(id) {
        const ok = await BsModal.fire({
            title: '¿Eliminar concepto?',
            text: 'Se eliminará permanentemente.',
            icon: 'warning',
            confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Nomina/EliminarConcepto/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                this._loaded = false;
                this.cargar();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al eliminar.');
        }
    }
};

// ── Auto-init ──
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('tablaNomina')) Nomina.init();

    document.getElementById('btnNuevoPrestamo')
        ?.addEventListener('click', () => NominaPrestamos.nuevo());

    document.getElementById('btnNuevoConcepto')
        ?.addEventListener('click', () => NominaConceptos.nuevo());
});

document.addEventListener('spaNavigated', () => {
    if (document.getElementById('tablaNomina')) {
        Nomina.init();
        NominaSalarios._loaded = false;
        NominaPrestamos._loaded = false;
        NominaConceptos._loaded = false;

        document.getElementById('btnNuevoPrestamo')
            ?.addEventListener('click', () => NominaPrestamos.nuevo());
        document.getElementById('btnNuevoConcepto')
            ?.addEventListener('click', () => NominaConceptos.nuevo());
    }
});