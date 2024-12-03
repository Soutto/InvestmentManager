using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System;
using InvestmentManager.Models.Enums;
namespace InvestmentManager.Utils
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this AssetType enumValue)
        {
            var displayAttribute = enumValue.GetType()
                                            .GetField(enumValue.ToString())
                                            ?.GetCustomAttribute<DisplayAttribute>();

            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}

