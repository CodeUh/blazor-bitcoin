using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBitcoin.Shared.Helpers
{
    public static class UnixTimeHelper
    {
        public static DateTime UnixTimeToDateTime(long unixTime)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
        }
    }

}
