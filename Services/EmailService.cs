using AlertaBoletaService.Models;
using AlertaBoletaService.Infrastructure;
using AlertaBoletaService.Provider;

namespace AlertaBoletaService.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailAsync(AlertaEmail email);
        string GerarCorpoEmailBoletas(List<BoletaReaprovacao> boletas);
    }

    public class EmailService(ILogger<EmailService> logger, CentralizedEmailService centralizedEmailService) : IEmailService
    {
        private readonly ILogger<EmailService> _logger = logger;
        private readonly CentralizedEmailService _centralizedEmailService = centralizedEmailService;

        public async Task<bool> EnviarEmailAsync(AlertaEmail email)
        {
            try
            {
                var emailFrom = Configuration.GetValue<string>("EmailSettings:FromEmail");
                
                var centralizedEmail = new Email(
                    from: emailFrom,
                    to: email.Para,
                    subject: email.Assunto,
                    body: email.Corpo
                );

                bool sucesso = await _centralizedEmailService.SendEmailAsync(centralizedEmail);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Email enviado com sucesso via API centralizada para: {email.Para}");
                }
                else
                {
                    _logger.LogError($"Falha ao enviar email via API centralizada para: {email.Para}");
                }

                return sucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email via API centralizada para: {email.Para}");
                return false;
            }
        }

        public string GerarCorpoEmailBoletas(List<BoletaReaprovacao> boletas)
        {
            var resumo = new
            {
                Total = boletas.Count,
                ValorTotal = boletas.Sum(b => b.ValorBoleta),
            };

            var tabelaBoletas = GerarTabelaBoletas(boletas);

            var mensagem = $@"
                <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin-bottom: 25px; border-left: 4px solid #ffc107;'>
                    <h3 style='color: #856404; margin: 0 0 10px 0;'>Resumo do Alerta</h3>
                    <p><strong>Total de boletas:</strong> {resumo.Total}</p>
                    <p><strong>Valor total:</strong> R$ {resumo.ValorTotal:N2}</p>
                </div>

                {tabelaBoletas}

                <div style='margin-top: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 5px;'>
                    <p style='color: #dc3545; font-weight: bold; margin: 0;'>AÇÃO NECESSÁRIA:</p>
                    <p style='margin: 5px 0 0 0;'>As boletas listadas acima estão pendentes de reaprovação e requerem atenção imediata.</p>
                    <p style='margin: 5px 0 0 0;'>Acesse o sistema Comex para processar estas aprovações.</p>
                </div>";

            var layoutEmail = new LayoutEmail(
                sistema: "AlertaBoleta - Comex",
                assunto: $"ATENÇÃO: {boletas.Count} boleta(s) pendente(s)",
                mensagem: mensagem
            );

            return layoutEmail.ToString();
        }

        private string GerarTabelaBoletas(List<BoletaReaprovacao> boletas)
        {
            var tabela = @"
                <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                    <thead>
                        <tr>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Nº Boleta</th>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Contrato</th>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Produto</th>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Valor</th>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Status</th>
                            <th style='background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold;'>Empresa</th>
                        </tr>
                    </thead>
                    <tbody>";

            foreach (var boleta in boletas)
            {

                tabela += $@"
                        <tr>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{boleta.NumeroBoleta}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{boleta.NumeroContrato}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{boleta.NomeProduto}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right; font-weight: bold;'>R$ {boleta.ValorBoleta:N2}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{boleta.StatusAtual}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{boleta.NomeEmpresa}</td>
                        </tr>";
            }

            tabela += @"
                    </tbody>
                </table>";

            return tabela;
        }
    }
} 