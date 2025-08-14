using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaBoletaService.Models
{
    [Table("MV_PARAM_EMPRESA", Schema = "OPUS")]
    public class ParametroEmpresa
    {
        [Key]
        [Column("ID_PARAM_EMPRESA")]
        public int IdParamEmpresa { get; set; }
        
        [Column("ID_EMPRESA")]
        public int IdEmpresa { get; set; }
        
        [Column("IN_NOTIFICA_BOLETA_REAPROVACAO")]
        [MaxLength(1)]
        public string FlagNotificaBoleta { get; set; } = "N";
        
        [Column("DT_ULTIMA_EXECUCAO_BOLETA")]
        public DateTime? UltimaExecucaoBoleta { get; set; }
        
    }
} 