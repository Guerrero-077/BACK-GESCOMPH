using Hangfire;
using Hangfire.SqlServer;
using TimeZoneConverter;
using WebGESCOMPH.RealTime;
using WebGESCOMPH.RealTime.Contract;
using WebGESCOMPH.RealTime.Obligations;
using WebGESCOMPH.Security;

namespace WebGESCOMPH.Extensions.Infrastructure
{
    /// <summary>
    /// Extensiones para registrar, configurar y ejecutar Hangfire dentro del pipeline de ASP.NET Core.
    /// </summary>
    /// <remarks>
    /// Esta clase centraliza toda la configuración de Hangfire, incluyendo:
    /// <list type="bullet">
    /// <item>Registro del almacenamiento persistente en SQL Server.</item>
    /// <item>Inicialización del servidor de procesamiento de jobs en segundo plano.</item>
    /// <item>Configuración del panel de control (Dashboard) con autorización personalizada.</item>
    /// <item>Programación de trabajos recurrentes como generación de obligaciones y expiración de contratos.</item>
    /// </list>
    /// 
    /// <b>Motivación:</b> encapsular la infraestructura de ejecución en background 
    /// para mantener el <c>Program.cs</c> o <c>Startup.cs</c> limpio y mantenible.
    /// </remarks>
    public static class HangfireExtensions
    {
        /// <summary>
        /// Registra los servicios necesarios de Hangfire en el contenedor de inyección de dependencias.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación.</param>
        /// <param name="configuration">Configuración de la aplicación (usualmente <c>appsettings.json</c>).</param>
        /// <returns>La misma instancia de <see cref="IServiceCollection"/> para encadenar llamadas.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si no se encuentra la cadena de conexión <c>ConnectionStrings:SqlServer</c>.
        /// </exception>
        /// <remarks>
        /// Configura Hangfire para utilizar SQL Server como backend, 
        /// aplicando opciones recomendadas de aislamiento, serialización y polling.
        /// 
        /// Además, arranca el servidor de procesamiento de jobs con dos colas:
        /// <list type="bullet">
        /// <item><b>default</b>: trabajos regulares.</item>
        /// <item><b>maintenance</b>: trabajos de mantenimiento o baja frecuencia.</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
        {
            var hfConn = configuration.GetConnectionString("SqlServer")
                       ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer en appsettings.json");

            services.AddHangfire(cfg =>
            {
                cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                   .UseSimpleAssemblyNameTypeSerializer()
                   .UseRecommendedSerializerSettings()
                   .UseSqlServerStorage(
                       hfConn,
                       new SqlServerStorageOptions
                       {
                           SchemaName = configuration["Hangfire:Schema"] ?? "hangfire",
                           UseRecommendedIsolationLevel = true,
                           TryAutoDetectSchemaDependentOptions = true,
                           SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                           QueuePollInterval = TimeSpan.Zero // Polling inmediato
                       });
            });

            services.AddHangfireServer(options =>
            {
                options.Queues = new[] { "default", "maintenance" };
            });

            return services;
        }

        /// <summary>
        /// Habilita el dashboard de Hangfire y registra los trabajos recurrentes 
        /// configurados desde el <c>appsettings.json</c>.
        /// </summary>
        /// <param name="app">Aplicación ASP.NET Core.</param>
        /// <param name="configuration">Configuración global de la aplicación.</param>
        /// <returns>Instancia de <see cref="IApplicationBuilder"/> para encadenar middlewares.</returns>
        /// <remarks>
        /// - Expone el Dashboard en la ruta <c>/hangfire</c>, protegido mediante 
        /// <see cref="HangfireDashboardAuth"/>.  
        /// - Usa la zona horaria configurada (<c>Hangfire:TimeZoneIana</c>), 
        /// por defecto <c>America/Bogota</c>.  
        /// - Registra dos trabajos recurrentes:
        ///   <list type="number">
        ///     <item><b>obligations-monthly</b>: genera obligaciones del mes actual.</item>
        ///     <item><b>contracts-expiration</b>: ejecuta barrido periódico de contratos expirados (si está habilitado).</item>
        ///   </list>
        /// </remarks>
        public static IApplicationBuilder UseHangfireDashboardAndJobs(this IApplicationBuilder app, IConfiguration configuration)
        {
            var env = (app as WebApplication)!.Environment;

            // Usa el mismo filtro de autorización tanto en dev como en prod
            var dashboardAuth = env.IsDevelopment()
                ? (Hangfire.Dashboard.IDashboardAuthorizationFilter)new HangfireDashboardAuth()
                : new HangfireDashboardAuth();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { dashboardAuth }
            });

            var tz = TZConvert.GetTimeZoneInfo(configuration["Hangfire:TimeZoneIana"] ?? "America/Bogota");

            // Job recurrente: generación mensual de obligaciones
            var cronObligations = configuration["Hangfire:CronObligations"] ?? "15 2 1 * *"; // Cada 1° del mes a las 2:15 AM
            RecurringJob.AddOrUpdate<ObligationJobs>(
                "obligations-monthly",
                j => j.GenerateForCurrentMonthAsync(JobCancellationToken.Null),
                cronObligations,
                new RecurringJobOptions { TimeZone = tz, QueueName = "maintenance" }
            );

            // Job recurrente: revisión periódica de contratos expirados
            if (configuration.GetValue<bool>("Contracts:Expiration:Enabled"))
            {
                var cronContracts = configuration["Contracts:Expiration:Cron"] ?? "*/10 * * * *"; // Cada 10 minutos
                RecurringJob.AddOrUpdate<ContractJobs>(
                    "contracts-expiration",
                    j => j.RunExpirationSweepAsync(CancellationToken.None),
                    cronContracts,
                    new RecurringJobOptions { TimeZone = tz, QueueName = "default" }
                );
            }

            return app;
        }
    }
}
