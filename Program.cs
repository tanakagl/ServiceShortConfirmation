using AlertaBoletaService.Services;
using AlertaBoletaService.BD;
using AlertaBoletaService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AlertaBoletaService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AlertaDbContext>(options =>
                    {
                        var connectionString = hostContext.Configuration.GetConnectionString("OracleConnection");
                        options.UseOracle(connectionString);
                    });
                    services.AddScoped<IBoletaRepository, BoletaRepository>();
                    
                    services.AddHttpClient();
                    services.AddScoped<Infrastructure.ParametrizacaoRepository>();
                    services.AddScoped<Infrastructure.CentralizedEmailService>();
                    services.AddScoped<IEmailService, EmailService>();
                    
                    services.AddHostedService<AlertaWorkerService>();
                    
                    services.Configure<HostOptions>(opts => 
                    {
                        opts.ShutdownTimeout = TimeSpan.FromSeconds(30);
                    });
                });
    }
}
