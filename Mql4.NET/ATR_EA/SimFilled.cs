using System;
using NQuotes;

namespace biiuse
{
    internal class SimFilled : OrderState
    {
        public SimFilled(SimOrder context, MqlApi mql4) : base(context, mql4)
        {
            this.context = context;
        }


        public override void update()
        {
            if (context.OrderType == OrderType.BUY)
            {
                if (mql4.Bid <= context.StopLoss)
                {
                    context.ClosePrice = mql4.Bid;
                    context.CloseTime = mql4.TimeCurrent();
                    context.OrderType = OrderType.FINAL;
                    context.State = new Final(context, mql4);
                    if (context.EntryPrice < mql4.Bid)
                    {
                        context.Profit = 100;
                        context.Trade.setRealizedPL(100);
                    }
                    else
                    {
                        context.Profit = -100;
                        context.Trade.setRealizedPL(-100);
                    }


                }
            }
            if (context.OrderType == OrderType.SELL)
            {
                if (mql4.Ask >= context.StopLoss)
                {
                    context.ClosePrice = mql4.Ask;
                    context.CloseTime = mql4.TimeCurrent();
                    context.OrderType = OrderType.FINAL;
                    context.State = new Final(context, mql4);
                    if (context.EntryPrice > mql4.Ask)
                    {
                        context.Profit = 100;
                        context.Trade.setRealizedPL(100);
                    }
                }
                else
                {
                    context.Profit = -100;
                    context.Trade.setRealizedPL(-100);
                }
            }
        }



        new private SimOrder context;
    }
}