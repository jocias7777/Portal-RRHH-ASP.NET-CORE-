/* ══════════════════════════════════════════════
   SICE — Router SPA + funciones globales
══════════════════════════════════════════════ */

toastr.options = {
    closeButton: true,
    progressBar: true,
    positionClass: 'toast-top-right',
    timeOut: 3000,
    extendedTimeOut: 1000,
    preventDuplicates: true,
    newestOnTop: true
};

const AppRouter = {
    loadedCSS: new Set(),
    loadedJS: new Set(),
    progressBar: null,
    currentUrl: null,

    init() {
        this.progressBar = document.getElementById('routerProgressBar');

        document.querySelectorAll('head link[rel="stylesheet"]').forEach(link => {
            if (link.href) {
                try {
                    const path = new URL(link.href).pathname;
                    this.loadedCSS.add(path);
                } catch (_) { }
            }
        });

        const currentModule = window.location.pathname.split('/').filter(Boolean)[0]?.toLowerCase();
        if (currentModule && currentModule !== 'home') {
            const cssPath = `/css/views/${currentModule}.css`;
            if (!document.querySelector(`link[href*="/css/views/${currentModule}.css"]`)) {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = cssPath;
                link.onerror = () => { };
                document.head.appendChild(link);
            }
            this.loadedCSS.add(cssPath);
        }

        document.addEventListener('click', e => {
            const link = e.target.closest('[data-route]');
            if (link && link.tagName === 'A' && link.href) {
                e.preventDefault();
                this.navigate(link.href);
            }
        });

        window.addEventListener('popstate', e => {
            if (e.state?.url) this.navigate(e.state.url, false);
        });

        this.updateActiveLink(window.location.pathname);
        this.actualizarNavbar(window.location.pathname);

        history.replaceState({ url: window.location.href }, '', window.location.href);
    },

    actualizarNavbar(pathname) {
        const nav = document.getElementById('navModulos');
        if (!nav) return;
        const esDashboard = pathname === '/' ||
            pathname.toLowerCase() === '/home/index' ||
            pathname.toLowerCase() === '/home';
        nav.style.display = esDashboard ? 'none' : 'flex';
    },

    async navigate(url, pushState = true) {
        if (url === this.currentUrl) return;
        this.currentUrl = url;
        this.showProgress();

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.status === 404) {
                this.doneProgress();
                this.currentUrl = null;
                Swal.fire({
                    icon: 'info',
                    title: 'Módulo no disponible',
                    html: '<p style="font-size:14px">Este módulo estará disponible próximamente.</p>',
                    confirmButtonText: 'Entendido',
                    confirmButtonColor: '#2563EB'
                });
                return;
            }

            if (!response.ok) throw new Error(`Error ${response.status}`);

            const html = await response.text();
            const mainContent = document.getElementById('main-content');

            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const newContent = doc.getElementById('main-content')?.innerHTML ?? html;

            mainContent.innerHTML = newContent;

            if (pushState) history.pushState({ url }, '', url);

            const pathname = new URL(url, location.origin).pathname;

            this.loadModuleAssets(url);
            this.updateActiveLink(pathname);
            this.reinitPageScripts();
            this.actualizarNavbar(pathname);

            document.dispatchEvent(new CustomEvent('spaNavigated', { detail: { url } }));

            this.doneProgress();
        } catch (err) {
            console.error('Router error:', err);
            this.doneProgress();
            this.currentUrl = null;
            Notify.error('Error al cargar el módulo.');
        }
    },

    reinitModules() {
        setTimeout(() => {
            if (typeof Personal !== 'undefined' &&
                document.getElementById('tablaEmpleados')) {
                Personal.init();
            }

            if (typeof Asistencia !== 'undefined' &&
                document.getElementById('tablaAsistencia')) {
                Asistencia.init();
            }

            if (typeof Vacaciones !== 'undefined' &&
                document.getElementById('tablaVacaciones')) {
                Vacaciones.init();
            }

            if (typeof Prestaciones !== 'undefined' &&
                document.getElementById('tablaPrestaciones')) {
                Prestaciones.init();
            }

            if (typeof Nomina !== 'undefined' &&
                document.getElementById('tablaNomina')) {
                Nomina.init();
            }

            if (typeof Reclutamiento !== 'undefined' &&
                (document.getElementById('tablaPlazas') ||
                    document.getElementById('tablaCandidatos'))) {
                Reclutamiento.init();
            }

            // ✅ FIX: ahora detecta cardsDeptos en vez de tablaDeptos
            if (typeof Configuracion !== 'undefined' &&
                document.getElementById('cardsDeptos')) {
                Configuracion.init();
            }

            if (typeof Evaluacion !== 'undefined' &&
                document.getElementById('tablaEvaluacion')) {
                Evaluacion.init();
            }

            const dashSearch = document.getElementById('dashboardSearch');
            if (dashSearch) {
                const clone = dashSearch.cloneNode(true);
                dashSearch.parentNode.replaceChild(clone, dashSearch);
                clone.addEventListener('input', e => {
                    const q = e.target.value.trim().toLowerCase();
                    document.querySelectorAll('#modulesGrid .module-card').forEach(card => {
                        const nombre = card.dataset.nombre || '';
                        card.style.display = (!q || nombre.includes(q)) ? '' : 'none';
                    });
                });
            }
        }, 100);
    },

    loadModuleAssets(url) {
        try {
            const path = new URL(url, location.origin).pathname;
            const parts = path.split('/').filter(Boolean);
            const module = parts[0]?.toLowerCase();

            if (!module || module === 'home') return;

            const cssPath = `/css/views/${module}.css`;
            const cssEnDOM = document.querySelector(`link[href*="/css/views/${module}.css"]`);

            if (cssEnDOM) {
                cssEnDOM.href = cssPath + '?v=' + Date.now();
            } else {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = cssPath + '?v=' + Date.now();
                link.onerror = () => { };
                document.head.appendChild(link);
            }
            this.loadedCSS.add(cssPath);

            const jsPath = `/js/modules/${module}.js`;
            if (!this.loadedJS.has(jsPath)) {
                const script = document.createElement('script');
                script.src = jsPath;
                script.onerror = () => { };
                document.body.appendChild(script);
                this.loadedJS.add(jsPath);
            } else {
                this.reinitModules();
            }
        } catch (e) { }
    },

    updateActiveLink(pathname) {
        document.querySelectorAll('.nav-module-link').forEach(link => {
            link.classList.remove('active');
            try {
                if (link.href && new URL(link.href).pathname === pathname) {
                    link.classList.add('active');
                }
            } catch (_) { }
        });
    },

    reinitPageScripts() {
        document.getElementById('main-content')
            ?.querySelectorAll('script')
            .forEach(oldScript => {
                const newScript = document.createElement('script');
                newScript.textContent = oldScript.textContent;
                oldScript.parentNode.replaceChild(newScript, oldScript);
            });
    },

    showProgress() {
        if (!this.progressBar) return;
        this.progressBar.className = 'router-progress-bar loading';
    },

    doneProgress() {
        if (!this.progressBar) return;
        this.progressBar.className = 'router-progress-bar done';
        setTimeout(() => {
            this.progressBar.className = 'router-progress-bar';
        }, 400);
    },

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
};

