using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services.Implementations;

public class ReclutamientoService : IReclutamientoService
{
    private readonly ApplicationDbContext _db;

    public ReclutamientoService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ════════════════════════════════════════════
    // PLAZAS — DataTable
    // ════════════════════════════════════════════
    public async Task<DataTablesResponse<PlazaVacanteListViewModel>>
        GetPlazasDataTablesAsync(DataTablesRequest req)
    {
        var query = _db.PlazasVacantes
            .Include(p => p.Departamento)
            .Include(p => p.Puesto)
            .Include(p => p.Candidatos)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(p =>
                p.Titulo.ToLower().Contains(s) ||
                p.Departamento.Nombre.ToLower().Contains(s));
        }

        if (req.DepartamentoId.HasValue)
            query = query.Where(p =>
                p.DepartamentoId == req.DepartamentoId);

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EstadoPlaza>(req.Estado, out var estado))
            query = query.Where(p => p.Estado == estado);

        int total = await query.CountAsync();

        var data = await query
            .OrderByDescending(p => p.FechaPublicacion)
            .Skip(req.Start)
            .Take(req.Length)
            .Select(p => new PlazaVacanteListViewModel
            {
                Id = p.Id,
                Titulo = p.Titulo,
                Departamento = p.Departamento.Nombre,
                Puesto = p.Puesto != null
                    ? p.Puesto.Nombre : null,
                SalarioOfrecido = p.SalarioOfrecido,
                CantidadVacantes = p.CantidadVacantes,
                TotalCandidatos = p.Candidatos
                    .Count(c => !c.IsDeleted),
                CandidatosActivos = p.Candidatos
                    .Count(c => !c.IsDeleted
                        && c.Etapa != EtapaCandidato.Rechazado
                        && c.Etapa != EtapaCandidato.Contratado),
                Estado = p.Estado.ToString(),
                FechaPublicacion =
                    p.FechaPublicacion.ToString("dd/MM/yyyy"),
                FechaCierre = p.FechaCierre.HasValue
                    ? p.FechaCierre.Value.ToString("dd/MM/yyyy")
                    : null,
                EsReemplazo = p.EsReemplazo,
                FuenteReclutamiento = p.FuenteReclutamiento,
                DiasAbierta = (DateTime.Today
                    - p.FechaPublicacion).Days
            })
            .ToListAsync();

        return new DataTablesResponse<PlazaVacanteListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    // ════════════════════════════════════════════
    // PLAZA — Get por ID (para editar)
    // ════════════════════════════════════════════
    public async Task<PlazaVacanteViewModel?> GetPlazaByIdAsync(int id)
    {
        var p = await _db.PlazasVacantes
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p == null) return null;

        return new PlazaVacanteViewModel
        {
            Id = p.Id,
            Titulo = p.Titulo,
            Descripcion = p.Descripcion,
            RequisitoMinimos = p.RequisitoMinimos,
            SalarioOfrecido = p.SalarioOfrecido,
            FechaPublicacion = p.FechaPublicacion,
            FechaCierre = p.FechaCierre,
            CantidadVacantes = p.CantidadVacantes,
            Estado = p.Estado,
            DepartamentoId = p.DepartamentoId,
            PuestoId = p.PuestoId,
            EsReemplazo = p.EsReemplazo,
            MotivoApertura = p.MotivoApertura,
            FuenteReclutamiento = p.FuenteReclutamiento
        };
    }

    // ════════════════════════════════════════════
    // PLAZA — Detalle completo con pipeline
    // ════════════════════════════════════════════
    public async Task<PlazaDetalleViewModel?> GetPlazaDetalleAsync(int id)
    {
        var p = await _db.PlazasVacantes
            .Include(x => x.Departamento)
            .Include(x => x.Puesto)
            .Include(x => x.Candidatos)
            .Include(x => x.Historial)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (p == null) return null;

        var candidatos = p.Candidatos
            .Where(c => !c.IsDeleted).ToList();

        return new PlazaDetalleViewModel
        {
            Id = p.Id,
            Titulo = p.Titulo,
            Departamento = p.Departamento.Nombre,
            Puesto = p.Puesto?.Nombre,
            Descripcion = p.Descripcion,
            RequisitoMinimos = p.RequisitoMinimos,
            SalarioOfrecido = p.SalarioOfrecido,
            Estado = p.Estado.ToString(),
            FechaPublicacion =
                p.FechaPublicacion.ToString("dd/MM/yyyy"),
            FechaCierre =
                p.FechaCierre?.ToString("dd/MM/yyyy"),
            CantidadVacantes = p.CantidadVacantes,
            EsReemplazo = p.EsReemplazo,
            MotivoApertura = p.MotivoApertura,
            FuenteReclutamiento = p.FuenteReclutamiento,
            DiasAbierta =
                (DateTime.Today - p.FechaPublicacion).Days,

            TotalCandidatos = candidatos.Count,
            Recibidos = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Recibido),
            EnEntrevista = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Entrevista),
            EnPruebas = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Pruebas),
            EnOferta = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Oferta),
            Contratados = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Contratado),
            Rechazados = candidatos.Count(
                c => c.Etapa == EtapaCandidato.Rechazado),

            Candidatos = candidatos.Select(c =>
                new CandidatoListViewModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Email = c.Email,
                    Telefono = c.Telefono,
                    Plaza = p.Titulo,
                    PlazaId = p.Id,
                    Departamento = p.Departamento.Nombre,
                    Etapa = c.Etapa.ToString(),
                    FechaPostulacion =
                        c.FechaPostulacion.ToString("dd/MM/yyyy"),
                    FechaEntrevista =
                        c.FechaEntrevista?.ToString("dd/MM/yyyy"),
                    CvUrl = c.CvUrl,
                    FuentePostulacion = c.FuentePostulacion,
                    CalificacionGeneral = c.CalificacionGeneral,
                    FueContratado = c.EmpleadoId.HasValue
                }).ToList(),

            Historial = p.Historial
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.FechaCambio)
                .Select(h => new HistorialEstadoViewModel
                {
                    EstadoAnterior = h.EstadoAnterior.ToString(),
                    EstadoNuevo = h.EstadoNuevo.ToString(),
                    Motivo = h.Motivo,
                    CambiadoPor = h.CambiadoPor,
                    Fecha =
                        h.FechaCambio.ToString("dd/MM/yyyy HH:mm")
                }).ToList()
        };
    }

    // ════════════════════════════════════════════
    // PLAZA — Crear
    // ════════════════════════════════════════════
    public async Task<(bool success, string message, int id)>
        CreatePlazaAsync(PlazaVacanteViewModel vm)
    {
        var plaza = new PlazaVacante
        {
            Titulo = vm.Titulo.Trim(),
            Descripcion = vm.Descripcion,
            RequisitoMinimos = vm.RequisitoMinimos,
            SalarioOfrecido = vm.SalarioOfrecido,
            FechaPublicacion = vm.FechaPublicacion,
            FechaCierre = vm.FechaCierre,
            CantidadVacantes = vm.CantidadVacantes,
            Estado = vm.Estado,
            DepartamentoId = vm.DepartamentoId,
            PuestoId = vm.PuestoId,
            EsReemplazo = vm.EsReemplazo,
            MotivoApertura = vm.MotivoApertura,
            FuenteReclutamiento = vm.FuenteReclutamiento
        };

        _db.PlazasVacantes.Add(plaza);
        await _db.SaveChangesAsync();

        return (true, "Plaza vacante creada correctamente.", plaza.Id);
    }

    // ════════════════════════════════════════════
    // PLAZA — Actualizar
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        UpdatePlazaAsync(int id, PlazaVacanteViewModel vm)
    {
        var plaza = await _db.PlazasVacantes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (plaza == null)
            return (false, "Plaza no encontrada.");

        plaza.Titulo = vm.Titulo.Trim();
        plaza.Descripcion = vm.Descripcion;
        plaza.RequisitoMinimos = vm.RequisitoMinimos;
        plaza.SalarioOfrecido = vm.SalarioOfrecido;
        plaza.FechaPublicacion = vm.FechaPublicacion;
        plaza.FechaCierre = vm.FechaCierre;
        plaza.CantidadVacantes = vm.CantidadVacantes;
        plaza.Estado = vm.Estado;
        plaza.DepartamentoId = vm.DepartamentoId;
        plaza.PuestoId = vm.PuestoId;
        plaza.EsReemplazo = vm.EsReemplazo;
        plaza.MotivoApertura = vm.MotivoApertura;
        plaza.FuenteReclutamiento = vm.FuenteReclutamiento;
        plaza.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Plaza actualizada correctamente.");
    }

    // ════════════════════════════════════════════
    // PLAZA — Eliminar (soft delete)
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        DeletePlazaAsync(int id)
    {
        var plaza = await _db.PlazasVacantes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (plaza == null)
            return (false, "Plaza no encontrada.");

        if (plaza.Estado == EstadoPlaza.EnProceso)
            return (false,
                "No se puede eliminar una plaza en proceso. " +
                "Primero cámbiala a Cancelada.");

        plaza.IsDeleted = true;
        plaza.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Plaza eliminada correctamente.");
    }

    // ════════════════════════════════════════════
    // PLAZA — Cambiar estado con historial
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        CambiarEstadoPlazaAsync(int id,
            CambiarEstadoPlazaViewModel vm, string usuario)
    {
        var plaza = await _db.PlazasVacantes
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (plaza == null)
            return (false, "Plaza no encontrada.");

        if (!Enum.TryParse<EstadoPlaza>(vm.Estado, out var nuevoEstado))
            return (false, "Estado inválido.");

        var estadoAnterior = plaza.Estado;
        plaza.Estado = nuevoEstado;
        plaza.UpdatedAt = DateTime.Now;

        // Si se cierra, registrar fecha de cierre
        if (nuevoEstado == EstadoPlaza.Cerrada
         || nuevoEstado == EstadoPlaza.Cancelada)
            plaza.FechaCierre = DateTime.Today;

        // Guardar historial
        _db.HistorialesEstadoPlaza.Add(new HistorialEstadoPlaza
        {
            PlazaVacanteId = id,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = nuevoEstado,
            Motivo = vm.Motivo,
            CambiadoPor = usuario,
            FechaCambio = DateTime.Now
        });

        await _db.SaveChangesAsync();
        return (true,
            $"Plaza cambiada a {nuevoEstado} correctamente.");
    }

    // ════════════════════════════════════════════
    // CANDIDATOS — DataTable
    // ════════════════════════════════════════════
    public async Task<DataTablesResponse<CandidatoListViewModel>>
        GetCandidatosDataTablesAsync(DataTablesRequest req)
    {
        var baseQuery = _db.Candidatos
            .Include(c => c.PlazaVacante)
                .ThenInclude(p => p.Departamento)
            .Include(c => c.Entrevistas)
            .Include(c => c.Notas)
            .Where(c => !c.IsDeleted && !c.PlazaVacante.IsDeleted)
            .AsQueryable();

        var total = await baseQuery.CountAsync();
        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(c =>
                c.Nombre.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.PlazaVacante.Titulo.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(req.Estado) &&
            Enum.TryParse<EtapaCandidato>(req.Estado, out var etapa))
            query = query.Where(c => c.Etapa == etapa);

        if (req.DepartamentoId.HasValue)
            query = query.Where(c =>
                c.PlazaVacante.DepartamentoId == req.DepartamentoId);

        var filtered = await query.CountAsync();
        var start = req.Start >= filtered ? 0 : req.Start;

        var data = await query
            .OrderByDescending(c => c.FechaPostulacion)
            .Skip(start)
            .Take(req.Length)
            .Select(c => new CandidatoListViewModel
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Email = c.Email,
                Telefono = c.Telefono,
                Plaza = c.PlazaVacante.Titulo,
                PlazaId = c.PlazaVacanteId,
                Departamento =
                    c.PlazaVacante.Departamento.Nombre,
                Etapa = c.Etapa.ToString(),
                FechaPostulacion =
                    c.FechaPostulacion.ToString("dd/MM/yyyy"),
                FechaEntrevista = c.FechaEntrevista.HasValue
                    ? c.FechaEntrevista.Value
                        .ToString("dd/MM/yyyy") : null,
                CvUrl = c.CvUrl,
                FuentePostulacion = c.FuentePostulacion,
                CalificacionGeneral = c.CalificacionGeneral,
                TotalEntrevistas = c.Entrevistas
                    .Count(e => !e.IsDeleted),
                TotalNotas = c.Notas
                    .Count(n => !n.IsDeleted),
                FueContratado = c.EmpleadoId.HasValue
            })
            .ToListAsync();

        return new DataTablesResponse<CandidatoListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = filtered,
            Data = data
        };
    }

    // ════════════════════════════════════════════
    // CANDIDATO — Get por ID
    // ════════════════════════════════════════════
    public async Task<CandidatoViewModel?> GetCandidatoByIdAsync(int id)
    {
        var c = await _db.Candidatos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c == null) return null;

        return new CandidatoViewModel
        {
            Id = c.Id,
            PlazaVacanteId = c.PlazaVacanteId,
            Nombre = c.Nombre,
            Email = c.Email,
            Telefono = c.Telefono,
            CvUrl = c.CvUrl,
            Etapa = c.Etapa,
            Observacion = c.Observacion,
            FechaEntrevista = c.FechaEntrevista,
            FuentePostulacion = c.FuentePostulacion,
            NombreReferido = c.NombreReferido
        };
    }

    // ════════════════════════════════════════════
    // CANDIDATO — Crear
    // ════════════════════════════════════════════
    public async Task<(bool success, string message, int id)>
        CreateCandidatoAsync(CandidatoViewModel vm)
    {
        // Validar que la plaza exista y esté abierta
        var plaza = await _db.PlazasVacantes
            .FirstOrDefaultAsync(p => p.Id == vm.PlazaVacanteId
                                   && !p.IsDeleted);
        if (plaza == null)
            return (false, "Plaza vacante no encontrada.", 0);

        if (plaza.Estado == EstadoPlaza.Cerrada
         || plaza.Estado == EstadoPlaza.Cancelada)
            return (false,
                "No se pueden agregar candidatos a una plaza " +
                "cerrada o cancelada.", 0);

        // Verificar que no exista el mismo email en la misma plaza
        var existe = await _db.Candidatos
            .AnyAsync(c => c.PlazaVacanteId == vm.PlazaVacanteId
                        && c.Email.ToLower() == vm.Email.ToLower()
                        && !c.IsDeleted);
        if (existe)
            return (false,
                "Ya existe un candidato con ese email " +
                "en esta plaza.", 0);

        var candidato = new Candidato
        {
            PlazaVacanteId = vm.PlazaVacanteId,
            Nombre = vm.Nombre.Trim(),
            Email = vm.Email.Trim().ToLower(),
            Telefono = vm.Telefono?.Trim(),
            CvUrl = vm.CvUrl,
            Etapa = EtapaCandidato.Recibido,
            Observacion = vm.Observacion,
            FechaPostulacion = DateTime.Today,
            FechaEntrevista = vm.FechaEntrevista,
            FuentePostulacion = vm.FuentePostulacion,
            NombreReferido = vm.NombreReferido
        };

        _db.Candidatos.Add(candidato);

        // Si era Abierta, pasar a EnProceso
        if (plaza.Estado == EstadoPlaza.Abierta)
        {
            plaza.Estado = EstadoPlaza.EnProceso;
            plaza.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        return (true, "Candidato registrado correctamente.",
            candidato.Id);
    }

    // ════════════════════════════════════════════
    // CANDIDATO — Actualizar
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        UpdateCandidatoAsync(int id, CandidatoViewModel vm)
    {
        var c = await _db.Candidatos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c == null)
            return (false, "Candidato no encontrado.");

        c.PlazaVacanteId = vm.PlazaVacanteId;
        c.Nombre = vm.Nombre.Trim();
        c.Email = vm.Email.Trim().ToLower();
        c.Telefono = vm.Telefono?.Trim();
        c.CvUrl = vm.CvUrl;
        c.Etapa = vm.Etapa;
        c.Observacion = vm.Observacion;
        c.FechaEntrevista = vm.FechaEntrevista;
        c.FuentePostulacion = vm.FuentePostulacion;
        c.NombreReferido = vm.NombreReferido;
        c.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Candidato actualizado correctamente.");
    }

    // ════════════════════════════════════════════
    // CANDIDATO — Eliminar
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        DeleteCandidatoAsync(int id)
    {
        var c = await _db.Candidatos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c == null)
            return (false, "Candidato no encontrado.");

        if (c.EmpleadoId.HasValue)
            return (false,
                "No se puede eliminar un candidato que " +
                "ya fue contratado como empleado.");

        c.IsDeleted = true;
        c.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Candidato eliminado correctamente.");
    }

    // ════════════════════════════════════════════
    // CANDIDATO — Cambiar etapa
    // ════════════════════════════════════════════
    public async Task<(bool success, string message)>
        CambiarEtapaAsync(int id, int etapa, string usuario)
    {
        var c = await _db.Candidatos
            .Include(x => x.PlazaVacante)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (c == null)
            return (false, "Candidato no encontrado.");

        var nuevaEtapa = (EtapaCandidato)etapa;

        // Si llega a Contratado, verificar que la plaza
        // tenga vacantes disponibles
        if (nuevaEtapa == EtapaCandidato.Contratado)
        {
            var contratados = await _db.Candidatos
                .CountAsync(x => x.PlazaVacanteId == c.PlazaVacanteId
                              && x.Etapa == EtapaCandidato.Contratado
                              && !x.IsDeleted
                              && x.Id != id);

            if (contratados >= c.PlazaVacante.CantidadVacantes)
                return (false,
                    $"Ya se cubrieron todas las vacantes " +
                    $"({c.PlazaVacante.CantidadVacantes}) " +
                    $"de esta plaza.");
        }

        c.Etapa = nuevaEtapa;
        c.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true,
            $"Candidato movido a etapa: {nuevaEtapa}.");
    }

    public async Task<(bool success, string message)>
        RegistrarOfertaAsync(OfertaCandidatoViewModel vm, string usuario)
    {
        var c = await _db.Candidatos
            .Include(x => x.PlazaVacante)
            .FirstOrDefaultAsync(x => x.Id == vm.CandidatoId && !x.IsDeleted);

        if (c == null)
            return (false, "Candidato no encontrado.");

        if (vm.SalarioOferta <= 0)
            return (false, "El salario de oferta debe ser mayor a 0.");

        c.Etapa = EtapaCandidato.Oferta;
        c.FechaEntrevista = vm.FechaIngresoPropuesta;
        c.Observacion =
            $"OFERTA | Salario: Q {vm.SalarioOferta:N2} | Contrato: {vm.TipoContrato} | Fecha propuesta: {vm.FechaIngresoPropuesta:dd/MM/yyyy}" +
            (string.IsNullOrWhiteSpace(vm.Observaciones)
                ? string.Empty
                : $" | Nota: {vm.Observaciones.Trim()}");
        c.UpdatedAt = DateTime.Now;

        // Mantener plaza en proceso cuando ya se emite oferta
        if (c.PlazaVacante.Estado == EstadoPlaza.Abierta)
        {
            c.PlazaVacante.Estado = EstadoPlaza.EnProceso;
            c.PlazaVacante.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();
        return (true, "Oferta registrada y etapa actualizada correctamente.");
    }


    // ════════════════════════════════════════════
    // ENTREVISTAS
    // ════════════════════════════════════════════
    public async Task<IEnumerable<EntrevistaViewModel>>
        GetEntrevistasCandidatoAsync(int candidatoId)
    {
        return await _db.Entrevistas
            .Where(e => e.CandidatoId == candidatoId && !e.IsDeleted)
            .Include(e => e.Candidato)
            .OrderByDescending(e => e.FechaHora)
            .Select(e => new EntrevistaViewModel
            {
                Id = e.Id,
                CandidatoId = e.CandidatoId,
                NombreCandidato = e.Candidato.Nombre,
                FechaHora = e.FechaHora,
                Entrevistador = e.Entrevistador,
                Lugar = e.Lugar,
                Resultado = e.Resultado.ToString(),
                Calificacion = e.Calificacion,
                Observaciones = e.Observaciones
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)>
        CrearEntrevistaAsync(EntrevistaViewModel vm)
    {
        var candidato = await _db.Candidatos
            .FirstOrDefaultAsync(c => c.Id == vm.CandidatoId
                                   && !c.IsDeleted);
        if (candidato == null)
            return (false, "Candidato no encontrado.");

        Enum.TryParse<ResultadoEntrevista>(
            vm.Resultado, out var resultado);

        _db.Entrevistas.Add(new Entrevista
        {
            CandidatoId = vm.CandidatoId,
            FechaHora = vm.FechaHora,
            Entrevistador = vm.Entrevistador,
            Lugar = vm.Lugar,
            Resultado = resultado,
            Calificacion = vm.Calificacion,
            Observaciones = vm.Observaciones
        });

        // Mover candidato a etapa Entrevista si está en Recibido
        if (candidato.Etapa == EtapaCandidato.Recibido)
        {
            candidato.Etapa = EtapaCandidato.Entrevista;
            candidato.UpdatedAt = DateTime.Now;
        }

        // Actualizar calificación general (promedio)
        await _db.SaveChangesAsync();

        var entrevistas = await _db.Entrevistas
            .Where(e => e.CandidatoId == vm.CandidatoId
                     && !e.IsDeleted
                     && e.Calificacion > 0)
            .ToListAsync();

        if (entrevistas.Any())
        {
            candidato.CalificacionGeneral = Math.Round(
                entrevistas.Average(e => e.Calificacion), 1);
            await _db.SaveChangesAsync();
        }

        return (true, "Entrevista registrada correctamente.");
    }

    public async Task<(bool success, string message)>
        ActualizarResultadoEntrevistaAsync(int id,
            EntrevistaViewModel vm)
    {
        var entrevista = await _db.Entrevistas
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entrevista == null)
            return (false, "Entrevista no encontrada.");

        Enum.TryParse<ResultadoEntrevista>(
            vm.Resultado, out var resultado);

        entrevista.Resultado = resultado;
        entrevista.Calificacion = vm.Calificacion;
        entrevista.Observaciones = vm.Observaciones;
        entrevista.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();

        // Recalcular calificación general del candidato
        var entrevistas = await _db.Entrevistas
            .Where(e => e.CandidatoId == entrevista.CandidatoId
                     && !e.IsDeleted
                     && e.Calificacion > 0)
            .ToListAsync();

        if (entrevistas.Any())
        {
            var candidato = await _db.Candidatos
                .FirstOrDefaultAsync(c =>
                    c.Id == entrevista.CandidatoId);
            if (candidato != null)
            {
                candidato.CalificacionGeneral = Math.Round(
                    entrevistas.Average(e => e.Calificacion), 1);
                await _db.SaveChangesAsync();
            }
        }

        return (true, "Resultado registrado correctamente.");
    }

    public async Task<(bool success, string message)>
        EliminarEntrevistaAsync(int id)
    {
        var e = await _db.Entrevistas
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e == null)
            return (false, "Entrevista no encontrada.");

        e.IsDeleted = true;
        e.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Entrevista eliminada.");
    }

    // ════════════════════════════════════════════
    // NOTAS
    // ════════════════════════════════════════════
    public async Task<IEnumerable<NotaCandidatoViewModel>>
        GetNotasCandidatoAsync(int candidatoId)
    {
        return await _db.NotasCandidato
            .Where(n => n.CandidatoId == candidatoId && !n.IsDeleted)
            .OrderByDescending(n => n.Fecha)
            .Select(n => new NotaCandidatoViewModel
            {
                Id = n.Id,
                CandidatoId = n.CandidatoId,
                Nota = n.Nota,
                CreadoPor = n.CreadoPor,
                Fecha = n.Fecha.ToString("dd/MM/yyyy HH:mm")
            })
            .ToListAsync();
    }

    public async Task<(bool success, string message)>
        AgregarNotaAsync(NotaCandidatoViewModel vm, string usuario)
    {
        if (string.IsNullOrWhiteSpace(vm.Nota))
            return (false, "La nota no puede estar vacía.");

        _db.NotasCandidato.Add(new NotaCandidato
        {
            CandidatoId = vm.CandidatoId,
            Nota = vm.Nota.Trim(),
            CreadoPor = usuario,
            Fecha = DateTime.Now
        });

        await _db.SaveChangesAsync();
        return (true, "Nota agregada correctamente.");
    }

    public async Task<(bool success, string message)>
        EliminarNotaAsync(int id)
    {
        var n = await _db.NotasCandidato
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (n == null)
            return (false, "Nota no encontrada.");

        n.IsDeleted = true;
        n.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        return (true, "Nota eliminada.");
    }

    // ════════════════════════════════════════════
    // CONVERTIR CANDIDATO EN EMPLEADO
    // ════════════════════════════════════════════
    public async Task<(bool success, string message, int empleadoId)>
        ConvertirEnEmpleadoAsync(ConvertirEmpleadoViewModel vm)
    {
        var candidato = await _db.Candidatos
            .Include(c => c.PlazaVacante)
            .FirstOrDefaultAsync(c => c.Id == vm.CandidatoId
                                   && !c.IsDeleted);
        if (candidato == null)
            return (false, "Candidato no encontrado.", 0);

        if (candidato.EmpleadoId.HasValue)
            return (false,
                "Este candidato ya fue convertido en empleado.", 0);

        // Verificar que el departamento y puesto existan
        var depto = await _db.Departamentos
            .FirstOrDefaultAsync(d => d.Id == vm.DepartamentoId
                                   && !d.IsDeleted);
        if (depto == null)
            return (false, "Departamento no encontrado.", 0);

        // Generar código de empleado automático
        var ultimo = await _db.Empleados
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        int numero = (ultimo?.Id ?? 0) + 1;
        string codigo = $"EMP-{numero:D4}";

        // Parsear nombre
        var partes = candidato.Nombre.Trim().Split(' ');
        string primerNombre = partes.Length > 0 ? partes[0] : candidato.Nombre;
        string primerApellido = partes.Length > 1 ? partes[^1] : "—";

        Enum.TryParse<TipoContrato>(vm.TipoContrato, out var tipoContrato);

        // Crear el empleado
        var empleado = new Empleado
        {
            Codigo = codigo,
            PrimerNombre = primerNombre,
            PrimerApellido = primerApellido,
            Email = candidato.Email,
            Telefono = candidato.Telefono,
            FechaIngreso = vm.FechaIngreso,
            FechaNacimiento = DateTime.Today.AddYears(-25),
            SalarioBase = vm.SalarioBase,
            DepartamentoId = vm.DepartamentoId,
            PuestoId = vm.PuestoId,
            TipoContrato = tipoContrato,
            Estado = EstadoEmpleado.Activo,
            CUI = "0000000000000" // placeholder
        };

        _db.Empleados.Add(empleado);
        await _db.SaveChangesAsync();

        // Vincular candidato con empleado
        candidato.EmpleadoId = empleado.Id;
        candidato.Etapa = EtapaCandidato.Contratado;
        candidato.UpdatedAt = DateTime.Now;

        // Cerrar plaza si ya se cubrieron todas las vacantes
        var contratados = await _db.Candidatos
            .CountAsync(c => c.PlazaVacanteId == candidato.PlazaVacanteId
                          && c.Etapa == EtapaCandidato.Contratado
                          && !c.IsDeleted);

        if (contratados >= candidato.PlazaVacante.CantidadVacantes)
        {
            candidato.PlazaVacante.Estado = EstadoPlaza.Cerrada;
            candidato.PlazaVacante.FechaCierre = DateTime.Today;
            candidato.PlazaVacante.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        return (true,
            $"Empleado {codigo} creado correctamente. " +
            "Completa sus datos en el módulo de Personal.",
            empleado.Id);
    }

    // ════════════════════════════════════════════
    // ESTADÍSTICAS
    // ════════════════════════════════════════════
    public async Task<EstadisticasReclutamientoViewModel>
        GetEstadisticasAsync()
    {
        var inicioMes = new DateTime(
            DateTime.Today.Year, DateTime.Today.Month, 1);

        var plazas = await _db.PlazasVacantes
            .Where(p => !p.IsDeleted).ToListAsync();

        var candidatos = await _db.Candidatos
            .Include(c => c.PlazaVacante)
            .Where(c => !c.IsDeleted && !c.PlazaVacante.IsDeleted)
            .ToListAsync();

        int total = candidatos.Count;
        int contratados = candidatos
            .Count(c => c.Etapa == EtapaCandidato.Contratado);

        // Tiempo promedio desde postulación hasta contratación
        var tiempos = candidatos
            .Where(c => c.Etapa == EtapaCandidato.Contratado
                     && c.FechaEntrevista.HasValue)
            .Select(c => (c.FechaEntrevista!.Value
                - c.FechaPostulacion).Days)
            .ToList();

        double tiempoPromedio = tiempos.Any()
            ? tiempos.Average() : 0;

        return new EstadisticasReclutamientoViewModel
        {
            PlazasAbiertas = plazas.Count(
                p => p.Estado == EstadoPlaza.Abierta),
            PlazasEnProceso = plazas.Count(
                p => p.Estado == EstadoPlaza.EnProceso),
            PlazasCerradas = plazas.Count(
                p => p.Estado == EstadoPlaza.Cerrada),
            TotalCandidatos = total,
            CandidatosEsteMes = candidatos.Count(
                c => c.FechaPostulacion >= inicioMes),
            Contratados = contratados,
            TiempoPromedioContratacion = Math.Round(
                tiempoPromedio, 1),
            TasaConversion = total > 0
                ? Math.Round((double)contratados / total * 100, 1)
                : 0
        };
    }
}