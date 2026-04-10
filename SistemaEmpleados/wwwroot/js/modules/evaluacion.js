/* ══════════════════════════════════════════════
   SICE — Módulo Evaluación de Desempeño
══════════════════════════════════════════════ */

window.Evaluacion = (() => {

    let table = null;
    let modoKPI = false;

    function init() {
        limpiarModal();
        initDataTable();
        initEvents();
        loadDepartamentosFilter();
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
        if ($.fn.DataTable.isDataTable('#tablaEvaluacion')) {
            $('#tablaEvaluacion').DataTable().destroy();
        }

        table = $('#tablaEvaluacion').DataTable({
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Evaluacion/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify({
                    draw: d.draw, start: d.start, length: d.length,
                    searchValue: d.search?.value || '',
                    departamentoId: document.getElementById('filtroDeptoEval')?.value || null,
                    estado: document.getElementById('filtroEstadoEval')?.value || ''
                }),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            },
            columns: [
                { data: null, orderable: false, render: (_, __, ___, m) => `<span class="row-num">${m.row + 1}</span>` },
                {
                    data: null,
                    render: row => {
                        const avatar = row.fotoUrl
                            ? `<img src="${row.fotoUrl}" class="avatar-circle me-2">`
                            : `<div class="avatar-circle me-2">${row.iniciales}</div>`;
                        return `<div class="d-flex align-items-center gap-2">
                            ${avatar}
                            <div>
                                <div class="fw-600">${row.nombreEmpleado}</div>
                                <small class="text-muted">${row.nombreEvaluador}</small>
                            </div>
                        </div>`;
                    }
                },
                { data: 'departamento' },
                { data: 'puesto' },
                { data: 'periodo', render: v => `<span class="periodo-badge">${v}</span>` },
                { data: 'tipoEvaluacion', render: v => `<span class="tipo-eval-badge">${v}</span>` },
                {
                    data: 'puntajeTotal',
                    render: v => `<div class="puntaje-bar-wrap">
                        <div class="puntaje-bar" style="width:${v}%"></div>
                        <span class="monospace">${Number(v).toFixed(1)}</span>
                    </div>`
                },
                {
                    data: 'calificacion',
                    render: v => `<span class="cal-badge cal-${v.toLowerCase().replace(' ', '')}">${v}</span>`
                },
                {
                    data: 'estado',
                    render: v => `<span class="badge-eval-${v.toLowerCase()}">${v}</span>`
                },
                { data: 'fechaEvaluacion' },
                {
                    data: 'id', orderable: false, className: 'text-center',
                    render: id => `
                        <button class="btn-action btn-editar" title="Editar"
                            onclick="Evaluacion.openForm(${id})">
                            <i class="fa-solid fa-pen"></i>
                        </button>
                        <button class="btn-action btn-eliminar" title="Eliminar"
                            onclick="Evaluacion.eliminar(${id})">
                            <i class="fa-solid fa-trash"></i>
                        </button>`
                }
            ],
            language: { url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json' },
            pageLength: 15, order: [[9, 'desc']],
            drawCallback(s) {
                const el = document.getElementById('totalEvaluaciones');
                if (el && s.json) el.textContent = `${s.json.recordsFiltered} evaluaciones`;
            }
        });
    }

    async function loadDepartamentosFilter() {
        try {
            const res = await Http.get('/api/departamentos');
            if (!res.success) return;
            const sel = document.getElementById('filtroDeptoEval');
            if (!sel) return;
            sel.querySelectorAll('option:not([value=""])').forEach(o => o.remove());
            res.data.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.id; opt.textContent = d.nombre;
                sel.appendChild(opt);
            });
        } catch (_) { }
    }

    function initEvents() {
        const btnNuevo = document.getElementById('btnNuevaEvaluacion');
        if (btnNuevo) {
            const clone = btnNuevo.cloneNode(true);
            btnNuevo.parentNode.replaceChild(clone, btnNuevo);
            clone.addEventListener('click', () => { modoKPI = false; openForm(null); });
        }

        const btnKPI = document.getElementById('btnGestionarKPIs');
        if (btnKPI) {
            const clone = btnKPI.cloneNode(true);
            btnKPI.parentNode.replaceChild(clone, btnKPI);
            clone.addEventListener('click', () => { modoKPI = true; openFormKPI(null); });
        }

        ['filtroDeptoEval', 'filtroEstadoEval'].forEach(id => {
            document.getElementById(id)?.addEventListener('change', () => table?.ajax.reload());
        });

        document.getElementById('btnLimpiarEval')?.addEventListener('click', () => {
            document.getElementById('filtroDeptoEval').value = '';
            document.getElementById('filtroEstadoEval').value = '';
            table?.ajax.reload();
        });

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.initialized) {
            modalEl.dataset.initialized = 'true';

            modalEl.addEventListener('submit', async e => {
                e.preventDefault();
                if (e.target.id === 'formEvaluacion') await guardar(e.target);
                else if (e.target.id === 'formKPI') await guardarKPI(e.target);
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
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border" style="color:#9B8EC4;width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        getModal()?.show();

        try {
            const url = id ? `/Evaluacion/Form?id=${id}` : '/Evaluacion/Form';
            body.innerHTML = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar</p>
            </div>`;
        }
    }

    async function openFormKPI(id) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border" style="color:#9B8EC4;width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        getModal()?.show();

        try {
            const url = id ? `/Evaluacion/FormKPI?id=${id}` : '/Evaluacion/FormKPI';
            body.innerHTML = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar</p>
            </div>`;
        }
    }

    async function guardar(form) {
        const btn = form.querySelector('#btnGuardarEval');
        FormHelper.clearErrors(form);
        FormHelper.setLoading(btn, true);

        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0 ? `/Evaluacion/Edit/${id}` : '/Evaluacion/Create';

        // Construir resultados desde los sliders
        const resultados = [];
        const rows = form.querySelectorAll('.kpi-row-item');
        rows.forEach((row, i) => {
            const kpiId = row.querySelector(`[name="Resultados[${i}].KPIId"]`)?.value;
            const cal = row.querySelector(`[name="Resultados[${i}].Calificacion"]`)?.value;
            const obs = row.querySelector(`[name="Resultados[${i}].Observacion"]`)?.value;
            if (kpiId) {
                resultados.push({
                    kpiId: parseInt(kpiId),
                    calificacion: parseFloat(cal) || 0,
                    observacion: obs || null
                });
            }
        });

        const payload = {
            id: id,
            empleadoId: parseInt(data.EmpleadoId) || 0,
            periodo: data.Periodo,
            tipoEvaluacion: parseInt(data.TipoEvaluacion) || 180,
            estado: parseInt(data.Estado) || 0,
            fechaEvaluacion: data.FechaEvaluacion,
            comentarios: data.Comentarios,
            planMejora: data.PlanMejora,
            resultados
        };

        try {
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            }).then(r => r.json());

            if (res.success) {
                cerrarModal();
                Notify.success(res.message);
                table?.ajax.reload(null, false);
            } else {
                Notify.error(res.message || 'Error al guardar');
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    async function guardarKPI(form) {
        const btn = form.querySelector('#btnGuardarKPI');
        FormHelper.clearErrors(form);
        FormHelper.setLoading(btn, true);

        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0 ? `/Evaluacion/EditKPI/${id}` : '/Evaluacion/CreateKPI';

        const payload = {
            id: id,
            nombre: data.Nombre,
            descripcion: data.Descripcion,
            peso: parseFloat(data.Peso) || 0,
            puestoId: parseInt(data.PuestoId) || null,
            activo: data.Activo === 'true' || data.Activo === 'on' || !!data.Activo
        };

        try {
            const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(payload)
            }).then(r => r.json());

            if (res.success) {
                cerrarModal();
                Notify.success(res.message);
            } else {
                Notify.error(res.message || 'Error al guardar');
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    async function eliminar(id) {
        const ok = await Confirm.delete('esta evaluación');
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch(`/Evaluacion/Delete/${id}`, {
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
            Notify.error('Error al procesar.');
        }
    }

    return { init, openForm, openFormKPI, eliminar, cerrarModal };
})();

if (document.getElementById('tablaEvaluacion')) {
    Evaluacion.init();
}