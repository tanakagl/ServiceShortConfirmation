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
                    _logger.LogInformation($"Email sent successfully via centralized API to: {email.Para}");
                }
                else
                {
                    _logger.LogError($"Failed to send email via centralized API to: {email.Para}");
                }

                return sucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email via centralized API to: {email.Para}");
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
                <div style='background-color: #E3E3E3; padding: 15px; border-radius: 5px; margin-bottom: 25px; border-left: 4px solid #555555;'>
                    <h3 style='font-style: bold; margin: 0 0 10px 0;'>Alert Summary</h3>
                    <p><strong>Total short confirmations:</strong> {resumo.Total}</p>
                    <p><strong>Total value:</strong> USD {resumo.ValorTotal:N2}</p>
                </div>

                {tabelaBoletas}

                <div style='margin-top: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 5px;'>
                    <p style='color: #dc3545; font-weight: bold; margin: 0;'>ACTION REQUIRED:</p>
                    <p style='margin: 5px 0 0 0;'>The short confirmations listed above are pending reapproval and require immediate attention.</p>
                    <p style='margin: 5px 0 0 0;'>Access the Comex system to process these approvals.</p>
                </div>";

            var layoutEmail = new LayoutEmail(
                sistema: "AlertaBoleta - Comex",
                assunto: $"ATTENTION: {boletas.Count} pending short confirmation(s)",
                mensagem: mensagem
            );

            return layoutEmail.ToString();
        }

        private static string GerarTabelaBoletas(List<BoletaReaprovacao> boletas)
        {
            var tabela = @"
                <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                    <thead>
                        <tr>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Short Confirmation #</th>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Contract</th>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Product</th>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Value</th>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Status</th>
                            <th style='color: white; padding: 12px; text-align: left; font-weight: bold;'>Company</th>
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
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right; font-weight: bold;'>USD {boleta.ValorBoleta:N2}</td>
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