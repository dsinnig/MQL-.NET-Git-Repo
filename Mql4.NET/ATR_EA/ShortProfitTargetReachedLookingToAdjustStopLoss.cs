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

        public ShortProfitTargetReachedLookingToAdjustStopLoss(ATRTrade aContext, MqlApi mql4) : base(mql4)
        {

            currentLL = 99999;
            this.timeWhenProfitTargetWasReached = mql4.TimeCurrent();
            this.context = aContext;
        }

        public override void update()
        {

            if (!context.Order.getOrderCloseTime().Equals(new DateTime()))
            {
                string logMessage;

                double riskReward = (double)(context.getActualEntry() - context.Order.getOrderClosePrice()) / (context.getOriginalStopLoss() - context.getActualEntry());

                double pips = mql4.MathAbs(context.Order.getOrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);

                if (context.Order.getOrderClosePrice() > context.getActualEntry()) logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
                else logMessage = "Gain of " + mql4.DoubleToString(pips, 1) + " micro pips (" + mql4.DoubleToString(riskReward, 2) + "R).";
                context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(context.Order.getOrderClosePrice(), mql4.Digits) + " " + logMessage, true);
                context.addLogEntry("P/L of: $" + mql4.DoubleToString(context.Order.getOrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(context.Order.getOrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(context.Order.getOrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);


                context.setRealizedPL(context.Order.getOrderProfit());
                context.setCommission(context.Order.getOrderCommission());
                context.setSwap(context.Order.getOrderSwap());
                context.setActualClose(context.Order.getOrderClosePrice());
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


                            ErrorType result = context.Order.modifyOrder(context.Order.getOrderOpenPrice(), mql4.NormalizeDouble(high + buffer, mql4.Digits), 0);

                            if (result == ErrorType.NO_ERROR)
                            {
                                context.setStopLoss(mql4.NormalizeDouble(high + buffer, mql4.Digits));
                                context.addLogEntry("Stop loss succssfully adjusted", true);
                            }

                            if ((result == ErrorType.RETRIABLE_ERROR) && (context.getOrderTicket() == -1))
                            {
                                context.addLogEntry("Order modification failed. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Will re-try at next tick", true);
                                return;
                            }

                            if ((result == ErrorType.NON_RETRIABLE_ERROR) && (context.getOrderTicket() == -1))
                            {
                                context.addLogEntry("Non-recoverable error occurred. Errorcode: " + mql4.IntegerToString(mql4.GetLastError()) + ". Trade will be canceled", true);
                                context.setState(new TradeClosed(context, mql4));
                                return;
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