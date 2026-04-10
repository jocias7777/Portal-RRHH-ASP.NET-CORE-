/* ══════════════════════════════════════════════
   SICE — Módulo Reclutamiento completo
   Pipeline | Entrevistas | Notas | Convertir
══════════════════════════════════════════════ */

window.BsModal = window.BsModal || {
    fire: async (options) => {
        const modalEl = document.getElementById('modalEmpleado');
        const modalInst = modalEl ? bootstrap.Modal.getInstance(modalEl) : null;
        const focusTrap = modalInst?._focustrap;

        // Evita que el foco del modal Bootstrap bloquee inputs de SweetAlert
        focusTrap?.deactivate();
        try {
            return await Swal.fire(options);
        } finally {
            if (modalEl?.classList.contains('show')) {
                focusTrap?.activate();
            }
        }
    },
    showValidationMessage: (message) => Swal.showValidationMessage(message),
    loadingShow: (title, text) => Swal.fire({
        title: title || 'Procesando...',
        text: text || '',
        allowOutsideClick: false,
        allowEscapeKey: false,
        showConfirmButton: false,
        didOpen: () => Swal.showLoading()
    }),
    loadingHide: () => Swal.close()
};

window.Reclutamiento = (() => {

    let tablaPlazas = null;
    let tablaCandidatos = null;

    const BsModal = window.BsModal;

    // ════════════════════════════════════════════
    // INIT
    // ════════════════════════════════════════════
    function init() {
        limpiarModal();
        initTablaPlazas();
        initTablaCandidatos();
        initEvents();
        loadDepartamentosFilter();
        cargarEstadisticas();
    }

    function limpiarModal() {
        document.querySelectorAll('.modal-backdrop')
            .forEach(el => el.remove());
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
        if (!inst) inst = new bootstrap.Modal(modalEl,
            { backdrop: true, keyboard: true });
        return inst;
    }

    // ════════════════════════════════════════════
    // ESTADÍSTICAS KPI
    // ════════════════════════════════════════════
    async function cargarEstadisticas() {
        try {
            const res = await fetch('/Reclutamiento/Estadisticas', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            const set = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val;
            };

            set('kpiPlazasAbiertas', res.plazasAbiertas);
            set('kpiPlazasEnProceso', res.plazasEnProceso);
            set('kpiTotalCandidatos', res.totalCandidatos);
            set('kpiContratados', res.contratados);
            set('kpiTiempoPromedio',
                `${res.tiempoPromedioContratacion} días`);
            set('kpiTasaConversion', `${res.tasaConversion}%`);
        } catch { }
    }

    // ════════════════════════════════════════════
    // TABLA PLAZAS
    // ════════════════════════════════════════════
    function initTablaPlazas() {
        if ($.fn.DataTable.isDataTable('#tablaPlazas'))
            $('#tablaPlazas').DataTable().destroy();

        tablaPlazas = $('#tablaPlazas').DataTable({
            dom: '<"toolbar-search"f>rtip',
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Reclutamiento/GetPlazas',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    searchValue: d.search?.value || '',
                    departamentoId: document.getElementById(
                        'filtroDeptoPlaza')?.value || null,
                    estado: document.getElementById(
                        'filtroEstadoPlaza')?.value || ''
                }),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            },
            columns: [
                {
                    data: null, orderable: false,
                    render: (_, __, ___, m) =>
                        `<span class="row-num">${m.row + 1}</span>`
                },
                {
                    data: null,
                    render: row => {
                        const badge = row.esReemplazo
                            ? `<span style="background:#FEF3C7;
                               color:#92400E;font-size:10px;
                               padding:1px 6px;border-radius:8px;
                               margin-left:6px">Reemplazo</span>`
                            : '';
                        return `<div class="fw-600">
                                    ${row.titulo}${badge}
                                </div>
                                <small class="text-muted">
                                    ${row.fuenteReclutamiento || ''}
                                </small>`;
                    }
                },
                { data: 'departamento' },
                {
                    data: 'puesto',
                    render: v => v || '<span class="text-muted">—</span>'
                },
                {
                    data: 'salarioOfrecido',
                    render: v => v > 0
                        ? `<span class="monospace">Q ${Number(v).toLocaleString('es-GT',
                            { minimumFractionDigits: 2 })}</span>`
                        : '<span class="text-muted">A convenir</span>'
                },
                {
                    data: 'cantidadVacantes',
                    render: v =>
                        `<span class="monospace">${v}</span>`
                },
                {
                    data: null,
                    render: row => {
                        const total = row.totalCandidatos;
                        const activos = row.candidatosActivos;
                        return `
                            <button class="btn-candidatos-count"
                                title="Ver candidatos"
                                onclick="Reclutamiento
                                    .verDetallePlaza(${row.id})">
                                <span class="candidatos-main">
                                    <i class="fa-solid fa-users"></i>
                                    <strong>${total}</strong>
                                </span>
                                ${activos > 0
                                ? `<small class="candidatos-sub">activos</small>`
                                : `<small class="candidatos-sub">sin activos</small>`}
                            </button>`;
                    }
                },
                {
                    data: 'estado',
                    render: v =>
                        `<span class="badge-plaza-${v.toLowerCase().replace(' ', '')}">${v}</span>`
                },
                {
                    data: null,
                    render: row => {
                        const dias = row.diasAbierta;
                        const color = dias > 60
                            ? '#DC2626' : dias > 30
                                ? '#D97706' : '#16A34A';
                        return `<div>${row.fechaPublicacion}</div>
                                <small style="color:${color}">
                                    ${dias} días abierta
                                </small>`;
                    }
                },
                {
                    data: 'fechaCierre',
                    render: v => v ||
                        '<span class="text-muted">—</span>'
                },
                {
                    data: 'id', orderable: false,
                    className: 'text-center',
                    render: id => `
                        <div style="display:inline-flex;
                                    align-items:center;gap:4px">
                            <button class="btn-action btn-ver"
                                title="Ver detalle"
                                onclick="Reclutamiento
                                    .verDetallePlaza(${id})">
                                <i class="fa-solid fa-eye"></i>
                            </button>
                            <button class="btn-action btn-editar"
                                title="Editar"
                                onclick="Reclutamiento
                                    .openFormPlaza(${id})">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn-action"
                                title="Cambiar estado"
                                style="background:#EDE9FE;color:#6D28D9"
                                onclick="Reclutamiento
                                    .cambiarEstadoPlaza(${id})">
                                <i class="fa-solid fa-arrows-rotate">
                                </i>
                            </button>
                            <button class="btn-action btn-eliminar"
                                title="Eliminar"
                                onclick="Reclutamiento
                                    .eliminarPlaza(${id})">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>`
                }
            ],
            language: {
                url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json'
            },
            pageLength: 15,
            order: [[8, 'desc']],
            drawCallback(s) {
                const el = document.getElementById('badgePlazas');
                if (el && s.json)
                    el.textContent = s.json.recordsFiltered;
                const tot = document.getElementById('totalPlazas');
                if (tot && s.json)
                    tot.textContent =
                        `${s.json.recordsFiltered} plazas`;
            }
        });
    }

    // ════════════════════════════════════════════
    // TABLA CANDIDATOS
    // ════════════════════════════════════════════
    function initTablaCandidatos() {
        if ($.fn.DataTable.isDataTable('#tablaCandidatos'))
            $('#tablaCandidatos').DataTable().destroy();

        tablaCandidatos = $('#tablaCandidatos').DataTable({
            dom: '<"toolbar-search"f>rtip',
            serverSide: true,
            processing: false,
            ajax: {
                url: '/Reclutamiento/GetCandidatos',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify({
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    searchValue: d.search?.value || '',
                    estado: document.getElementById(
                        'filtroEtapa')?.value || ''
                }),
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            },
            columns: [
                {
                    data: null, orderable: false,
                    render: (_, __, ___, m) =>
                        `<span class="row-num">${m.row + 1}</span>`
                },
                {
                    data: null,
                    render: row => {
                        const iconos = [];
                        if (row.totalEntrevistas > 0)
                            iconos.push(
                                `<span title="${row.totalEntrevistas}
                                 entrevista(s)"
                                 style="color:#1D4ED8;font-size:11px">
                                 <i class="fa-solid fa-comments"></i>
                                 ${row.totalEntrevistas}</span>`);
                        if (row.totalNotas > 0)
                            iconos.push(
                                `<span title="${row.totalNotas} nota(s)"
                                 style="color:#C4A35A;font-size:11px">
                                 <i class="fa-solid fa-note-sticky"></i>
                                 ${row.totalNotas}</span>`);
                        if (row.fueContratado)
                            iconos.push(
                                `<span title="Contratado"
                                 style="color:#16A34A;font-size:11px">
                                 <i class="fa-solid fa-check-circle">
                                 </i></span>`);

                        return `
                            <div>
                                <div style="font-size:13px;
                                     font-weight:600;color:var(--color-dark)">
                                    ${row.nombre}
                                    ${iconos.length
                                ? `<span style="display:inline-flex;
                                           gap:6px;margin-left:6px">
                                           ${iconos.join('')}</span>`
                                : ''}
                                </div>
                                <small class="text-muted">
                                    ${row.email}
                                    ${row.fuentePostulacion
                                ? `· ${row.fuentePostulacion}`
                                : ''}
                                </small>
                            </div>`;
                    }
                },
                { data: 'plaza' },
                { data: 'departamento' },
                {
                    data: null,
                    render: row => {
                        const colores = {
                            recibido: '#F1F5F9',
                            entrevista: '#DBEAFE',
                            pruebas: '#FEF3C7',
                            oferta: '#D1FAE5',
                            contratado: '#065F46',
                            rechazado: '#FEE2E2'
                        };
                        const textCol = {
                            recibido: '#475569',
                            entrevista: '#1D4ED8',
                            pruebas: '#92400E',
                            oferta: '#065F46',
                            contratado: '#fff',
                            rechazado: '#991B1B'
                        };
                        const etapaLow = row.etapa.toLowerCase();
                        const cal = row.calificacionGeneral > 0
                            ? `<span style="margin-left:4px;
                               font-size:10px;color:#C4A35A">
                               ★${row.calificacionGeneral}</span>`
                            : '';

                        return `
                            <select class="etapa-select"
                                style="background:${colores[etapaLow] || '#F1F5F9'};
                                color:${textCol[etapaLow] || '#475569'};
                                border:none;border-radius:10px;
                                padding:3px 8px;font-size:12px;
                                font-weight:600;cursor:pointer"
                                onchange="Reclutamiento.cambiarEtapa(
                                    ${row.id}, this.value)">
                                ${['Recibido', 'Entrevista', 'Pruebas',
                                'Oferta', 'Contratado', 'Rechazado']
                                .map((e, i) =>
                                    `<option value="${i}"
                                        ${e === row.etapa
                                        ? 'selected' : ''}>
                                        ${e}
                                    </option>`
                                ).join('')}
                            </select>${cal}`;
                    }
                },
                { data: 'fechaPostulacion' },
                {
                    data: 'fechaEntrevista',
                    render: v => v ||
                        '<span class="text-muted">—</span>'
                },
                {
                    data: 'cvUrl',
                    render: v => v
                        ? `<a href="${v}" target="_blank"
                              class="btn-cv" title="Ver CV">
                               <i class="fa-solid fa-file-pdf"></i>
                           </a>`
                        : '<span class="text-muted">—</span>'
                },
                {
                    data: 'id', orderable: false,
                    className: 'text-center',
                    render: id => `
                        <div style="display:inline-flex;align-items:center;gap:6px;justify-content:center">
                            <button class="btn-action" title="Editar"
                                onclick="Reclutamiento.openFormCandidato(${id})">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn-action" title="Eliminar"
                                onclick="Reclutamiento.eliminarCandidato(${id})">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>`
                }
            ],
            language: {
                url: 'https://cdn.datatables.net/plug-ins/1.13.8/i18n/es-MX.json'
            },
            pageLength: 15,
            order: [[5, 'desc']],
            drawCallback(s) {
                const el = document.getElementById('badgeCandidatos');
                if (el && s.json)
                    el.textContent = s.json.recordsFiltered;
                const tot = document.getElementById('totalCandidatos');
                if (tot && s.json)
                    tot.textContent =
                        `${s.json.recordsFiltered} candidatos`;
            }
        });
    }

    // ════════════════════════════════════════════
    // FILTROS Y EVENTOS
    // ════════════════════════════════════════════
    async function loadDepartamentosFilter() {
        try {
            const res = await fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            const deptos = [...new Map(
                (res.data || []).map(e =>
                    [e.departamentoId, {
                        id: e.departamentoId,
                        nombre: e.departamento
                    }])
            ).values()];

            const sel = document.getElementById('filtroDeptoPlaza');
            if (!sel) return;
            while (sel.options.length > 1) sel.remove(1);
            deptos.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.id;
                opt.textContent = d.nombre;
                sel.appendChild(opt);
            });
        } catch { }
    }

    function initEvents() {
        // Botones principales
        ['btnNuevaPlaza', 'btnNuevoCandidato'].forEach(btnId => {
            const btn = document.getElementById(btnId);
            if (btn) {
                const clone = btn.cloneNode(true);
                btn.parentNode.replaceChild(clone, btn);
                clone.addEventListener('click', () => {
                    if (btnId === 'btnNuevaPlaza')
                        openFormPlaza(null);
                    else
                        openFormCandidato(null);
                });
            }
        });

        // Filtros plazas
        ['filtroDeptoPlaza', 'filtroEstadoPlaza'].forEach(id => {
            document.getElementById(id)
                ?.addEventListener('change', () =>
                    tablaPlazas?.ajax.reload());
        });
        document.getElementById('btnLimpiarPlazas')
            ?.addEventListener('click', () => {
                document.getElementById(
                    'filtroDeptoPlaza').value = '';
                document.getElementById(
                    'filtroEstadoPlaza').value = '';
                tablaPlazas?.ajax.reload();
            });

        // Filtros candidatos
        document.getElementById('filtroEtapa')
            ?.addEventListener('change', () =>
                tablaCandidatos?.ajax.reload());
        document.getElementById('btnLimpiarCandidatos')
            ?.addEventListener('click', () => {
                document.getElementById('filtroEtapa').value = '';
                tablaCandidatos?.ajax.reload();
            });

        // Modal submit
        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.recluInit) {
            modalEl.dataset.recluInit = 'true';
            modalEl.addEventListener('submit', async e => {
                e.preventDefault();
                if (e.target.id === 'formPlaza')
                    await guardarPlaza(e.target);
                else if (e.target.id === 'formCandidato')
                    await guardarCandidato(e.target);
            });
            modalEl.addEventListener('hidden.bs.modal', () => {
                document.querySelectorAll('.modal-backdrop')
                    .forEach(el => el.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('padding-right');
                document.body.style.removeProperty('overflow');
            });
        }
    }

    // ════════════════════════════════════════════
    // FORMULARIO PLAZA
    // ════════════════════════════════════════════
    async function openFormPlaza(id) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--reclu-color);
                            width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;
                          font-size:13px">Cargando...</p>
            </div>`;

        getModal()?.show();

        try {
            const url = id
                ? `/Reclutamiento/FormPlaza?id=${id}`
                : '/Reclutamiento/FormPlaza';
            body.innerHTML = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation
                               fa-2x mb-2"></i>
                    <p>Error al cargar el formulario</p>
                </div>`;
        }
    }

    async function guardarPlaza(form) {
        const btn = form.querySelector('#btnGuardarPlaza');
        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0
            ? `/Reclutamiento/EditPlaza/${id}`
            : '/Reclutamiento/CreatePlaza';

        data.Id = id;
        data.DepartamentoId = parseInt(data.DepartamentoId) || 0;
        data.PuestoId = parseInt(data.PuestoId) || null;
        data.SalarioOfrecido = parseFloat(data.SalarioOfrecido) || 0;
        data.CantidadVacantes = parseInt(data.CantidadVacantes) || 1;
        data.Estado = parseInt(data.Estado) || 0;
        data.EsReemplazo = data.EsReemplazo === 'true'
            || data.EsReemplazo === true;
        delete data.FormType;

        FormHelper.setLoading(btn, true);
        try {
            const token = form.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
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
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
            } else {
                Notify.error(res.message || 'Error al guardar');
            }
        } catch {
            Notify.error('Error de conexión.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    // ════════════════════════════════════════════
    // DETALLE DE PLAZA — Pipeline visual
    // ════════════════════════════════════════════
    async function verDetallePlaza(id) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--reclu-color);
                            width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;
                          font-size:13px">Cargando plaza...</p>
            </div>`;

        getModal()?.show();
        await refreshDetallePlaza(id);
    }

    async function refreshDetallePlaza(id) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        try {
            const res = await fetch(`/Reclutamiento/DetallePlaza/${id}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json());

            body.innerHTML = _renderDetallePlaza(res);
        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                    <p>Error al cargar el detalle</p>
                </div>`;
        }
    }


    function _renderDetallePlaza(p) {
        const fmt = n => `Q ${Number(n).toLocaleString('es-GT',
            { minimumFractionDigits: 2 })}`;

        const etapas = [
            { nombre: 'Recibidos', val: p.recibidos, icon: 'fa-inbox' },
            { nombre: 'Entrevista', val: p.enEntrevista, icon: 'fa-comments' },
            { nombre: 'Pruebas', val: p.enPruebas, icon: 'fa-clipboard' },
            { nombre: 'Oferta', val: p.enOferta, icon: 'fa-handshake' },
            { nombre: 'Contratados', val: p.contratados, icon: 'fa-user-check' },
            { nombre: 'Rechazados', val: p.rechazados, icon: 'fa-user-xmark' }
        ];

        const estadoStyles = {
            abierta: { bg: '#10B981', color: '#ffffff' },
            enproceso: { bg: '#0d9488', color: '#ffffff' },
            cerrada: { bg: '#64748B', color: '#ffffff' },
            cancelada: { bg: '#DC2626', color: '#ffffff' }
        };
        const keyEstado = (p.estado || '').toLowerCase().replace(/\s/g, '');
        const est = estadoStyles[keyEstado] || { bg: '#0d9488', color: '#ffffff' };

        const pipelineHtml = etapas.map(e => `
            <div style="text-align:center;flex:1;padding:6px 4px">
                <div style="font-size:16px;color:#334155;margin-bottom:6px">
                    <i class="fa-solid ${e.icon}"></i>
                </div>
                <div style="font-size:22px;font-weight:800;color:#111827;line-height:1.1">${e.val}</div>
                <div style="font-size:11px;font-weight:700;color:#475569;margin-top:4px">${e.nombre}</div>
            </div>`).join(`
            <div style="width:1px;background:#E5E7EB;align-self:stretch"></div>`);

        const candidatosHtml = p.candidatos?.length
            ? p.candidatos.map(c => `
                <div style="display:flex;align-items:center;justify-content:space-between;padding:12px 14px;margin-bottom:8px;background:#F8FAFC;border:1px solid #94A3B8;border-radius:2px">
                    <div>
                        <div style="font-size:13px;font-weight:800;color:#111827">${c.nombre}</div>
                        <small style="color:#64748B">${c.email}</small>
                    </div>
                    <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;justify-content:flex-end">
                        ${c.calificacionGeneral > 0
                    ? `<span style="font-size:12px;font-weight:700;color:#334155">★ ${c.calificacionGeneral}</span>`
                    : ''}
                        <span style="font-size:11px;font-weight:700;color:#475569">${c.etapa}</span>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Entrevistas / perfil" onclick="Reclutamiento.verPerfilCandidato(${c.id})">
                            <i class="fa-solid fa-comments"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Agendar entrevista" onclick="Reclutamiento.agendarEntrevista(${c.id}, ${p.id})">
                            <i class="fa-solid fa-calendar-plus"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Marcar en pruebas" onclick="Reclutamiento.cambiarEtapa(${c.id}, 2, ${p.id})">
                            <i class="fa-solid fa-clipboard-check"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Registrar oferta" onclick="Reclutamiento.registrarOferta(${c.id}, ${p.id})">
                            <i class="fa-solid fa-handshake"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Marcar contratado" onclick="Reclutamiento.cambiarEtapa(${c.id}, 4, ${p.id})">
                            <i class="fa-solid fa-user-check"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Marcar rechazado" onclick="Reclutamiento.cambiarEtapa(${c.id}, 5, ${p.id})">
                            <i class="fa-solid fa-user-xmark"></i>
                        </button>
                        <button class="btn-action" style="background:transparent;border:none;color:#111827" title="Eliminar candidato" onclick="Reclutamiento.eliminarCandidato(${c.id}, ${p.id})">
                            <i class="fa-solid fa-trash"></i>
                        </button>
                        ${!c.fueContratado && c.etapa === 'Contratado'
                    ? `<button class="btn-action" style="background:transparent;border:none;color:#111827" title="Convertir en empleado" onclick="Reclutamiento.convertirEnEmpleado(${c.id})">
                               <i class="fa-solid fa-user-plus"></i>
                           </button>`
                    : ''}
                    </div>
                </div>`).join('')
            : `<p class="text-muted text-center py-3" style="font-size:13px">No hay candidatos registrados</p>`;

        const historialHtml = p.historial?.length
            ? p.historial.map(h => `
                <div style="display:flex;gap:12px;margin-bottom:10px;font-size:12px">
                    <div style="color:#94A3B8;white-space:nowrap">${h.fecha}</div>
                    <div>
                        <span style="color:#64748B">${h.estadoAnterior}</span>
                        <i class="fa-solid fa-arrow-right mx-1" style="color:#94A3B8;font-size:10px"></i>
                        <strong style="color:#111827">${h.estadoNuevo}</strong>
                        ${h.motivo ? `<span class="text-muted"> — ${h.motivo}</span>` : ''}
                        <small class="text-muted d-block">${h.cambiadoPor}</small>
                    </div>
                </div>`).join('')
            : `<p class="text-muted" style="font-size:12px">Sin cambios registrados</p>`;

        return `
            <div class="modal-header" style="background:#fff;border-bottom:1px solid #E5E7EB;padding:16px 20px;border-radius:2px 2px 0 0">
                <div>
                    <h5 class="modal-title" style="font-size:17px;font-weight:800;color:#111827">
                        <i class="fa-solid fa-briefcase me-2" style="color:#334155"></i>${p.titulo}
                    </h5>
                    <small style="color:#64748B">${p.departamento}${p.puesto ? ` · ${p.puesto}` : ''} · ${p.diasAbierta} días abierta</small>
                </div>
                <button type="button" class="btn-close" onclick="Reclutamiento.cerrarModal()"></button>
            </div>

            <div class="modal-body" style="padding:0;max-height:75vh;overflow-y:auto">
                <div style="display:grid;grid-template-columns:repeat(4,1fr);border-bottom:1px solid #E5E7EB">
                    <div style="padding:12px 16px;border-right:1px solid #E5E7EB">
                        <div style="font-size:11px;font-weight:700;color:#64748B">Estado</div>
                        <span style="display:inline-block;margin-top:2px;background:${est.bg};color:${est.color};font-size:13px;font-weight:800;padding:3px 10px;border-radius:2px">${p.estado}</span>
                    </div>
                    <div style="padding:12px 16px;border-right:1px solid #E5E7EB">
                        <div style="font-size:11px;font-weight:700;color:#64748B">Salario</div>
                        <div style="font-size:13px;font-weight:800;color:#111827">${p.salarioOfrecido > 0 ? fmt(p.salarioOfrecido) : 'A convenir'}</div>
                    </div>
                    <div style="padding:12px 16px;border-right:1px solid #E5E7EB">
                        <div style="font-size:11px;font-weight:700;color:#64748B">Vacantes</div>
                        <div style="font-size:13px;font-weight:800;color:#111827">${p.cantidadVacantes}${p.esReemplazo ? ' (Reemplazo)' : ''}</div>
                    </div>
                    <div style="padding:12px 16px">
                        <div style="font-size:11px;font-weight:700;color:#64748B">Cierre</div>
                        <div style="font-size:13px;font-weight:800;color:#111827">${p.fechaCierre || 'Sin fecha'}</div>
                    </div>
                </div>

                <div style="padding:16px 20px;border-bottom:1px solid #E5E7EB">
                    <div style="font-size:12px;font-weight:800;color:#111827;text-transform:uppercase;letter-spacing:.5px;margin-bottom:10px">Pipeline de candidatos</div>
                    <div style="display:flex;align-items:stretch;border:1px solid #E5E7EB;border-radius:2px;padding:8px">
                        ${pipelineHtml}
                    </div>
                </div>

                <div style="padding:16px 20px;border-bottom:1px solid #E5E7EB">
                    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px">
                        <div style="font-size:12px;font-weight:800;color:#111827;text-transform:uppercase;letter-spacing:.5px">Candidatos (${p.totalCandidatos})</div>
                        <button class="btn btn-sm btn-reclu" onclick="Reclutamiento.openFormCandidato(null, ${p.id})">
                            <i class="fa-solid fa-plus me-1"></i>Agregar candidato
                        </button>
                    </div>
                    ${candidatosHtml}
                </div>

                ${p.descripcion || p.requisitoMinimos ? `
                <div style="padding:16px 20px;border-bottom:1px solid #E5E7EB">
                    <div style="font-size:12px;font-weight:800;color:#111827;text-transform:uppercase;letter-spacing:.5px;margin-bottom:8px">Descripción y requisitos</div>
                    ${p.descripcion ? `<p style="font-size:13px;color:#374151;margin-bottom:8px">${p.descripcion}</p>` : ''}
                    ${p.requisitoMinimos ? `<div style="border-left:2px solid #E5E7EB;padding:8px 12px;font-size:12px;color:#374151"><strong>Requisitos mínimos:</strong><br>${p.requisitoMinimos}</div>` : ''}
                </div>` : ''}

                <div style="padding:16px 20px">
                    <div style="font-size:12px;font-weight:800;color:#111827;text-transform:uppercase;letter-spacing:.5px;margin-bottom:12px">Historial de estados</div>
                    ${historialHtml}
                </div>
            </div>

            <div class="modal-footer" style="gap:8px;border-top:1px solid #E5E7EB;border-radius:0 0 2px 2px">
                <button type="button" class="btn btn-outline-secondary btn-sm" style="border-radius:2px" onclick="Reclutamiento.cerrarModal()">
                    <i class="fa-solid fa-xmark me-1"></i>Cerrar
                </button>
                <button type="button" class="btn btn-sm btn-outline-secondary" style="border-radius:2px" onclick="Reclutamiento.cambiarEstadoPlaza(${p.id})">
                    <i class="fa-solid fa-arrows-rotate me-1"></i>Cambiar estado
                </button>
                <button type="button" class="btn btn-sm btn-reclu" style="border-radius:2px" onclick="Reclutamiento.openFormPlaza(${p.id})">
                    <i class="fa-solid fa-pen me-1"></i>Editar plaza
                </button>
            </div>`;
    }

    // ════════════════════════════════════════════
    // PERFIL COMPLETO DE CANDIDATO
    // ════════════════════════════════════════════
    async function verPerfilCandidato(id) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--reclu-color);
                            width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;
                          font-size:13px">
                    Cargando perfil...
                </p>
            </div>`;

        getModal()?.show();

        try {
            // Cargar datos del candidato, entrevistas y notas
            const [entrevistas, notas] = await Promise.all([
                fetch(`/Reclutamiento/GetEntrevistas?candidatoId=${id}`,
                    {
                        headers:
                            { 'X-Requested-With': 'XMLHttpRequest' }
                    }).then(r => r.json()),
                fetch(`/Reclutamiento/GetNotas?candidatoId=${id}`,
                    {
                        headers:
                            { 'X-Requested-With': 'XMLHttpRequest' }
                    }).then(r => r.json())
            ]);

            body.innerHTML = _renderPerfilCandidato(
                id, entrevistas.data || [], notas.data || []);

        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation
                               fa-2x mb-2"></i>
                    <p>Error al cargar el perfil</p>
                </div>`;
        }
    }

    function _renderPerfilCandidato(id, entrevistas, notas) {
        const entrevistasHtml = entrevistas.length
            ? entrevistas.map(e => {
                const resCol = {
                    pendiente: '#94A3B8',
                    aprobado: '#16A34A',
                    reprobado: '#DC2626',
                    postergado: '#D97706'
                }[e.resultado.toLowerCase()] || '#94A3B8';

                const estrellas = e.calificacion > 0
                    ? '★'.repeat(Math.round(e.calificacion))
                    + '☆'.repeat(10 - Math.round(e.calificacion))
                    : '—';

                return `
                    <div style="background:#F8FAFC;border-radius:8px;
                         padding:12px 14px;margin-bottom:8px;
                         border:1px solid #E2E8F0">
                        <div style="display:flex;
                                    justify-content:space-between;
                                    align-items:flex-start">
                            <div>
                                <div class="fw-600"
                                     style="font-size:13px">
                                    ${e.entrevistador}
                                </div>
                                <small class="text-muted">
                                    ${new Date(e.fechaHora)
                        .toLocaleDateString('es-GT')}
                                    ${e.lugar
                        ? ` · ${e.lugar}` : ''}
                                </small>
                            </div>
                            <div style="text-align:right">
                                <span style="background:${resCol}18;
                                      color:${resCol};font-size:11px;
                                      font-weight:600;padding:2px 8px;
                                      border-radius:8px">
                                    ${e.resultado}
                                </span>
                                ${e.calificacion > 0
                        ? `<div style="color:#C4A35A;
                                       font-size:11px;margin-top:2px">
                                       ${e.calificacion}/10</div>`
                        : ''}
                            </div>
                        </div>
                        ${e.observaciones
                        ? `<p style="font-size:12px;color:#64748B;
                                 margin-top:6px;margin-bottom:0">
                                 ${e.observaciones}
                               </p>`
                        : ''}
                        <div style="display:flex;gap:6px;
                                    margin-top:8px">
                            <button class="btn btn-outline-secondary
                                           btn-sm"
                                style="font-size:11px;padding:2px 8px"
                                onclick="Reclutamiento
                                    .editarEntrevista(${e.id}, ${id})">
                                <i class="fa-solid fa-pen me-1"></i>
                                Editar resultado
                            </button>
                            <button class="btn btn-sm"
                                style="font-size:11px;padding:2px 8px;
                                       background:#FEE2E2;color:#DC2626;
                                       border:none"
                                onclick="Reclutamiento
                                    .eliminarEntrevista(
                                        ${e.id}, ${id})">
                                <i class="fa-solid fa-trash me-1"></i>
                                Eliminar
                            </button>
                        </div>
                    </div>`;
            }).join('')
            : `<p class="text-muted" style="font-size:12px">
                  Sin entrevistas registradas
               </p>`;

        const notasHtml = notas.length
            ? notas.map(n => `
                <div style="border-left:3px solid #C4A35A;
                     padding:8px 12px;margin-bottom:8px;
                     background:#FFFBEB;border-radius:0 6px 6px 0">
                    <div style="font-size:12px;color:#475569">
                        ${n.nota}
                    </div>
                    <div style="display:flex;
                                justify-content:space-between;
                                margin-top:4px">
                        <small class="text-muted">
                            ${n.creadoPor} · ${n.fecha}
                        </small>
                        <button style="background:none;border:none;
                                color:#94A3B8;cursor:pointer;
                                font-size:11px;padding:0"
                            onclick="Reclutamiento
                                .eliminarNota(${n.id}, ${id})">
                            <i class="fa-solid fa-xmark"></i>
                        </button>
                    </div>
                </div>`).join('')
            : `<p class="text-muted" style="font-size:12px">
                  Sin notas
               </p>`;

        return `
            <div class="modal-header"
                 style="background:#fff;
                        border-bottom:1px solid #E2E8F0;
                        padding:16px 20px">
                <h5 class="modal-title fw-700"
                    style="font-size:16px">
                    <i class="fa-solid fa-user me-2"
                       style="color:var(--reclu-color)"></i>
                    Perfil del candidato
                </h5>
                <button type="button" class="btn-close"
                        onclick="Reclutamiento.cerrarModal()">
                </button>
            </div>

            <div class="modal-body"
                 style="max-height:72vh;overflow-y:auto;
                        padding:16px 20px">

                <!-- ENTREVISTAS -->
                <div style="margin-bottom:20px">
                    <div style="display:flex;
                                justify-content:space-between;
                                align-items:center;
                                margin-bottom:10px">
                        <div style="font-size:12px;color:#64748B;
                                    font-weight:600;
                                    text-transform:uppercase;
                                    letter-spacing:.5px">
                            Entrevistas
                        </div>
                        <button class="btn btn-sm btn-reclu"
                            onclick="Reclutamiento
                                .agendarEntrevista(${id})">
                            <i class="fa-solid fa-plus me-1"></i>
                            Agendar entrevista
                        </button>
                    </div>
                    ${entrevistasHtml}
                </div>

                <!-- NOTAS -->
                <div>
                    <div style="font-size:12px;color:#64748B;
                                font-weight:600;
                                text-transform:uppercase;
                                letter-spacing:.5px;
                                margin-bottom:10px">
                        Notas de seguimiento
                    </div>
                    ${notasHtml}
                    <div style="display:flex;gap:8px;margin-top:10px">
                        <textarea id="nuevaNota"
                                  class="form-control form-control-sm"
                                  style="height:60px;resize:none"
                                  placeholder="Agregar nota...">
                        </textarea>
                        <button class="btn btn-sm btn-reclu"
                            style="flex-shrink:0;align-self:flex-end"
                            onclick="Reclutamiento
                                .agregarNota(${id})">
                            <i class="fa-solid fa-paper-plane"></i>
                        </button>
                    </div>
                </div>
            </div>

            <div class="modal-footer" style="gap:8px">
                <button type="button"
                        class="btn btn-outline-secondary btn-sm"
                        onclick="Reclutamiento.cerrarModal()">
                    <i class="fa-solid fa-xmark me-1"></i>Cerrar
                </button>
                <button type="button"
                        class="btn btn-sm"
                        style="background:#16A34A;color:#fff"
                        onclick="Reclutamiento
                            .convertirEnEmpleado(${id})">
                    <i class="fa-solid fa-user-plus me-1"></i>
                    Convertir en empleado
                </button>
                <button type="button" class="btn btn-sm btn-reclu"
                        onclick="Reclutamiento
                            .openFormCandidato(${id})">
                    <i class="fa-solid fa-pen me-1"></i>
                    Editar candidato
                </button>
            </div>`;
    }

    // ════════════════════════════════════════════
    // FORMULARIO CANDIDATO
    // ════════════════════════════════════════════
    async function openFormCandidato(id, plazaId = null) {
        const body = document.getElementById('modalEmpleadoBody');
        if (!body) return;

        body.innerHTML = `
            <div class="modal-loading">
                <div class="spinner-border"
                     style="color:var(--reclu-color);
                            width:2rem;height:2rem"></div>
                <p style="margin-top:12px;color:#64748B;
                          font-size:13px">Cargando...</p>
            </div>`;

        getModal()?.show();

        try {
            const url = id
                ? `/Reclutamiento/FormCandidato?id=${id}`
                : `/Reclutamiento/FormCandidato${plazaId ? `?plazaId=${plazaId}` : ''}`;
            body.innerHTML = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
        } catch {
            body.innerHTML = `
                <div class="modal-loading text-danger">
                    <i class="fa-solid fa-circle-exclamation
                               fa-2x mb-2"></i>
                    <p>Error al cargar el formulario</p>
                </div>`;
        }
    }

    async function guardarCandidato(form) {
        const btn = form.querySelector('#btnGuardarCandidato');
        const formData = new FormData(form);
        const data = Object.fromEntries(formData.entries());
        const id = parseInt(data.Id) || 0;
        const url = id > 0
            ? `/Reclutamiento/EditCandidato/${id}`
            : '/Reclutamiento/CreateCandidato';

        // Normalizar para binder del backend
        formData.set('Id', String(id));
        formData.set('PlazaVacanteId', String(parseInt(data.PlazaVacanteId) || 0));
        formData.set('Etapa', String(parseInt(data.Etapa) || 0));

        const fechaEntrevista = (data.FechaEntrevista || '').trim();
        const fechaValida = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(fechaEntrevista);
        formData.set('FechaEntrevista', fechaValida ? fechaEntrevista : '');

        FormHelper.setLoading(btn, true);
        try {
            const token = form.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(url, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: formData
            }).then(r => r.json());

            if (res.success) {
                cerrarModal();
                Notify.success(res.message);
                tablaCandidatos?.ajax.reload(null, false);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
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


    // ════════════════════════════════════════════
    // CAMBIAR ETAPA
    // ════════════════════════════════════════════
    async function cambiarEtapa(id, etapa, plazaId = null) {
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/CambiarEtapa/${id}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(parseInt(etapa))
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                tablaCandidatos?.ajax.reload(null, false);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
                if (plazaId) {
                    await refreshDetallePlaza(plazaId);
                }

                // Si llegó a Contratado, ofrecer convertir
                if (parseInt(etapa) === 4) {
                    setTimeout(() => {
                        convertirEnEmpleado(id);
                    }, 800);
                }
            } else {
                Notify.error(res.message);
                tablaCandidatos?.ajax.reload(null, false);
            }
        } catch {
            Notify.error('Error al actualizar etapa.');
        }
    }

    async function registrarOferta(candidatoId, plazaId = null) {
        const { value: form } = await BsModal.fire({
            title: 'Registrar oferta laboral',
            html: `
                <div style="text-align:left">
                    <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:12px">
                        <div>
                            <label style="font-size:12px;color:#64748B;font-weight:600;display:block;margin-bottom:4px">Salario oferta (Q) *</label>
                            <input id="ofSalario" type="number" step="0.01" min="0" class="form-control form-control-sm" placeholder="0.00">
                        </div>
                        <div>
                            <label style="font-size:12px;color:#64748B;font-weight:600;display:block;margin-bottom:4px">Fecha ingreso propuesta *</label>
                            <input id="ofFecha" type="date" class="form-control form-control-sm" value="${new Date().toISOString().split('T')[0]}">
                        </div>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;font-weight:600;display:block;margin-bottom:4px">Tipo de contrato *</label>
                        <select id="ofContrato" class="form-select form-select-sm">
                            <option value="Indefinido">Indefinido</option>
                            <option value="Temporal">Temporal</option>
                            <option value="Prueba">Prueba</option>
                        </select>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;font-weight:600;display:block;margin-bottom:4px">Observaciones</label>
                        <textarea id="ofObs" class="form-control form-control-sm" style="height:75px" placeholder="Condiciones, bono, fecha límite de respuesta..."></textarea>
                    </div>
                </div>`,
            confirmButtonText: '<i class="fa-solid fa-handshake me-1"></i> Guardar oferta',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 460,
            preConfirm: () => {
                const salarioOferta = parseFloat(document.getElementById('ofSalario').value || '0');
                const fechaIngresoPropuesta = document.getElementById('ofFecha').value;
                const tipoContrato = document.getElementById('ofContrato').value;
                const observaciones = document.getElementById('ofObs').value;

                if (!salarioOferta || salarioOferta <= 0) {
                    return BsModal.showValidationMessage('Ingresa un salario de oferta válido.');
                }
                if (!fechaIngresoPropuesta) {
                    return BsModal.showValidationMessage('Selecciona una fecha de ingreso propuesta.');
                }

                return {
                    candidatoId,
                    salarioOferta,
                    fechaIngresoPropuesta,
                    tipoContrato,
                    observaciones
                };
            }
        });

        if (!form) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            const res = await fetch('/Reclutamiento/RegistrarOferta', {
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
                tablaCandidatos?.ajax.reload(null, false);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
                if (plazaId) {
                    await refreshDetallePlaza(plazaId);
                }
            } else {
                Notify.error(res.message || 'No se pudo registrar la oferta.');
            }
        } catch {
            Notify.error('Error al registrar oferta.');
        }
    }


    // ════════════════════════════════════════════
    // CAMBIAR ESTADO DE PLAZA
    // ════════════════════════════════════════════
    async function cambiarEstadoPlaza(id) {
        const { value: form } = await BsModal.fire({
            title: 'Cambiar estado de plaza',
            html: `
                <div style="text-align:left">
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Nuevo estado
                        </label>
                        <select id="nuevoEstado"
                                class="form-select form-select-sm">
                            <option value="Abierta">Abierta</option>
                            <option value="EnProceso">
                                En proceso
                            </option>
                            <option value="Cerrada">Cerrada</option>
                            <option value="Cancelada">Cancelada</option>
                        </select>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Motivo (opcional)
                        </label>
                        <textarea id="motivoEstado"
                                  class="form-control form-control-sm"
                                  style="height:70px"
                                  placeholder="Razón del cambio...">
                        </textarea>
                    </div>
                </div>`,
            confirmButtonText: 'Guardar cambio',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 400,
            preConfirm: () => ({
                estado: document.getElementById(
                    'nuevoEstado').value,
                motivo: document.getElementById(
                    'motivoEstado').value
            })
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/CambiarEstadoPlaza/${id}`, {
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
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
                cerrarModal();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al cambiar estado.');
        }
    }

    // ════════════════════════════════════════════
    // ENTREVISTAS
    // ════════════════════════════════════════════
    async function agendarEntrevista(candidatoId, plazaId = null) {
        const { value: form } = await BsModal.fire({
            title: 'Agendar entrevista',
            html: `
                <div style="text-align:left">
                    <div style="display:grid;
                                grid-template-columns:1fr 1fr;
                                gap:10px;margin-bottom:12px">
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Fecha y hora *
                            </label>
                            <input id="entFecha"
                                   type="datetime-local"
                                   class="form-control form-control-sm"
                                   value="${new Date()
                    .toISOString()
                    .slice(0, 16)}">
                        </div>
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Entrevistador *
                            </label>
                            <input id="entEntrevistador"
                                   type="text"
                                   class="form-control form-control-sm"
                                   placeholder="Nombre...">
                        </div>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Lugar / modalidad
                        </label>
                        <input id="entLugar" type="text"
                               class="form-control form-control-sm"
                               placeholder="Ej: Sala 3, Google Meet...">
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Observaciones
                        </label>
                        <textarea id="entObs"
                                  class="form-control form-control-sm"
                                  style="height:60px">
                        </textarea>
                    </div>
                </div>`,
            confirmButtonText: 'Agendar',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 460,
            preConfirm: () => {
                const entrevistador =
                    document.getElementById('entEntrevistador').value;
                if (!entrevistador)
                    return BsModal.showValidationMessage(
                        'El entrevistador es requerido');
                return {
                    candidatoId,
                    fechaHora: document.getElementById(
                        'entFecha').value,
                    entrevistador,
                    lugar: document.getElementById('entLugar').value,
                    resultado: 'Pendiente',
                    calificacion: 0,
                    observaciones: document.getElementById(
                        'entObs').value
                };
            }
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch('/Reclutamiento/CrearEntrevista', {
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
                if (plazaId) {
                    await refreshDetallePlaza(plazaId);
                } else {
                    verPerfilCandidato(candidatoId);
                }
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al agendar entrevista.');
        }
    }

    async function editarEntrevista(entrevistaId, candidatoId) {
        const { value: form } = await BsModal.fire({
            title: 'Registrar resultado de entrevista',
            html: `
                <div style="text-align:left">
                    <div style="display:grid;
                                grid-template-columns:1fr 1fr;
                                gap:10px;margin-bottom:12px">
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Resultado
                            </label>
                            <select id="resResultado"
                                    class="form-select form-select-sm">
                                <option value="Pendiente">
                                    Pendiente
                                </option>
                                <option value="Aprobado">
                                    Aprobado
                                </option>
                                <option value="Reprobado">
                                    Reprobado
                                </option>
                                <option value="Postergado">
                                    Postergado
                                </option>
                            </select>
                        </div>
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Calificación (1-10)
                            </label>
                            <input id="resCal" type="number"
                                   class="form-control form-control-sm"
                                   min="0" max="10" step="0.5"
                                   value="0">
                        </div>
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Observaciones
                        </label>
                        <textarea id="resObs"
                                  class="form-control form-control-sm"
                                  style="height:80px"
                                  placeholder="Notas de la entrevista...">
                        </textarea>
                    </div>
                </div>`,
            confirmButtonText: 'Guardar resultado',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 420,
            preConfirm: () => ({
                candidatoId,
                resultado: document.getElementById(
                    'resResultado').value,
                calificacion: parseFloat(
                    document.getElementById('resCal').value) || 0,
                observaciones: document.getElementById(
                    'resObs').value,
                fechaHora: new Date().toISOString(),
                entrevistador: '—'
            })
        });

        if (!form) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/ActualizarEntrevista/${entrevistaId}`,
                {
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
                verPerfilCandidato(candidatoId);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al actualizar resultado.');
        }
    }

    async function eliminarEntrevista(entrevistaId, candidatoId) {
        const ok = await BsModal.fire({
            title: '¿Eliminar esta entrevista?',
            text: 'Se eliminará el registro permanentemente.',
            icon: 'warning',
            confirmButtonText: 'Sí, eliminar',
            confirmButtonColor: '#DC2626',
            cancelButtonText: 'Cancelar',
            showCancelButton: true
        }).then(r => r.isConfirmed);

        if (!ok) return;

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/EliminarEntrevista/${entrevistaId}`,
                {
                    method: 'POST',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                verPerfilCandidato(candidatoId);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al eliminar.');
        }
    }

    // ════════════════════════════════════════════
    // NOTAS
    // ════════════════════════════════════════════
    async function agregarNota(candidatoId) {
        const ta = document.getElementById('nuevaNota');
        const nota = ta?.value?.trim();
        if (!nota) {
            Notify.error('Escribe una nota antes de guardar.');
            return;
        }

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch('/Reclutamiento/AgregarNota', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ candidatoId, nota })
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                verPerfilCandidato(candidatoId);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al agregar nota.');
        }
    }

    async function eliminarNota(notaId, candidatoId) {
        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/EliminarNota/${notaId}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                verPerfilCandidato(candidatoId);
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al eliminar nota.');
        }
    }

    // ════════════════════════════════════════════
    // CONVERTIR EN EMPLEADO
    // ════════════════════════════════════════════
    async function convertirEnEmpleado(candidatoId) {
        // Cargar departamentos y puestos
        const [deptos, puestos] = await Promise.all([
            fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json()),
            fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json())
        ]);

        const deptosUnicos = [...new Map(
            (deptos.data || []).map(e => [e.departamentoId,
            { id: e.departamentoId, nombre: e.departamento }])
        ).values()];

        const optsDepto = deptosUnicos.map(d =>
            `<option value="${d.id}">${d.nombre}</option>`
        ).join('');

        const { value: form } = await BsModal.fire({
            title: '¡Contratar candidato!',
            html: `
                <div style="text-align:left">
                    <div style="background:#D1FAE5;
                         border-radius:8px;padding:12px 14px;
                         margin-bottom:16px;font-size:13px;
                         color:#065F46">
                        <i class="fa-solid fa-circle-check me-2"></i>
                        El candidato será creado como empleado activo
                        en el sistema. Podrás completar sus datos
                        en el módulo de Personal.
                    </div>
                    <div style="display:grid;
                                grid-template-columns:1fr 1fr;
                                gap:10px;margin-bottom:12px">
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Salario base (Q) *
                            </label>
                            <input id="convSalario" type="number"
                                   class="form-control form-control-sm"
                                   step="0.01" placeholder="0.00">
                        </div>
                        <div>
                            <label style="font-size:12px;color:#64748B;
                                   font-weight:600;display:block;
                                   margin-bottom:4px">
                                Fecha de ingreso *
                            </label>
                            <input id="convFecha" type="date"
                                   class="form-control form-control-sm"
                                   value="${new Date()
                    .toISOString()
                    .split('T')[0]}">
                        </div>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Departamento *
                        </label>
                        <select id="convDepto"
                                class="form-select form-select-sm">
                            <option value="">Seleccionar...</option>
                            ${optsDepto}
                        </select>
                    </div>
                    <div style="margin-bottom:12px">
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Puesto ID (temporal)
                        </label>
                        <input id="convPuesto" type="number"
                               class="form-control form-control-sm"
                               value="1">
                    </div>
                    <div>
                        <label style="font-size:12px;color:#64748B;
                               font-weight:600;display:block;
                               margin-bottom:4px">
                            Tipo de contrato
                        </label>
                        <select id="convContrato"
                                class="form-select form-select-sm">
                            <option value="Indefinido">Indefinido</option>
                            <option value="Temporal">Temporal</option>
                            <option value="Prueba">Prueba</option>
                        </select>
                    </div>
                </div>`,
            confirmButtonText:
                '<i class="fa-solid fa-user-plus me-1"></i>'
                + ' Contratar',
            confirmButtonColor: '#16A34A',
            cancelButtonText: 'Cancelar',
            showCancelButton: true,
            width: 480,
            preConfirm: () => {
                const salario = parseFloat(
                    document.getElementById('convSalario').value);
                const deptoId = parseInt(
                    document.getElementById('convDepto').value);
                if (!salario || salario <= 0)
                    return BsModal.showValidationMessage(
                        'Ingresa el salario base');
                if (!deptoId)
                    return BsModal.showValidationMessage(
                        'Selecciona el departamento');
                return {
                    candidatoId,
                    salarioBase: salario,
                    departamentoId: deptoId,
                    puestoId: parseInt(
                        document.getElementById('convPuesto').value)
                        || 1,
                    fechaIngreso: document.getElementById(
                        'convFecha').value,
                    tipoContrato: document.getElementById(
                        'convContrato').value
                };
            }
        });

        if (!form) return;

        BsModal.loadingShow('Creando empleado...',
            'Registrando en el sistema...');

        try {
            const token = document.querySelector(
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                '/Reclutamiento/ConvertirEnEmpleado', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(form)
            }).then(r => r.json());

            BsModal.loadingHide();

            if (res.success) {
                await BsModal.fire({
                    icon: 'success',
                    title: '¡Empleado creado!',
                    html: `<p style="font-size:13px;color:#64748B">
                              ${res.message}
                           </p>`,
                    confirmButtonText: 'Ir a Personal'
                });

                cerrarModal();
                tablaCandidatos?.ajax.reload(null, false);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();

                // Navegar a Personal si el usuario lo desea
                if (typeof AppRouter !== 'undefined')
                    AppRouter.navigate('/Personal/Index');
            } else {
                Notify.error(res.message);
            }
        } catch {
            BsModal.loadingHide();
            Notify.error('Error al crear el empleado.');
        }
    }

    // ════════════════════════════════════════════
    // ELIMINAR PLAZA / CANDIDATO
    // ════════════════════════════════════════════
    async function eliminarPlaza(id) {
        const ok = await BsModal.fire({
            title: '¿Eliminar esta plaza?',
            html: `<p style="font-size:14px;color:#64748B;
                      text-align:center;margin:0">
                      Se eliminará la plaza y todos sus datos.
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
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/DeletePlaza/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    async function eliminarCandidato(id, plazaId = null) {
        const ok = await BsModal.fire({
            title: '¿Eliminar este candidato?',
            html: `<p style="font-size:14px;color:#64748B;
                      text-align:center;margin:0">
                      Se eliminará el candidato permanentemente.
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
                'input[name="__RequestVerificationToken"]')
                ?.value || '';
            const res = await fetch(
                `/Reclutamiento/DeleteCandidato/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                tablaCandidatos?.ajax.reload(null, false);
                tablaPlazas?.ajax.reload(null, false);
                cargarEstadisticas();
                if (plazaId) {
                    await refreshDetallePlaza(plazaId);
                }
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar.');
        }
    }

    function verCandidatosDePlaza(plazaId) {
        document.getElementById('tabCandidatos')?.click();
        setTimeout(() =>
            tablaCandidatos?.ajax.reload(), 200);
    }

    // ════════════════════════════════════════════
    // API PÚBLICA
    // ════════════════════════════════════════════
    return {
        init,
        openFormPlaza,
        openFormCandidato,
        verDetallePlaza,
        verPerfilCandidato,
        cambiarEtapa,
        registrarOferta,
        cambiarEstadoPlaza,
        agendarEntrevista,
        editarEntrevista,
        eliminarEntrevista,
        agregarNota,
        eliminarNota,
        convertirEnEmpleado,
        verCandidatosDePlaza,
        eliminarPlaza,
        eliminarCandidato,
        cerrarModal
    };
})();

// ── Auto-init ──
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('tablaPlazas') ||
        document.getElementById('tablaCandidatos'))
        Reclutamiento.init();
});

document.addEventListener('spaNavigated', () => {
    if (document.getElementById('tablaPlazas') ||
        document.getElementById('tablaCandidatos'))
        Reclutamiento.init();
});


