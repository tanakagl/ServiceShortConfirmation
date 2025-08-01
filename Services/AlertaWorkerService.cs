using AlertaBoletaService.Models;
using AlertaBoletaService.Repositories;
using AlertaBoletaService.Provider;
using Oracle.ManagedDataAccess.Client;

namespace AlertaBoletaService.Services
{
    public class AlertaWorkerService : BackgroundService
    {
        private readonly ILogger<AlertaWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _appLifetime;
        
        public AlertaWorkerService(
            ILogger<AlertaWorkerService> logger,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _appLifetime = appLifetime;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var connectionString = Configuration.GetValue<string>("ConnectionStrings:OracleConnection");
                _logger.LogInformation("Serviço de Alerta de Boletas iniciado - Execução única");
                _logger.LogInformation($"Testando conexão Oracle...");
                
                if (!await TestarConexaoAsync(connectionString))
                {
                    _logger.LogError("Falha no teste de conexão. Finalizando serviço.");
                    Environment.ExitCode = 1;
                    return;
                }
                
                _logger.LogInformation("Conexão Oracle OK. Iniciando processamento...");
                
                await ProcessarAlertasAsync();
                
                _logger.LogInformation("Execução concluída. Finalizando serviço...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante execução do serviço");
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
                    _logger.LogInformation($"Data do servidor: {data:dd/MM/yyyy HH:mm:ss}");
                    _logger.LogInformation($"Usuário conectado: {usuario}");
                }
                
                await connection.CloseAsync();
                return true;
            }
            catch (OracleException ex)
            {
                _logger.LogError($"Erro Oracle ({ex.Number}): {ex.Message}");
                
                switch (ex.Number)
                {
                    case 1017:
                        _logger.LogError("Usuário/senha inválidos");
                        break;
                    case 12154:
                        _logger.LogError("TNS: Não foi possível resolver o identificador de conexão");
                        break;
                    case 12514:
                        _logger.LogError("TNS: listener não reconhece o serviço solicitado");
                        break;
                    case 12541:
                        _logger.LogError("TNS: sem listener - verifique se o Oracle está rodando");
                        break;
                    case 12170:
                    case 12571:
                        _logger.LogError("TNS: connect timeout - problema de rede ou firewall");
                        break;
                    default:
                        _logger.LogError("Verifique host, porta, SID e credenciais");
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro geral de conexão: {ex.Message}");
                return false;
            }
        }
        
        private async Task ProcessarAlertasAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var boletaRepo = scope.ServiceProvider.GetRequiredService<IBoletaRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            
            _logger.LogInformation("Iniciando processamento de alertas...");
            
            var empresasAtivas = await boletaRepo.ObterEmpresasAtivasAsync();
            _logger.LogInformation($"Encontradas {empresasAtivas.Count} empresas com alerta ativo");
            
            int empresasProcessadas = 0;
            int emailsEnviados = 0;
            
            foreach (var empresa in empresasAtivas)
            {
                _logger.LogInformation($"Processando empresa {empresa.IdEmpresa}...");
                
                var resultadoProcessamento = await ProcessarEmpresaAsync(empresa, boletaRepo, emailService);
                empresasProcessadas++;
                
                if (resultadoProcessamento)
                {
                    emailsEnviados++;
                }
            }
            
            _logger.LogInformation($"Processamento concluído - {empresasProcessadas} empresas processadas, {emailsEnviados} emails enviados");
        }
        
        private async Task<bool> ProcessarEmpresaAsync(
            ParametroEmpresa empresa, 
            IBoletaRepository boletaRepo, 
            IEmailService emailService)
        {
            try
            {
                var emailsUsuarios = await boletaRepo.ObterEmailsUsuariosPermissaoAsync(empresa.IdEmpresa);
                
                if (emailsUsuarios.Count == 0)
                {
                    _logger.LogWarning($"Empresa {empresa.IdEmpresa}: nenhum usuário com permissão para receber alertas encontrado");
                    await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                    return false;
                }
                
                var boletas = await boletaRepo.ObterBoletasReaprovacaoAsync(empresa.IdEmpresa);
                
                if (boletas.Count != 0)
                {
                    _logger.LogWarning($"Empresa {empresa.IdEmpresa}: {boletas.Count} boleta(s) em reaprovação encontradas");
                    
                    var emailBody = emailService.GerarCorpoEmailBoletas(boletas, $"Empresa {empresa.IdEmpresa}");
                    var emailsPara = string.Join(";", emailsUsuarios);
                    
                    var email = new AlertaEmail
                    {
                        Para = emailsPara,
                        Assunto = $"URGENTE: {boletas.Count} boleta(s) em reaprovação - Empresa {empresa.IdEmpresa}",
                        Corpo = emailBody
                    };
                    
                    if (await emailService.EnviarEmailAsync(email))
                    {
                        await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                        _logger.LogInformation($"Empresa {empresa.IdEmpresa}: alerta enviado com sucesso para {emailsUsuarios.Count} usuário(s)");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"Empresa {empresa.IdEmpresa}: falha ao enviar email");
                        return false;
                    }
                }
                else
                {
                    _logger.LogInformation($"Empresa {empresa.IdEmpresa}: nenhuma boleta pendente");
                    await boletaRepo.AtualizarUltimaExecucaoAsync(empresa.IdEmpresa);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar empresa {empresa.IdEmpresa}");
                return false;
            }
        }
    }
} 