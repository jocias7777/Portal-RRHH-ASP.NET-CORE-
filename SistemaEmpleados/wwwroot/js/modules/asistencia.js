/**
 * asistencia.js
 * Módulo completo: Registros de Asistencia + Horarios/Turnos
 */
const Asistencia = (() => {

    const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    // ✅ FIX UTC — devuelve la fecha local del cliente (no UTC)
    const fechaHoy = () => {
        const d = new Date();
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    };

    let tablaAsistencia, tablaHorarios;
    let horarioIdParaEliminar = null;
    let asistenciaIdParaEliminar = null;

    const dtLang = {
        processing: 'Cargando...',
        search: '',
        searchPlaceholder: 'Buscar...',
        lengthMenu: 'Mostrar _MENU_',
        info: '',
        infoEmpty: '',
        infoFiltered: '',
        zeroRecords: 'No se encontraron resultados',
        emptyTable: 'No hay datos disponibles',
        paginate: { first: '«', last: '»', next: '›', previous: '‹' }
    };

    function init() {
        if (!document.getElementById('tablaAsistencia')) return;
        destroyTablas();
        initTablaAsistencia();
        initTablaHorarios();
        cargarKpis();
        bindEventos();
    }

    // ══════════════════════════════════════════════════════
    //  DESTRUIR Y REINICIALIZAR TABLAS
    // ══════════════════════════════════════════════════════
    function destroyTablas() {
        if (tablaAsistencia) {
            tablaAsistencia.destroy();
            tablaAsistencia = null;
        }
        if (tablaHorarios) {
            tablaHorarios.destroy();
            tablaHorarios = null;
        }
    }

    // ══════════════════════════════════════════════════════
    //  KPI CARDS
    // ══════════════════════════════════════════════════════
    function cargarKpis() {
        fetch('/Asistencia/GetKpisHoy')
            .then(r => r.json())
            .then(d => {
                document.getElementById('kpiPresentes').textContent = d.presentes ?? '—';
                document.getElementById('kpiAusentes').textContent = d.ausentes ?? '—';
                document.getElementById('kpiTardanzas').textContent = d.tardanzas ?? '—';
                document.getElementById('kpiHExtras').textContent = (d.horasExtra ?? 0).toFixed(1) + ' h';
            })
            .catch(() => { });
    }

    // ══════════════════════════════════════════════════════
    //  TABLA ASISTENCIA
    // ══════════════════════════════════════════════════════
    function initTablaAsistencia() {
        tablaAsistencia = $('#tablaAsistencia').DataTable({
            serverSide: true,
            processing: true,
            dom: '<"table-card"rt><"asistencia-pagination"p>',
            pagingType: 'simple_numbers',
            ajax: {
                url: '/Asistencia/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify(buildRequest(d)),
                headers: { 'RequestVerificationToken': token() }
            },
            columns: [
                { data: null, render: (_, __, ___, meta) => meta.row + 1, orderable: false, width: '44px' },
                { data: null, render: renderEmpleado, orderable: false },
                { data: 'departamento', orderable: false },
                { data: 'fecha', orderable: false, width: '110px' },
                { data: null, render: d => d.horaEntrada ? `<span class="badge-hora entrada">${d.horaEntrada}</span>` : '<span class="text-muted">—</span>', orderable: false, width: '90px' },
                { data: null, render: d => d.horaSalida ? `<span class="badge-hora salida">${d.horaSalida}</span>` : '<span class="text-muted">—</span>', orderable: false, width: '90px' },
                { data: null, render: d => d.horasTrabajadas > 0 ? `${Number(d.horasTrabajadas).toFixed(1)} h` : '—', orderable: false, width: '90px' },
                { data: null, render: d => d.horasExtra > 0 ? `<span class="text-success fw-semibold">${Number(d.horasExtra).toFixed(1)} h</span>` : '—', orderable: false, width: '90px' },
                { data: null, render: d => d.minutosAtraso > 0 ? `<span class="text-warning fw-semibold">${d.minutosAtraso} min</span>` : '—', orderable: false, width: '90px' },
                { data: null, render: d => renderMetodo(d.metodo), orderable: false, width: '100px' },
                { data: null, render: d => renderEstado(d.estado), orderable: false, width: '130px' },
                { data: null, render: renderAccionesAsistencia, orderable: false, width: '140px', className: 'text-center' }
            ],
            language: dtLang,
            pageLength: 25,
            order: [],
            ordering: false,
            drawCallback: () => {
                const info = tablaAsistencia.page.info();
                const el = document.getElementById('totalAsistencias');
                if (el) el.textContent = `${info.recordsDisplay} registros`;
            }
        });
    }

    function buildRequest(d) {
        return {
            draw: d.draw,
            start: d.start,
            length: d.length,
            searchValue: d.search?.value ?? '',
            orderColumn: 'fecha',
            orderDir: 'desc',
            departamentoId: document.getElementById('filtroDeptoAsistencia')?.value || null,
            estado: document.getElementById('filtroEstadoAsistencia')?.value || null,
            fechaDesde: document.getElementById('filtroFechaDesde')?.value || null,
            fechaHasta: document.getElementById('filtroFechaHasta')?.value || null
        };
    }

    function renderEmpleado(d) {
        return `<span class="fw-medium">${d.nombreEmpleado}</span>`;
    }

    function renderMetodo(m) {
        const map = {
            'Manual': ['#e0f2fe', '#0369a1', 'fa-keyboard'],
            'Biometrico': ['#f0fdf4', '#166534', 'fa-fingerprint'],
            'QR': ['#fef9c3', '#854d0e', 'fa-qrcode']
        };
        const [bg, color, icon] = map[m] || ['#f1f5f9', '#475569', 'fa-circle-question'];
        return `<span class="badge-metodo" style="background:${bg};color:${color};">
                    <i class="fa-solid ${icon} me-1"></i>${m}
                </span>`;
    }

    function renderEstado(e) {
        const map = {
            'Presente': ['#D1FAE5', '#065F46'],
            'Tardanza': ['#FEF9C3', '#854D0E'],
            'Ausente': ['#FEE2E2', '#991B1B'],
            'PermisoJustificado': ['#DBEAFE', '#1E40AF']
        };
        const label = { 'PermisoJustificado': 'Permiso' };
        const [bg, color] = map[e] || ['#F1F5F9', '#475569'];
        return `<span class="badge-estado-activo" style="background:${bg};color:${color};">${label[e] ?? e}</span>`;
    }

    function renderAccionesAsistencia(d) {
        const btnSalida = (d.horaEntrada && !d.horaSalida)
            ? `<button class="btn-act-ver" title="Registrar Salida"
                   style="color:#0d9488;"
                   onclick="Asistencia.registrarSalida(${d.id})">
                   <i class="fa-solid fa-clock"></i>
               </button>`
            : '';

        return `
            <div class="d-flex gap-1 justify-content-center">
                ${btnSalida}
                <button class="btn-act-ver"      title="Ver"      onclick="Asistencia.verDetalle(${d.id})"><i class="fa-solid fa-eye"></i></button>
                <button class="btn-act-editar"   title="Editar"   onclick="Asistencia.editarAsistencia(${d.id})"><i class="fa-solid fa-pen"></i></button>
                <button class="btn-act-eliminar" title="Eliminar" onclick="Asistencia.confirmarEliminarAsistencia(${d.id})"><i class="fa-solid fa-trash"></i></button>
            </div>`;
    }

    // ══════════════════════════════════════════════════════
    //  TABLA HORARIOS
    // ══════════════════════════════════════════════════════
    function initTablaHorarios() {
        tablaHorarios = $('#tablaHorarios').DataTable({
            serverSide: true,
            processing: true,
            dom: '<"table-card"rt><"asistencia-pagination"p>',
            pagingType: 'simple_numbers',
            ajax: {
                url: '/Horarios/GetData',
                type: 'POST',
                contentType: 'application/json',
                data: d => JSON.stringify({ draw: d.draw, start: d.start, length: d.length, searchValue: d.search?.value ?? '' }),
                headers: { 'RequestVerificationToken': token() }
            },
            columns: [
                { data: null, render: (_, __, ___, meta) => meta.row + 1, orderable: false, width: '44px' },
                { data: 'nombre', orderable: false },
                { data: null, render: d => `<span class="badge-hora entrada">${d.horaEntrada}</span>`, orderable: false, width: '100px' },
                { data: null, render: d => `<span class="badge-hora salida">${d.horaSalida}</span>`, orderable: false, width: '100px' },
                { data: null, render: () => 'Lunes – Viernes', orderable: false, width: '120px' },
                { data: null, render: d => `${d.minutosToleranciaTardanza} min`, orderable: false, width: '100px' },
                { data: null, render: d => d.activo ? `<span class="badge-estado-activo">Activo</span>` : `<span class="badge-estado-inactivo">Inactivo</span>`, orderable: false, width: '120px' },
                { data: null, render: renderAccionesHorario, orderable: false, width: '110px', className: 'text-center' }
            ],
            language: dtLang,
            pageLength: 10,
            order: [],
            ordering: false
        });
    }

    function renderAccionesHorario(d) {
        const toggleIcon = d.activo ? 'fa-toggle-on text-success' : 'fa-toggle-off text-secondary';
        const toggleTitle = d.activo ? 'Desactivar' : 'Activar';
        return `
            <div class="d-flex gap-1 justify-content-center">
                <button class="btn-act-editar"   title="Editar"         onclick="Asistencia.editarHorario(${d.id})"><i class="fa-solid fa-pen"></i></button>
                <button class="btn-act-ver"      title="${toggleTitle}" onclick="Asistencia.toggleHorario(${d.id})"><i class="fa-solid ${toggleIcon}"></i></button>
                <button class="btn-act-eliminar" title="Eliminar"       onclick="Asistencia.confirmarEliminarHorario(${d.id},'${d.nombre}')"><i class="fa-solid fa-trash"></i></button>
            </div>`;
    }

    // ══════════════════════════════════════════════════════
    //  BIND EVENTOS
    // ══════════════════════════════════════════════════════
    function bindEventos() {
        // Limpiar listeners anteriores si existen
        ['filtroDeptoAsistencia', 'filtroEstadoAsistencia', 'filtroFechaDesde', 'filtroFechaHasta']
            .forEach(id => {
                const el = document.getElementById(id);
                if (el) {
                    el.removeEventListener('change', tablaAsistencia_onChange);
                    el.addEventListener('change', tablaAsistencia_onChange);
                }
            });

        const btnLimpiar = document.getElementById('btnLimpiarAsistencia');
        if (btnLimpiar) {
            btnLimpiar.removeEventListener('click', limpiarAsistencia_onClick);
            btnLimpiar.addEventListener('click', limpiarAsistencia_onClick);
        }

        const btnNuevaAsis = document.getElementById('btnNuevaAsistencia');
        if (btnNuevaAsis) {
            btnNuevaAsis.removeEventListener('click', nuevaAsistencia_onClick);
            btnNuevaAsis.addEventListener('click', nuevaAsistencia_onClick);
        }

        const btnGuardarAsis = document.getElementById('btnGuardarAsistencia');
        if (btnGuardarAsis) {
            btnGuardarAsis.removeEventListener('click', guardarAsistencia);
            btnGuardarAsis.addEventListener('click', guardarAsistencia);
        }

        ['asistenciaEntrada', 'asistenciaSalida'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.removeEventListener('change', calcularHoras);
                el.addEventListener('change', calcularHoras);
            }
        });

        const turnoSelect = document.getElementById('asistenciaTurno');
        if (turnoSelect) {
            turnoSelect.removeEventListener('change', turno_onChange);
            turnoSelect.addEventListener('change', turno_onChange);
        }

        const btnNuevoHor = document.getElementById('btnNuevoHorario');
        if (btnNuevoHor) {
            btnNuevoHor.removeEventListener('click', nuevoHorario_onClick);
            btnNuevoHor.addEventListener('click', nuevoHorario_onClick);
        }

        const btnGuardarHor = document.getElementById('btnGuardarHorario');
        if (btnGuardarHor) {
            btnGuardarHor.removeEventListener('click', guardarHorario);
            btnGuardarHor.addEventListener('click', guardarHorario);
        }

        ['horarioEntrada', 'horarioSalida'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.removeEventListener('change', calcularDuracionTurno);
                el.addEventListener('change', calcularDuracionTurno);
            }
        });

        const btnConfElimHor = document.getElementById('btnConfirmarEliminarHorario');
        if (btnConfElimHor) {
            btnConfElimHor.removeEventListener('click', confirmarElim_onClickHor);
            btnConfElimHor.addEventListener('click', confirmarElim_onClickHor);
        }

        const btnConfElimAsis = document.getElementById('btnConfirmarEliminarAsistencia');
        if (btnConfElimAsis) {
            btnConfElimAsis.removeEventListener('click', confirmarElim_onClickAsis);
            btnConfElimAsis.addEventListener('click', confirmarElim_onClickAsis);
        }
    }

    // Funciones helper para bindEventos
    function tablaAsistencia_onChange() {
        if (tablaAsistencia) tablaAsistencia.draw();
    }

    function limpiarAsistencia_onClick() {
        document.getElementById('filtroDeptoAsistencia').value = '';
        document.getElementById('filtroEstadoAsistencia').value = '';
        document.getElementById('filtroFechaDesde').value = fechaHoy().slice(0, 7) + '-01';
        document.getElementById('filtroFechaHasta').value = fechaHoy();
        if (tablaAsistencia) tablaAsistencia.draw();
    }

    function nuevaAsistencia_onClick() {
        abrirModalAsistencia();
    }

    function turno_onChange() {
        const opt = this.options[this.selectedIndex];
        const entrada = opt.dataset.entrada;
        const salida = opt.dataset.salida;
        const tolerancia = parseInt(opt.dataset.tolerancia) || 0;
        const esEdicion = parseInt(document.getElementById('asistenciaId').value) > 0;

        if (entrada) document.getElementById('asistenciaEntrada').value = entrada.slice(0, 5);

        if (esEdicion && salida) {
            document.getElementById('asistenciaSalida').value = salida.slice(0, 5);
        } else {
            document.getElementById('asistenciaSalida').value = '';
        }

        calcularHoras(tolerancia);
    }

    function nuevoHorario_onClick() {
        abrirModalHorario();
    }

    function confirmarElim_onClickHor() {
        if (horarioIdParaEliminar) eliminarHorario(horarioIdParaEliminar);
    }

    function confirmarElim_onClickAsis() {
        if (asistenciaIdParaEliminar) eliminarAsistencia(asistenciaIdParaEliminar);
    }

    // ══════════════════════════════════════════════════════
    //  Recargar turnos activos desde el servidor
    // ══════════════════════════════════════════════════════
    async function recargarTurnos() {
        const select = document.getElementById('asistenciaTurno');
        if (!select) return;

        const r = await fetch('/Asistencia/GetHorariosActivos');
        if (!r.ok) return;
        const lista = await r.json();

        const valorActual = select.value;
        select.innerHTML = '<option value="">— Sin turno (manual) —</option>';
        lista.forEach(h => {
            const opt = document.createElement('option');
            opt.value = h.id;
            opt.textContent = `${h.nombre} (${h.horaEntrada} — ${h.horaSalida})`;
            opt.dataset.entrada = h.horaEntrada;
            opt.dataset.salida = h.horaSalida;
            opt.dataset.tolerancia = h.minutosToleranciaTardanza;
            select.appendChild(opt);
        });
        select.value = valorActual;
    }

    // ══════════════════════════════════════════════════════
    //  CARGAR DATOS PARA EDITAR / VER
    // ══════════════════════════════════════════════════════
    async function cargarDatosAsistencia(id) {
        const r = await fetch(`/Asistencia/GetById/${id}`);
        if (!r.ok) return null;
        return await r.json().catch(() => null);
    }

    function normalizarEnum(valor, lista) {
        return typeof valor === 'number' ? (lista[valor] ?? String(valor)) : valor;
    }

    const METODOS = ['Manual', 'Biometrico', 'QR', 'Tarjeta', 'App'];
    const ESTADOS = ['Presente', 'Ausente', 'Tardanza', 'PermisoJustificado'];

    // ══════════════════════════════════════════════════════
    //  MODAL ASISTENCIA
    // ══════════════════════════════════════════════════════
    function abrirModalAsistencia(data = null) {
        recargarTurnos().then(() => {
            const turnoSelect = document.getElementById('asistenciaTurno');
            if (turnoSelect) turnoSelect.value = data?.horarioId ?? '';
        });

        ['asistenciaEmpleado', 'asistenciaFecha', 'asistenciaEntrada', 'asistenciaTurno']
            .forEach(id => {
                const el = document.getElementById(id);
                if (el) el.disabled = false;
            });

        const label = document.getElementById('modalAsistenciaLabel');
        const es = data?.id > 0;

        document.getElementById('asistenciaId').value = data?.id ?? 0;
        document.getElementById('asistenciaEmpleado').value = data?.empleadoId ?? '';
        // ✅ FIX UTC — usa fechaHoy() para el default, y slice para edición
        document.getElementById('asistenciaFecha').value = data?.fecha
            ? (typeof data.fecha === 'string' ? data.fecha.slice(0, 10) : data.fecha.toISOString().slice(0, 10))
            : fechaHoy();
        document.getElementById('asistenciaEntrada').value = data?.horaEntrada ?? '';
        document.getElementById('asistenciaSalida').value = data?.horaSalida ?? '';
        document.getElementById('asistenciaMetodo').value = normalizarEnum(data?.metodo, METODOS) ?? 'Manual';
        document.getElementById('asistenciaEstado').value = normalizarEnum(data?.estado, ESTADOS) ?? 'Presente';
        document.getElementById('asistenciaObservaciones').value = data?.observacion ?? '';
        document.getElementById('asistenciaHsTrabajo').value = '';
        document.getElementById('asistenciaHsExtra').value = '';
        document.getElementById('asistenciaAtraso').value = '';

        if (label) label.innerHTML = es
            ? '<i class="fa-solid fa-pen me-2" style="color:#0d9488;"></i>Editar Asistencia'
            : '<i class="fa-solid fa-clock-rotate-left me-2" style="color:#0d9488;"></i>Registrar Asistencia';

        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalAsistencia')).show();
    }

    async function editarAsistencia(id) {
        const vm = await cargarDatosAsistencia(id);
        if (!vm) { mostrarToast('No se pudo cargar el registro.', 'danger'); return; }
        abrirModalAsistencia(vm);
    }

    async function registrarSalida(id) {
        const vm = await cargarDatosAsistencia(id);
        if (!vm) { mostrarToast('No se pudo cargar el registro.', 'danger'); return; }

        abrirModalAsistencia(vm);

        setTimeout(() => {
            ['asistenciaEmpleado', 'asistenciaFecha', 'asistenciaEntrada', 'asistenciaTurno']
                .forEach(id => {
                    const el = document.getElementById(id);
                    if (el) el.disabled = true;
                });

            document.getElementById('asistenciaSalida').value = '';
            document.getElementById('asistenciaHsTrabajo').value = '';
            document.getElementById('asistenciaHsExtra').value = '';
            document.getElementById('asistenciaAtraso').value = '';
            document.getElementById('asistenciaSalida').focus();

            const label = document.getElementById('modalAsistenciaLabel');
            if (label) label.innerHTML =
                '<i class="fa-solid fa-clock me-2" style="color:#0d9488;"></i>Registrar Salida';
        }, 350);
    }

    // ══════════════════════════════════════════════════════
    //  Ver detalle
    // ══════════════════════════════════════════════════════
    async function verDetalle(id) {
        const vm = await cargarDatosAsistencia(id);
        if (!vm) { mostrarToast('No se pudo cargar el registro.', 'danger'); return; }

        const body = document.getElementById('verAsistenciaBody');
        if (!body) return;

        const estadoStr = normalizarEnum(vm.estado, ESTADOS);
        const metodoStr = normalizarEnum(vm.metodo, METODOS);

        const estadoMap = {
            'Presente': ['#D1FAE5', '#065F46'],
            'Ausente': ['#FEE2E2', '#991B1B'],
            'Tardanza': ['#FEF9C3', '#854D0E'],
            'PermisoJustificado': ['#DBEAFE', '#1E40AF']
        };
        const [bg, color] = estadoMap[estadoStr] || ['#F1F5F9', '#475569'];
        const fecha = vm.fecha
            ? (typeof vm.fecha === 'string' ? vm.fecha.slice(0, 10) : new Date(vm.fecha).toISOString().slice(0, 10))
            : '—';

        const calcHsTrabajadas = (() => {
            if (!vm.horaEntrada || !vm.horaSalida) return '—';
            const [eh, em] = vm.horaEntrada.slice(0, 5).split(':').map(Number);
            const [sh, sm] = vm.horaSalida.slice(0, 5).split(':').map(Number);
            let diff = (sh * 60 + sm) - (eh * 60 + em);
            if (diff <= 0) diff += 24 * 60;
            return (diff / 60).toFixed(1) + ' h';
        })();

        body.innerHTML = `
    <div style="font-size:13px;">

        <!-- Fila 1: Empleado + Fecha -->
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:16px;">
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">Empleado</div>
                <div style="font-weight:600;color:#111827;">${vm.nombreEmpleado ?? '—'}</div>
            </div>
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">Fecha</div>
                <div style="font-weight:600;color:#111827;">${fecha}</div>
            </div>
        </div>

        <!-- Separador -->
        <hr style="border:none;border-top:1px solid #f1f5f9;margin:0 0 16px;">

        <!-- Fila 2: Entrada + Salida + Estado -->
        <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px;margin-bottom:16px;">
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px;">Entrada</div>
                ${vm.horaEntrada
                ? `<span class="badge-hora entrada">${vm.horaEntrada}</span>`
                : `<span style="color:#9ca3af;">—</span>`}
            </div>
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px;">Salida</div>
                ${vm.horaSalida
                ? `<span class="badge-hora salida">${vm.horaSalida}</span>`
                : `<span style="color:#9ca3af;">—</span>`}
            </div>
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px;">Estado</div>
                <span class="badge-estado-activo" style="background:${bg};color:${color};">${estadoStr}</span>
            </div>
        </div>

        <!-- Separador -->
        <hr style="border:none;border-top:1px solid #f1f5f9;margin:0 0 16px;">

        <!-- Fila 3: H. Trabajadas + Método + Atraso/Extra -->
        <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px;margin-bottom:${vm.observacion ? '16px' : '0'};">
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">H. Trabajadas</div>
                <div style="font-weight:600;color:#111827;">${calcHsTrabajadas}</div>
            </div>
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">Método</div>
                <div style="font-weight:600;color:#111827;">${metodoStr}</div>
            </div>
            <div>
                <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">Atraso</div>
                <div style="font-weight:600;color:${vm.minutosAtraso > 0 ? '#d97706' : '#111827'};">
                    ${vm.minutosAtraso > 0 ? vm.minutosAtraso + ' min' : '—'}
                </div>
            </div>
        </div>

        <!-- Observaciones (solo si hay) -->
        ${vm.observacion ? `
        <hr style="border:none;border-top:1px solid #f1f5f9;margin:0 0 16px;">
        <div>
            <div style="font-size:11px;color:#6b7280;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px;">Observaciones</div>
            <div style="color:#374151;background:#f9fafb;border-radius:6px;padding:8px 12px;">${vm.observacion}</div>
        </div>` : ''}
    </div>`;

        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalVerAsistencia')).show();
    }

    function calcularHoras(toleranciaMinutos = 0) {
        const e = document.getElementById('asistenciaEntrada').value;
        const s = document.getElementById('asistenciaSalida').value;
        if (!e || !s) return;

        const [eh, em] = e.split(':').map(Number);
        const [sh, sm] = s.split(':').map(Number);

        let diff = (sh * 60 + sm) - (eh * 60 + em);
        if (diff <= 0) diff += 24 * 60;

        document.getElementById('asistenciaHsTrabajo').value = (diff / 60).toFixed(2) + ' h';

        const turnoSelect = document.getElementById('asistenciaTurno');
        const opt = turnoSelect?.options[turnoSelect.selectedIndex];
        const turnoEntrada = opt?.dataset.entrada;
        const turnoSalida = opt?.dataset.salida;

        if (turnoEntrada) {
            const [teh, tem] = turnoEntrada.slice(0, 5).split(':').map(Number);
            const minutosAtraso = (eh * 60 + em) - (teh * 60 + tem);
            const atrasoReal = minutosAtraso - (parseInt(opt.dataset.tolerancia) || 0);
            document.getElementById('asistenciaAtraso').value =
                atrasoReal > 0 ? `${atrasoReal} min` : '—';
        }

        if (turnoSalida) {
            const [tsh, tsm] = turnoSalida.slice(0, 5).split(':').map(Number);
            let extra = (sh * 60 + sm) - (tsh * 60 + tsm);
            document.getElementById('asistenciaHsExtra').value =
                extra > 0 ? (extra / 60).toFixed(2) + ' h' : '—';
        }
    }

    async function guardarAsistencia() {
        const id = parseInt(document.getElementById('asistenciaId').value);
        const payload = {
            empleadoId: parseInt(document.getElementById('asistenciaEmpleado').value),
            fecha: document.getElementById('asistenciaFecha').value,
            horaEntrada: document.getElementById('asistenciaEntrada').value || null,
            horaSalida: document.getElementById('asistenciaSalida').value || null,
            metodo: document.getElementById('asistenciaMetodo').value,
            estado: document.getElementById('asistenciaEstado').value,
            observacion: document.getElementById('asistenciaObservaciones').value,
            horasExtra: 0,
            minutosAtraso: 0,
            horarioId: parseInt(document.getElementById('asistenciaTurno')?.value) || null
        };

        if (!payload.empleadoId || !payload.fecha) {
            mostrarToast('Empleado y fecha son requeridos.', 'danger'); return;
        }

        const url = id > 0 ? `/Asistencia/Edit/${id}` : '/Asistencia/Create';
        const result = await postJson(url, payload);

        if (result?.success) {
            bootstrap.Modal.getInstance(document.getElementById('modalAsistencia'))?.hide();
            tablaAsistencia.draw(false);
            cargarKpis();
            mostrarToast(result.message, 'success');
        } else {
            mostrarToast(result?.message ?? 'Error al guardar.', 'danger');
        }
    }

    function confirmarEliminarAsistencia(id) {
        asistenciaIdParaEliminar = id;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalEliminarAsistencia')).show();
    }

    async function eliminarAsistencia(id) {
        const result = await postForm(`/Asistencia/Delete/${id}`);
        bootstrap.Modal.getInstance(document.getElementById('modalEliminarAsistencia'))?.hide();
        if (result?.success) {
            tablaAsistencia.draw(false);
            cargarKpis();
            mostrarToast(result.message, 'success');
        } else {
            mostrarToast(result?.message ?? 'Error al eliminar.', 'danger');
        }
        asistenciaIdParaEliminar = null;
    }

    // ══════════════════════════════════════════════════════
    //  MODAL HORARIO
    // ══════════════════════════════════════════════════════
    function abrirModalHorario(data = null) {
        const label = document.getElementById('modalHorarioLabel');
        document.getElementById('horarioId').value = data?.id ?? 0;
        document.getElementById('horarioNombre').value = data?.nombre ?? '';
        document.getElementById('horarioEntrada').value = data?.horaEntrada ?? '08:00';
        document.getElementById('horarioSalida').value = data?.horaSalida ?? '17:00';
        document.getElementById('horarioTolerancia').value = data?.minutosToleranciaTardanza ?? 15;
        document.getElementById('horarioActivo').checked = data?.activo !== false;
        if (label) label.innerHTML = data?.id > 0
            ? '<i class="fa-solid fa-pen me-2" style="color:#0d9488;"></i>Editar Horario'
            : '<i class="fa-solid fa-calendar-days me-2" style="color:#0d9488;"></i>Nuevo Horario';
        calcularDuracionTurno();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalHorario')).show();
    }

    function calcularDuracionTurno() {
        const e = document.getElementById('horarioEntrada')?.value;
        const s = document.getElementById('horarioSalida')?.value;
        const el = document.getElementById('horarioDuracion');
        if (!el) return;
        if (!e || !s) { el.value = '—'; return; }

        const [eh, em] = e.split(':').map(Number);
        const [sh, sm] = s.split(':').map(Number);

        let diff = (sh * 60 + sm) - (eh * 60 + em);
        if (diff <= 0) diff += 24 * 60;

        const fmt12 = (h, m) => {
            const ampm = h >= 12 ? 'PM' : 'AM';
            const h12 = h % 12 === 0 ? 12 : h % 12;
            return `${h12}:${String(m).padStart(2, '0')} ${ampm}`;
        };

        el.value = `${Math.floor(diff / 60)}h ${diff % 60}min  (${fmt12(eh, em)} → ${fmt12(sh, sm)})`;
    }

    async function guardarHorario() {
        const id = parseInt(document.getElementById('horarioId').value);
        const payload = {
            nombre: document.getElementById('horarioNombre').value.trim(),
            horaEntrada: document.getElementById('horarioEntrada').value,
            horaSalida: document.getElementById('horarioSalida').value,
            minutosToleranciaTardanza: parseInt(document.getElementById('horarioTolerancia').value) || 15,
            activo: document.getElementById('horarioActivo').checked
        };
        if (!payload.nombre) { mostrarToast('El nombre es requerido.', 'danger'); return; }

        const url = id > 0 ? `/Horarios/Edit/${id}` : '/Horarios/Create';
        const result = await postJson(url, payload);

        if (result?.success) {
            bootstrap.Modal.getInstance(document.getElementById('modalHorario'))?.hide();
            tablaHorarios.draw(false);
            recargarTurnos();
            mostrarToast(result.message, 'success');
        } else {
            mostrarToast(result?.message ?? 'Error al guardar.', 'danger');
        }
    }

    async function editarHorario(id) {
        const r = await fetch(`/Horarios/GetById/${id}`);
        if (!r.ok) { mostrarToast('No se pudo cargar el horario.', 'danger'); return; }
        const vm = await r.json().catch(() => null);
        if (vm) abrirModalHorario(vm);
        else mostrarToast('Error al cargar los datos.', 'danger');
    }

    function confirmarEliminarHorario(id, nombre) {
        horarioIdParaEliminar = id;
        document.getElementById('eliminarHorarioNombre').textContent = nombre;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('modalEliminarHorario')).show();
    }

    async function eliminarHorario(id) {
        const result = await postForm(`/Horarios/Delete/${id}`);
        bootstrap.Modal.getInstance(document.getElementById('modalEliminarHorario'))?.hide();
        if (result?.success) {
            tablaHorarios.draw(false);
            recargarTurnos();
            mostrarToast(result.message, 'success');
        } else {
            mostrarToast(result?.message ?? 'No se puede eliminar.', 'danger');
        }
        horarioIdParaEliminar = null;
    }

    async function toggleHorario(id) {
        const result = await postForm(`/Horarios/ToggleActivo/${id}`);
        if (result?.success) {
            tablaHorarios.draw(false);
            recargarTurnos();
            mostrarToast(result.message, 'success');
        } else {
            mostrarToast(result?.message ?? 'Error.', 'danger');
        }
    }

    // ══════════════════════════════════════════════════════
    //  HELPERS HTTP
    // ══════════════════════════════════════════════════════
    async function postJson(url, payload) {
        try {
            const r = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify(payload)
            });
            return await r.json();
        } catch { return null; }
    }

    async function postForm(url) {
        try {
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            const r = await fetch(url, { method: 'POST', body: fd });
            return await r.json();
        } catch { return null; }
    }

    // ══════════════════════════════════════════════════════
    //  TOAST
    // ══════════════════════════════════════════════════════
    function mostrarToast(mensaje, tipo = 'success') {
        const container = document.getElementById('toastContainer') ?? (() => {
            const d = document.createElement('div');
            d.id = 'toastContainer';
            d.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:9999;display:flex;flex-direction:column;gap:8px;';
            document.body.appendChild(d);
            return d;
        })();
        const bg = tipo === 'success' ? '#0d9488' : tipo === 'danger' ? '#dc2626' : '#1e40af';
        const toast = document.createElement('div');
        toast.style.cssText = `background:${bg};color:#fff;padding:12px 18px;border-radius:6px;font-size:13px;box-shadow:0 4px 12px rgba(0,0,0,.15);max-width:320px;animation:fadeIn .2s;`;
        toast.textContent = mensaje;
        container.appendChild(toast);
        setTimeout(() => toast.remove(), 3500);
    }

    // ══════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════
    return {
        init,
        refresh: init, // Alias para reinicializar
        editarAsistencia,
        verDetalle,
        registrarSalida,
        confirmarEliminarAsistencia,
        eliminarAsistencia,
        editarHorario,
        confirmarEliminarHorario,
        toggleHorario
    };

})();