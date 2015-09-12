﻿using System;
using NQuotes;

namespace biiuse
{
    internal class SellLimitOrderOpened : TradeState
    {
        private ATRTrade context;

    public SellLimitOrderOpened(ATRTrade aContext, MqlApi mql4) : base(mql4) {
      this.context = aContext;
}

    public override void update()
    {
        if (mql4.Bid < context.getCancelPrice())
        {
            context.addLogEntry("Bid price went below cancel level", true);
            ErrorType result = OrderManager.deleteOrder(context.getOrderTicket(), context, mql4);

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


        if (mql4.OrderSelect(context.getOrderTicket(), MqlApi.SELECT_BY_TICKET, MqlApi.MODE_TRADES))
        {
            if (mql4.OrderType() == MqlApi.OP_SELL)
            {
                context.addLogEntry("Order got filled at price: " + mql4.DoubleToStr(mql4.OrderOpenPrice(), mql4.Digits), true);
                context.setActualEntry(mql4.OrderOpenPrice());
                context.setState(new SellOrderFilledProfitTargetNotReached(context, mql4));
                return;
            }
        }
    }
}
}