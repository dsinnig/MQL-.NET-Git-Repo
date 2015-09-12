using System;
using NQuotes;


namespace biiuse
{
    internal class SellOrderFilledProfitTargetNotReached : TradeState
    {
        private ATRTrade context; //hides conext in Trade
        public SellOrderFilledProfitTargetNotReached(ATRTrade aContext, MqlApi mql4) : base(mql4) {
        this.context = aContext;
        context.setOrderFilledDate(mql4.TimeCurrent());
    }

    public override void update()
    {
        //see if stopped out
        bool success = mql4.OrderSelect(context.getOrderTicket(), MqlApi.SELECT_BY_TICKET);
        if (!success)
        {
            context.addLogEntry("Unable to find order. Trade must have been closed", true);
            context.setState(new TradeClosed(context, mql4));
            return;
        }

        if (!mql4.OrderCloseTime().Equals(new DateTime()))
        {
            double pips = mql4.MathAbs(mql4.OrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);
            string logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
            context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(mql4.OrderClosePrice(), mql4.Digits) + " " + logMessage, true);
            context.addLogEntry("P/L: $" + mql4.DoubleToString(mql4.OrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(mql4.OrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(mql4.OrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);


            context.setRealizedPL(mql4.OrderProfit());
            context.setOrderCommission(mql4.OrderCommission());
            context.setOrderSwap(mql4.OrderSwap());

            context.setActualClose(mql4.OrderClosePrice());
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