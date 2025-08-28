using AlertaBoletaService.Models;
using AlertaBoletaService.Repositories;
using AlertaBoletaService.Provider;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

namespace AlertaBoletaService.Services
{
    public class AlertaWorkerService : BackgroundService
    {
        private readonly ILogger<AlertaWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IConfiguration _configuration;
        
        public AlertaWorkerService(
            ILogger<AlertaWorkerService> logger,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime appLifetime,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appLifetime = appLifetime;
            _configuration = configuration;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection") ?? "Configuration not found";
                _logger.LogInformation("Short Confirmation Alert Service started - Single execution");
                

                
                _logger.LogInformation("Testing Oracle connection...");
                
                if (!await TestarConexaoAsync(connectionString))
                {
                    _logger.LogError("Connection test failed. Stopping service.");
                    Environment.ExitCode = 1;
                    return;
                }
                
                _logger.LogInformation("Oracle connection OK. Starting processing...");
                
                await ProcessarAlertasAsync();
                
                _logger.LogInformation("Execution completed. Stopping service...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during service execution");
                Environment.ExitCode = 1;
            }
            finally
            {
                _appLifetime.StopApplication();
            }
        }
        
        private async Task<bool> TestarConexaoAsync(string connectionString)
        {
            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT SYSDATE, USER FROM DUAL";
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var data = reader.GetDateTime(0);
                    var usuario = reader.GetString(1);
                    _logger.LogInformation($"Server date: {data:dd/MM/yyyy HH:mm:ss}");
                    _logger.LogInformation($"Connected user: {usuario}");
                }
                
                await connection.CloseAsync();
                return true;
            }
            catch (OracleException ex)
            {
                _logger.LogError($"Oracle error ({ex.Number}): {ex.Message}");
                
                switch (ex.Number)
                {
                    case 1017:
                        _logger.LogError("Invalid username/password");
                        break;
                    case 12154:
                        _logger.LogError("TNS: Could not resolve connection identifier");
                        break;
                    case 12514:
                        _logger.LogError("TNS: listener does not recognize the requested service");
                        break;
                    case 12541:
                        _logger.LogError("TNS: no listener - check if Oracle is running");
                        break;
                    case 12170:
                    case 12571:
                        _logger.LogError("TNS: connect timeout - network or firewall issue");
                        break;
                    default:
                        _logger.LogError("Check host, port, SID and credentials");
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General connection error: {ex.Message}");
                return false;
            }
        }
        
        private async Task ProcessarAlertasAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var boletaRepo = scope.ServiceProvider.GetRequiredService<IBoletaRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            _logger.LogInformation("Starting alert processing...");
            
            var empresasAtivas = await boletaRepo.ObterEmpresasAtivasAsync();
            _logger.LogInformation($"Found {empresasAtivas.Count} companies with active alerts");
            
            int empresasProcessadas = 0;
            int emailsEnviados = 0;
            
            foreach (var empresa in empresasAtivas)
            {
                _logger.LogInformation($"Processing company {empresa.IdEmpresa}...");
                
                var resultadoProcessamento = await ProcessarEmpresaAsync(empresa, boletaRepo, emailService);
                empresasProcessadas++;
                
                if (resultadoProcessamento)
                {
                    emailsEnviados++;
                }
            }
            
            _logger.LogInformation($"Processing completed - {empresasProcessadas} companies processed, {emailsEnviados} emails sent");
        }
        
        private async Task<bool> ProcessarEmpresaAsync(
            ParametroEmpresa empresa, 
            IBoletaRepository boletaRepo, 
            IEmailService emailService)
        {
            try
            {
                var emailsUsuariosPermissao = await boletaRepo.ObterEmailsUsuariosPermissaoAsync(empresa.IdEmpresa);
                var emailsUsuariosParametrizados = await boletaRepo.ObterEmailsParametrizadosAsync(empresa.IdEmpresa);
                var todosEmails = new List<string>();
                todosEmails.AddRange(emailsUsuariosPermissao);
                todosEmails.AddRange(emailsUsuariosParametrizados);
                todosEmails.Add("gestao.riscos@amaggi.com.br");

                var boletas = await boletaRepo.ObterBoletasAsync(empresa.IdEmpresa);
                var boletasAprovacao = boletas.FindAll(b => b.StatusAtual == BoletaStatus.PendingApproval.ToDisplayString());
                var boletaReaprovacao = boletas.FindAll(b => b.StatusAtual == BoletaStatus.PendingReApproval.ToDisplayString());

                if (boletasAprovacao.Count > 0 || boletaReaprovacao.Count > 0)
                {
                    _logger.LogWarning($"Company {empresa.IdEmpresa}: Found {boletasAprovacao.Count} pending approval and {boletaReaprovacao.Count} pending re-approval short confirmation(s)");
                    
                    if (todosEmails.Count == 0)
                    {
                        _logger.LogWarning($"Company {empresa.IdEmpresa}: no email addresses configured for alerts");
                        await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                        return false;
                    }
                    
                    var emailBody = emailService.GerarCorpoEmailBoletas(boletas);

                    var emailsPara = string.Join(";", todosEmails);
                    
                    var email = new AlertaEmail
                    {
                        Para = emailsPara,
                        Assunto = $"ALERT: {boletasAprovacao.Count} pending approval, {boletaReaprovacao.Count} pending re-approval short confirmation(s)",
                        Corpo = emailBody
                    };
                    
                    if (await emailService.EnviarEmailAsync(email))
                    {
                        await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                        _logger.LogInformation($"Company {empresa.IdEmpresa}: alert sent successfully to {todosEmails.Count} recipient(s)");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"Company {empresa.IdEmpresa}: failed to send email");
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation($"Company {empresa.IdEmpresa}: no pending short confirmations");
                    await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing company {empresa.IdEmpresa}");
                return false;
            }
        }
    }
} 