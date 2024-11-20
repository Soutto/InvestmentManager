using InvestmentManager.Data;
using InvestmentManager.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentManager.Models
{
    public class Transaction
    {
        /// <summary>
        /// Unique identifier for the transaction.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user who owns this transaction.
        /// </summary>
        [Required]
        public string? UserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of transaction: Buy or Sell.
        /// </summary>
        [Required]
        public bool IsBuy { get; set; }

        /// <summary>
        /// Date and time when the transaction was executed.
        /// </summary>
        [Required]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// Ticker symbol of the security being transactioned.
        /// </summary>
        [Required]
        [MaxLength(14)]
        public string Ticker { get; set; } = string.Empty;

        /// <summary>
        /// Number of shares involved in the transaction.
        /// </summary>
        [Required]
        [Range  (1, 10000000)]
        public int Quantity { get; set; }

        /// <summary>
        /// Price per share of the security.
        /// </summary>
        [Required]
        [Range (0.01, 10000.00)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PricePerShare { get; set; }

        /// <summary>
        /// Other transaction costs.
        /// </summary>
        [Required]
        [Range(0.00, 100000000.00)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OtherCosts { get; set; }

        /// <summary>
        /// Total value of the transaction 
        /// Buys -> (Quantity * PricePerShare) + OtherCosts).
        /// Sells -> (Quantity * PricePerShare) - OtherCosts).
        /// </summary>
        [NotMapped]
        public decimal TotalValue 
        {     
            get
            {
                return IsBuy ? (Quantity * PricePerShare) + OtherCosts
                             : (Quantity * PricePerShare) - OtherCosts;
            } 
        }

        /// <summary>
        /// Date and time when the transaction was created
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// Optional: Notes or comments about the transaction.
        /// </summary>
        [MaxLength (500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Type of the asset being transacted (e.g., Stock, Cryptocurrency).
        /// </summary>
        [Required]
        AssetType Type { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
