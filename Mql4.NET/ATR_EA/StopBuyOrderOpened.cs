using System;
using NQuotes;

namespace biiuse
{
    internal class StopBuyOrderOpened : TradeState
    {
        private ATRTrade context; 

    public StopBuyOrderOpened(ATRTrade aContext, MqlApi mql4) : base(mql4) {
        this.context = aContext;
    }

    public override void update()
    {
        if (mql4.Ask <= context.getCancelPrice())
        {
            context.addLogEntry("Ask price went below cancel level. Attempting to delete order.", true);

            //delete Order
            ErrorType result = OrderManager.deleteOrder(context.getOrderTicket(), context, mql4);

            if (result == ErrorType.NO_ERROR)
            {
                context.addLogEntry("Order deleted successfully", true);
                context.setState(new TradeClosed(context, mql4));
                return;
            }

            if (result == ErrorType.RETRIABLE_ERROR)
            {
                context.addLogEntry("Order could not be deleted. Will re-try at next tick.", true);
                return;
            }

            if (result == ErrorType.NON_RETRIABLE_ERROR)
            {
                context.addLogEntry("Order could not be deleted. Abort trade.", true);
                context.setState(new TradeClosed(context, mql4));
                return;
            }
        }

        if (mql4.OrderSelect(context.getOrderTicket(), MqlApi.SELECT_BY_TICKET, MqlApi.MODE_TRADES))
        {
            if (mql4.OrderType() == MqlApi.OP_BUY)
            {
                context.addLogEntry("Order got filled at price: " + mql4.DoubleToStr(mql4.OrderOpenPrice(), mql4.Digits), true);
                context.setActualEntry(mql4.OrderOpenPrice());
                context.setState(new BuyOrderFilledProfitTargetNotReached(context, mql4));
                return;
            }
        }
    }
}
}