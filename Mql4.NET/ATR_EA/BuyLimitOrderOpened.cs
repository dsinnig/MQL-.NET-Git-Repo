using System;
using NQuotes;

namespace biiuse
{
    internal class BuyLimitOrderOpened : TradeState
    {
        private ATRTrade context; //hides context in Trade

        public BuyLimitOrderOpened(ATRTrade aContext, MqlApi mql4) : base(mql4)
        {
            this.context = aContext;
        }
        public override void update()
        {
            if (mql4.Ask > context.getCancelPrice())
            {
                context.addLogEntry(true, "Ask price went above cancel level");

                ErrorType result = context.Order.deleteOrder();

                if (result == ErrorType.NO_ERROR)
                {
                    context.addLogEntry("Order deleted successfully", true);
                    context.setState(new TradeClosed(context, mql4));
                    return;
                }

                if (result == ErrorType.RETRIABLE_ERROR)
                {
                    context.addLogEntry("Order could not be deleted. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + " Will re-try at next tick.", true);
                    return;
                }

                if (result == ErrorType.NON_RETRIABLE_ERROR)
                {
                    ///review the logic of this 
                    context.addLogEntry("Order could not be deleted. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + " Trade will close", true);
                    context.setState(new TradeClosed(context, mql4));
                    return;
                }
            }

            if (context.Order.OrderType == OrderType.BUY)
            {
                context.addLogEntry(true, "Order got filled at price: " + mql4.DoubleToStr(context.Order.getOrderOpenPrice(), mql4.Digits));
                context.setActualEntry(context.Order.getOrderOpenPrice());
                context.setState(new BuyOrderFilledProfitTargetNotReached(context, mql4));
                return;
            }
        }
    }
}