using System.Linq;

namespace Cajonic.Services
{
    public static class NullableIntExtension
    {
        public static int? ToVisualNullableInt(this string input, int? oldNumber)
        {
            if (input.Length > 4)
            {
                return oldNumber;
            }
            
            if (int.TryParse(input, out int nullable))
            {
                if (nullable > 0)
                {
                    return nullable;
                }

                if (int.TryParse(new string(input.Where(char.IsDigit).ToArray()), out nullable))
                {
                    return nullable;
                }
            }

            if (int.TryParse(new string(input.Where(char.IsDigit).ToArray()), out nullable))
            {
                return nullable;
            }

            return null;
        }
        
        public static string NullableIntToString(this int? input)
        {
            return input.ToString();
        }
    }
}