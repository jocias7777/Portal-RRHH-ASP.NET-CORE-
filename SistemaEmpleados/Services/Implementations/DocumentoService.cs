using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.DTOs;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Models.ViewModels;
using SistemaEmpleados.Services.Interfaces;
using System.IO;

namespace SistemaEmpleados.Services.Implementations;

public class DocumentoService : IDocumentoService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentoRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IWebHostEnvironment _env;

    public DocumentoService(
        ApplicationDbContext context,
        IDocumentoRepository repo,
        IUnitOfWork uow,
        IWebHostEnvironment env)
    {
        _context = context;
        _repo = repo;
        _uow = uow;
        _env = env;
    }

    public async Task<DataTablesResponse<DocumentoListViewModel>> GetDataTablesAsync(DataTablesRequest req)
    {
        var query = _context.Documentos
            .Include(d => d.Empleado)
            .ThenInclude(e => e.Departamento)
            .Where(d => !d.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.SearchValue))
        {
            var s = req.SearchValue.ToLower();
            query = query.Where(d =>
                d.Titulo.ToLower().Contains(s) ||
                d.Descripcion!.ToLower().Contains(s) ||
                d.Empleado.PrimerNombre.ToLower().Contains(s) ||
                d.Empleado.PrimerApellido.ToLower().Contains(s));
        }

        if (req.EmpleadoId.HasValue && req.EmpleadoId > 0)
            query = query.Where(d => d.EmpleadoId == req.EmpleadoId);

        if (!string.IsNullOrWhiteSpace(req.Tipo) &&
            Enum.TryParse<TipoDocumento>(req.Tipo, out var tipo))
            query = query.Where(d => d.Tipo == tipo);

        if (req.DepartamentoId.HasValue && req.DepartamentoId > 0)
            query = query.Where(d => d.Empleado.DepartamentoId == req.DepartamentoId);

        var total = await query.CountAsync();

        query = req.OrderColumn switch
        {
            "titulo" => req.OrderDir == "asc" ? query.OrderBy(d => d.Titulo) : query.OrderByDescending(d => d.Titulo),
            "tipo" => req.OrderDir == "asc" ? query.OrderBy(d => d.Tipo) : query.OrderByDescending(d => d.Tipo),
            "estado" => req.OrderDir == "asc" ? query.OrderBy(d => d.Estado) : query.OrderByDescending(d => d.Estado),
            "fecha" => req.OrderDir == "asc" ? query.OrderBy(d => d.CreatedAt) : query.OrderByDescending(d => d.CreatedAt),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var data = await query
            .Skip(req.Start)
            .Take(req.Length)
            .Select(d => new DocumentoListViewModel
            {
                Id = d.Id,
                Titulo = d.Titulo,
                Descripcion = d.Descripcion,
                Tipo = d.Tipo.ToString(),
                Modalidad = d.Modalidad.ToString(),
                Estado = d.Estado.ToString(),
                UrlArchivo = d.UrlArchivo,
                NombreArchivo = d.NombreArchivo,
                FechaExpiracion = d.FechaExpiracion.HasValue ? d.FechaExpiracion.Value.ToString("dd/MM/yyyy") : null,
                CreatedAt = d.CreatedAt.ToString("dd/MM/yyyy"),
                EmpleadoId = d.EmpleadoId,
                EmpleadoNombre = d.Empleado.PrimerNombre + " " + d.Empleado.PrimerApellido,
                EmpleadoDepartamento = d.Empleado.Departamento.Nombre,
                EstaExpirado = d.FechaExpiracion.HasValue && d.FechaExpiracion < DateTime.Today,
                PorExpirar = d.FechaExpiracion.HasValue && d.FechaExpiracion >= DateTime.Today && d.FechaExpiracion <= DateTime.Today.AddDays(30)
            })
            .ToListAsync();

        return new DataTablesResponse<DocumentoListViewModel>
        {
            Draw = req.Draw,
            RecordsTotal = total,
            RecordsFiltered = total,
            Data = data
        };
    }

    public async Task<DocumentoDetalleViewModel?> GetByIdAsync(int id)
    {
        var d = await _repo.GetByIdWithRelationsAsync(id);
        if (d == null) return null;

        return new DocumentoDetalleViewModel
        {
            Id = d.Id,
            Titulo = d.Titulo,
            Descripcion = d.Descripcion,
            Tipo = d.Tipo,
            Modalidad = d.Modalidad,
            Estado = d.Estado,
            UrlArchivo = d.UrlArchivo,
            NombreArchivo = d.NombreArchivo,
            ContentType = d.ContentType,
            TamanioBytes = d.TamanioBytes,
            FechaExpiracion = d.FechaExpiracion,
            NumeroFolio = d.NumeroFolio,
            Observaciones = d.Observaciones,
            EmpleadoId = d.EmpleadoId,
            EmpleadoNombre = d.Empleado.NombreCompleto,
            EmpleadoCodigo = d.Empleado.Codigo,
            EmpleadoDepartamento = d.Empleado.Departamento?.Nombre ?? "",
            EstaExpirado = d.FechaExpiracion.HasValue && d.FechaExpiracion.Value < DateTime.Today,
            PorExpirar = d.FechaExpiracion.HasValue && 
                d.FechaExpiracion.Value >= DateTime.Today && 
                d.FechaExpiracion.Value <= DateTime.Today.AddDays(30),
            FechaCreacion = d.CreatedAt.ToString("dd/MM/yyyy")
        };
    }

    public async Task<DocumentoViewModel?> GetFormViewModelAsync(int id)
    {
        var d = await _repo.GetByIdWithRelationsAsync(id);
        if (d == null) return null;

        return new DocumentoViewModel
        {
            Id = d.Id,
            Titulo = d.Titulo,
            Descripcion = d.Descripcion,
            Tipo = d.Tipo,
            Modalidad = d.Modalidad,
            Estado = d.Estado,
            UrlArchivo = d.UrlArchivo,
            NombreArchivo = d.NombreArchivo,
            ContentType = d.ContentType,
            TamanioBytes = d.TamanioBytes,
            FechaExpiracion = d.FechaExpiracion,
            NumeroFolio = d.NumeroFolio,
            Observaciones = d.Observaciones,
            EmpleadoId = d.EmpleadoId,
            DepartamentoId = d.Empleado.DepartamentoId
        };
    }

    public async Task<(bool success, string message, int id)> CreateAsync(DocumentoViewModel vm, IFormFile? archivo = null)
    {
        if (await _repo.ExisteTituloAsync(vm.Titulo, null, vm.EmpleadoId))
            return (false, "Ya existe un documento con ese titulo para este empleado.", 0);

        string? nombreArchivo = vm.NombreArchivo;
        string? contentType = vm.ContentType;
        long? tamanioBytes = vm.TamanioBytes;

        if (archivo != null && archivo.Length > 0)
        {
            var uploadsPath = System.IO.Path.Combine(_env.WebRootPath, "uploads", "documentos");
            Directory.CreateDirectory(uploadsPath);

            var ext = System.IO.Path.GetExtension(archivo.FileName);
            nombreArchivo = $"doc_{Guid.NewGuid():N}{ext}";
            var fullPath = System.IO.Path.Combine(uploadsPath, nombreArchivo);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            contentType = archivo.ContentType;
            tamanioBytes = archivo.Length;
        }

        var documento = new Documento
        {
            Titulo = vm.Titulo.Trim(),
            Descripcion = vm.Descripcion?.Trim(),
            Tipo = vm.Tipo,
            Modalidad = vm.Modalidad,
            Estado = vm.Estado,
            UrlArchivo = nombreArchivo ?? "",
            NombreArchivo = nombreArchivo,
            ContentType = contentType,
            TamanioBytes = tamanioBytes,
            FechaExpiracion = vm.FechaExpiracion,
            NumeroFolio = vm.NumeroFolio?.Trim(),
            Observaciones = vm.Observaciones?.Trim(),
            EmpleadoId = vm.EmpleadoId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(documento);
        await _uow.SaveChangesAsync();

        return (true, $"Documento {documento.Titulo} guardado correctamente.", documento.Id);
    }

    public async Task<(bool success, string message)> UpdateAsync(int id, DocumentoViewModel vm, IFormFile? archivo = null)
    {
        var documento = await _repo.GetByIdAsync(id);
        if (documento == null) return (false, "Documento no encontrado.");

        if (await _repo.ExisteTituloAsync(vm.Titulo, id, vm.EmpleadoId))
            return (false, "Ya existe otro documento con ese titulo para este empleado.");

        documento.Titulo = vm.Titulo.Trim();
        documento.Descripcion = vm.Descripcion?.Trim();
        documento.Tipo = vm.Tipo;
        documento.Modalidad = vm.Modalidad;
        documento.Estado = vm.Estado;
        documento.FechaExpiracion = vm.FechaExpiracion;
        documento.NumeroFolio = vm.NumeroFolio?.Trim();
        documento.Observaciones = vm.Observaciones?.Trim();
        documento.EmpleadoId = vm.EmpleadoId;

        if (archivo != null && archivo.Length > 0)
        {
            var uploadsPath = System.IO.Path.Combine(_env.WebRootPath, "uploads", "documentos");
            Directory.CreateDirectory(uploadsPath);

            if (!string.IsNullOrEmpty(documento.NombreArchivo))
            {
                var oldPath = System.IO.Path.Combine(uploadsPath, documento.NombreArchivo);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var ext = System.IO.Path.GetExtension(archivo.FileName);
            var nombreArchivo = $"doc_{Guid.NewGuid():N}{ext}";
            var fullPath = System.IO.Path.Combine(uploadsPath, nombreArchivo);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await archivo.CopyToAsync(stream);

            documento.UrlArchivo = nombreArchivo;
            documento.NombreArchivo = nombreArchivo;
            documento.ContentType = archivo.ContentType;
            documento.TamanioBytes = archivo.Length;
        }
        else
        {
            documento.UrlArchivo = vm.UrlArchivo?.Trim() ?? "";
            documento.NombreArchivo = vm.NombreArchivo?.Trim();
            documento.ContentType = vm.ContentType;
            documento.TamanioBytes = vm.TamanioBytes;
        }

        documento.UpdatedAt = DateTime.UtcNow;

        _repo.Update(documento);
        await _uow.SaveChangesAsync();

        return (true, $"Documento {documento.Titulo} actualizado correctamente.");
    }

    public async Task<(bool success, string message)> DeleteAsync(int id)
    {
        var documento = await _repo.GetByIdAsync(id);
        if (documento == null) return (false, "Documento no encontrado.");

        documento.IsDeleted = true;
        documento.Estado = EstadoDocumento.Archivado;
        documento.UpdatedAt = DateTime.UtcNow;

        _repo.Update(documento);
        await _uow.SaveChangesAsync();

        return (true, "Documento archiveado correctamente.");
    }

    public async Task<IEnumerable<object>> SearchForGlobalAsync(string term)
    {
        var results = await _repo.SearchAsync(term);
        return results.Select(d => new
        {
            id = d.Id,
            texto = d.Titulo,
            sub = d.Empleado?.NombreCompleto ?? "",
            url = $"/Documentos/Detalle/{d.Id}",
            icono = "fa-file-lines",
            color = "#7C3AED"
        });
    }

    public async Task<IEnumerable<object>> GetExpiringAsync(int dias = 30)
    {
        var documentos = await _repo.GetExpiringAsync(dias);
        return documentos.Select(d => new
        {
            id = d.Id,
            titulo = d.Titulo,
            tipo = d.Tipo.ToString(),
            fechaExpiracion = d.FechaExpiracion?.ToString("dd/MM/yyyy"),
            empleado = d.Empleado?.NombreCompleto,
            url = $"/Documentos/Detalle/{d.Id}"
        });
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _repo.CountActivosAsync();
    }

    public async Task<List<DocumentoAlertaViewModel>> GetAlertasAsync()
    {
        var alertas = new List<DocumentoAlertaViewModel>();
        var hoy = DateTime.Today;

        var contratosVencidos = await _context.Documentos
            .Include(d => d.Empleado)
            .Where(d => !d.IsDeleted && d.Tipo == TipoDocumento.Contrato 
                && d.Estado == EstadoDocumento.Activo
                && d.FechaExpiracion < hoy)
            .ToListAsync();

        foreach (var c in contratosVencidos)
        {
            alertas.Add(new DocumentoAlertaViewModel
            {
                Id = c.Id,
                Titulo = c.Titulo,
                TipoAlerta = "Contrato vencido",
                Descripcion = $"El contrato de {c.Empleado?.NombreCompleto} vencio el {c.FechaExpiracion:dd/MM/yyyy}",
                EmpleadoNombre = c.Empleado?.NombreCompleto ?? "",
                EmpleadoId = c.EmpleadoId,
                DocumentoId = c.Id,
                FechaVencimiento = c.FechaExpiracion?.ToString("dd/MM/yyyy"),
                DiasRestantes = (int)(hoy - c.FechaExpiracion!.Value).TotalDays
            });
        }

        var porVencer = await _context.Documentos
            .Include(d => d.Empleado)
            .Where(d => !d.IsDeleted && d.Tipo == TipoDocumento.Contrato
                && d.Estado == EstadoDocumento.Activo
                && d.FechaExpiracion >= hoy
                && d.FechaExpiracion <= hoy.AddDays(30))
            .ToListAsync();

        foreach (var c in porVencer)
        {
            alertas.Add(new DocumentoAlertaViewModel
            {
                Id = c.Id,
                Titulo = c.Titulo,
                TipoAlerta = "Por vencer",
                Descripcion = $"El contrato de {c.Empleado?.NombreCompleto} vence en {(c.FechaExpiracion - hoy).Value.Days} dias",
                EmpleadoNombre = c.Empleado?.NombreCompleto ?? "",
                EmpleadoId = c.EmpleadoId,
                DocumentoId = c.Id,
                FechaVencimiento = c.FechaExpiracion?.ToString("dd/MM/yyyy"),
                DiasRestantes = (int)(c.FechaExpiracion - hoy).Value.Days
            });
        }

        var antecedentesVencidos = await _context.Documentos
            .Include(d => d.Empleado)
            .Where(d => !d.IsDeleted && d.Tipo == TipoDocumento.Antecedentes
                && d.Estado == EstadoDocumento.Activo
                && d.FechaExpiracion < hoy.AddDays(-90))
            .ToListAsync();

        foreach (var a in antecedentesVencidos)
        {
            alertas.Add(new DocumentoAlertaViewModel
            {
                Id = a.Id,
                Titulo = a.Titulo,
                TipoAlerta = "Antecedentes vencidos",
                Descripcion = $"Los antecedentes de {a.Empleado?.NombreCompleto} estan vencidos (caducan a los 90 dias)",
                EmpleadoNombre = a.Empleado?.NombreCompleto ?? "",
                EmpleadoId = a.EmpleadoId,
                DocumentoId = a.Id,
                FechaVencimiento = a.FechaExpiracion?.ToString("dd/MM/yyyy"),
                DiasRestantes = (int)(hoy - a.FechaExpiracion!.Value).TotalDays
            });
        }

        return alertas.OrderBy(a => a.DiasRestantes).ToList();
    }

    public async Task<ExpedienteEmpleadoViewModel> GetExpedienteEmpleadoAsync(int empleadoId)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Departamento)
            .FirstOrDefaultAsync(e => e.Id == empleadoId);

        if (empleado == null) return new ExpedienteEmpleadoViewModel();

        var documentos = await _context.Documentos
            .Where(d => !d.IsDeleted && d.EmpleadoId == empleadoId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentoListViewModel
            {
                Id = d.Id,
                Titulo = d.Titulo,
                Tipo = d.Tipo.ToString(),
                Estado = d.Estado.ToString(),
                FechaExpiracion = d.FechaExpiracion.HasValue ? d.FechaExpiracion.Value.ToString("dd/MM/yyyy") : null,
                CreatedAt = d.CreatedAt.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        var requeridos = new List<DocumentoRequeridoViewModel>
        {
            new() { Tipo = "Contrato", Nombre = "Contrato de trabajo", EstaPresente = documentos.Any(d => d.Tipo == "Contrato"), DocumentoId = documentos.FirstOrDefault(d => d.Tipo == "Contrato")?.Id },
            new() { Tipo = "Credencial", Nombre = "Credencial de identificacion", EstaPresente = documentos.Any(d => d.Tipo == "Credencial"), DocumentoId = documentos.FirstOrDefault(d => d.Tipo == "Credencial")?.Id },
            new() { Tipo = "Certificado", Nombre = "Certificado de salud", EstaPresente = documentos.Any(d => d.Tipo == "Certificado"), DocumentoId = documentos.FirstOrDefault(d => d.Tipo == "Certificado")?.Id },
            new() { Tipo = "Antecedentes", Nombre = "Antecedentes penales", EstaPresente = documentos.Any(d => d.Tipo == "Antecedentes"), DocumentoId = documentos.FirstOrDefault(d => d.Tipo == "Antecedentes")?.Id }
        };

        var completos = requeridos.Count(r => r.EstaPresente);

        return new ExpedienteEmpleadoViewModel
        {
            EmpleadoId = empleadoId,
            EmpleadoNombre = empleado.NombreCompleto,
            EmpleadoCodigo = empleado.Codigo,
            Departamento = empleado.Departamento?.Nombre ?? "",
            Documentos = documentos,
            Requeridos = requeridos,
            DocumentosCompletos = completos,
            DocumentosTotales = requeridos.Count,
            PorcentajeCompletado = requeridos.Count > 0 ? (completos * 100.0 / requeridos.Count) : 0
        };
    }
}
