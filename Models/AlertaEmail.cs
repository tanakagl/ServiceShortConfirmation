namespace AlertaBoletaService.Models
{
    public class AlertaEmail
    {
        public string Para { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> Anexos { get; set; } = new List<string>();
    }
    
    public class BoletaReaprovacao
    {
        public string NumeroBoleta { get; set; } = string.Empty;
        public string NumeroContrato { get; set; } = string.Empty;
        public string NomeProduto { get; set; } = string.Empty;
        public DateTime DataVencimento { get; set; }
        public string StatusAtual { get; set; } = string.Empty;
        public int DiasVencidos { get; set; }
        public decimal ValorBoleta { get; set; }
    }
} 