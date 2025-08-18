using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

namespace AlertaBoletaService.Infrastructure
{
    public class ParametrizacaoRepository(ILogger<ParametrizacaoRepository> logger, IConfiguration configuration)
    {
        private readonly ILogger<ParametrizacaoRepository> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        public async Task<string?> GetParametrizacaoAsync(string chave)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT DS_VALOR FROM OPUS.MV_PARAMETRIZACAO WHERE DS_CHAVE = :DSCHAVE";

                var parameter = new OracleParameter("DSCHAVE", OracleDbType.Varchar2) { Value = chave };
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                
                string? valor = result?.ToString();
                _logger.LogInformation($"Configuration '{chave}': {valor ?? "not found"}");
                
                return valor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching configuration '{chave}'");
                return null;
            }
        }
    }
} 