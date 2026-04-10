/* SICE - Modulo Gestion de Documentos */

var Documentos = (function() {
    var datos = [];
    var pagina = 1;
    var porPagina = 7;

    function init() {
        eventos();
        cargar();
        cargarFiltros();
    }

    function cargarFiltros() {
        Promise.all([
            fetch('/api/departamentos', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json()),
            fetch('/Personal/GetAll', {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.json())
        ])
        .then(function(results) {
            var deptos = results[0];
            var empleados = results[1];
            
            var deptoSel = document.getElementById('filtroDepartamento');
            if (deptos.success && Array.isArray(deptos.data) && deptoSel) {
                deptos.data.forEach(function(d) {
                    var opt = document.createElement('option');
                    opt.value = d.id;
                    opt.textContent = d.nombre;
                    deptoSel.appendChild(opt);
                });
            }
            
            var empSel = document.getElementById('filtroEmpleado');
            if (empleados.success && empSel) {
                empleados.data.forEach(function(e) {
                    var opt = document.createElement('option');
                    opt.value = e.id;
                    opt.textContent = e.nombreCompleto;
                    empSel.appendChild(opt);
                });
            }
            
            var expSel = document.getElementById('filtroExpediente');
            if (empleados.success && expSel) {
                empleados.data.forEach(function(e) {
                    var opt = document.createElement('option');
                    opt.value = e.id;
                    opt.textContent = e.nombreCompleto;
                    expSel.appendChild(opt);
                });
            }
        });
    }

    function cargarEmpleadosExpediente() {
        fetch('/Personal/GetAll', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                var sel = document.getElementById('filtroExpediente');
                if (sel) {
                    res.data.forEach(function(e) {
                        var opt = document.createElement('option');
                        opt.value = e.id;
                        opt.textContent = e.nombreCompleto;
                        sel.appendChild(opt);
                    });
                }
            }
        });
    }

    function cargar() {
        var depto = document.getElementById('filtroDepartamento')?.value || '';
        var emp = document.getElementById('filtroEmpleado')?.value || '';
        var tipo = document.getElementById('filtroTipo')?.value || '';
        var estado = document.getElementById('filtroEstado')?.value || '';

        var params = new URLSearchParams({ departamentoId: depto, empleadoId: emp, tipo: tipo, estado: estado });
        
        fetch('/Documentos/GetAll?' + params, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                datos = res.data || [];
                pagina = 1;
                render();
            }
        })
        .catch(() => rendervacio());
    }

