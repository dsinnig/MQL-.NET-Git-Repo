using System;
using NQuotes;

namespace biiuse
{
    internal class LongProfitTargetReachedLookingToAdjustStopLoss : TradeState
    {
        private ATRTrade context;
        private DateTime barStartTimeOfCurrentHH;
        private double currentHH;
        private DateTime timeWhenProfitTargetWasReached;
        private DateTime lastbar = new DateTime();

        public LongProfitTargetReachedLookingToAdjustStopLoss(ATRTrade aContext, MqlApi mql4) : base(mql4)
        {
            this.currentHH = 0;
            this.timeWhenProfitTargetWasReached = mql4.TimeCurrent();
            this.context = aContext;
        }


        public override void update()
        {
            if (!context.Order.getOrderCloseTime().Equals(new DateTime()))
            {
                double riskReward = (context.Order.getOrderClosePrice() - context.getActualEntry()) / (context.getActualEntry() - context.getOriginalStopLoss());
                string logMessage;
                double pips = mql4.MathAbs(context.Order.getOrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);
                if (context.Order.getOrderClosePrice() > context.getActualEntry())
                {
                    logMessage = "Gain of " + mql4.DoubleToString(pips, 1) + " micro pips (" + mql4.DoubleToString(riskReward, 2) + "R).";
                }
                else
                {
                    logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
                }
                context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(context.Order.getOrderClosePrice(), mql4.Digits) + " " + logMessage, true);
                context.addLogEntry("P/L of: $" + mql4.DoubleToString(context.Order.getOrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(context.Order.getOrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(context.Order.getOrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);


                context.setRealizedPL(context.Order.getOrderProfit());
                context.setActualClose(context.Order.getOrderClosePrice());
                context.setState(new TradeClosed(context, mql4));
                return;
            }

            //order still open...

            //if still in the same minute that reached the target -> wait for next bar
            if ((mql4.TimeMinute(mql4.TimeCurrent()) == mql4.TimeMinute(timeWhenProfitTargetWasReached)) &&
               (mql4.TimeHour(mql4.TimeCurrent()) == mql4.TimeHour(timeWhenProfitTargetWasReached)) &&
               (mql4.TimeDay(mql4.TimeCurrent()) == mql4.TimeDay(timeWhenProfitTargetWasReached)))
            { return; }

            if (isNewBar())
            {
                if (currentHH == 0)
                {
                    currentHH = mql4.High[1];
                    barStartTimeOfCurrentHH = mql4.Time[1];
                    context.addLogEntry("Initial high established at: " + mql4.DoubleToString(currentHH, mql4.Digits), true);
                }

                if (currentHH != 0)
                {
                    if (mql4.High[1] > currentHH)
                    {
                        //save info rel. to previous HH
                        double previousHH = currentHH;
                        DateTime barStartTimeOfPreviousHH = barStartTimeOfCurrentHH;
                        //set new info
                        currentHH = mql4.High[1];
                        barStartTimeOfCurrentHH = mql4.Time[1];

                        context.addLogEntry("Found new high at: " + mql4.DoubleToString(currentHH, mql4.Digits), true);

                        //look if stop loss can be adjusted
                        int shiftOfPreviousHH = mql4.iBarShift(mql4.Symbol(), MqlApi.PERIOD_M1, barStartTimeOfPreviousHH, true);
                        if (shiftOfPreviousHH == -1)
                        {
                            context.addLogEntry("Error: Could not fine start time of previous HH.", true);
                            return;
                        }
                        int i = shiftOfPreviousHH - 1; //exclude bar that made the previous HH
                        bool downBarFound = false;
                        double low = 99999;
                        while (i > 1)
                        {
                            if (mql4.Open[i] > mql4.Close[i]) downBarFound = true;
                            if (mql4.Low[i] < low) low = mql4.Low[i];
                            i--;
                        }
                        if (!downBarFound || (low == 99999))
                        {
                            context.addLogEntry("Coninuation bar - Do not adjust stop loss", true);
                            return;
                        }


                        if (low != 99999)
                        {
                            context.addLogEntry("Low point between highs is: " + mql4.DoubleToString(low, mql4.Digits), true);
                        }

                        //factor in 20 micropips
                        double buffer = context.getRangeBufferInMicroPips() / OrderManager.getPipConversionFactor(mql4); ///Check for 3 digit pais
                        if (downBarFound && (low - buffer > context.getInitialProfitTarget()) && (low - buffer > context.getStopLoss()))
                        {
                            context.addLogEntry("Attempting to adjust stop loss to: " + mql4.DoubleToString(low - buffer, mql4.Digits), true);

                            ErrorType result = context.Order.modifyOrder(context.Order.getOrderOpenPrice(), mql4.NormalizeDouble(low - buffer, mql4.Digits), 0);
                            
                            
                            if (result == ErrorType.NO_ERROR)
                            {
                                context.setStopLoss(mql4.NormalizeDouble(low - buffer, mql4.Digits));
                                context.addLogEntry("Stop loss succssfully adjusted", true);
                            }

                            if ((result == ErrorType.RETRIABLE_ERROR) && (context.Order.OrderTicket == -1))
                            {
                                context.addLogEntry("Order modification failed. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Will re-try at next tick", true);
                                return;
                            }

                            if ((result == ErrorType.NON_RETRIABLE_ERROR) && (context.Order.OrderTicket == -1))
                            {
                                context.addLogEntry("Non-recoverable error occurred. Errorcode: " + mql4.IntegerToString(mql4.GetLastError()) + ". Trade will be canceled", true);
                                context.setState(new TradeClosed(context, mql4));
                                return;
                            }
                            

                        }

                        if (low - buffer <= context.getInitialProfitTarget())
                        {
                            context.addLogEntry("Low minus range buffer of " + mql4.IntegerToString(context.getRangeBufferInMicroPips()) + " micro pips is below initial profit target of: " + mql4.DoubleToString(context.getInitialProfitTarget(), mql4.Digits) + ". Do not adjust stop loss", true);
                            return;
                        }

                        if (low - buffer < context.getStopLoss())
                        {
                            context.addLogEntry("Low minus range buffer of " + mql4.IntegerToString(context.getRangeBufferInMicroPips()) + " micro pips is below previous stop loss of: " + mql4.DoubleToString(context.getStopLoss(), mql4.Digits) + ". Do not adjust stop loss", true);
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