using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biiuse
{
    public class RoyalZigZagTrade : Trade
    {
        public RoyalZigZagTrade(int _lotDigits, string _logFileName, NQuotes.MqlApi mql4) : base(false, _lotDigits, _logFileName, mql4)
        {
        }

    };
}
