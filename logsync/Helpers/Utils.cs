using System;
using System.Collections.Generic;
using System.Text;

namespace LogSync
{
    public static class Utils
    {
        public static string FormatNum(long n)
        {
            if (n > 1_000_000_000)
                return String.Format("{0:F2}B", n / 1_000_000_000D);
            if (n > 1_000_000)
                return String.Format("{0:F2}M", n / 1_000_000D);
            if (n > 1000)
                return String.Format("{0:F0}K", n / 1_000D);
            return n.ToString();
        }
    }
}
