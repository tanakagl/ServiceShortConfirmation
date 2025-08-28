using AlertaBoletaService.Models;
using AlertaBoletaService.Infrastructure;
using AlertaBoletaService.Provider;
using Microsoft.Extensions.Configuration;

namespace AlertaBoletaService.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailAsync(AlertaEmail email);
        string GerarCorpoEmailBoletas(List<BoletaReaprovacao> boletas);
    }

    public class EmailService(ILogger<EmailService> logger, CentralizedEmailService centralizedEmailService, IConfiguration configuration) : IEmailService
    {
        private readonly ILogger<EmailService> _logger = logger;
        private readonly CentralizedEmailService _centralizedEmailService = centralizedEmailService;
        private readonly IConfiguration _configuration = configuration;
        public async Task<bool> EnviarEmailAsync(AlertaEmail email)
        {
            try
            {
                var emailFrom = _configuration.GetValue<string>("EmailSettings:FromEmail") ?? "noreply@amaggi.com.br";
                var user = _configuration.GetValue<string>("ApiSettings:User") ?? "NoUser";
                var password = _configuration.GetValue<string>("ApiSettings:Password") ?? "NoPassword";
                
                var centralizedEmail = new Email(
                    from: emailFrom,
                    to: email.Para,
                    subject: email.Assunto,
                    body: email.Corpo,
                    user: user,
                    password: password
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
            var boletasAprovacao = boletas.FindAll(b => b.StatusAtual == BoletaStatus.PendingApproval.ToDisplayString());
            var boletasReaprovacao = boletas.FindAll(b => b.StatusAtual == BoletaStatus.PendingReApproval.ToDisplayString());

            var resumo = new
            {
                Total = boletas.Count,
                TotalAprovacao = boletasAprovacao.Count,
                TotalReaprovacao = boletasReaprovacao.Count,
                TotalAprovacaoValor = boletasAprovacao.Sum(b => b.ValorBoleta),
                TotalReaprovacaoValor = boletasReaprovacao.Sum(b => b.ValorBoleta),
            };

            var tabelaBoletas = "";
            
            if (boletasAprovacao.Count > 0)
            {
                tabelaBoletas += GerarTabelaBoletas(boletasAprovacao, "approval");
            }

            if (boletasReaprovacao.Count > 0)
            {
                if (tabelaBoletas.Length > 0) tabelaBoletas += "\n";
                tabelaBoletas += GerarTabelaBoletas(boletasReaprovacao, "re-approval");
            }

            var mensagem = $@"
                <div style='background-color: #E3E3E3; padding: 15px; border-radius: 5px; margin-bottom: 25px; border-left: 4px solid #555555;'>
                    <h3 style='font-style: bold; margin: 0 0 10px 0;'>Alert Summary</h3>
                    <p><strong>Total short confirmations in approval:</strong> {resumo.TotalAprovacao}</p>
                    <p><strong>Total short confirmations in re-approval:</strong> {resumo.TotalReaprovacao}</p>
                    <p><strong>Total value approval:</strong> USD {resumo.TotalAprovacaoValor:N2}</p>
                    <p><strong>Total value re-approval:</strong> USD {resumo.TotalReaprovacaoValor:N2}</p>
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

        private static string GerarTabelaBoletas(List<BoletaReaprovacao> boletas, string statusBoleta)
        {
            var headerText = statusBoleta.ToLower() == "approval"
                ? "PENDING APPROVAL SHORT CONFIRMATIONS"
                : "PENDING RE-APPROVAL SHORT CONFIRMATIONS";

            var tabela = $@"
                <div style='margin-top: 20px;'>
                    <div style='padding: 10px; border-radius: 5px 5px 0 0;'>
                        <h3 style='margin: 0; font-size: 16px;'>{headerText}</h3>
                    </div>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <thead>
                            <tr>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Short Confirmation #</th>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Contract</th>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Product</th>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Value</th>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Status</th>
                                <th style='padding: 12px; text-align: left; font-weight: bold;'>Company</th>
                            </tr>
                        </thead>
                        <tbody>";

            foreach (var boleta in boletas)
            {

                var rowStyle = "background-color: #f8f9fa;";
                tabela += $@"
                        <tr style='{(boletas.IndexOf(boleta) % 2 == 0 ? rowStyle : "")}'>
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
                </table>
                </div>";

            return tabela;
        }
    }
} 