using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvestmentManager.Models.Enums;

namespace InvestmentManager.Models
{
    public class Asset
    {
        [Key]
        [MaxLength(12)]       
        public string IsinCode { get; set; } = string.Empty;

        [Required]
        [MaxLength (14)]
        public string Ticker { get; set; } = string.Empty;

        [Required]
        public AssetType Type { get; set; }

        [Required]
        [Range(0.01, 10000.00)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentPrice { get; set ; }
    }
}