const GlobalSearch = {
    input: null,
    dropdown: null,
    debounceTimer: null,

    init() {
        this.input = document.getElementById('globalSearchInput');
        this.dropdown = document.getElementById('searchResultsDropdown');
        if (!this.input) return;

        document.addEventListener('keydown', e => {
            if ((e.ctrlKey && e.key === 'k') ||
                (e.key === '/' && document.activeElement === document.body)) {
                e.preventDefault();
                this.input.focus();
                this.input.select();
            }
            if (e.key === 'Escape') {
                this.hide();
                this.input.blur();
            }
        });

        this.input.addEventListener('input', () => {
            clearTimeout(this.debounceTimer);
            const q = this.input.value.trim();
            if (q.length < 2) { this.hide(); return; }
            this.debounceTimer = setTimeout(() => this.search(q), 300);
        });

        this.input.addEventListener('focus', () => {
            if (this.input.value.trim().length >= 2) this.show();
        });

        document.addEventListener('click', e => {
            if (!e.target.closest('#globalSearchWrapper')) this.hide();
        });
    },

    async search(q) {
        try {
            const data = await Http.get(`/api/search?q=${encodeURIComponent(q)}`);
            this.render(data);
        } catch {
            this.hide();
        }
    },

    render(data) {
        if (!data || !data.groups || data.groups.length === 0) {
            this.dropdown.innerHTML =
                '<div style="padding:16px;text-align:center;color:#9CA3AF;font-size:13px">Sin resultados</div>';
            this.show();
            return;
        }
        let html = '';
        data.groups.forEach(group => {
            html += `<div class="search-group-title">${group.modulo} (${group.items.length})</div>`;
            group.items.forEach(item => {
                html += `<div class="search-result-item"
                    onclick="AppRouter.navigate('${item.url}')">
                    <i class="fa-solid ${item.icono}"
                       style="color:${item.color};font-size:14px"></i>
                    <span>${item.texto}</span>
                </div>`;
            });
        });
        this.dropdown.innerHTML = html;
        this.show();
    },

    show() { this.dropdown.classList.add('visible'); },
    hide() { this.dropdown.classList.remove('visible'); }
};

document.addEventListener('DOMContentLoaded', () => {
    AppRouter.init();
    GlobalSearch.init();
});