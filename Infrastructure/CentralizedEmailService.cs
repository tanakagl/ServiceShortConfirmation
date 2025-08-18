using System.Net.Http.Headers;
using System.Text;
using AlertaBoletaService.Provider;
using Microsoft.Extensions.Configuration;

namespace AlertaBoletaService.Infrastructure
{
    public class CentralizedEmailService(
        ILogger<CentralizedEmailService> logger,
        HttpClient httpClient,
        ParametrizacaoRepository parametrizacaoRepository,
        IConfiguration configuration)
    {
        private readonly ILogger<CentralizedEmailService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly ParametrizacaoRepository _parametrizacaoRepository = parametrizacaoRepository;
        private readonly IConfiguration _configuration = configuration;

        public async Task<bool> SendEmailAsync(Email email)
        {
            try
            {
                var apiUrl = await _parametrizacaoRepository.GetParametrizacaoAsync("URL_API_EMAIL");
                if (string.IsNullOrEmpty(apiUrl))
                {
                    _logger.LogError("URL_API_EMAIL not found in database configuration");
                    return false;
                }

                var requestUrl = $"{apiUrl}v1/email/enviar";   
                _logger.LogInformation($"Sending email via API ({apiUrl}) to: {email.To}");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                var dictionary = new Dictionary<string, string>
                {
                    ["From"] = email.From ?? "noreply@amaggi.com.br",
                    ["To"] = email.To ?? "",
                    ["Subject"] = email.Subject ?? "",
                    ["Body"] = email.Body ?? ""
                };

                if (!string.IsNullOrEmpty(email.CC))
                    dictionary["CC"] = email.CC;
                
                if (!string.IsNullOrEmpty(email.CCO))
                    dictionary["CCO"] = email.CCO;
                
                if (email.DataAgendamento.HasValue)
                    dictionary["DataAgendamento"] = email.DataAgendamento.Value.ToString("yyyy-MM-dd HH:mm:ss");

                if (email.Attachments?.Count > 0)
                    dictionary["Attachments"] = string.Join(",", email.Attachments);

                httpRequest.Content = new FormUrlEncodedContent(dictionary);

                var apiUser = _configuration.GetValue<string>("ApiSettings:User");
                var apiPassword = _configuration.GetValue<string>("ApiSettings:Password");
                
                if (!string.IsNullOrEmpty(apiUser) && !string.IsNullOrEmpty(apiPassword))
                {
                    var basicCredential = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiUser}:{apiPassword}"));
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredential);
                }

                using var httpResponse = await _httpClient.SendAsync(httpRequest);
                string bodyResponse = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully via API to: {email.To}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send email via API. Status: {httpResponse.StatusCode}, Response: {bodyResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email via API to: {email.To}");
                return false;
            }
        }
    }
} 