using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Models.Entities;

namespace SistemaEmpleados.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Departamento> Departamentos { get; set; }
    public DbSet<Puesto> Puestos { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Horario> Horarios { get; set; }
    public DbSet<Asistencia> Asistencias { get; set; }
    public DbSet<Vacacion> Vacaciones { get; set; }
    public DbSet<Ausencia> Ausencias { get; set; }
    public DbSet<Prestacion> Prestaciones { get; set; }
    public DbSet<Planilla> Planillas { get; set; }
    public DbSet<DetallePlanilla> DetallesPlanilla { get; set; }
    public DbSet<PlazaVacante> PlazasVacantes { get; set; }
    public DbSet<Candidato> Candidatos { get; set; }
    public DbSet<KPI> KPIs { get; set; }
    public DbSet<Evaluacion> Evaluaciones { get; set; }
    public DbSet<ResultadoKPI> ResultadosKPI { get; set; }
    public DbSet<HistorialSalario> HistorialSalarios { get; set; }
    // ── Nómina extendida ──
    public DbSet<HistorialSalario> HistorialesSalario { get; set; }
    public DbSet<ConceptoNomina> ConceptosNomina { get; set; }
    public DbSet<PrestamoEmpleado> PrestamosEmpleado { get; set; }
    public DbSet<HistorialEstadoPlaza> HistorialesEstadoPlaza { get; set; }
    public DbSet<Entrevista> Entrevistas { get; set; }
    public DbSet<NotaCandidato> NotasCandidato { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Renombrar tablas de Identity
        builder.Entity<ApplicationUser>().ToTable("Usuarios");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UsuarioRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RolClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UsuarioTokens");

        // Soft delete
        builder.Entity<Departamento>().HasQueryFilter(d => !d.IsDeleted);
        builder.Entity<Puesto>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Vacacion>().HasQueryFilter(v => !v.IsDeleted);
        builder.Entity<Ausencia>().HasQueryFilter(a => !a.IsDeleted);
        builder.Entity<Horario>().HasQueryFilter(h => !h.IsDeleted);
        builder.Entity<Asistencia>().HasQueryFilter(a => !a.IsDeleted);
        builder.Entity<Empleado>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Prestacion>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Planilla>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<DetallePlanilla>().HasQueryFilter(d => !d.IsDeleted);
        builder.Entity<PlazaVacante>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Candidato>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<KPI>().HasQueryFilter(k => !k.IsDeleted);
        builder.Entity<Evaluacion>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ResultadoKPI>().HasQueryFilter(r => !r.IsDeleted);


        builder.Entity<Empleado>()
            .HasOne(e => e.ApplicationUser)
            .WithMany() // CLAVE: evita duplicación
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimales
        builder.Entity<Puesto>().Property(p => p.SalarioMinimo).HasPrecision(18, 2);
        builder.Entity<Puesto>().Property(p => p.SalarioMaximo).HasPrecision(18, 2);

        // Índices
        builder.Entity<Departamento>().HasIndex(d => d.Codigo).IsUnique();
        builder.Entity<Puesto>().HasIndex(p => p.Codigo).IsUnique();

        builder.Entity<Empleado>().HasIndex(e => e.Codigo).IsUnique();
        builder.Entity<Empleado>().HasIndex(e => e.CUI).IsUnique();
        builder.Entity<Empleado>().Property(e => e.SalarioBase).HasPrecision(18, 2);

        // Relaciones normales
        builder.Entity<Empleado>()
            .HasOne(e => e.Departamento)
            .WithMany(d => d.Empleados)
            .HasForeignKey(e => e.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Empleado>()
            .HasOne(e => e.Puesto)
            .WithMany(p => p.Empleados)
            .HasForeignKey(e => e.PuestoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asistencia>()
            .Property(a => a.HorasExtra).HasPrecision(5, 2);

        builder.Entity<Asistencia>()
            .HasOne(a => a.Empleado)
            .WithMany()
            .HasForeignKey(a => a.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Asistencia>()
            .HasOne(a => a.Horario)
            .WithMany(h => h.Asistencias)
            .HasForeignKey(a => a.HorarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Asistencia>()
            .HasIndex(a => new { a.EmpleadoId, a.Fecha });

        builder.Entity<Vacacion>()
            .HasOne(v => v.Empleado)
            .WithMany()
            .HasForeignKey(v => v.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ausencia>()
            .HasOne(a => a.Empleado)
            .WithMany()
            .HasForeignKey(a => a.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prestacion>()
            .Property(p => p.Monto).HasPrecision(18, 2);

        builder.Entity<Prestacion>()
            .Property(p => p.SalarioBase).HasPrecision(18, 2);

        builder.Entity<Prestacion>()
            .HasOne(p => p.Empleado)
            .WithMany()
            .HasForeignKey(p => p.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Planilla>()
            .Property(p => p.TotalDevengado).HasPrecision(18, 2);

        builder.Entity<Planilla>()
            .Property(p => p.TotalDeducciones).HasPrecision(18, 2);

        builder.Entity<Planilla>()
            .Property(p => p.TotalNeto).HasPrecision(18, 2);

        builder.Entity<DetallePlanilla>()
            .HasOne(d => d.Planilla)
            .WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PlanillaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DetallePlanilla>()
            .HasOne(d => d.Empleado)
            .WithMany()
            .HasForeignKey(d => d.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Planilla>()
            .HasIndex(p => new { p.Mes, p.Anio, p.Estado });

        builder.Entity<PlazaVacante>()
            .Property(p => p.SalarioOfrecido).HasPrecision(18, 2);

        builder.Entity<PlazaVacante>()
            .HasOne(p => p.Departamento)
            .WithMany()
            .HasForeignKey(p => p.DepartamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PlazaVacante>()
            .HasOne(p => p.Puesto)
            .WithMany()
            .HasForeignKey(p => p.PuestoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Candidato>()
            .HasOne(c => c.PlazaVacante)
            .WithMany(p => p.Candidatos)
            .HasForeignKey(c => c.PlazaVacanteId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.Entity<KPI>()
            .Property(k => k.Peso).HasPrecision(5, 2);

        builder.Entity<Evaluacion>()
            .Property(e => e.PuntajeTotal).HasPrecision(5, 2);

        builder.Entity<ResultadoKPI>()
            .Property(r => r.Calificacion).HasPrecision(5, 2);

        builder.Entity<ResultadoKPI>()
            .Property(r => r.PuntajePonderado).HasPrecision(5, 2);

        builder.Entity<Evaluacion>()
            .HasOne(e => e.Empleado)
            .WithMany()
            .HasForeignKey(e => e.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Evaluacion>()
            .HasOne(e => e.Evaluador)
            .WithMany()
            .HasForeignKey(e => e.EvaluadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ResultadoKPI>()
            .HasOne(r => r.Evaluacion)
            .WithMany(e => e.Resultados)
            .HasForeignKey(r => r.EvaluacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResultadoKPI>()
            .HasOne(r => r.KPI)
            .WithMany()
            .HasForeignKey(r => r.KPIId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── HistorialSalario ──
        builder.Entity<HistorialSalario>()
            .HasQueryFilter(h => !h.IsDeleted);

        builder.Entity<HistorialSalario>()
            .Property(h => h.SalarioAnterior).HasPrecision(18, 2);

        builder.Entity<HistorialSalario>()
            .Property(h => h.SalarioNuevo).HasPrecision(18, 2);

        builder.Entity<HistorialSalario>()
            .HasOne(h => h.Empleado)
            .WithMany()
            .HasForeignKey(h => h.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ConceptoNomina ──
        builder.Entity<ConceptoNomina>()
            .HasQueryFilter(c => !c.IsDeleted);

        builder.Entity<ConceptoNomina>()
            .Property(c => c.Valor).HasPrecision(10, 4);

        builder.Entity<ConceptoNomina>()
            .HasIndex(c => c.Codigo).IsUnique();

        // ── PrestamoEmpleado ──
        builder.Entity<PrestamoEmpleado>()
            .HasQueryFilter(p => !p.IsDeleted);

        builder.Entity<PrestamoEmpleado>()
            .Property(p => p.MontoTotal).HasPrecision(18, 2);

        builder.Entity<PrestamoEmpleado>()
            .Property(p => p.CuotaMensual).HasPrecision(18, 2);

        builder.Entity<PrestamoEmpleado>()
            .Property(p => p.SaldoPendiente).HasPrecision(18, 2);

        builder.Entity<PrestamoEmpleado>()
            .HasOne(p => p.Empleado)
            .WithMany()
            .HasForeignKey(p => p.EmpleadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<KPI>()
            .HasOne(k => k.Puesto)
            .WithMany()
            .HasForeignKey(k => k.PuestoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<DetallePlanilla>()
            .Property(d => d.CuotaIGSSPatronal).HasPrecision(18, 2);
        builder.Entity<DetallePlanilla>()
            .Property(d => d.Bono14).HasPrecision(18, 2);
        builder.Entity<DetallePlanilla>()
            .Property(d => d.Aguinaldo).HasPrecision(18, 2);
        builder.Entity<DetallePlanilla>()
            .Property(d => d.DescuentoPrestamo).HasPrecision(18, 2);

        builder.Entity<HistorialEstadoPlaza>()
            .HasQueryFilter(h => !h.IsDeleted);
        builder.Entity<HistorialEstadoPlaza>()
            .HasOne(h => h.PlazaVacante)
            .WithMany(p => p.Historial)
            .HasForeignKey(h => h.PlazaVacanteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Entrevista>()
            .HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Entrevista>()
            .Property(e => e.Calificacion).HasPrecision(3, 1);
        builder.Entity<Entrevista>()
            .HasOne(e => e.Candidato)
            .WithMany(c => c.Entrevistas)
            .HasForeignKey(e => e.CandidatoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<NotaCandidato>()
            .HasQueryFilter(n => !n.IsDeleted);
        builder.Entity<NotaCandidato>()
            .HasOne(n => n.Candidato)
            .WithMany(c => c.Notas)
            .HasForeignKey(n => n.CandidatoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Candidato>()
            .Property(c => c.CalificacionGeneral).HasPrecision(3, 1);
        builder.Entity<Candidato>()
            .HasOne(c => c.Empleado)
            .WithMany()
            .HasForeignKey(c => c.EmpleadoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}