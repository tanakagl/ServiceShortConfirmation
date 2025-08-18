using AlertaBoletaService.Repositories;
using System.Text.Json.Serialization;

namespace AlertaBoletaService.Infrastructure
{
    public class Email
    {
        public string From { get; private set; }
        public string To { get; private set; }
        public string Subject { get; private set; }
        public string Body { get; private set; }
        public string? CC { get; private set; }
        public string? CCO { get; private set; }
        public string User { get; private set; }
        public string Password { get; private set; }

        public IReadOnlyCollection<string> Attachments => _attachments;
        private readonly List<string> _attachments = [];

        public DateTime? DataAgendamento { get; set; }

        public Email(string from, string to, string subject, string body, string user, string password)
        {
            From = from;
            To = to;
            Subject = subject;
            Body = body;
            User = user;
            Password = password;

            string ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (ambiente == "Development")
            {
                To = "ti.comex@amaggi.com.br";
                Subject = $"[{ambiente}] {subject}";
            }
            else if (ambiente == "Homolog")
            {   
                Subject = $"[{ambiente}] {subject}";
            }
        }
    }
} 