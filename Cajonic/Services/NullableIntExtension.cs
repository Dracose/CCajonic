using System;
using System.Collections.Generic;
using System.Text;

namespace Cajonic.Services
{
    public static class NullableIntExtension
    {
        public static int? ToNullableInt(this string s)
        {
            return int.TryParse(s, out int i) ? i : (int?)null;
        }
    }
}
