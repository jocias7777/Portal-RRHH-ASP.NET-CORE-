using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaEmpleados.Data;
using SistemaEmpleados.Data.Repositories;
using SistemaEmpleados.Data.Seeder;
using SistemaEmpleados.Data.UnitOfWork;
using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Services;
using SistemaEmpleados.Services.Implementations;
using SistemaEmpleados.Services.Interfaces;



var builder = WebApplication.CreateBuilder(args);

// ── Base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── Cookie  ← FIX 1: SameSite.Lax y sin SecurePolicy
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // ← SecurePolicy.Always ELIMINADO (rompía HTTP local)
});

// ── Servicios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
builder.Services.AddScoped<IEmpleadoService, EmpleadoService>();

builder.Services.AddScoped<IAsistenciaRepository, AsistenciaRepository>();
builder.Services.AddScoped<IAsistenciaService, AsistenciaService>();

builder.Services.AddScoped<IVacacionRepository, VacacionRepository>();
builder.Services.AddScoped<IAusenciaRepository, AusenciaRepository>();
builder.Services.AddScoped<IVacacionService, VacacionService>();

builder.Services.AddScoped<IPrestacionRepository, PrestacionRepository>();
builder.Services.AddScoped<IPrestacionService, PrestacionService>();

builder.Services.AddScoped<IPlanillaRepository, PlanillaRepository>();
builder.Services.AddScoped<INominaService, NominaService>();
builder.Services.AddScoped<INominaService, NominaService>();

builder.Services.AddScoped<IPlazaVacanteRepository, PlazaVacanteRepository>();
builder.Services.AddScoped<ICandidatoRepository, CandidatoRepository>();

builder.Services.AddScoped<IEvaluacionRepository, EvaluacionRepository>();
builder.Services.AddScoped<IKPIRepository, KPIRepository>();
builder.Services.AddScoped<IEvaluacionService, EvaluacionService>();

builder.Services.AddScoped<IReclutamientoService, SistemaEmpleados.Services.Implementations.ReclutamientoService>();

builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
builder.Services.AddScoped<IDocumentoService, DocumentoService>();

builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<ReporteProgramadoBackgroundService>();

// ── AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// ── MVC  ← FIX 2: sin filtro global de Authorize
builder.Services.AddControllersWithViews();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        ));

var app = builder.Build();



// ── Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// ── Middleware  ← FIX 3: UseHttpsRedirection DENTRO del if
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// ── Seed conceptos de nómina legales Guatemala ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    if (!db.ConceptosNomina.Any())
    {
        db.ConceptosNomina.AddRange(new[]
        {
            // ── DEVENGADOS ──
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "SAL-BASE",
                Nombre = "Salario Base",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Devengado,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.MontoFijo,
                Valor = 0,
                EsObligatorio = true,
                EsSistema = true,
                Activo = true,
                Descripcion = "Salario base mensual del empleado"
            },
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "BONO-250",
                Nombre = "Bonificación Incentivo Dto. 78-89",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Devengado,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.MontoFijo,
                Valor = 250,
                EsObligatorio = true,
                EsSistema = true,
                Activo = true,
                Descripcion = "Bonificación incentivo obligatoria Q250 mensuales"
            },
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "H-EXTRA",
                Nombre = "Horas Extra",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Devengado,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.Formula,
                Valor = 1.5m,
                EsObligatorio = false,
                EsSistema = true,
                Activo = true,
                Descripcion = "Horas extra al 150% del valor hora ordinaria"
            },

            // ── DEDUCCIONES ──
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "IGSS-LAB",
                Nombre = "Cuota IGSS Laboral",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Deduccion,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.Porcentaje,
                Valor = 4.83m,
                EsObligatorio = true,
                EsSistema = true,
                Activo = true,
                Descripcion = "Cuota laboral IGSS 4.83% sobre salario"
            },
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "ISR-RET",
                Nombre = "Retención ISR",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Deduccion,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.Formula,
                Valor = 5m,
                EsObligatorio = true,
                EsSistema = true,
                Activo = true,
                Descripcion = "Retención mensual ISR según tabla SAT"
            },
            new SistemaEmpleados.Models.Entities.ConceptoNomina
            {
                Codigo = "PREST",
                Nombre = "Cuota Préstamo",
                Tipo = SistemaEmpleados.Models.Entities.TipoConcepto.Deduccion,
                Aplicacion = SistemaEmpleados.Models.Entities.AplicacionConcepto.MontoFijo,
                Valor = 0,
                EsObligatorio = false,
                EsSistema = false,
                Activo = true,
                Descripcion = "Descuento de cuota mensual de préstamo"
            }
        });

        await db.SaveChangesAsync();
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ← Ruta default va directo al Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();