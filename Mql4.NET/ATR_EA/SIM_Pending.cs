using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;

namespace biiuse
{
    class SIM_Pending : OrderState
    {
        public SIM_Pending(SimOrder context, MqlApi mql4) : base(context, mql4)
        {
            this.context = context;
        }

        new private SimOrder context;
        public override void update()
        {
            if (context.OrderType == OrderType.BUY_LIMIT)
            {
                if (mql4.Ask <= context.EntryPrice)
                {
                    context.OpenPrice = mql4.Ask;
                    context.OrderType = OrderType.BUY;
                    context.State = new SimFilled(context, mql4);
                }
            }

            if (context.OrderType == OrderType.BUY_STOP) { 
                if (mql4.Ask >= context.EntryPrice)
                {
                    context.OpenPrice = mql4.Ask;
                    context.OrderType = OrderType.BUY;
                    context.State = new SimFilled(context, mql4);
                }
            }

            if (context.OrderType == OrderType.SELL_LIMIT)
            {
                if (mql4.Bid >= context.EntryPrice)
                {
                    context.OpenPrice = mql4.Bid;
                    context.OrderType = OrderType.SELL;
                    context.State = new SimFilled(context, mql4);
                }
            }

            if (context.OrderType == OrderType.SELL_STOP)
            {
                if (mql4.Bid <= context.EntryPrice)
                {
                    context.OpenPrice = mql4.Ask;
                    context.OrderType = OrderType.SELL;
                    context.State = new SimFilled(context, mql4);
                }
            }



        }

        
            
    }
}
