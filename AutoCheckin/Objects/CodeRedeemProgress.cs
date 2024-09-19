using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCheckin.Objects
{
    public class CodeRedeemProgress
    {
        public long valid;
        public long invalid;
        public long used;
        public long expired;
        public long skipped;
        public long unknown;

        public readonly long Total;
        public long Tried => valid + invalid + used + expired + skipped + unknown;
        public double Ratio => Total == 0 ? 0 : (Tried / (double)Total);
        public CodeRedeemProgress(long total)
        {
            Total = total;
        }

        public string? Add(HoyoResponse hoyoResponse)
        {
            if (hoyoResponse.IsValidCode)
            {
                ++valid;
                return "Success!";
            }
            if (hoyoResponse.IsInvalidCode)
            {
                ++invalid;
                return "Invalid code.";
            }
            if (hoyoResponse.IsUsedCode)
            {
                ++used;
                return "Already used.";
            }
            if (hoyoResponse.IsExpiredCode)
            {
                ++expired;
                return "Expired.";
            }
            ++unknown;
            return null;
        }
    }
}
