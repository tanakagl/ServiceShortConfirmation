namespace AlertaBoletaService.Models
{
    public class AlertaEmail
    {
        public string Para { get; set; } = string.Empty;
        public string Assunto { get; set; } = string.Empty;
        public string Corpo { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> Anexos { get; set; } = [];
    }
    
    public class BoletaReaprovacao
    {
        public string NumeroBoleta { get; set; } = string.Empty;
        public string NumeroContrato { get; set; } = string.Empty;
        public string NomeProduto { get; set; } = string.Empty;
        public string StatusAtual { get; set; } = string.Empty;
        public decimal ValorBoleta { get; set; }
        public string NomeEmpresa { get; set; } = string.Empty;
    }
} 