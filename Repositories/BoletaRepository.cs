using AlertaBoletaService.Models;
using AlertaBoletaService.BD;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using AlertaBoletaService.Provider;
using Microsoft.Extensions.Configuration;

namespace AlertaBoletaService.Repositories
{
    public interface IBoletaRepository
    {
        Task<List<BoletaReaprovacao>> ObterBoletasAsync(int empresaId);
        Task<List<ParametroEmpresa>> ObterEmpresasAtivasAsync();
        Task<List<string>> ObterEmailsUsuariosPermissaoAsync(int empresaId);
        Task AtualizarUltimaExecucaoAsync(int empresaId);
        Task<ParametroEmpresa?> ObterParametroEmpresaAsync(int empresaId);
        Task<List<string>> ObterEmailsParametrizadosAsync(int? empresaId);
    }

    public class BoletaRepository(AlertaDbContext context, ILogger<BoletaRepository> logger, IConfiguration configuration) : IBoletaRepository
    {
        private readonly AlertaDbContext _context = context;
        private readonly ILogger<BoletaRepository> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        

        
        public async Task<List<BoletaReaprovacao>> ObterBoletasAsync(int empresaId)
        {
            var boletas = new List<BoletaReaprovacao>();
            
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT 
                    b.ID_BOLETA,
                    mc.NR_CONTRATO,
                    b.NR_BOLETA,
                    CASE 
                        WHEN b.DS_STATUS_BOLETA = '5' THEN 'Pending Re-Approval'
                        WHEN b.DS_STATUS_BOLETA = '0' THEN 'Pending Approval'
                        ELSE b.DS_STATUS_BOLETA 
                    END as DS_STATUS_BOLETA,
                    b.NR_VALOR_TOTAL_CONTRATO,
                    p.DS_PRODUTO as NOME_PRODUTO,
                    se.NOME_EMPRESA
                FROM OPUS.MV_BOLETA b
                LEFT JOIN OPUS.MV_PRODUTO p ON p.ID_PRODUTO = b.ID_PRODUTO
                LEFT JOIN OPUS.SIGAM_EMPRESA se ON se.COD_EMPRESA = b.CD_EMPRESA
                LEFT JOIN OPUS.MV_CONTRATO mc ON mc.ID_BOLETA  = b.ID_BOLETA 
                WHERE b.CD_EMPRESA = :empresaId
                AND b.DS_STATUS_BOLETA IN ('5', '0')
                ORDER BY b.DT_BOLETA ASC";
                
                var parameter = new OracleParameter("empresaId", OracleDbType.Int32) { Value = empresaId };
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    boletas.Add(new BoletaReaprovacao
                    {
                        NumeroBoleta = reader.IsDBNull("NR_BOLETA") ? "" : reader.GetValue("NR_BOLETA")?.ToString() ?? "",
                        NumeroContrato = reader.IsDBNull("NR_CONTRATO") ? "" : reader.GetString("NR_CONTRATO"),
                        NomeProduto = reader.IsDBNull("NOME_PRODUTO") ? "N/I" : reader.GetString("NOME_PRODUTO"),
                        StatusAtual = reader.IsDBNull("DS_STATUS_BOLETA") ? "RP" : reader.GetString("DS_STATUS_BOLETA"),
                        ValorBoleta = reader.IsDBNull("NR_VALOR_TOTAL_CONTRATO") ? 0 : reader.GetDecimal("NR_VALOR_TOTAL_CONTRATO"),
                        NomeEmpresa = reader.IsDBNull("NOME_EMPRESA") ? "" : reader.GetString("NOME_EMPRESA"),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching short confirmations for company {empresaId}");
            }

            return boletas;
        }

        public async Task<List<ParametroEmpresa>> ObterEmpresasAtivasAsync()
        {
            _logger.LogInformation("Fetching active companies...");
            
            var empresas = await _context.ParametroEmpresas
                .Where(p => p.FlagNotificaBoleta == "S")
                .ToListAsync();

            _logger.LogInformation($"Found {empresas.Count} companies with active alerts");
            
            return empresas;
        }

       public async Task<List<string>> ObterEmailsUsuariosPermissaoAsync(int empresaId)
        {
            var emails = new List<string>();
            
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                
                command.CommandText = @"
                    SELECT DISTINCT u.DS_EMAIL
                    FROM OPUS.MC_USUARIO u
                    INNER JOIN OPUS.MC_USUARIO_PERFIL up ON up.ID_USUARIO = u.ID_USUARIO
                    INNER JOIN OPUS.MC_PERFIL_FUNCAO_EVENTO pfe ON pfe.ID_PERFIL = up.ID_PERFIL
                    WHERE pfe.ID_EVENTO = 27
                    AND u.DS_EMAIL IS NOT NULL
                    ORDER BY u.DS_EMAIL";

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var email = reader.GetString("DS_EMAIL");
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        emails.Add(email.Trim());
                    }
                }
                
                _logger.LogInformation($"Found {emails.Count} user emails with permission to approve short confirmations");
                
                if (emails.Count == 0)
                {
                    _logger.LogWarning("No emails found to receive alerts");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching user emails with permission to approve short confirmations");
            }

            return emails;
        }

        public async Task AtualizarUltimaExecucaoAsync(int empresaId)
        {
            var parametro = await _context.ParametroEmpresas
                .FirstOrDefaultAsync(p => p.IdEmpresa == empresaId);
                
            if (parametro != null)
            {
                parametro.UltimaExecucaoBoleta = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ParametroEmpresa?> ObterParametroEmpresaAsync(int empresaId)
        {
            return await _context.ParametroEmpresas
                .FirstOrDefaultAsync(p => p.IdEmpresa == empresaId);
        }

        public async Task<List<string>> ObterEmailsParametrizadosAsync(int? empresaId = null)
        {
            try
            {
                var query = _context.ParametroBoletaEmails.AsQueryable();
                
                if (empresaId.HasValue)
                {
                    query = query.Where(p => p.IdEmpresa == empresaId.Value);
                }
                
                var emailsFromDb = await query
                    .Where(p => !string.IsNullOrWhiteSpace(p.DsEmail))
                    .Select(p => p.DsEmail!.Trim())
                    .ToListAsync();
                
                var emailsIndividuais = new List<string>();
                foreach (var email in emailsFromDb)
                {
                    if (email.Contains(';'))
                    {
                        var emailsSeparados = email.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var emailSeparado in emailsSeparados)
                        {
                            var emailLimpo = emailSeparado.Trim();
                            if (!string.IsNullOrWhiteSpace(emailLimpo) && !emailsIndividuais.Contains(emailLimpo))
                            {
                                emailsIndividuais.Add(emailLimpo);
                            }
                        }
                    }
                    else
                    {
                        if (!emailsIndividuais.Contains(email))
                        {
                            emailsIndividuais.Add(email);
                        }
                    }
                }
                
                var emailsOrdenados = emailsIndividuais.OrderBy(email => email).ToList();
                
                _logger.LogInformation($"Found {emailsOrdenados.Count} parameterized emails" + 
                    (empresaId.HasValue ? $" for company {empresaId}" : " (all companies)"));
                
                return emailsOrdenados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching parameterized emails" + 
                    (empresaId.HasValue ? $" for company {empresaId}" : ""));
                return [];
            }
        }
    }
} 