using System.ComponentModel.DataAnnotations;

namespace InvestmentManager.Models.Enums
{
    public enum AssetType
    {
        [Display(Name = "Ação")]
        Stock = 0, // Ações

        [Display(Name = "ETF (Fundo de Índice)")]
        ETF = 1, // Fundos de Índice

        [Display(Name = "Criptomoeda")]
        Cryptocurrency = 2, // Criptomoedas

        [Display(Name = "Fundo Imobiliário")]
        RealEstateFund = 3, // Fundos Imobiliários (FIIs)

        [Display(Name = "Renda Fixa")]
        FixedIncome = 4, // Títulos de Renda Fixa

        [Display(Name = "Derivativo")]
        Derivative = 5, // Derivativos (ex: opções, futuros)

        [Display(Name = "Ação no Exterior")]
        ForeignStock = 6, // Ações listadas em bolsas internacionais

        [Display(Name = "Moeda")]
        Currency = 7, // Moedas (ex: dólar, euro)

        [Display(Name = "Fundo Multimercado")]
        MultiMarketFund = 8, // Fundos Multimercado

        [Display(Name = "Outro")]
        Other = 9 // Outros tipos de ativos não especificados
    }

}
