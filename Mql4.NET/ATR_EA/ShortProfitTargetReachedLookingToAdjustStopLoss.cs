using System;
using NQuotes;

namespace biiuse
{
    internal class ShortProfitTargetReachedLookingToAdjustStopLoss : TradeState
    {
        private DateTime barStartTimeOfCurrentLL;
        private double currentLL;
        private DateTime timeWhenProfitTargetWasReached;
        ATRTrade context;
        private DateTime lastbar = new DateTime();
        
        public ShortProfitTargetReachedLookingToAdjustStopLoss(ATRTrade aContext, MqlApi mql4) : base(mql4) {
   
   currentLL=99999;
   this.timeWhenProfitTargetWasReached= mql4.TimeCurrent();
   this.context = aContext;
}

    public override void update()
    {

        //check if order closed
        bool success = mql4.OrderSelect(context.getOrderTicket(), MqlApi.SELECT_BY_TICKET);
        if (!success)
        {
            context.addLogEntry("Unable to find order. Trade must have been closed", true);
            context.setState(new TradeClosed(context, mql4));
            return;
        }
        if (!mql4.OrderCloseTime().Equals(new DateTime()))
        {
            string logMessage;

            double riskReward = (double) (context.getActualEntry() - mql4.OrderClosePrice()) / (context.getOriginalStopLoss() - context.getActualEntry());

            double pips = mql4.MathAbs(mql4.OrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);

            if (mql4.OrderClosePrice() > context.getActualEntry()) logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
            else logMessage = "Gain of " + mql4.DoubleToString(pips, 1) + " micro pips (" + mql4.DoubleToString(riskReward, 2) + "R).";
            context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(mql4.OrderClosePrice(), mql4.Digits) + " " + logMessage, true);
            context.addLogEntry("P/L of: $" + mql4.DoubleToString(mql4.OrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(mql4.OrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(mql4.OrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);


            context.setRealizedPL(mql4.OrderProfit());
            context.setOrderCommission(mql4.OrderCommission());
            context.setOrderSwap(mql4.OrderSwap());
            context.setActualClose(mql4.OrderClosePrice());
            context.setState(new TradeClosed(context, mql4));
            return;
        }

        //order still open...

        //if still in same bar that made the profit target -> wait for next bar. 
        if ((mql4.TimeMinute(mql4.TimeCurrent()) == mql4.TimeMinute(timeWhenProfitTargetWasReached)) &&
           (mql4.TimeHour(mql4.TimeCurrent()) == mql4.TimeHour(timeWhenProfitTargetWasReached)) &&
           (mql4.TimeDay(mql4.TimeCurrent()) == mql4.TimeDay(timeWhenProfitTargetWasReached)))
        { return; }

        if (isNewBar())
        {
            if (currentLL == 99999)
            {
                currentLL = mql4.Low[1];
                barStartTimeOfCurrentLL = mql4.Time[1];
                context.addLogEntry("Initial low established at: " + mql4.DoubleToString(currentLL, mql4.Digits), true);
            }

            if (currentLL != 99999)
            {
                if (mql4.Low[1] < currentLL)
                {
                    //save info rel. to previous HH
                    double previousLL = currentLL;
                    DateTime barStartTimeOfPreviousLL = barStartTimeOfCurrentLL;
                    //set new info
                    currentLL = mql4.Low[1];
                    barStartTimeOfCurrentLL = mql4.Time[1];

                    context.addLogEntry("Found new low at: " + mql4.DoubleToString(currentLL, mql4.Digits), true);

                    //look if stop loss can be adjusted
                    int shiftOfPreviousLL = mql4.iBarShift(mql4.Symbol(), MqlApi.PERIOD_M1, barStartTimeOfPreviousLL, true);
                    if (shiftOfPreviousLL == -1)
                    {
                        context.addLogEntry("Error: Could not fine start time of previous LL.", true);
                        return;
                    }
                    int i = shiftOfPreviousLL - 1; //exclude bar that made the previous HH
                    bool upBarFound = false;
                    double high = -1;
                    while (i > 1)
                    {
                        if (mql4.Open[i] < mql4.Close[i]) upBarFound = true;
                        if (mql4.High[i] > high) high = mql4.High[i];
                        i--;
                    }
                    if (!upBarFound || (high == -1))
                    {
                        context.addLogEntry("Coninuation bar - Do not adjust stop loss", true);
                        return;
                    }


                    if (high != -1)
                    {
                        context.addLogEntry("High point between highs is: " + mql4.DoubleToString(high, mql4.Digits), true);
                    }
                    double buffer = context.getRangeBufferInMicroPips() / OrderManager.getPipConversionFactor(mql4); ///Check for 3 digit pais
                    if (upBarFound && (high + buffer < context.getInitialProfitTarget()) && (high + buffer < context.getStopLoss()))
                    {
                        //adjust stop loss
                        context.addLogEntry("Attempting to adjust stop loss to: " + mql4.DoubleToString(high + buffer, mql4.Digits), true);
                        bool orderSelectResult = mql4.OrderSelect(context.getOrderTicket(), MqlApi.SELECT_BY_TICKET);
                        if (!orderSelectResult)
                        {
                            context.addLogEntry("Error: Unable to adjust stop loss - Order not found", true);
                            return;
                        }
                        bool res = mql4.OrderModify(mql4.OrderTicket(), mql4.OrderOpenPrice(), mql4.NormalizeDouble(high + buffer, mql4.Digits), 0, new DateTime(), System.Drawing.Color.Blue);
                        ErrorType result = OrderManager.analzeAndProcessResult(context, mql4);
                        if (result == ErrorType.NO_ERROR)
                        {
                            context.setStopLoss(mql4.NormalizeDouble(high + buffer, mql4.Digits));
                            context.addLogEntry("Stop loss succssfully adjusted", true);
                        }
                    }

                    if (high + buffer >= context.getInitialProfitTarget())
                    {
                        context.addLogEntry("High plus range buffer of " + mql4.IntegerToString(context.getRangeBufferInMicroPips()) + " micro pips is above initial profit target of: " + mql4.DoubleToString(context.getInitialProfitTarget(), mql4.Digits) + ". Do not adjust stop loss", true);
                        return;
                    }

                    if (high + buffer > context.getStopLoss())
                    {
                        context.addLogEntry("High plus range buffer of " + mql4.IntegerToString(context.getRangeBufferInMicroPips()) + " micro pips is above previous stop loss: " + mql4.DoubleToString(context.getStopLoss(), mql4.Digits) + ". Do not adjust stop loss", true);
                        return;
                    }
                }
            }
        }
    }

    private bool isNewBar()
    {
        
        DateTime curbar = mql4.Time[0];
        if (lastbar != curbar)
        {
            lastbar = curbar;
            return (true);
        }
        else
        {
            return (false);
        }
    }
}
}