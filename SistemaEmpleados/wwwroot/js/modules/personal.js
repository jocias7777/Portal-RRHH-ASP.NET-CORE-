/* ══════════════════════════════════════════════
   SICE — Módulo Gestión de Personal
══════════════════════════════════════════════ */

window.Personal = (() => {

    let todosEmpleados = [];
    let puestosData = [];
    let paginaActual = 1;
    const porPagina = 7;

    function init() {
        limpiarModal();
        initEvents();
        loadDepartamentosFilter();
        cargarEmpleados();
    }

    function limpiarModal() {
        document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('padding-right');
        document.body.style.removeProperty('overflow');

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && typeof bootstrap !== 'undefined') {
            const instancia = bootstrap.Modal.getInstance(modalEl);
            if (instancia) instancia.dispose();
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
        const instancia = bootstrap.Modal.getInstance(modalEl);
        if (instancia) {
            instancia.hide();
        } else {
            limpiarModal();
        }
    }

    /* ── Carga y renderizado ── */

    async function cargarEmpleados() {
        try {
            const departamentoId = document.getElementById('filtroDepartamento')?.value || '';
            const estado = document.getElementById('filtroEstado')?.value || '';

            const params = new URLSearchParams({ departamentoId, estado });
            const res = await Http.get(`/Personal/GetAll?${params}`);

            if (!res.success) { renderLista([]); return; }

            todosEmpleados = res.data || [];
            paginaActual = 1;
            actualizarContador();
            renderPagina();
        } catch {
            renderLista([]);
        }
    }

    function renderPagina() {
        const inicio = (paginaActual - 1) * porPagina;
        const slice = todosEmpleados.slice(inicio, inicio + porPagina);
        renderLista(slice);
        renderPaginacion();
    }

    function renderLista(empleados) {
        const lista = document.getElementById('listaEmpleados');
        if (!lista) return;

        if (!empleados.length) {
            lista.innerHTML = `<div class="lista-empty">
                <i class="fa-solid fa-users-slash" style="font-size:24px;margin-bottom:8px;opacity:0.4"></i>
                <p style="margin:0">No se encontraron empleados</p>
            </div>`;
            return;
        }

        lista.innerHTML = empleados.map(e => `
            <div class="lista-item" data-id="${e.id}">
                <div class="lista-item-icon">
                    ${e.fotoUrl
                ? `<img src="${e.fotoUrl}" style="width:38px;height:38px;border-radius:8px;object-fit:cover">`
                : `<i class="fa-solid fa-user"></i>`}
                </div>
                <div class="lista-item-main">
                    <div class="lista-item-title">
                        ${e.nombreCompleto}
                        <span class="codigo-badge">${e.codigo}</span>
                        <span class="estado-badge estado-${e.estado.toLowerCase()}">${e.estado}</span>
                    </div>
                    <div class="lista-item-subtitle">
                        <span><i class="fa-solid fa-building"></i> ${e.departamento}</span>
                        <span><i class="fa-solid fa-briefcase"></i> ${e.puesto}</span>
                        <span><i class="fa-solid fa-file-contract"></i>
                            <span class="contrato-badge contrato-${e.tipoContrato.toLowerCase()}">${e.tipoContrato}</span>
                        </span>
                        <span><i class="fa-solid fa-calendar"></i> ${e.fechaIngreso}</span>
                        <span style="color:#9ca3af"><i class="fa-solid fa-envelope"></i> ${e.email}</span>
                    </div>
                </div>
                <div class="lista-item-actions">
                    <button class="btn-action btn-ver" title="Ver detalle"
                        onclick="Personal.openDetalle(${e.id})">
                        <i class="fa-solid fa-eye"></i>
                    </button>
                    <button class="btn-action btn-editar" title="Editar"
                        onclick="Personal.openForm(${e.id})">
                        <i class="fa-solid fa-pen"></i>
                    </button>
                    <button class="btn-action btn-eliminar" title="Eliminar"
                        onclick="Personal.eliminar(${e.id})">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    function renderPaginacion() {
        const bar = document.getElementById('paginacionEmpleados');
        if (!bar) return;

        const totalPaginas = Math.ceil(todosEmpleados.length / porPagina);
        if (totalPaginas <= 1) { bar.innerHTML = ''; return; }

        let html = `
            <button class="page-btn" onclick="Personal._irPagina(${paginaActual - 1})"
                ${paginaActual === 1 ? 'disabled' : ''}>
                <i class="fa-solid fa-chevron-left" style="font-size:11px"></i>
            </button>`;

        for (let i = 1; i <= totalPaginas; i++) {
            if (
                i === 1 || i === totalPaginas ||
                (i >= paginaActual - 1 && i <= paginaActual + 1)
            ) {
                html += `<button class="page-btn ${i === paginaActual ? 'active' : ''}"
                    onclick="Personal._irPagina(${i})">${i}</button>`;
            } else if (i === paginaActual - 2 || i === paginaActual + 2) {
                html += `<span style="padding:0 4px;color:#9ca3af;font-size:13px">…</span>`;
            }
        }

        html += `
            <button class="page-btn" onclick="Personal._irPagina(${paginaActual + 1})"
                ${paginaActual === totalPaginas ? 'disabled' : ''}>
                <i class="fa-solid fa-chevron-right" style="font-size:11px"></i>
            </button>`;

        bar.innerHTML = html;
    }

    function _irPagina(n) {
        const totalPaginas = Math.ceil(todosEmpleados.length / porPagina);
        if (n < 1 || n > totalPaginas) return;
        paginaActual = n;
        renderPagina();
        document.getElementById('listaEmpleados')
            ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function actualizarContador() {
        const el = document.getElementById('totalEmpleados');
        if (el) el.textContent = `${todosEmpleados.length} empleados`;
    }

    /* ── Filtros y departamentos ── */

    async function loadDepartamentosFilter() {
        try {
            const res = await Http.get('/api/departamentos');
            if (!res.success) return;
            const sel = document.getElementById('filtroDepartamento');
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

    function initEvents() {
        const btnNuevo = document.getElementById('btnNuevoEmpleado');
        if (btnNuevo) {
            const clone = btnNuevo.cloneNode(true);
            btnNuevo.parentNode.replaceChild(clone, btnNuevo);
            clone.addEventListener('click', () => openForm(null));
        }

        document.getElementById('filtroDepartamento')
            ?.addEventListener('change', () => cargarEmpleados());

        document.getElementById('filtroEstado')
            ?.addEventListener('change', () => cargarEmpleados());

        document.getElementById('btnLimpiarFiltros')
            ?.addEventListener('click', () => {
                document.getElementById('filtroDepartamento').value = '';
                document.getElementById('filtroEstado').value = '';
                cargarEmpleados();
            });

        const modalEl = document.getElementById('modalEmpleado');
        if (modalEl && !modalEl.dataset.initialized) {
            modalEl.dataset.initialized = 'true';

            modalEl.addEventListener('submit', async e => {
                e.preventDefault();
                if (e.target.id === 'formEmpleado') await guardar(e.target);
            });

            modalEl.addEventListener('input', e => {
                if (e.target.id === 'FotoUrl')
                    updateAvatarPreview(e.target.value);
            });

            modalEl.addEventListener('hidden.bs.modal', () => {
                document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
                document.body.classList.remove('modal-open');
                document.body.style.removeProperty('padding-right');
                document.body.style.removeProperty('overflow');
            });
        }
    }

    /* ── Modal helpers ── */

    function getModal() {
        const modalEl = document.getElementById('modalEmpleado');
        if (!modalEl) return null;
        let instancia = bootstrap.Modal.getInstance(modalEl);
        if (!instancia) {
            instancia = new bootstrap.Modal(modalEl, { backdrop: true, keyboard: true });
        }
        return instancia;
    }

    async function openForm(id) {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border text-primary" style="width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        getModal()?.show();

        try {
            const url = id ? `/Personal/Form?id=${id}` : '/Personal/Form';
            const html = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());

            body.innerHTML = html;

            // ── Guardar todos los puestos disponibles ──
            puestosData = Array.from(
                body.querySelectorAll('#selectPuesto option[data-depto]')
            ).map(o => ({
                value: o.value,
                text: o.textContent.trim(),
                depto: o.dataset.depto
            }));

            // ── Adjuntar evento directamente al select ya en el DOM ──
            const deptSel = body.querySelector('#selectDepartamento');
            if (deptSel) {
                if (deptSel.value) filtrarPuestos(deptSel.value);
                deptSel.addEventListener('change', e => filtrarPuestos(e.target.value));
            }

        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar el formulario</p>
            </div>`;
        }
    }

    async function openDetalle(id) {
        const modalEl = document.getElementById('modalEmpleado');
        const body = document.getElementById('modalEmpleadoBody');
        if (!modalEl || !body) return;

        body.innerHTML = `<div class="modal-loading">
            <div class="spinner-border text-primary" style="width:2rem;height:2rem"></div>
            <p>Cargando...</p>
        </div>`;

        getModal()?.show();

        try {
            const html = await fetch(`/Personal/Detalle/${id}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }).then(r => r.text());
            body.innerHTML = html;
        } catch {
            body.innerHTML = `<div class="modal-loading text-danger">
                <i class="fa-solid fa-circle-exclamation fa-2x mb-2"></i>
                <p>Error al cargar el detalle</p>
            </div>`;
        }
    }

    /* ── Guardar y eliminar ── */

    async function guardar(form) {
        const btn = form.querySelector('#btnGuardarEmpleado');
        FormHelper.clearErrors(form);
        FormHelper.setLoading(btn, true);

        const data = FormHelper.serialize(form);
        const id = parseInt(data.Id) || 0;
        const url = id > 0 ? `/Personal/Edit/${id}` : '/Personal/Create';

        data.Id = id;
        data.Genero = parseInt(data.Genero) || 0;
        data.TipoContrato = parseInt(data.TipoContrato) || 0;
        data.Estado = parseInt(data.Estado) || 0;
        data.SalarioBase = parseFloat(data.SalarioBase) || 0;
        data.DepartamentoId = parseInt(data.DepartamentoId) || 0;
        data.PuestoId = parseInt(data.PuestoId) || 0;
        data.FechaSalida = data.FechaSalida || null;
        data.FechaNacimiento = data.FechaNacimiento || null;
        data.FechaIngreso = data.FechaIngreso || null;
        data.FotoUrl = data.FotoUrl || null;
        data.SegundoNombre = data.SegundoNombre || null;
        data.SegundoApellido = data.SegundoApellido || null;
        data.Telefono = data.Telefono || null;
        data.NIT = data.NIT || null;
        data.NumeroIGSS = data.NumeroIGSS || null;
        data.NumeroIRTRA = data.NumeroIRTRA || null;
        data.Observaciones = data.Observaciones || null;

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
                await cargarEmpleados();
                if (res.data?.id) highlightItem(res.data.id);
            } else {
                Notify.error(res.message || 'Error al guardar');
                if (res.errors?.length) res.errors.forEach(e => Notify.warning(e));
            }
        } catch {
            Notify.error('Error de conexión. Intenta de nuevo.');
        } finally {
            FormHelper.setLoading(btn, false);
        }
    }

    async function eliminar(id) {
        const ok = await Confirm.delete('este empleado');
        if (!ok) return;

        try {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

            const res = await fetch(`/Personal/Delete/${id}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': token,
                    'Content-Type': 'application/json'
                }
            }).then(r => r.json());

            if (res.success) {
                Notify.success(res.message);
                await cargarEmpleados();
            } else {
                Notify.error(res.message);
            }
        } catch {
            Notify.error('Error al procesar la solicitud.');
        }
    }

    /* ── Helpers ── */

    function filtrarPuestos(deptoId) {
        const sel = document.getElementById('selectPuesto');
        if (!sel) return;
        const currentVal = sel.value;
        sel.innerHTML = '<option value="">— Seleccionar puesto —</option>';
        puestosData
            .filter(p => !deptoId || p.depto == deptoId)
            .forEach(p => {
                const opt = document.createElement('option');
                opt.value = p.value;
                opt.textContent = p.text;
                if (p.value == currentVal) opt.selected = true;
                sel.appendChild(opt);
            });
    }

    function updateAvatarPreview(url) {
        const img = document.getElementById('avatarImg');
        const initials = document.getElementById('avatarInitials');
        if (url && url.startsWith('http')) {
            if (!img) {
                const preview = document.getElementById('avatarPreview');
                if (preview) preview.innerHTML =
                    `<img src="${url}" alt="Foto" id="avatarImg"
                     style="width:100%;height:100%;object-fit:cover;border-radius:50%">`;
            } else {
                img.src = url;
            }
        } else {
            if (img) img.remove();
            if (initials) initials.style.display = 'flex';
        }
    }

    function highlightItem(id) {
        setTimeout(() => {
            const item = document.querySelector(`.lista-item[data-id="${id}"]`);
            if (item) {
                item.style.transition = 'background 0.4s';
                item.style.background = '#EFF6FF';
                setTimeout(() => { item.style.background = ''; }, 1800);
            }
        }, 300);
    }

    return { init, openForm, openDetalle, eliminar, cerrarModal, _irPagina };
})();

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('listaEmpleados')) Personal.init();
});

document.addEventListener('spaNavigated', () => {
    if (document.getElementById('listaEmpleados')) Personal.init();
});