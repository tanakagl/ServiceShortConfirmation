using AlertaBoletaService.Provider;
using Oracle.ManagedDataAccess.Client;

namespace AlertaBoletaService.Infrastructure
{
    public class ParametrizacaoRepository(ILogger<ParametrizacaoRepository> logger)
    {
        private readonly ILogger<ParametrizacaoRepository> _logger = logger;

        public async Task<string?> GetParametrizacaoAsync(string chave)
        {
            try
            {
                var connectionString = Configuration.GetValue<string>("ConnectionStrings:OracleConnection");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT DS_VALOR FROM OPUS.MV_PARAMETRIZACAO WHERE DS_CHAVE = :DSCHAVE";

                var parameter = new OracleParameter("DSCHAVE", OracleDbType.Varchar2) { Value = chave };
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                
                string? valor = result?.ToString();
                _logger.LogInformation($"Parametrização '{chave}': {valor ?? "não encontrada"}");
                
                return valor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar parametrização '{chave}'");
                return null;
            }
        }
    }
} 