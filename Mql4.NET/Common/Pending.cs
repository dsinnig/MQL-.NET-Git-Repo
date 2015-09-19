using System;
using NQuotes;

namespace biiuse
{
    internal class Pending : OrderState
    {
        public Pending(Order context, MqlApi mql4) : base(context, mql4)
        {
        }

        public override void update()
        {
            if (mql4.OrderSelect(context.OrderTicket, MqlApi.SELECT_BY_TICKET)) {
                if (mql4.OrderType() == MqlApi.OP_BUY)
                {
                    context.OrderType = OrderType.BUY;
                    context.State = new Filled(context, mql4);
                }

                if (mql4.OrderType() == MqlApi.OP_SELL)
                {
                    context.OrderType = OrderType.SELL;
                    context.State = new Filled(context, mql4);
                }
            }

            else context.OrderType = OrderType.FINAL;
        }
    }
}