// ════════════════════════════════════════════
// SICE — Módulo Reportes RRHH
// ════════════════════════════════════════════

var Reportes = (function () {

    // ════════════════════════════════════════════
    // VARIABLES Y CONFIGURACIÓN
    // ════════════════════════════════════════════

    let reporteActual = null;
    let filtrosActuales = {};

    const endpoints = {
        'planilla-mensual': '/Reportes/PlanillaMensual',
        'bono14-aguinaldo': '/Reportes/Bono14Aguinaldo',
        'costo-departamento': '/Reportes/CostoPorDepartamento',
        'contratos-vencer': '/Reportes/ContratosPorVencer',
        'sin-documentos': '/Reportes/EmpleadosSinDocumentos',
        'nacionalidad': '/Reportes/Nacionalidad',
        'vacaciones-no-tomadas': '/Reportes/VacacionesNoTomadas',
        'inasistencias-tardanzas': '/Reportes/InasistenciasTardanzas',
        'horas-extra': '/Reportes/HorasExtra',
        'mas-de-tres-faltas': '/Reportes/EmpleadosConMasDeTresFaltas',
        'altas-bajas': '/Reportes/AltasBajas',
        'motivos-salida': '/Reportes/MotivosSalida',
        'tiempo-permanencia': '/Reportes/TiempoPermanencia',
        'proyeccion-indemnizacion': '/Reportes/ProyeccionIndemnizacion',
        'vacaciones-acumuladas': '/Reportes/VacacionesAcumuladas',
        'finiquitos': '/Reportes/FiniquitosEmitidos',
        'completitud-expedientes': '/Reportes/CompletitudExpedientes',
        'documentos-vencidos': '/Reportes/DocumentosVencidos'
    };

    const titulosReportes = {
        'planilla-mensual': 'Planilla Mensual',
        'bono14-aguinaldo': 'Bono 14 y Aguinaldo Proyectado',
        'costo-departamento': 'Costo por Departamento',
        'contratos-vencer': 'Contratos por Vencer',
        'sin-documentos': 'Empleados sin Documentos Obligatorios',
        'nacionalidad': 'Cumplimiento Art. 14 Código de Trabajo',
        'vacaciones-no-tomadas': 'Vacaciones No Tomadas',
        'inasistencias-tardanzas': 'Inasistencias y Tardanzas',
        'horas-extra': 'Horas Extra Trabajadas',
        'mas-de-tres-faltas': 'Empleados con Más de 3 Faltas',
        'altas-bajas': 'Altas y Bajas de Personal',
        'motivos-salida': 'Motivos de Salida',
        'tiempo-permanencia': 'Tiempo de Permanencia por Departamento',
        'proyeccion-indemnizacion': 'Proyección de Indemnización',
        'vacaciones-acumuladas': 'Vacaciones Acumuladas Valorizadas',
        'finiquitos': 'Finiquitos Emitidos',
        'completitud-expedientes': 'Completitud de Expedientes',
        'documentos-vencidos': 'Documentos Vencidos o por Vencer'
    };

    // ════════════════════════════════════════════
    // INICIALIZACIÓN
    // ════════════════════════════════════════════

    function init() {
        console.log('Reportes: inicializando módulo...');

        // Cargar departamentos en el modal de filtros
        cargarDepartamentos();

        // Bind de botones de reporte
        document.querySelectorAll('.btn-reporte').forEach(btn => {
            btn.addEventListener('click', onBtnReporteClick);
        });

        // Bind del botón generar reporte
        const btnGenerar = document.getElementById('btnGenerarReporte');
        if (btnGenerar) {
            btnGenerar.addEventListener('click', generarReporte);
        }

        // Bind de botones de exportación
        const btnExcel = document.getElementById('btnExportarExcel');
        if (btnExcel) {
            btnExcel.addEventListener('click', exportarExcel);
        }

        const btnPdf = document.getElementById('btnExportarPDF');
        if (btnPdf) {
            btnPdf.addEventListener('click', exportarPDF);
        }

        console.log('Reportes: módulo inicializado correctamente.');
    }

    // ════════════════════════════════════════════
    // CARGA DE DEPARTAMENTOS
    // ════════════════════════════════════════════

    async function cargarDepartamentos() {
        try {
            const response = await fetch('/Configuracion/GetDepartamentos');
            if (response.ok) {
                const data = await response.json();
                const select = document.getElementById('filtroDepartamento');
                if (select && data.data) {
                    data.data.forEach(dept => {
                        const option = document.createElement('option');
                        option.value = dept.id;
                        option.textContent = dept.nombre;
                        select.appendChild(option);
                    });
                }
            }
        } catch (error) {
            console.error('Error al cargar departamentos:', error);
        }
    }

    // ════════════════════════════════════════════
    // REPORTES PROGRAMADOS
    // ════════════════════════════════════════════

    async function cargarReportesProgramados() {
        try {
            const response = await fetch('/Reportes/GetReportesProgramados');
            const json = await response.json();
            
            const tbody = document.querySelector('#tablaReportesProgramados tbody');
            if (!tbody) return;
            
            if (!json.success || json.data.length === 0) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="7" class="text-center text-muted py-4">
                            <i class="fa-solid fa-calendar-xmark fa-2x mb-2 d-block"></i>
                            No hay reportes programados
                            <br><small>Configura el envío automático de reportes</small>
                        </td>
                    </tr>
                `;
                return;
            }
            
            tbody.innerHTML = json.data.map(r => `
                <tr>
                    <td><strong>${r.nombre}</strong></td>
                    <td>${r.tipoReporteNombre}</td>
                    <td><span class="badge bg-info">${r.frecuenciaNombre}</span></td>
                    <td><small>${r.emailDestino}</small></td>
                    <td>${r.proximoEnvio ? new Date(r.proximoEnvio).toLocaleString('es-GT') : '—'}</td>
                    <td>
                        <span class="badge ${r.activo ? 'bg-success' : 'bg-secondary'}">
                            ${r.activo ? 'Activo' : 'Inactivo'}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-primary" onclick="ReportesProgramados.editarReporte(${r.id})" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn ${r.activo ? 'btn-outline-warning' : 'btn-outline-success'}" 
                                    onclick="ReportesProgramados.toggleActivo(${r.id}, ${!r.activo})" 
                                    title="${r.activo ? 'Desactivar' : 'Activar'}">
                                <i class="fa-solid fa-${r.activo ? 'pause' : 'play'}"></i>
                            </button>
                            <button class="btn btn-outline-danger" onclick="ReportesProgramados.eliminar(${r.id})" title="Eliminar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');
            
        } catch (error) {
            console.error('Error cargando reportes programados:', error);
        }
    }

    async function guardarReporteProgramado() {
        const nombre = document.getElementById('rpNombre')?.value?.trim();
        const tipoReporte = document.getElementById('rpTipoReporte')?.value;
        const frecuencia = document.getElementById('rpFrecuencia')?.value;
        const emailDestino = document.getElementById('rpEmailDestino')?.value?.trim();
        
        if (!nombre || !tipoReporte || !frecuencia || !emailDestino) {
            toastr.error('Por favor complete los campos requeridos');
            return;
        }
        
        const model = {
            nombre: nombre,
            descripcion: document.getElementById('rpDescripcion')?.value,
            tipoReporte: parseInt(tipoReporte),
            frecuencia: parseInt(frecuencia),
            departamentoId: document.getElementById('rpDepartamento')?.value || null,
            emailDestino: emailDestino,
            emailsCC: document.getElementById('rpEmailsCC')?.value || null,
            horaEnvio: document.getElementById('rpHoraEnvio')?.value || "08:00",
            diaSemana: document.getElementById('rpDiaSemana')?.value ? parseInt(document.getElementById('rpDiaSemana').value) : null,
            diaMes: document.getElementById('rpDiaMes')?.value ? parseInt(document.getElementById('rpDiaMes').value) : 1,
            incluirExcel: document.getElementById('rpIncluirExcel')?.checked ?? true,
            incluirPDF: document.getElementById('rpIncluirPDF')?.checked ?? false,
            enviarAlertas: document.getElementById('rpEnviarAlertas')?.checked ?? true
        };
        
        const id = document.getElementById('reporteProgramadoId')?.value;
        const method = id ? 'PUT' : 'POST';
        const url = id ? `/Reportes/ActualizarReporteProgramado/${id}` : '/Reportes/CrearReporteProgramado';
        
        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(model)
            });
            
            const json = await response.json();
            
            if (json.success) {
                toastr.success('Reporte programado guardado correctamente');
                bootstrap.Modal.getInstance(document.getElementById('modalReporteProgramado'))?.hide();
                cargarReportesProgramados();
            } else {
                toastr.error(json.message || 'Error al guardar');
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }

    async function eliminarReporteProgramado(id) {
        if (!confirm('¿Está seguro de eliminar este reporte programado?')) return;
        
        try {
            const response = await fetch(`/Reportes/EliminarReporteProgramado/${id}`, {
                method: 'DELETE'
            });
            const json = await response.json();
            
            if (json.success) {
                toastr.success('Reporte eliminado correctamente');
                cargarReportesProgramados();
            } else {
                toastr.error(json.message);
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }

    async function activarDesactivarReporte(id, activo) {
        try {
            const response = await fetch(`/Reportes/ActivarDesactivarReporteProgramado/${id}?activo=${activo}`, {
                method: 'POST'
            });
            const json = await response.json();
            
            if (json.success) {
                toastr.success(json.message);
                cargarReportesProgramados();
            } else {
                toastr.error(json.message);
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }

    // ════════════════════════════════════════════
    // MANEJO DE CLICK EN BOTÓN DE REPORTE
    // ════════════════════════════════════════════

    function onBtnReporteClick(e) {
        e.preventDefault();
        const reporteKey = e.currentTarget.getAttribute('data-reporte');
        reporteActual = reporteKey;

        // Configurar filtros según el reporte
        configurarFiltros(reporteKey);

        // Abrir modal de filtros
        const modal = new bootstrap.Modal(document.getElementById('modalFiltros'));
        modal.show();
    }

    function configurarFiltros(reporteKey) {
        const fechaDesde = document.getElementById('filtroFechaDesde');
        const fechaHasta = document.getElementById('filtroFechaHasta');
        const deptContainer = document.getElementById('filtroDepartamento').closest('.mb-3');
        const empContainer = document.getElementById('filtroEmpleadoContainer');

        // Resetear visibilidad
        if (deptContainer) deptContainer.style.display = 'block';
        if (empContainer) empContainer.style.display = 'none';

        // Reportes que requieren fechas
        const conFechas = [
            'planilla-mensual', 'inasistencias-tardanzas', 'horas-extra',
            'altas-bajas', 'motivos-salida', 'finiquitos'
        ];

        // Reportes que requieren departamento
        const conDepartamento = [
            'planilla-mensual', 'bono14-aguinaldo', 'costo-departamento',
            'inasistencias-tardanzas', 'horas-extra', 'proyeccion-indemnizacion',
            'vacaciones-acumuladas'
        ];

        // Ocultar fechas si no las necesita
        if (fechaDesde && fechaHasta) {
            if (!conFechas.includes(reporteKey)) {
                fechaDesde.closest('.mb-3').style.display = 'none';
                fechaHasta.closest('.mb-3').style.display = 'none';
            } else {
                // Valores por defecto
                const hoy = new Date();
                const primerDiaMes = new Date(hoy.getFullYear(), hoy.getMonth(), 1);
                fechaHasta.value = hoy.toISOString().split('T')[0];
                fechaDesde.value = primerDiaMes.toISOString().split('T')[0];
            }
        }

        // Ocultar departamento si no lo necesita
        if (deptContainer && !conDepartamento.includes(reporteKey)) {
            deptContainer.style.display = 'none';
        }
    }

    // ════════════════════════════════════════════
    // GENERAR REPORTE
    // ════════════════════════════════════════════

    async function generarReporte() {
        if (!reporteActual) return;

        const endpoint = endpoints[reporteActual];
        if (!endpoint) {
            toastr.error('Endpoint no configurado para este reporte');
            return;
        }

        // Recopilar filtros
        const fechaDesde = document.getElementById('filtroFechaDesde')?.value;
        const fechaHasta = document.getElementById('filtroFechaHasta')?.value;
        const departamentoId = document.getElementById('filtroDepartamento')?.value;
        const empleadoId = document.getElementById('filtroEmpleado')?.value;

        // Construir URL con query params
        const params = new URLSearchParams();

        if (fechaDesde) {
            params.append('desde', fechaDesde);
            params.append('desde', fechaDesde); // Para algunos endpoints usa 'desde'
        }

        if (fechaHasta) {
            params.append('hasta', fechaHasta);
            params.append('hasta', fechaHasta);
        }

        // Mes y año para reportes específicos
        if (fechaDesde) {
            const d = new Date(fechaDesde);
            params.append('mes', (d.getMonth() + 1).toString());
            params.append('anio', d.getFullYear().toString());
        }

        if (departamentoId && departamentoId !== '') {
            params.append('departamentoId', departamentoId);
        }

        if (empleadoId && empleadoId !== '') {
            params.append('empleadoId', empleadoId);
        }

        // Días para contratos por vencer
        if (reporteActual === 'contratos-vencer') {
            params.append('dias', '90');
        }

        // Cerrar modal de filtros
        bootstrap.Modal.getInstance(document.getElementById('modalFiltros'))?.hide();

        // Abrir modal de resultados con loading
        const modalResultados = new bootstrap.Modal(document.getElementById('modalResultados'));
        document.getElementById('modalResultadosTitle').textContent = titulosReportes[reporteActual] || 'Reporte';
        document.getElementById('resultadosContenido').innerHTML = `
            <div class="loading-reporte">
                <div class="spinner-reportes"></div>
                <span>Generando reporte...</span>
            </div>
        `;
        modalResultados.show();

        try {
            const url = `${endpoint}?${params.toString()}`;
            const response = await fetch(url);
            const json = await response.json();

            if (json.success) {
                filtrosActuales = { fechaDesde, fechaHasta, departamentoId, empleadoId };
                renderizarResultados(json.data, reporteActual);
            } else {
                document.getElementById('resultadosContenido').innerHTML = `
                    <div class="sin-datos">
                        <div class="sin-datos-icon"><i class="fa-solid fa-triangle-exclamation"></i></div>
                        <div class="sin-datos-text">${json.message || 'Error al generar el reporte'}</div>
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error:', error);
            document.getElementById('resultadosContenido').innerHTML = `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-circle-xmark"></i></div>
                    <div class="sin-datos-text">Error de conexión</div>
                    <div class="sin-datos-sub">${error.message}</div>
                </div>
            `;
        }
    }

    // ════════════════════════════════════════════
    // RENDERIZADO DE RESULTADOS
    // ════════════════════════════════════════════

    function renderizarResultados(data, reporteKey) {
        const container = document.getElementById('resultadosContenido');

        if (!data || data.length === 0) {
            const msg = getMensajeVacio(reporteKey);
            container.innerHTML = '<div class="sin-datos"><div class="sin-datos-icon"><i class="fa-solid ' + msg.icon + '"></i></div><div class="sin-datos-text">' + msg.titulo + '</div><div class="sin-datos-sub">' + msg.sub + '</div></div>';
            return;
        }

        let html = '';

        switch (reporteKey) {
            case 'planilla-mensual':
                html = renderPlanillaMensual(data);
                break;
            case 'bono14-aguinaldo':
                html = renderBono14Aguinaldo(data);
                break;
            case 'costo-departamento':
                html = renderCostoPorDepartamento(data);
                break;
            case 'contratos-vencer':
                html = renderContratosPorVencer(data);
                break;
            case 'sin-documentos':
                html = renderSinDocumentos(data);
                break;
            case 'nacionalidad':
                html = renderNacionalidad(data[0]);
                break;
            case 'vacaciones-no-tomadas':
                html = renderVacacionesNoTomadas(data);
                break;
            case 'inasistencias-tardanzas':
                html = renderInasistenciasTardanzas(data);
                break;
            case 'horas-extra':
                html = renderHorasExtra(data);
                break;
            case 'mas-de-tres-faltas':
                html = renderMasDeTresFaltas(data);
                break;
            case 'altas-bajas':
                html = renderAltasBajas(data);
                break;
            case 'motivos-salida':
                html = renderMotivosSalida(data);
                break;
            case 'tiempo-permanencia':
                html = renderTiempoPermanencia(data);
                break;
            case 'proyeccion-indemnizacion':
                html = renderProyeccionIndemnizacion(data);
                break;
            case 'vacaciones-acumuladas':
                html = renderVacacionesAcumuladas(data);
                break;
            case 'finiquitos':
                html = renderFiniquitos(data);
                break;
            case 'completitud-expedientes':
                html = renderCompletitudExpedientes(data);
                break;
            case 'documentos-vencidos':
                html = renderDocumentosVencidos(data);
                break;
            default:
                html = renderGenerico(data);
        }

        container.innerHTML = html;
    }

    // ════════════════════════════════════════════
    // RENDERIZADORES ESPECÍFICOS
    // ════════════════════════════════════════════

    function renderPlanillaMensual(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Planilla Mensual</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Código</th>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Salario Base</th>
                            <th>H. Extra</th>
                            <th>Bonificación</th>
                            <th>Total Devengado</th>
                            <th>IGSS</th>
                            <th>Neto</th>
                            <th>Costo Total</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalSalarios = 0, totalHorasExtra = 0, totalBonificacion = 0, totalDevengado = 0;
        let totalIGSS = 0, totalNeto = 0, totalCosto = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><span class="codigo-badge">${row.codigo}</span></td>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td class="text-end">Q${row.salarioBase.toFixed(2)}</td>
                    <td class="text-end">Q${row.horasExtra.toFixed(2)}</td>
                    <td class="text-end">Q${row.bonificacion250.toFixed(2)}</td>
                    <td class="text-end"><strong>Q${row.totalDevengado.toFixed(2)}</strong></td>
                    <td class="text-end">Q${row.igssLaboral.toFixed(2)}</td>
                    <td class="text-end"><strong class="text-success">Q${row.salarioNeto.toFixed(2)}</strong></td>
                    <td class="text-end">Q${row.costoTotal.toFixed(2)}</td>
                </tr>
            `;

            totalSalarios += row.salarioBase;
            totalHorasExtra += row.horasExtra;
            totalBonificacion += row.bonificacion250;
            totalDevengado += row.totalDevengado;
            totalIGSS += row.igssLaboral;
            totalNeto += row.salarioNeto;
            totalCosto += row.costoTotal;
        });

        html += `
            <tr class="total-row">
                <td colspan="3">TOTALES</td>
                <td class="text-end">Q${totalSalarios.toFixed(2)}</td>
                <td class="text-end">Q${totalHorasExtra.toFixed(2)}</td>
                <td class="text-end">Q${totalBonificacion.toFixed(2)}</td>
                <td class="text-end">Q${totalDevengado.toFixed(2)}</td>
                <td class="text-end">Q${totalIGSS.toFixed(2)}</td>
                <td class="text-end">Q${totalNeto.toFixed(2)}</td>
                <td class="text-end">Q${totalCosto.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderBono14Aguinaldo(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Bono 14 y Aguinaldo Proyectado</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>F. Ingreso</th>
                            <th>Meses</th>
                            <th>Salario Base</th>
                            <th>Bono 14</th>
                            <th>Aguinaldo</th>
                            <th>Total</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalBono14 = 0, totalAguinaldo = 0, totalPrestaciones = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${new Date(row.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    <td>${row.mesesTrabajados}</td>
                    <td class="text-end">Q${row.salarioBase.toFixed(2)}</td>
                    <td class="text-end">Q${row.bono14Proyectado.toFixed(2)}</td>
                    <td class="text-end">Q${row.aguinaldoProyectado.toFixed(2)}</td>
                    <td class="text-end"><strong>Q${row.totalPrestaciones.toFixed(2)}</strong></td>
                </tr>
            `;

            totalBono14 += row.bono14Proyectado;
            totalAguinaldo += row.aguinaldoProyectado;
            totalPrestaciones += row.totalPrestaciones;
        });

        html += `
            <tr class="total-row">
                <td colspan="4">TOTALES</td>
                <td colspan="2"></td>
                <td class="text-end">Q${totalBono14.toFixed(2)}</td>
                <td class="text-end">Q${totalAguinaldo.toFixed(2)}</td>
                <td class="text-end">Q${totalPrestaciones.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderCostoPorDepartamento(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Costo por Departamento</span>
                <span class="resultados-meta">${data.length} departamentos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Departamento</th>
                            <th>Empleados</th>
                            <th>Salarios</th>
                            <th>IGSS Patronal</th>
                            <th>Bono 14</th>
                            <th>Aguinaldo</th>
                            <th>Total Prestaciones</th>
                            <th>Costo Total</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalEmpleados = 0, totalSalarios = 0, totalIGSS = 0;
        let totalBono14 = 0, totalAguinaldo = 0, totalPrestaciones = 0, totalCosto = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.departamento}</strong></td>
                    <td>${row.cantidadEmpleados}</td>
                    <td class="text-end">Q${row.totalSalarios.toFixed(2)}</td>
                    <td class="text-end">Q${row.totalIGSSPatronal.toFixed(2)}</td>
                    <td class="text-end">Q${row.totalBono14.toFixed(2)}</td>
                    <td class="text-end">Q${row.totalAguinaldo.toFixed(2)}</td>
                    <td class="text-end">Q${row.totalPrestaciones.toFixed(2)}</td>
                    <td class="text-end"><strong>Q${row.costoTotalDepartamento.toFixed(2)}</strong></td>
                </tr>
            `;

            totalEmpleados += row.cantidadEmpleados;
            totalSalarios += row.totalSalarios;
            totalIGSS += row.totalIGSSPatronal;
            totalBono14 += row.totalBono14;
            totalAguinaldo += row.totalAguinaldo;
            totalPrestaciones += row.totalPrestaciones;
            totalCosto += row.costoTotalDepartamento;
        });

        html += `
            <tr class="total-row">
                <td>TOTALES</td>
                <td>${totalEmpleados}</td>
                <td class="text-end">Q${totalSalarios.toFixed(2)}</td>
                <td class="text-end">Q${totalIGSS.toFixed(2)}</td>
                <td class="text-end">Q${totalBono14.toFixed(2)}</td>
                <td class="text-end">Q${totalAguinaldo.toFixed(2)}</td>
                <td class="text-end">Q${totalPrestaciones.toFixed(2)}</td>
                <td class="text-end">Q${totalCosto.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderContratosPorVencer(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">¡Todo en orden!</div>
                    <div class="sin-datos-sub">No hay contratos por vencer en los próximos 90 días</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Contratos por Vencer</span>
                <span class="resultados-meta">${data.length} contratos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Puesto</th>
                            <th>Tipo Contrato</th>
                            <th>F. Ingreso</th>
                            <th>F. Vencimiento</th>
                            <th>Días</th>
                            <th>Alerta</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            let alertaClass = row.diasParaVencer <= 30 ? 'dias-alert-critico' : row.diasParaVencer <= 60 ? 'dias-alert-medio' : 'dias-alert-bajo';

            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${row.puesto}</td>
                    <td><span class="contrato-badge contrato-${row.tipoContrato.toLowerCase().replace(' ', '')}">${row.tipoContrato}</span></td>
                    <td>${new Date(row.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    <td>${row.fechaVencimiento ? new Date(row.fechaVencimiento).toLocaleDateString('es-GT') : '—'}</td>
                    <td><strong>${row.diasParaVencer} días</strong></td>
                    <td><span class="dias-alert ${alertaClass}">${row.alerta}</span></td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderSinDocumentos(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">¡Expedientes completos!</div>
                    <div class="sin-datos-sub">Todos los empleados tienen documentos obligatorios</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Empleados sin Documentos Obligatorios</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Email</th>
                            <th>CUI</th>
                            <th>IGSS</th>
                            <th>NIT</th>
                            <th>Antecedentes</th>
                            <th>Faltantes</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${row.email}</td>
                    <td class="text-center">${row.tieneCUI ? '<i class="fa-solid fa-check text-success"></i>' : '<i class="fa-solid fa-xmark text-danger"></i>'}</td>
                    <td class="text-center">${row.tieneIGSS ? '<i class="fa-solid fa-check text-success"></i>' : '<i class="fa-solid fa-xmark text-danger"></i>'}</td>
                    <td class="text-center">${row.tieneNIT ? '<i class="fa-solid fa-check text-success"></i>' : '<i class="fa-solid fa-xmark text-danger"></i>'}</td>
                    <td class="text-center">${row.tieneAntecedentes ? '<i class="fa-solid fa-check text-success"></i>' : '<i class="fa-solid fa-xmark text-danger"></i>'}</td>
                    <td>
                        <span class="reporte-badge badge-pendiente">${row.documentosFaltantes.join(', ')}</span>
                    </td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderNacionalidad(data) {
        if (!data) return '<div class="sin-datos"><p>No hay datos disponibles</p></div>';

        const cumpleClass = data.cumpleArticulo14 ? 'badge-cumplido' : 'badge-pendiente';
        const cumpleTexto = data.cumpleArticulo14 ? 'CUMPLE' : 'NO CUMPLE';

        return `
            <div class="resumen-card mb-4">
                <div class="row">
                    <div class="col-md-3">
                        <div class="resumen-card-title">Total Empleados</div>
                        <div class="resumen-card-value">${data.totalEmpleados}</div>
                    </div>
                    <div class="col-md-3">
                        <div class="resumen-card-title">Guatemaltecos</div>
                        <div class="resumen-card-value">${data.empleadosGuatemaltecos}</div>
                        <div class="resumen-card-sub">${data.porcentajeGuatemaltecos.toFixed(2)}%</div>
                    </div>
                    <div class="col-md-3">
                        <div class="resumen-card-title">Extranjeros</div>
                        <div class="resumen-card-value">${data.empleadosExtranjeros}</div>
                        <div class="resumen-card-sub">${data.porcentajeExtranjeros.toFixed(2)}%</div>
                    </div>
                    <div class="col-md-3">
                        <div class="resumen-card-title">Art. 14 CT</div>
                        <div class="resumen-card-value"><span class="reporte-badge ${cumpleClass}">${cumpleTexto}</span></div>
                        <div class="resumen-card-sub">Máx 10% extranjeros</div>
                    </div>
                </div>
            </div>
            <div class="alert ${data.cumpleArticulo14 ? 'alert-success' : 'alert-danger'}">
                <i class="fa-solid fa-${data.cumpleArticulo14 ? 'check-circle' : 'triangle-exclamation'} me-2"></i>
                ${data.observacion}
            </div>
        `;
    }

    function renderVacacionesNoTomadas(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">¡Todos al día!</div>
                    <div class="sin-datos-sub">No hay vacaciones pendientes por tomar</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Vacaciones No Tomadas</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>F. Ingreso</th>
                            <th>Días Disponibles</th>
                            <th>Días Tomados</th>
                            <th>Últimas Vacaciones</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${new Date(row.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    <td><strong class="text-success">${row.diasDisponibles} días</strong></td>
                    <td>${row.diasTomadosAnioActual}</td>
                    <td>${row.ultimaVacacionFecha ? new Date(row.ultimaVacacionFecha).toLocaleDateString('es-GT') : '—'}</td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderInasistenciasTardanzas(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Inasistencias y Tardanzas</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Presentes</th>
                            <th>Ausentes</th>
                            <th>Tardanzas</th>
                            <th>Justificados</th>
                            <th>H. Extra</th>
                            <th>Asistencia</th>
                            <th>Estado</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            const estadoClass = row.estado === 'Normal' ? 'badge-normal' : 'badge-riesgo';

            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${row.diasPresentes}</td>
                    <td>${row.diasAusentes}</td>
                    <td>${row.diasTardanza}</td>
                    <td>${row.diasJustificados}</td>
                    <td>${row.totalHorasExtra.toFixed(1)}</td>
                    <td>
                        <div class="progress" style="height: 6px; width: 100px;">
                            <div class="progress-bar ${row.porcentajeAsistencia < 90 ? 'bg-danger' : 'bg-success'}"
                                 style="width: ${Math.min(100, row.porcentajeAsistencia)}%"></div>
                        </div>
                        <small>${row.porcentajeAsistencia.toFixed(1)}%</small>
                    </td>
                    <td><span class="reporte-badge ${estadoClass}">${row.estado}</span></td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderHorasExtra(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-clock"></i></div>
                    <div class="sin-datos-text">Sin horas extra</div>
                    <div class="sin-datos-sub">No se registraron horas extra en el período</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Horas Extra Trabajadas</span>
                <span class="resultados-meta">${data.length} empleados con H.E.</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Horas Extra</th>
                            <th>Días con H.E.</th>
                            <th>Monto H.E.</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalHoras = 0, totalMonto = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td><strong>${row.totalHorasExtraMes.toFixed(2)} hrs</strong></td>
                    <td>${row.cantidadDiasConHoraExtra}</td>
                    <td class="text-end"><strong class="text-success">Q${row.montoHorasExtra.toFixed(2)}</strong></td>
                </tr>
            `;

            totalHoras += row.totalHorasExtraMes;
            totalMonto += row.montoHorasExtra;
        });

        html += `
            <tr class="total-row">
                <td colspan="2">TOTALES</td>
                <td>${totalHoras.toFixed(2)} hrs</td>
                <td></td>
                <td class="text-end">Q${totalMonto.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderMasDeTresFaltas(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">Sin empleados con +3 faltas</div>
                    <div class="sin-datos-sub">Buen trabajo manteniendo la asistencia</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Empleados con Más de 3 Faltas</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Total Faltas</th>
                            <th>Reincidente</th>
                            <th>Fechas de Falta</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            const fechas = row.fechasFalta.map(f => new Date(f).toLocaleDateString('es-GT')).join(', ');

            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong><br><small class="text-muted">${row.email}</small></td>
                    <td>${row.departamento}</td>
                    <td><strong class="text-danger">${row.totalFaltasMes} faltas</strong></td>
                    <td>${row.esReincidente ? '<span class="reporte-badge badge-riesgo">Sí</span>' : '<span class="reporte-badge badge-normal">No</span>'}</td>
                    <td><small>${fechas}</small></td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderAltasBajas(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Altas y Bajas de Personal</span>
                <span class="resultados-meta">${data.length} períodos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Período</th>
                            <th>Altas</th>
                            <th>Bajas</th>
                            <th>Neto</th>
                            <th>Tasa Rotación</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            const netoClass = row.neto >= 0 ? 'text-success' : 'text-danger';
            const tasaClass = row.tasaRotacion > 10 ? 'text-danger' : row.tasaRotacion > 5 ? 'text-warning' : 'text-success';

            html += `
                <tr>
                    <td><strong>${new Date(row.periodo).toLocaleDateString('es-GT', { month: 'long', year: 'numeric' })}</strong></td>
                    <td class="text-success">+${row.altas}</td>
                    <td class="text-danger">-${row.bajas}</td>
                    <td class="${netoClass}"><strong>${row.neto > 0 ? '+' : ''}${row.neto}</strong></td>
                    <td class="${tasaClass}">${row.tasaRotacion.toFixed(2)}%</td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderMotivosSalida(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Motivos de Salida</span>
                <span class="resultados-meta">${data.length} tipos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Motivo</th>
                            <th>Cantidad</th>
                            <th>Porcentaje</th>
                            <th>Total Indemnizaciones</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.motivo}</strong></td>
                    <td>${row.cantidad}</td>
                    <td>${row.porcentaje.toFixed(1)}%</td>
                    <td class="text-end">Q${row.totalIndemnizaciones.toFixed(2)}</td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderTiempoPermanencia(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Tiempo de Permanencia por Departamento</span>
                <span class="resultados-meta">${data.length} departamentos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Departamento</th>
                            <th>Empleados</th>
                            <th>Tiempo Promedio</th>
                            <th>&lt; 1 año</th>
                            <th>1-3 años</th>
                            <th>3-5 años</th>
                            <th>&gt; 5 años</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            const rango = row.rangoAntiguedad;
            html += `
                <tr>
                    <td><strong>${row.departamento}</strong></td>
                    <td>${row.cantidadEmpleados}</td>
                    <td>${Math.floor(row.tiempoPromedioAnios)}a ${row.tiempoPromedioMeses}m</td>
                    <td>${rango.menos1Anio}</td>
                    <td>${rango.de1a3Anios}</td>
                    <td>${rango.de3a5Anios}</td>
                    <td>${rango.masDe5Anios}</td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderProyeccionIndemnizacion(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Proyección de Indemnización</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>F. Ingreso</th>
                            <th>Años</th>
                            <th>Salario</th>
                            <th>Indem. Años</th>
                            <th>Vacaciones</th>
                            <th>Bono 14</th>
                            <th>Aguinaldo</th>
                            <th>Total</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalIndem = 0, totalVacaciones = 0, totalBono14 = 0, totalAguinaldo = 0, totalGeneral = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${new Date(row.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    <td>${row.aniosServicio}a ${row.mesesAdicionales}m</td>
                    <td class="text-end">Q${row.salarioBase.toFixed(2)}</td>
                    <td class="text-end">Q${row.indemnizacionPorAnios.toFixed(2)}</td>
                    <td class="text-end">Q${row.vacacionesNoGozadas.toFixed(2)}</td>
                    <td class="text-end">Q${row.bono14Proporcional.toFixed(2)}</td>
                    <td class="text-end">Q${row.aguinaldoProporcional.toFixed(2)}</td>
                    <td class="text-end"><strong>Q${row.totalIndemnificacion.toFixed(2)}</strong></td>
                </tr>
            `;

            totalIndem += row.indemnizacionPorAnios;
            totalVacaciones += row.vacacionesNoGozadas;
            totalBono14 += row.bono14Proporcional;
            totalAguinaldo += row.aguinaldoProporcional;
            totalGeneral += row.totalIndemnificacion;
        });

        html += `
            <tr class="total-row">
                <td colspan="5">TOTALES</td>
                <td class="text-end">Q${totalIndem.toFixed(2)}</td>
                <td class="text-end">Q${totalVacaciones.toFixed(2)}</td>
                <td class="text-end">Q${totalBono14.toFixed(2)}</td>
                <td class="text-end">Q${totalAguinaldo.toFixed(2)}</td>
                <td class="text-end">Q${totalGeneral.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderVacacionesAcumuladas(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">Sin vacaciones acumuladas</div>
                    <div class="sin-datos-sub">Todos los empleados han tomado sus vacaciones</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Vacaciones Acumuladas Valorizadas</span>
                <span class="resultados-meta">${data.length} empleados</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>F. Ingreso</th>
                            <th>Días Acumulados</th>
                            <th>Días Tomados</th>
                            <th>Valor Diario</th>
                            <th>Valor Q</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalDias = 0, totalValor = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${new Date(row.fechaIngreso).toLocaleDateString('es-GT')}</td>
                    <td><strong class="text-success">${row.diasAcumulados} días</strong></td>
                    <td>${row.diasTomadosAnio}</td>
                    <td class="text-end">Q${row.valorDiario.toFixed(2)}</td>
                    <td class="text-end"><strong>Q${row.valorQuetzales.toFixed(2)}</strong></td>
                </tr>
            `;

            totalDias += row.diasAcumulados;
            totalValor += row.valorQuetzales;
        });

        html += `
            <tr class="total-row">
                <td colspan="3">TOTALES</td>
                <td>${totalDias} días</td>
                <td></td>
                <td></td>
                <td class="text-end">Q${totalValor.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderFiniquitos(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-file-signature"></i></div>
                    <div class="sin-datos-text">Sin finiquitos</div>
                    <div class="sin-datos-sub">No se emitieron finiquitos en el período</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Finiquitos Emitidos</span>
                <span class="resultados-meta">${data.length} finiquitos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>F. Salida</th>
                            <th>Motivo</th>
                            <th>Salario</th>
                            <th>Vacaciones</th>
                            <th>Bono 14</th>
                            <th>Aguinaldo</th>
                            <th>Deducciones</th>
                            <th>Neto</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalSalario = 0, totalVacaciones = 0, totalBono14 = 0, totalAguinaldo = 0, totalDeducciones = 0, totalNeto = 0;

        data.forEach(row => {
            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${new Date(row.fechaSalida).toLocaleDateString('es-GT')}</td>
                    <td><span class="reporte-badge ${row.motivoSalida === 'Despido' ? 'badge-riesgo' : 'badge-normal'}">${row.motivoSalida}</span></td>
                    <td class="text-end">Q${row.salarioDevengado.toFixed(2)}</td>
                    <td class="text-end">Q${row.vacacionesNoGozadas.toFixed(2)}</td>
                    <td class="text-end">Q${row.bono14Proporcional.toFixed(2)}</td>
                    <td class="text-end">Q${row.aguinaldoProporcional.toFixed(2)}</td>
                    <td class="text-end text-danger">Q${row.totalDeducciones.toFixed(2)}</td>
                    <td class="text-end"><strong class="text-success">Q${row.totalNetoPagar.toFixed(2)}</strong></td>
                </tr>
            `;

            totalSalario += row.salarioDevengado;
            totalVacaciones += row.vacacionesNoGozadas;
            totalBono14 += row.bono14Proporcional;
            totalAguinaldo += row.aguinaldoProporcional;
            totalDeducciones += row.totalDeducciones;
            totalNeto += row.totalNetoPagar;
        });

        html += `
            <tr class="total-row">
                <td colspan="3">TOTALES</td>
                <td class="text-end">Q${totalSalario.toFixed(2)}</td>
                <td class="text-end">Q${totalVacaciones.toFixed(2)}</td>
                <td class="text-end">Q${totalBono14.toFixed(2)}</td>
                <td class="text-end">Q${totalAguinaldo.toFixed(2)}</td>
                <td class="text-end">Q${totalDeducciones.toFixed(2)}</td>
                <td class="text-end">Q${totalNeto.toFixed(2)}</td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderCompletitudExpedientes(data) {
        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Completitud de Expedientes</span>
                <span class="resultados-meta">${data.length} departamentos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Departamento</th>
                            <th>Total Empleados</th>
                            <th>Completos</th>
                            <th>Incompletos</th>
                            <th>% Completitud</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        let totalEmpleados = 0, totalCompletos = 0, totalIncompletos = 0;

        data.forEach(row => {
            const porcentajeClass = row.porcentajeCompletitud >= 80 ? 'text-success' : row.porcentajeCompletitud >= 50 ? 'text-warning' : 'text-danger';

            html += `
                <tr>
                    <td><strong>${row.departamento}</strong></td>
                    <td>${row.totalEmpleados}</td>
                    <td class="text-success">${row.expedientesCompletos}</td>
                    <td class="text-danger">${row.expedientesIncompletos}</td>
                    <td>
                        <div class="d-flex align-items-center gap-2">
                            <div class="progress" style="height: 8px; width: 100px;">
                                <div class="progress-bar ${porcentajeClass}" style="width: ${row.porcentajeCompletitud}%"></div>
                            </div>
                            <strong class="${porcentajeClass}">${row.porcentajeCompletitud.toFixed(1)}%</strong>
                        </div>
                    </td>
                </tr>
            `;

            totalEmpleados += row.totalEmpleados;
            totalCompletos += row.expedientesCompletos;
            totalIncompletos += row.expedientesIncompletos;
        });

        const porcGeneral = totalEmpleados > 0 ? (totalCompletos * 100 / totalEmpleados) : 0;

        html += `
            <tr class="total-row">
                <td>TOTALES</td>
                <td>${totalEmpleados}</td>
                <td class="text-success">${totalCompletos}</td>
                <td class="text-danger">${totalIncompletos}</td>
                <td>
                    <strong class="${porcGeneral >= 80 ? 'text-success' : porcGeneral >= 50 ? 'text-warning' : 'text-danger'}">
                        ${porcGeneral.toFixed(1)}%
                    </strong>
                </td>
            </tr>
        `;

        html += `</tbody></table></div>`;
        return html;
    }

    function renderDocumentosVencidos(data) {
        if (data.length === 0) {
            return `
                <div class="sin-datos">
                    <div class="sin-datos-icon"><i class="fa-solid fa-check-circle"></i></div>
                    <div class="sin-datos-text">¡Documentos al día!</div>
                    <div class="sin-datos-sub">No hay documentos vencidos o por vencer</div>
                </div>
            `;
        }

        let html = `
            <div class="resultados-header">
                <span class="resultados-title">Documentos Vencidos o por Vencer</span>
                <span class="resultados-meta">${data.length} documentos</span>
            </div>
            <div class="resultados-table-container">
                <table class="resultados-table">
                    <thead>
                        <tr>
                            <th>Empleado</th>
                            <th>Departamento</th>
                            <th>Documento</th>
                            <th>F. Vencimiento</th>
                            <th>Días</th>
                            <th>Estado</th>
                        </tr>
                    </thead>
                    <tbody>
        `;

        data.forEach(row => {
            const estadoClass = row.estado === 'Vencido' ? 'badge-vencido' : 'badge-por-vencer';
            const diasTexto = row.estado === 'Vencido' ? `${row.diasVencido} días vencido` : `Vence en ${row.diasVencido} días`;

            html += `
                <tr>
                    <td><strong>${row.nombreCompleto}</strong></td>
                    <td>${row.departamento}</td>
                    <td>${row.tipoDocumento}</td>
                    <td>${row.fechaVencimiento ? new Date(row.fechaVencimiento).toLocaleDateString('es-GT') : '—'}</td>
                    <td><small>${diasTexto}</small></td>
                    <td><span class="reporte-badge ${estadoClass}">${row.estado}</span></td>
                </tr>
            `;
        });

        html += `</tbody></table></div>`;
        return html;
    }

    function renderGenerico(data) {
        // Fallback genérico para datos no mapeados
        if (!Array.isArray(data) || data.length === 0) {
            return '<div class="sin-datos"><p>No hay datos para mostrar</p></div>';
        }

        const keys = Object.keys(data[0]);
        let html = '<div class="resultados-table-container"><table class="resultados-table"><thead><tr>';

        keys.forEach(key => {
            html += `<th>${key}</th>`;
        });

        html += '</tr></thead><tbody>';

        data.forEach(row => {
            html += '<tr>';
            keys.forEach(key => {
                html += `<td>${row[key]}</td>`;
            });
            html += '</tr>';
        });

        html += '</tbody></table></div>';
        return html;
    }

    // ════════════════════════════════════════════
    // EXPORTAR A EXCEL
    // ════════════════════════════════════════════

    async function exportarExcel() {
        toastr.info('Generando Excel...', 'Exportar');
        // Implementación futura con SheetJS o endpoint backend
    }

    // ════════════════════════════════════════════
    // EXPORTAR A PDF
    // ════════════════════════════════════════════

    async function exportarPDF() {
        toastr.info('Generando PDF...', 'Exportar');
        // Implementación futura con window.print() o endpoint backend
        window.print();
    }

    // ════════════════════════════════════════════
    // API PÚBLICA
    // ════════════════════════════════════════════

    return {
        init: init,
        cargarReportesProgramados: cargarReportesProgramados,
        guardarReporteProgramado: guardarReporteProgramado,
        eliminarReporteProgramado: eliminarReporteProgramado,
        activarDesactivarReporte: activarDesactivarReporte
    };
})();

// ════════════════════════════════════════════
// FUNCIONES PARA REPORTES PROGRAMADOS
// ════════════════════════════════════════════

var ReportesProgramados = (function () {
    
    function init() {
        console.log('Inicializando Reportes Programados...');
        
        // Evento tab change
        document.querySelector('#tab-programados-tab')?.addEventListener('shown.bs.tab', function () {
            cargarReportesProgramados();
        });
        
        // Cargar departamentos en modal
        cargarDepartamentosProgramados();
        
        // Evento guardar
        document.getElementById('btnGuardarReporteProgramado')?.addEventListener('click', guardarReporteProgramado);
        
        // Evento limpiar modal al cerrar
        document.getElementById('modalReporteProgramado')?.addEventListener('hidden.bs.modal', function () {
            limpiarFormularioReporteProgramado();
        });
    }
    
    async function cargarDepartamentosProgramados() {
        try {
            const response = await fetch('/Reportes/ObtenerDepartamentos');
            if (response.ok) {
                const json = await response.json();
                if (json.success) {
                    const select = document.getElementById('rpDepartamento');
                    if (select) {
                        json.data.forEach(dept => {
                            const option = document.createElement('option');
                            option.value = dept.id;
                            option.textContent = dept.nombre;
                            select.appendChild(option);
                        });
                    }
                }
            }
        } catch (error) {
            console.error('Error cargando departamentos:', error);
        }
    }
    
    async function cargarReportesProgramados() {
        try {
            const response = await fetch('/Reportes/GetReportesProgramados');
            const json = await response.json();
            
            const tbody = document.querySelector('#tablaReportesProgramados tbody');
            if (!tbody) return;
            
            if (!json.success || json.data.length === 0) {
                tbody.innerHTML = `
                    <tr>
                        <td colspan="7" class="text-center text-muted py-4">
                            <i class="fa-solid fa-calendar-xmark fa-2x mb-2 d-block"></i>
                            No hay reportes programados
                            <br><small>Configura el envío automático de reportes</small>
                        </td>
                    </tr>
                `;
                return;
            }
            
            tbody.innerHTML = json.data.map(r => `
                <tr>
                    <td><strong>${r.nombre}</strong></td>
                    <td>${r.tipoReporteNombre}</td>
                    <td><span class="badge bg-info">${r.frecuenciaNombre}</span></td>
                    <td><small>${r.emailDestino}</small></td>
                    <td>${r.proximoEnvio ? new Date(r.proximoEnvio).toLocaleString('es-GT') : '—'}</td>
                    <td>
                        <span class="badge ${r.activo ? 'bg-success' : 'bg-secondary'}">
                            ${r.activo ? 'Activo' : 'Inactivo'}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm">
                            <button class="btn btn-outline-primary" onclick="ReportesProgramados.editarReporte(${r.id})" title="Editar">
                                <i class="fa-solid fa-pen"></i>
                            </button>
                            <button class="btn ${r.activo ? 'btn-outline-warning' : 'btn-outline-success'}" 
                                    onclick="ReportesProgramados.toggleActivo(${r.id}, ${!r.activo})" 
                                    title="${r.activo ? 'Desactivar' : 'Activar'}">
                                <i class="fa-solid fa-${r.activo ? 'pause' : 'play'}"></i>
                            </button>
                            <button class="btn btn-outline-danger" onclick="ReportesProgramados.eliminar(${r.id})" title="Eliminar">
                                <i class="fa-solid fa-trash"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');
            
        } catch (error) {
            console.error('Error cargando reportes programados:', error);
        }
    }
    
    async function guardarReporteProgramado() {
        const nombre = document.getElementById('rpNombre')?.value?.trim();
        const tipoReporte = document.getElementById('rpTipoReporte')?.value;
        const frecuencia = document.getElementById('rpFrecuencia')?.value;
        const emailDestino = document.getElementById('rpEmailDestino')?.value?.trim();
        
        if (!nombre || !tipoReporte || !frecuencia || !emailDestino) {
            toastr.error('Por favor complete los campos requeridos');
            return;
        }
        
        const model = {
            nombre: nombre,
            descripcion: document.getElementById('rpDescripcion')?.value,
            tipoReporte: parseInt(tipoReporte),
            frecuencia: parseInt(frecuencia),
            departamentoId: document.getElementById('rpDepartamento')?.value || null,
            emailDestino: emailDestino,
            emailsCC: document.getElementById('rpEmailsCC')?.value || null,
            horaEnvio: document.getElementById('rpHoraEnvio')?.value || "08:00",
            diaSemana: document.getElementById('rpDiaSemana')?.value ? parseInt(document.getElementById('rpDiaSemana').value) : null,
            diaMes: document.getElementById('rpDiaMes')?.value ? parseInt(document.getElementById('rpDiaMes').value) : 1,
            incluirExcel: document.getElementById('rpIncluirExcel')?.checked ?? true,
            incluirPDF: document.getElementById('rpIncluirPDF')?.checked ?? false,
            enviarAlertas: document.getElementById('rpEnviarAlertas')?.checked ?? true
        };
        
        const id = document.getElementById('reporteProgramadoId')?.value;
        const method = id ? 'PUT' : 'POST';
        const url = id ? `/Reportes/ActualizarReporteProgramado/${id}` : '/Reportes/CrearReporteProgramado';
        
        try {
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(model)
            });
            
            const json = await response.json();
            
            if (json.success) {
                toastr.success('Reporte programado guardado correctamente');
                bootstrap.Modal.getInstance(document.getElementById('modalReporteProgramado'))?.hide();
                cargarReportesProgramados();
            } else {
                toastr.error(json.message || 'Error al guardar');
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }
    
    async function editarReporte(id) {
        try {
            const response = await fetch(`/Reportes/GetReporteProgramado/${id}`);
            const json = await response.json();
            
            if (!json.success) {
                toastr.error('Error al cargar el reporte');
                return;
            }
            
            const r = json.data;
            
            document.getElementById('reporteProgramadoId').value = r.id;
            document.getElementById('rpNombre').value = r.nombre;
            document.getElementById('rpDescripcion').value = r.descripcion || '';
            document.getElementById('rpTipoReporte').value = r.tipoReporte;
            document.getElementById('rpFrecuencia').value = r.frecuencia;
            document.getElementById('rpDepartamento').value = r.departamentoId || '';
            document.getElementById('rpEmailDestino').value = r.emailDestino;
            document.getElementById('rpEmailsCC').value = r.emailsCC || '';
            
            if (r.horaEnvio) {
                const parts = r.horaEnvio.split(':');
                document.getElementById('rpHoraEnvio').value = `${parts[0]}:${parts[1]}`;
            }
            
            document.getElementById('rpDiaSemana').value = r.diaSemana ?? '';
            document.getElementById('rpDiaMes').value = r.diaMes || 1;
            document.getElementById('rpIncluirExcel').checked = r.incluirExcel;
            document.getElementById('rpIncluirPDF').checked = r.incluirPDF;
            document.getElementById('rpEnviarAlertas').checked = r.enviarAlertas;
            
            // Cambiar título del modal
            document.querySelector('#modalReporteProgramado .modal-title').innerHTML = 'Editar Reporte Programado';
            
            const modal = new bootstrap.Modal(document.getElementById('modalReporteProgramado'));
            modal.show();
            
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error al cargar el reporte');
        }
    }
    
    async function toggleActivo(id, activo) {
        try {
            const response = await fetch(`/Reportes/ActivarDesactivarReporteProgramado/${id}?activo=${activo}`, {
                method: 'POST'
            });
            const json = await response.json();
            
            if (json.success) {
                toastr.success(json.message);
                cargarReportesProgramados();
            } else {
                toastr.error(json.message);
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }
    
    async function eliminar(id) {
        if (!confirm('¿Está seguro de eliminar este reporte programado?')) return;
        
        try {
            const response = await fetch(`/Reportes/EliminarReporteProgramado/${id}`, {
                method: 'DELETE'
            });
            const json = await response.json();
            
            if (json.success) {
                toastr.success('Reporte eliminado correctamente');
                cargarReportesProgramados();
            } else {
                toastr.error(json.message);
            }
        } catch (error) {
            console.error('Error:', error);
            toastr.error('Error de conexión');
        }
    }
    
    function limpiarFormularioReporteProgramado() {
        document.getElementById('reporteProgramadoId').value = '';
        document.getElementById('rpNombre').value = '';
        document.getElementById('rpDescripcion').value = '';
        document.getElementById('rpTipoReporte').value = '';
        document.getElementById('rpFrecuencia').value = '0';
        document.getElementById('rpDepartamento').value = '';
        document.getElementById('rpEmailDestino').value = '';
        document.getElementById('rpEmailsCC').value = '';
        document.getElementById('rpHoraEnvio').value = '08:00';
        document.getElementById('rpDiaSemana').value = '';
        document.getElementById('rpDiaMes').value = '1';
        document.getElementById('rpIncluirExcel').checked = true;
        document.getElementById('rpIncluirPDF').checked = false;
        document.getElementById('rpEnviarAlertas').checked = true;
        
        document.querySelector('#modalReporteProgramado .modal-title').innerHTML = 'Programar Reporte Automático';
    }

    function getMensajeVacio(reporteKey) {
        var mensajes = {
            'planilla-mensual': { icon: 'fa-file-invoice-dollar', titulo: 'No hay empleados en planilla', sub: 'Registra empleados con salarios asignados' },
            'bono14-aguinaldo': { icon: 'fa-gift', titulo: 'No hay datos de Bono 14/Aguinaldo', sub: 'Empleados con más de 1 año de antigüedad' },
            'costo-departamento': { icon: 'fa-building', titulo: 'No hay costos por departamento', sub: 'Registra empleados y sus salarios' },
            'contratos-vencer': { icon: 'fa-file-contract', titulo: 'No hay contratos por vencer', sub: 'Registra fecha fin de contrato en empleados con contrato temporal' },
            'sin-documentos': { icon: 'fa-folder-open', titulo: 'Todos los empleados tienen documentos', sub: 'Sube DPI, contrato, IGSS y antecedentes en módulo Documentos' },
            'nacionalidad': { icon: 'fa-passport', titulo: 'No hay datos de nacionalidad', sub: 'Registra la nacionalidad de cada empleado' },
            'vacaciones-no-tomadas': { icon: 'fa-calendar-xmark', titulo: 'No hay vacaciones pendientes', sub: 'Registra las vacaciones tomadas en módulo Vacaciones' },
            'inasistencias-tardanzas': { icon: 'fa-calendar-day', titulo: 'No hay inasistencias registradas', sub: 'El módulo de Asistencia debe registrar entradas/salidas' },
            'horas-extra': { icon: 'fa-hourglass-half', titulo: 'No hay horas extra registradas', sub: 'El módulo de Asistencia debe registrar horas extra' },
            'mas-de-tres-faltas': { icon: 'fa-triangle-exclamation', titulo: 'No hay empleados con 3+ faltas', sub: 'Registra asistencia en módulo Asistencia' },
            'altas-bajas': { icon: 'fa-chart-line', titulo: 'No hay movimiento de personal', sub: 'Registrar altas y bajas de empleados' },
            'motivos-salida': { icon: 'fa-door-open', titulo: 'No hay salidas registradas', sub: 'Registrar finiquitos cuando empleados salgan' },
            'tiempo-permanencia': { icon: 'fa-hourglass-start', titulo: 'No hay datos de permanencia', sub: 'Empleados con fecha de ingreso registrada' },
            'proyeccion-indemnizacion': { icon: 'fa-calculator', titulo: 'No hay indemnización proyectada', sub: 'Empleados con contrato indefinido' },
            'vacaciones-acumuladas': { icon: 'fa-calendar-check', titulo: 'No hay vacaciones acumuladas', sub: 'Registra vacaciones tomadas en módulo Vacaciones' },
            'finiquitos': { icon: 'fa-file-signature', titulo: 'No hay finiquitos emitidos', sub: 'Registrar finiquitos cuando empleados salgan' },
            'completitud-expedientes': { icon: 'fa-clipboard-check', titulo: 'No hay expedientes', sub: 'Registra documentos de empleados en módulo Documentos' },
            'documentos-vencidos': { icon: 'fa-calendar-xmark', titulo: 'No hay documentos vencidos', sub: 'Registra fecha de expiración en documentos' }
        };
        return mensajes[reporteKey] || { icon: 'fa-folder-open', titulo: 'No hay datos para mostrar', sub: 'Intenta ajustar los filtros' };
    }

    return {
        init: init,
        cargarReportesProgramados: cargarReportesProgramados,
        guardarReporteProgramado: guardarReporteProgramado,
        editarReporte: editarReporte,
        toggleActivo: toggleActivo,
        eliminar: eliminar
    };
})();

(function () {
    if (typeof Reportes !== 'undefined') {
        Reportes.init();
    }
    
    if (typeof ReportesProgramados !== 'undefined') {
        ReportesProgramados.init();
    }
    
    document.addEventListener('DOMContentLoaded', function () {
        if (typeof Reportes !== 'undefined') Reportes.init();
        if (typeof ReportesProgramados !== 'undefined') ReportesProgramados.init();
    });
})();
