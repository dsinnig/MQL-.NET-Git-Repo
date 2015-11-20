using NQuotes;

namespace biiuse
{
    internal class StopSellOrderOpened : TradeState
    {
        private ATRTrade context; //hides context in Trade

        public StopSellOrderOpened(ATRTrade aContext, MqlApi mql4) : base(mql4)
        {
            this.context = aContext;
        }

        public override void update()
        {
            if (mql4.Bid >= context.getCancelPrice())
            {
                context.addLogEntry(true, "Bid price went above cancel level. Attempting to delete order.");
                //delete Order
                ErrorType result = context.Order.deleteOrder();

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


            if (context.Order.OrderType == OrderType.SELL)
            {
                context.addLogEntry(true, "Order got filled at price: " + mql4.DoubleToStr(context.Order.getOrderOpenPrice(), mql4.Digits));
                context.setActualEntry(context.Order.getOrderOpenPrice());
                context.setState(new SellOrderFilledProfitTargetNotReached(context, mql4));
                return;
            }

        }
    }
}