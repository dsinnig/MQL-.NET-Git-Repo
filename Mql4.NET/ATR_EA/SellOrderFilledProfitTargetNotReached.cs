﻿using System;
using NQuotes;


namespace biiuse
{
    internal class SellOrderFilledProfitTargetNotReached : TradeState
    {
        private ATRTrade context; //hides conext in Trade
        public SellOrderFilledProfitTargetNotReached(ATRTrade aContext, MqlApi mql4) : base(mql4) {
            this.context = aContext;
            context.setOrderFilledDate(mql4.TimeCurrent());
            context.Order.OrderType = OrderType.SELL;
    }

    public override void update()
    {
        if (!context.Order.getOrderCloseTime().Equals(new DateTime()))
        {
            double pips = mql4.MathAbs(context.Order.getOrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);
            string logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
            context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(context.Order.getOrderClosePrice(), mql4.Digits) + " " + logMessage, true);
            context.addLogEntry("P/L: $" + mql4.DoubleToString(context.Order.getOrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(context.Order.getOrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(context.Order.getOrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);


            context.setRealizedPL(context.Order.getOrderProfit());
            context.setActualClose(context.Order.getOrderClosePrice());
            context.setState(new TradeClosed(context, mql4));
            return;
        }


        if (mql4.Ask < context.getInitialProfitTarget())
        {
            context.addLogEntry("Initial profit target reached. Looking to adjust stop loss", true);
            context.setState(new ShortProfitTargetReachedLookingToAdjustStopLoss(context, mql4));
         }
    }

}
}