function render() {
        var div = document.getElementById('listaDocumentos');
        if (!div) return;

        if (!datos.length) {
            div.innerHTML = '<div class="lista-empty"><i class="fa-solid fa-folder-open"></i><p>No hay documentos</p></div>';
            return;
        }

        var ini = (pagina - 1) * porPagina;
        var ped = datos.slice(ini, ini + porPagina);

        var grouped = {};
        ped.forEach(d => {
            var key = d.empleadoId;
            if (!grouped[key]) {
                grouped[key] = {
                    empleadoId: d.empleadoId,
                    nombre: d.empleadoNombre,
                    departamento: d.empleadoDepartamento,
                    documentos: []
                };
            }
            grouped[key].documentos.push(d);
        });

        div.innerHTML = '<div class="doc-cards">' + Object.values(grouped).map(g => {
            var tieneContrato = g.documentos.some(d => d.tipo === 'Contrato');
            var tieneCredencial = g.documentos.some(d => d.tipo === 'Credencial');
            var tieneCertificado = g.documentos.some(d => d.tipo === 'Certificado');
            var tieneAntecedentes = g.documentos.some(d => d.tipo === 'Antecedentes');
            
            return `
            <div class="doc-card" data-id="${g.empleadoId}">
                <div class="doc-card-header">
                    <div class="doc-card-avatar">
                        <i class="fa-solid fa-user"></i>
                    </div>
                    <div class="doc-card-info-header">
                        <div class="doc-card-title">${g.nombre}</div>
                        <div class="doc-card-empleado">${g.departamento}</div>
                    </div>
                </div>
                <div class="doc-card-body">
                    <div class="doc-checklist">
                        <div class="doc-check-item ${tieneContrato ? 'completo' : 'faltante'}">
                            <i class="fa-solid fa-${tieneContrato ? 'check-circle' : 'circle-xmark'}"></i>
                            <span>Contrato</span>
                        </div>
                        <div class="doc-check-item ${tieneCredencial ? 'completo' : 'faltante'}">
                            <i class="fa-solid fa-${tieneCredencial ? 'check-circle' : 'circle-xmark'}"></i>
                            <span>Credencial</span>
                        </div>
                        <div class="doc-check-item ${tieneCertificado ? 'completo' : 'faltante'}">
                            <i class="fa-solid fa-${tieneCertificado ? 'check-circle' : 'circle-xmark'}"></i>
                            <span>Certificado</span>
                        </div>
                        <div class="doc-check-item ${tieneAntecedentes ? 'completo' : 'faltante'}">
                            <i class="fa-solid fa-${tieneAntecedentes ? 'check-circle' : 'circle-xmark'}"></i>
                            <span>Antecedentes</span>
                        </div>
                    </div>
                    <div class="doc-list-mini">
                        ${g.documentos.map(d => `
                            <div class="doc-mini-item" onclick="Documentos.ver(${d.id})">
                                <i class="fa-solid fa-file"></i>
                                <span>${d.titulo}</span>
                                <span class="doc-mini-badge">${d.estado}</span>
                            </div>
                        `).join('')}
                    </div>
                </div>
                <div class="doc-card-actions">
                    <button class="btn-action" title="Agregar documento" onclick="Documentos.openForm(null, null, ${g.empleadoId})">
                        <i class="fa-solid fa-plus"></i>
                    </button>
                    <button class="btn-action btn-eliminar" title="Eliminar" onclick="Documentos.eliminar(${g.documentos[0]?.id || 0})">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
            `;
        }).join('') + '</div>';

        paginar();
    }

    function paginar() {
        var div = document.getElementById('paginacionDocumentos');
        var total = Math.ceil(datos.length / porPagina);
        if (total <= 1) { div.innerHTML = ''; return; }
        
        var html = '';
        for (var i = 1; i <= total; i++) {
            html += `<button class="page-btn ${i === pagina ? 'active' : ''}" onclick="Documentos.irPagina(${i})">${i}</button>`;
        }
        div.innerHTML = html;
    }

    function irPagina(n) {
        pagina = n;
        render();
    }

    function rendervacio() {
        var div = document.getElementById('listaDocumentos');
        if (div) div.innerHTML = '<div class="lista-empty"><p>No hay documentos</p></div>';
    }

    function eventos() {
        document.getElementById('btnNuevoDocumento')?.addEventListener('click', nuevo);
        document.getElementById('btnCargaMasiva')?.addEventListener('click', cargaMasiva);
        document.getElementById('filtroDepartamento')?.addEventListener('change', cargar);
        document.getElementById('filtroEmpleado')?.addEventListener('change', cargar);
        document.getElementById('filtroTipo')?.addEventListener('change', cargar);
        document.getElementById('filtroEstado')?.addEventListener('change', cargar);
        document.getElementById('btnLimpiarFiltros')?.addEventListener('click', function() {
            document.getElementById('filtroDepartamento').value = '';
            document.getElementById('filtroEmpleado').value = '';
            document.getElementById('filtroTipo').value = '';
            document.getElementById('filtroEstado').value = '';
            cargar();
        });

        document.getElementById('modalDocumento')?.addEventListener('submit', function(e) {
            e.preventDefault();
            guardar(document.getElementById('formDocumento'));
        });

        var drop = document.getElementById('dropZone');
        if (drop) {
            drop.addEventListener('click', function() {
                document.getElementById('fileInput')?.click();
            });
            drop.addEventListener('dragover', e => { e.preventDefault(); drop.classList.add('drag-over'); });
            drop.addEventListener('dragleave', () => drop.classList.remove('drag-over'));
            drop.addEventListener('drop', e => {
                e.preventDefault();
                drop.classList.remove('drag-over');
                var file = e.dataTransfer.files[0];
                if (file) subirArchivo(file);
            });
        }

        var inp = document.getElementById('fileInput');
        if (inp) {
            inp.addEventListener('change', e => {
                if (e.target.files[0]) subirArchivo(e.target.files[0]);
            });
        }

        var tabs = document.getElementById('documentosTabs');
        if (tabs) {
            tabs.addEventListener('shown.bs.tab', e => {
                if (e.target.getAttribute('data-bs-target') === '#tabAlertas') cargarAlertas();
            });
        }
    }

    function nuevo() {
        openForm(null);
    }

    function openFormExpediente(tipo) {
        var empId = document.getElementById('filtroExpediente')?.value;
        if (!empId) return;
        
        openForm(null, tipo, parseInt(empId));
    }

    function openForm(id, tipoPreSeleccionado, empleadoIdPreSeleccionado) {
        var body = document.getElementById('modalDocumentoBody');
        fetch('/Documentos/Form' + (id ? '?id=' + id : ''), {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.text())
        .then(html => {
            body.innerHTML = html;
            new bootstrap.Modal(document.getElementById('modalDocumento')).show();
            
            if (tipoPreSeleccionado) {
                var tipoSelect = document.getElementById('Tipo');
                if (tipoSelect) {
                    var tipoMap = {
                        'Contrato': '0', 'Rol': '1', 'Certificado': '2', 
                        'Constancia': '3', 'Credencial': '4', 'Antecedentes': '5', 'Otro': '6'
                    };
                    tipoSelect.value = tipoMap[tipoPreSeleccionado] || tipoPreSeleccionado;
                    Documentos.actualizarNotaLegal();
                }
            }

            // Pre-seleccionar empleado si se proporciono (desde expediente)
            if (empleadoIdPreSeleccionado) {
                var empSelect = document.getElementById('selectEmpleado');
                if (empSelect) {
                    empSelect.value = empleadoIdPreSeleccionado;
                }
            }
            
            // Si es edicion, seleccionar valores guardados
            if (id) {
                var currentTipo = document.getElementById('currentTipo');
                var currentEmp = document.getElementById('currentEmpleadoId');
                var currentDepto = document.getElementById('currentDepartamentoId');
                var currentEstado = document.getElementById('currentEstado');
                var currentFechaExp = document.getElementById('currentFechaExp');
                
                var tipoSel = document.getElementById('Tipo');
                var empSel = document.getElementById('selectEmpleado');
                var deptoSel = document.getElementById('selectDepartamento');
                var estadoSel = document.getElementById('Estado');
                var fechaExpInp = document.getElementById('FechaExpiracion');
                
                if (currentTipo && currentTipo.value && tipoSel) {
                    tipoSel.value = currentTipo.value;
                    Documentos.actualizarNotaLegal();
                }
                if (currentEmp && currentEmp.value && empSel) {
                    empSel.value = currentEmp.value;
                }
                if (currentDepto && currentDepto.value && deptoSel) {
                    deptoSel.value = currentDepto.value;
                }
                if (currentEstado && currentEstado.value && estadoSel) {
                    estadoSel.value = currentEstado.value;
                }
                if (currentFechaExp && currentFechaExp.value && fechaExpInp) {
                    fechaExpInp.value = currentFechaExp.value;
                }
            }
            
            // Agregar eventos despues de cargar el formulario
            var dropZone = document.getElementById('dropZone');
            if (dropZone) {
                dropZone.onclick = function() {
                    document.getElementById('fileInput').click();
                };
                // Drag and drop
                dropZone.ondragover = function(e) {
                    e.preventDefault();
                    dropZone.style.borderColor = '#0d9488';
                    dropZone.style.background = '#f0fdfa';
                };
                dropZone.ondragleave = function() {
                    dropZone.style.borderColor = '#ccc';
                    dropZone.style.background = '';
                };
                dropZone.ondrop = function(e) {
                    e.preventDefault();
                    dropZone.style.borderColor = '#ccc';
                    dropZone.style.background = '';
                    if (e.dataTransfer.files[0]) {
                        subirArchivo(e.dataTransfer.files[0]);
                    }
                };
            }
            var fileInput = document.getElementById('fileInput');
            if (fileInput) {
                fileInput.onchange = function(e) {
                    if (e.target.files[0]) {
                        subirArchivo(e.target.files[0]);
                    }
                };
            }
        });
    }

    function cargaMasiva() {
        var body = document.getElementById('modalDocumentoBody');
        body.innerHTML = `
            <div class="modal-header">
                <h5 class="modal-title">Carga Masiva de Documentos</h5>
                <button type="button" class="btn-close" onclick="Documentos.cerrarModal()"></button>
            </div>
            <div class="modal-body">
                <div class="drop-zone text-center p-5" id="dropZoneMasiva">
                    <i class="fa-solid fa-cloud-arrow-up fa-3x mb-3 text-muted"></i>
                    <p class="mb-2">Arrastra archivos aqui o haz clic para seleccionar</p>
                    <small class="text-muted">PDF, JPG, PNG (max. 10MB cada uno)</small>
                    <input type="file" id="fileInputMasiva" accept=".pdf,.jpg,.jpeg,.png" multiple hidden />
                </div>
                <div id="listaArchivos" class="mt-3"></div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" onclick="Documentos.cerrarModal()">Cancelar</button>
                <button type="button" class="btn btn-primary" id="btnSubirMasivos" disabled>Subir</button>
            </div>
        `;
        new bootstrap.Modal(document.getElementById('modalDocumento')).show();
        
        document.getElementById('dropZoneMasiva').addEventListener('click', function() {
            document.getElementById('fileInputMasiva').click();
        });
        document.getElementById('fileInputMasiva').addEventListener('change', function(e) {
            var archivos = e.target.files;
            if (archivos.length > 0) {
                var html = '<table class="table table-sm"><thead><tr><th>Archivo</th><th>Tamano</th></tr></thead><tbody>';
                for (var i = 0; i < archivos.length; i++) {
                    html += '<tr><td>' + archivos[i].name + '</td><td>' + (archivos[i].size / 1024).toFixed(1) + ' KB</td></tr>';
                }
                html += '</tbody></table>';
                document.getElementById('listaArchivos').innerHTML = html;
                document.getElementById('btnSubirMasivos').disabled = false;
            }
        });
    }

    function editar(id) {
        openForm(id);
    }

    function ver(id) {
        var body = document.getElementById('modalDocumentoBody');
        fetch('/Documentos/Detalle/' + id, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.text())
        .then(html => {
            body.innerHTML = html;
            new bootstrap.Modal(document.getElementById('modalDocumento')).show();
        });
    }

    function eliminar(id) {
        fetch('/Documentos/GetAll', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.json())
        .then(res => {
            var doc = res.data.find(d => d.id === id);
            var nombre = doc ? doc.titulo : 'este documento';
            Confirm.delete(nombre).then(function(ok) {
                if (!ok) return;
                var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                fetch('/Documentos/Delete/' + id, {
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token }
                })
                .then(r => r.json())
                .then(res => {
                    if (res.success) {
                        Notify.success(res.message);
                        cargar();
                    } else {
                        Notify.error(res.message);
                    }
                });
            });
        });
    }

    function guardar(form) {
        var btn = document.getElementById('btnGuardarDocumento');
        btn.disabled = true;
        btn.innerHTML = 'Guardando...';

        var fd = new FormData(form);
        var esEdicion = fd.get('Id') > 0;
        var url = esEdicion ? '/Documentos/Edit/' + fd.get('Id') : '/Documentos/Create';

        fetch(url, {
            method: 'POST',
            body: fd
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                bootstrap.Modal.getInstance(document.getElementById('modalDocumento')).hide();
                cargar();
                Notify.success(res.message);
            } else {
                Notify.error(res.message);
            }
        })
        .finally(() => {
            btn.disabled = false;
            btn.innerHTML = 'Guardar';
        });
    }

    function subirArchivo(file) {
        if (file.size > 10 * 1024 * 1024) {
            alert('Max 10MB');
            return;
        }

        var dropZone = document.getElementById('dropZone');
        var p = document.getElementById('progressUpload');
        var pb = p.querySelector('.progress-bar');
        
        dropZone.style.display = 'none';
        p.style.display = 'block';
        pb.style.width = '0%';

        var prog = 0;
        var iv = setInterval(() => {
            prog += 5;
            pb.style.width = prog + '%';
            if (document.getElementById('progressPercent')) {
                document.getElementById('progressPercent').innerText = prog + '%';
            }
            if (prog >= 100) {
                clearInterval(iv);
                setTimeout(function() {
                    p.style.display = 'none';
                    
                    var preview = document.getElementById('previewArchivo');
                    var nombre = document.getElementById('previewNombre');
                    var tamano = document.getElementById('previewTamano');
                    var icono = document.getElementById('previewIcono');
                    
                    if (nombre) nombre.innerText = file.name;
                    if (tamano) tamano.innerText = (file.size / 1024).toFixed(1) + ' KB';
                    
                    if (file.type.includes('pdf') && icono) {
                        icono.className = 'fa-solid fa-file-pdf fa-2x me-3 text-danger';
                    } else if (file.type.includes('image') && icono) {
                        icono.className = 'fa-solid fa-file-image fa-2x me-3 text-success';
                    } else if (icono) {
                        icono.className = 'fa-solid fa-file fa-2x me-3 text-muted';
                    }
                    
                    if (preview) preview.style.display = 'block';
                    
                    document.getElementById('hiddenNombreArchivo').value = file.name;
                    document.getElementById('hiddenContentType').value = file.type;
                    document.getElementById('hiddenTamanioBytes').value = file.size;
                    document.getElementById('UrlArchivo').value = file.name;
                }, 300);
            }
        }, 50);
    }

    function quitarArchivo() {
        // Limpiar campos hidden
        document.getElementById('hiddenNombreArchivo').value = '';
        document.getElementById('hiddenContentType').value = '';
        document.getElementById('hiddenTamanioBytes').value = '';
        document.getElementById('UrlArchivo').value = '';
        
        // Ocultar preview y mostrar drop zone
        document.getElementById('previewArchivo').style.display = 'none';
        document.getElementById('dropZone').style.display = 'block';
        
        // Limpiar input file
        var fileInput = document.getElementById('fileInput');
        if (fileInput) fileInput.value = '';
    }

    function cargarAlertas() {
        fetch('/Documentos/Alertas', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.text())
        .then(html => {
            document.getElementById('listaAlertas').innerHTML = html;
        });
    }

    function cerrarModal() {
        bootstrap.Modal.getInstance(document.getElementById('modalDocumento')).hide();
    }

    function actualizarNotaLegal() {
        var tipo = document.getElementById('Tipo')?.value;
        var txt = document.getElementById('notaLegalTexto');
        if (txt && window.notasLegales) {
            txt.innerText = window.notasLegales[tipo] || '';
        }
        var mod = document.getElementById('divModalidad');
        if (mod) mod.style.display = tipo === '0' ? 'block' : 'none';
    }

function verExpediente() {
        var emp = document.getElementById('filtroExpediente')?.value;
        if (!emp) return;
        verExpedienteByEmpleado(parseInt(emp));
    }

    function verExpedienteByEmpleado(empleadoId) {
        var body = document.getElementById('modalDocumentoBody');
        fetch('/Documentos/Expediente?id=' + empleadoId, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(r => r.text())
        .then(html => {
            body.innerHTML = html;
            new bootstrap.Modal(document.getElementById('modalDocumento')).show();
        });
    }

    return {
        init: init,
        nuevo: nuevo,
        openFormExpediente: openFormExpediente,
        openForm: openForm,
        editar: editar,
        ver: ver,
        eliminar: eliminar,
        guardar: guardar,
        cerrarModal: cerrarModal,
        actualizarNotaLegal: actualizarNotaLegal,
        quitarArchivo: quitarArchivo,
        cargarAlertas: cargarAlertas,
        verExpediente: verExpediente,
        irPagina: irPagina,
        cargar: cargar
    };
})();

document.addEventListener('DOMContentLoaded', function() {
    if (document.getElementById('listaDocumentos')) Documentos.init();
});

document.addEventListener('spaNavigated', function() {
    if (document.getElementById('listaDocumentos')) Documentos.init();
});