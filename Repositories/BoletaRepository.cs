using AlertaBoletaService.Models;
using AlertaBoletaService.BD;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using AlertaBoletaService.Provider;

namespace AlertaBoletaService.Repositories
{
    public interface IBoletaRepository
    {
        Task<List<BoletaReaprovacao>> ObterBoletasReaprovacaoAsync(int empresaId);
        Task<List<ParametroEmpresa>> ObterEmpresasAtivasAsync();
        Task<List<string>> ObterEmailsUsuariosPermissaoAsync(int empresaId);
        Task AtualizarUltimaExecucaoAsync(int empresaId);
        Task<ParametroEmpresa?> ObterParametroEmpresaAsync(int empresaId);
    }

    public class BoletaRepository(AlertaDbContext context, ILogger<BoletaRepository> logger) : IBoletaRepository
    {
        private readonly AlertaDbContext _context = context;
        private readonly ILogger<BoletaRepository> _logger = logger;
        
        public async Task<List<BoletaReaprovacao>> ObterBoletasReaprovacaoAsync(int empresaId)
        {
            var boletas = new List<BoletaReaprovacao>();
            
            try
            {
                var connectionString = Configuration.GetValue<string>("ConnectionStrings:OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT 
                    b.ID_BOLETA,
                    b.NR_BOLETA,
                    CASE 
                        WHEN b.DS_STATUS_BOLETA = '5' THEN 'Pendente Reaprovação'
                        ELSE b.DS_STATUS_BOLETA 
                    END as DS_STATUS_BOLETA,
                    b.NR_VALOR_TOTAL_CONTRATO,
                    p.DS_PRODUTO as NOME_PRODUTO,
                    se.NOME_EMPRESA
                FROM OPUS.MV_BOLETA b
                LEFT JOIN OPUS.MV_PRODUTO p ON p.ID_PRODUTO = b.ID_PRODUTO
                LEFT JOIN OPUS.SIGAM_EMPRESA se ON se.COD_EMPRESA = b.CD_EMPRESA
                WHERE b.CD_EMPRESA = :empresaId
                AND b.DS_STATUS_BOLETA = '5'
                ORDER BY b.DT_BOLETA ASC";
                
                var parameter = new OracleParameter("empresaId", OracleDbType.Int32) { Value = empresaId };
                command.Parameters.Add(parameter);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    boletas.Add(new BoletaReaprovacao
                    {
                        NumeroBoleta = reader.IsDBNull("NR_BOLETA") ? "" : reader.GetString("NR_BOLETA"),
                        NumeroContrato = reader.IsDBNull("ID_BOLETA") ? "" : reader.GetInt32("ID_BOLETA").ToString(),
                        NomeProduto = reader.IsDBNull("NOME_PRODUTO") ? "N/I" : reader.GetString("NOME_PRODUTO"),
                        StatusAtual = reader.IsDBNull("DS_STATUS_BOLETA") ? "RP" : reader.GetString("DS_STATUS_BOLETA"),
                        ValorBoleta = reader.IsDBNull("NR_VALOR_TOTAL_CONTRATO") ? 0 : reader.GetDecimal("NR_VALOR_TOTAL_CONTRATO"),
                        NomeEmpresa = reader.IsDBNull("NOME_EMPRESA") ? "" : reader.GetString("NOME_EMPRESA"),
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar boletas para empresa {empresaId}");
            }

            return boletas;
        }

        public async Task<List<ParametroEmpresa>> ObterEmpresasAtivasAsync()
        {
            return await _context.ParametroEmpresas
                .Where(p => p.FlagNotificaBoleta == "S")
                .ToListAsync();
        }

       public async Task<List<string>> ObterEmailsUsuariosPermissaoAsync(int empresaId)
        {
            var emails = new List<string>();
            
            try
            {
                var connectionString = Configuration.GetValue<string>("ConnectionStrings:OracleConnection");
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
                
                _logger.LogInformation($"Encontrados {emails.Count} emails de usuários com permissão para aprovar boletas");
                
                if (emails.Count == 0)
                {
                    _logger.LogWarning("Nenhum email encontrado para receber alertas");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar emails de usuários com permissão para aprovar boletas");
            }

            emails.Clear();
            emails.Add("matheo.bonucia@amaggi.com.br");

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
    }
} 