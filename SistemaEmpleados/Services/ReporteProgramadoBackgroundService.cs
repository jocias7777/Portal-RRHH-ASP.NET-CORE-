using SistemaEmpleados.Models.Entities;
using SistemaEmpleados.Services.Interfaces;

namespace SistemaEmpleados.Services;

public class ReporteProgramadoBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReporteProgramadoBackgroundService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(15);

    public ReporteProgramadoBackgroundService(IServiceProvider serviceProvider, ILogger<ReporteProgramadoBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Reportes Programados iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reportesService = scope.ServiceProvider.GetRequiredService<IReportesService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var reportesPendientes = await reportesService.GetReportesPorEnviarAsync();

                foreach (var reporte in reportesPendientes)
                {
                    try
                    {
                        _logger.LogInformation("Procesando reporte programado: {Nombre}", reporte.Nombre);

                        var tipoReporteStr = reporte.TipoReporte switch
                        {
                            TipoReporteProgramado.PlanillaMensual => "PlanillaMensual",
                            TipoReporteProgramado.Bono14Aguinaldo => "Bono14Aguinaldo",
                            TipoReporteProgramado.ContratosVencer => "ContratosVencer",
                            TipoReporteProgramado.DocumentosVencidos => "DocumentosVencidos",
                            TipoReporteProgramado.ResumenGeneral => "ResumenGeneral",
                            _ => "ResumenGeneral"
                        };

                        var archivos = new Dictionary<string, byte[]>();

                        if (reporte.IncluirExcel)
                        {
                            var excelData = await reportesService.GenerarExcelReporteAsync(tipoReporteStr, reporte.DepartamentoId);
                            archivos[$"{reporte.Nombre}_{DateTime.Now:yyyyMMdd}.xlsx"] = excelData;
                        }

                        var asunto = $"[Portal RRHH] Reporte Automático: {reporte.Nombre}";
                        var cuerpo = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #0d9488; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .footer {{ background: #eee; padding: 10px; text-align: center; font-size: 12px; color: #666; }}
        .info-box {{ background: white; padding: 15px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #0d9488; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>Portal RRHH - Reporte Automático</h2>
        </div>
        <div class='content'>
            <div class='info-box'>
                <p><strong>Reporte:</strong> {reporte.Nombre}</p>
                <p><strong>Tipo:</strong> {reporte.TipoReporteNombre}</p>
                <p><strong>Fecha de generación:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                <p><strong>Frecuencia:</strong> {reporte.FrecuenciaNombre}</p>
            </div>
            <p>Adjunto encontrará el reporte solicitado en formato Excel.</p>
            <p>Este es un envío automático del sistema de Reportes Programados del Portal RRHH.</p>
        </div>
        <div class='footer'>
            <p>Portal RRHH - Sistema de Gestión de Recursos Humanos</p>
            <p>Este mensaje fue enviado automáticamente. Por favor no responder a este correo.</p>
        </div>
    </div>
</body>
</html>";

                        var success = await emailService.SendEmailWithAttachmentsAsync(
                            reporte.EmailDestino,
                            asunto,
                            cuerpo,
                            archivos
                        );

                        if (success)
                        {
                            await reportesService.MarcarEnvioExitosoAsync(reporte.Id, DateTime.Now);
                            _logger.LogInformation("Reporte {Nombre} enviado exitosamente a {Email}", reporte.Nombre, reporte.EmailDestino);
                        }
                        else
                        {
                            await reportesService.MarcarEnvioFallidoAsync(reporte.Id, "Error al enviar email");
                            _logger.LogError("Error al enviar reporte {Nombre}", reporte.Nombre);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando reporte {Nombre}", reporte.Nombre);
                        await reportesService.MarcarEnvioFallidoAsync(reporte.Id, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el servicio de reportes programados");
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }
}