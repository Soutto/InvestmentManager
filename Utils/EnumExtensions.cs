using System.ComponentModel.DataAnnotations;
using System.Reflection;
namespace InvestmentManager.Utils
{
    public static class EnumExtensions
    {
        public static string ToDescription<TEnum>(this TEnum EnumValue) where TEnum : struct
        {
            string? enumName = EnumValue.ToString();
            if (string.IsNullOrEmpty(enumName))
            {
                throw new ArgumentException("The provided enum value is invalid.", nameof(EnumValue));
            }

            var displayAttribute = EnumValue.GetType().GetField(enumName)?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? enumName;
        }

    }
}

