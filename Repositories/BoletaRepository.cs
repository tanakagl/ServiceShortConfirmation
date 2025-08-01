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
                        b.DS_STATUS_BOLETA,
                        b.DT_BOLETA,
                        b.DT_CONTRATO,
                        b.NR_VALOR_TOTAL_CONTRATO,
                        p.DS_PRODUTO as NOME_PRODUTO,
                        TRUNC(SYSDATE - b.DT_BOLETA) as DIAS_PENDENTES
                    FROM OPUS.MV_BOLETA b
                    LEFT JOIN OPUS.MV_PRODUTO p ON p.ID_PRODUTO = b.ID_PRODUTO
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
                        DataVencimento = reader.IsDBNull("DT_BOLETA") ? DateTime.MinValue : reader.GetDateTime("DT_BOLETA"),
                        StatusAtual = reader.IsDBNull("DS_STATUS_BOLETA") ? "RP" : reader.GetString("DS_STATUS_BOLETA"),
                        DiasVencidos = reader.IsDBNull("DIAS_PENDENTES") ? 0 : reader.GetInt32("DIAS_PENDENTES"),
                        ValorBoleta = reader.IsDBNull("NR_VALOR_TOTAL_CONTRATO") ? 0 : reader.GetDecimal("NR_VALOR_TOTAL_CONTRATO"),
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
                    _logger.LogWarning("Nenhum email encontrado. Executando query de debug...");
                    
                    command.CommandText = @"
                        SELECT COUNT(DISTINCT u.ID_USUARIO) as QTD_USUARIOS
                        FROM OPUS.MC_USUARIO u
                        INNER JOIN OPUS.MC_USUARIO_PERFIL up ON up.ID_USUARIO = u.ID_USUARIO
                        INNER JOIN OPUS.MC_PERFIL_FUNCAO_EVENTO pfe ON pfe.ID_PERFIL = up.ID_PERFIL
                        WHERE pfe.ID_EVENTO = 27";
                        
                    var totalUsuarios = Convert.ToInt32(await command.ExecuteScalarAsync());
                    _logger.LogInformation($"Total de usuários com permissão: {totalUsuarios}");
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