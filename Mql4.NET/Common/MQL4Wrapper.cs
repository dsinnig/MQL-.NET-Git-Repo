using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biiuse
{
    public class MQL4Wrapper
    {
        public MQL4Wrapper(NQuotes.MqlApi mql4)
        {
            this.mql4 = mql4;
        }
        protected NQuotes.MqlApi mql4;
    }
}
