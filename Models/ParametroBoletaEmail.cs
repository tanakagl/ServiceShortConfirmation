using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlertaBoletaService.Models
{
    [Table("MV_PARAM_BOLETAS_EMAIL", Schema = "OPUS")]
    public class ParametroBoletaEmail
    {
        [Key]
        [Column("ID_PARAM_BOLETAS_EMAIL")]
        public int IdParamBoletasEmail { get; set; }

        [Column("ID_EMPRESA")]
        public int? IdEmpresa { get; set; }

        [Column("DS_EMAIL")]
        public string? DsEmail { get; set; }

    }

}