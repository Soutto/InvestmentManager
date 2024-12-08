using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentManager.Shared.Models
{
    public class Transaction
    {
        /// <summary>
        /// Unique identifier for this transaction.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Unique identifier of the user who owns this transaction.
        /// </summary>
        [Required]
        public string? UserId { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this transaction is a Buy (true) or Sell (false).
        /// </summary>
        [Required]
        public bool IsBuy { get; set; }

        /// <summary>
        /// The date and time when the transaction was executed.
        /// </summary>
        [Required]
        public DateTime TransactionDate { get; set; }

        /// <summary>
        /// The number of units (shares, tokens, etc.) involved in the transaction.
        /// </summary>
        [Required]
        [Range(0.00000001, 10000000)]
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// The price per unit of the asset at the time of the transaction.
        /// </summary>
        [Required]
        [Range(0.01, 100000000.00)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Additional costs associated with this transaction (e.g., fees, taxes).
        /// </summary>
        [Required]
        [Range(0.00, 100000000.00)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OtherCosts { get; set; }

        /// <summary>
        /// The total value of the transaction:
        /// - For Buy transactions: (Quantity * UnitPrice) + OtherCosts.
        /// - For Sell transactions: (Quantity * UnitPrice) - OtherCosts.
        /// This value is computed dynamically and not stored in the database.
        /// </summary>
        [NotMapped]
        public decimal TotalValue 
        {     
            get
            {
                return IsBuy ? (Quantity * UnitPrice) + OtherCosts
                             : (Quantity * UnitPrice) - OtherCosts;
            } 
        }

        /// <summary>
        /// The date and time when this transaction record was created.
        /// </summary>
        [Required]
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// The ISIN code of the asset involved in the transaction.
        /// </summary>
        [Required]
        public string? AssetIsinCode { get; set; }

        /// <summary>
        /// The asset involved in the transaction, represented as a foreign key relationship.
        /// </summary>
        [ForeignKey("AssetIsinCode")]
        public Asset? Asset { get; set; }

        /// <summary>
        /// The user who owns this transaction, represented as a foreign key relationship.
        /// </summary>
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}