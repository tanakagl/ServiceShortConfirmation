using AlertaBoletaService.Services;
using AlertaBoletaService.BD;
using AlertaBoletaService.Repositories;

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
                .ConfigureAppConfiguration((context, config) =>
                {
                   
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AlertaDbContext>();
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
