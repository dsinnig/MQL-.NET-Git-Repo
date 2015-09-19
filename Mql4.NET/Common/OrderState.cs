using System;
using System.Collections.Generic;
using System.Text;
using NQuotes;

namespace biiuse
{
    public abstract class OrderState : MQL4Wrapper
    {
        public OrderState(Order context, MqlApi mql4) : base(mql4)
        {
            this.context = context;
        }
        public abstract void update();
        protected Order context;
    }
}
