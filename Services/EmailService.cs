using System.Net.Mail;
using System.Net;
using AlertaBoletaService.Models;
using AlertaBoletaService.Provider;
using System.Text;

namespace AlertaBoletaService.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailAsync(AlertaEmail email);
        string GerarCorpoEmailBoletas(List<BoletaReaprovacao> boletas, string nomeEmpresa);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> EnviarEmailAsync(AlertaEmail email)
        {
            try
            {
                var smtpHost = Configuration.GetValue<string>("EmailSettings:SmtpServer");
                var smtpPort = Configuration.GetValue<int>("EmailSettings:SmtpPort");
                var emailFrom = Configuration.GetValue<string>("EmailSettings:FromEmail");
                var emailSenha = Configuration.GetValue<string>("EmailSettings:FromPassword");
                var enableSsl = Configuration.GetValue<bool>("EmailSettings:EnableSsl");

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(emailFrom, emailSenha);

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(emailFrom, "Sistema COMEX - Alertas");
                
                var emails = email.Para.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var emailDestino in emails)
                {
                    if (!string.IsNullOrWhiteSpace(emailDestino))
                    {
                        mailMessage.To.Add(emailDestino.Trim());
                    }
                }

                mailMessage.Subject = email.Assunto;
                mailMessage.Body = email.Corpo;
                mailMessage.IsBodyHtml = email.IsHtml;
                mailMessage.Priority = MailPriority.High;

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email enviado com sucesso para: {email.Para}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email para: {email.Para}");
                return false;
            }
        }

        public string GerarCorpoEmailBoletas(List<BoletaReaprovacao> boletas, string nomeEmpresa)
        {
            var resumo = new
            {
                Total = boletas.Count,
                ValorTotal = boletas.Sum(b => b.ValorBoleta),
                MaisAntiga = boletas.OrderBy(b => b.DataVencimento).FirstOrDefault()?.DataVencimento,
                MaisRecente = boletas.OrderByDescending(b => b.DataVencimento).FirstOrDefault()?.DataVencimento
            };

            var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
                        .container {{ max-width: 800px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; border-bottom: 3px solid #dc3545; padding-bottom: 20px; margin-bottom: 30px; }}
                        .header h1 {{ color: #dc3545; margin: 0; font-size: 24px; }}
                        .summary {{ background-color: #fff3cd; padding: 15px; border-radius: 5px; margin-bottom: 25px; border-left: 4px solid #ffc107; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                        th {{ background-color: #dc3545; color: white; padding: 12px; text-align: left; font-weight: bold; }}
                        td {{ padding: 10px; border-bottom: 1px solid #ddd; }}
                        tr:nth-child(even) {{ background-color: #f9f9f9; }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; font-size: 12px; }}
                        .urgent {{ color: #dc3545; font-weight: bold; }}
                        .value {{ text-align: right; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>ALERTA: Boletas Pendentes em Reaprovação</h1>
                            <p><strong>{nomeEmpresa}</strong></p>
                        </div>
                        
                        <div class='summary'>
                            <h3>Resumo do Alerta</h3>
                            <p><strong>Total de boletas:</strong> {resumo.Total}</p>
                            <p><strong>Valor total:</strong> R$ {resumo.ValorTotal:N2}</p>
                            <p><strong>Período:</strong> {resumo.MaisAntiga:dd/MM/yyyy} até {resumo.MaisRecente:dd/MM/yyyy}</p>
                        </div>

                        <table>
                            <thead>
                                <tr>
                                    <th>Nº Boleta</th>
                                    <th>Contrato</th>
                                    <th>Produto</th>
                                    <th>Data</th>
                                    <th>Dias Pendentes</th>
                                    <th>Valor</th>
                                    <th>Status</th>
                                </tr>
                            </thead>
                            <tbody>";
            
            foreach (var boleta in boletas)
            {
                var corLinha = boleta.DiasVencidos > 30 ? "background-color: #ffebee;" : "";
                var classeUrgente = boleta.DiasVencidos > 30 ? "urgent" : "";
                
                html += $@"
                            <tr style='{corLinha}'>
                                <td>{boleta.NumeroBoleta}</td>
                                <td>{boleta.NumeroContrato}</td>
                                <td>{boleta.NomeProduto}</td>
                                <td>{boleta.DataVencimento:dd/MM/yyyy}</td>
                                <td class='{classeUrgente}'><strong>{boleta.DiasVencidos} dias</strong></td>
                                <td class='value'>R$ {boleta.ValorBoleta:N2}</td>
                                <td>{boleta.StatusAtual}</td>
                            </tr>";
            }
            
            html += @"
                        </tbody>
                    </table>
                    
                    <div class='footer'>
                        <p><i>Este é um email automático do Sistema COMEX - Alerta de Boletas.</i></p>
                        <p><i>Para dúvidas, entre em contato com o suporte técnico.</i></p>
                    </div>
                    </div>
                </body>
                </html>";

            return html;
        }
    }
} 