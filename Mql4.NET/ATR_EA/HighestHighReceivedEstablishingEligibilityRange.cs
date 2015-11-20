using System;
using NQuotes;

namespace biiuse
{
    internal class HighestHighReceivedEstablishingEligibilityRange : TradeState
    {


        private ATRTrade context; //hides conext in Trade
        DateTime entryTime;
        double rangeLow;
        double rangeHigh;
        int barCounter;
        private System.DateTime lastbar = new System.DateTime();
        double buffer;




        public HighestHighReceivedEstablishingEligibilityRange(ATRTrade aContext, MqlApi mql4) : base(mql4)
        {
            this.context = aContext;
            this.entryTime = mql4.Time[0];
            this.rangeHigh = mql4.High[0];
            this.rangeLow = mql4.Low[0];
            this.barCounter = 0;
            this.buffer = aContext.getRangeBufferInMicroPips() / OrderManager.getPipConversionFactor(mql4); ///Works for 5 Digts pairs. Verify that calculation is valid for 3 Digits pairs

            context.setTradeType(TradeType.SHORT);

            //context.addLogEntry("Highest high found - establishing eligibility range. Highest high: " + mql4.DoubleToString(mql4.Close[0], mql4.Digits), true);
        }


        public override void update()
        {
            double factor = OrderManager.getPipConversionFactor(mql4);

            //update range lows and range highs
            if (mql4.Low[0] < rangeLow)
            {
                rangeLow = mql4.Low[0];
            }
            if (mql4.High[0] > rangeHigh)
            {
                rangeHigh = mql4.High[0];
            }

            
            if (isNewBar()) barCounter++;



            //Waiting Period over? (deault is 10mins + 1min)
            //if(Time[0]-entryTime>=60*(context.getLengthIn1MBarsOfWaitingPeriod()+1))  {
            if (barCounter > context.getLengthIn1MBarsOfWaitingPeriod() + 1)
            {

                //adjust range or buffer pips
                rangeLow -= this.buffer;
                rangeHigh += this.buffer;

                context.setRSI(mql4.iCustom(null, 0, "RSI", 15, 0, 0));
                context.setRSI5M(mql4.iCustom(null, MqlApi.PERIOD_M5, "RSI", 15, 0, 0));
                context.setRSI15M(mql4.iCustom(null, MqlApi.PERIOD_M15, "RSI", 15, 0, 0));
                context.setRSI30M(mql4.iCustom(null, MqlApi.PERIOD_M30, "RSI", 15, 0, 0));
                context.setMomentum(mql4.iCustom(null, 0, "Momentum", 14, 0, 0));
                context.setMomentum5M(mql4.iCustom(null, MqlApi.PERIOD_M5, "Momentum", 14, 0, 0));
                context.setMomentum15M(mql4.iCustom(null, MqlApi.PERIOD_M15, "Momentum", 14, 0, 0));
                context.setMomentum30M(mql4.iCustom(null, MqlApi.PERIOD_M30, "Momentum", 14, 0, 0));


                //TODO Add Stochastic indicator
                context.setStochastics(0);
                context.setStochastics5M(0);
                context.setStochastics15M(0);
                context.setStochastics30M(0);
                //context.setStochastics(iCustom(NULL, 0, "Stochastic", 5, 3, 3, 0, 0));
                //context.setStochastics5M(iCustom(NULL, PERIOD_M5, "Stochastic", 5, 3, 3, 0, 0));
                //context.setStochastics15M(iCustom(NULL, PERIOD_M15, "Stochastic", 5, 3, 3, 0, 0));
                //context.setStochastics30M(iCustom(NULL, PERIOD_M30, "Stochastic", 5, 3, 3, 0, 0));

                int rangePips = (int)((rangeHigh - rangeLow) * factor);
                int ATRPips = (int)(context.getATR() * factor);

                //context.addLogEntry("Range established at: " + mql4.IntegerToString(rangePips) + " micro pips (incl. 2* buffer) of " + mql4.DoubleToString(buffer, mql4.Digits) + ". HH=" + mql4.DoubleToString(rangeHigh, mql4.Digits) + ", LL=" + mql4.DoubleToString(rangeLow, mql4.Digits), true);
                

                context.setRangeLow(rangeLow);
                context.setRangeHigh(rangeHigh);
                context.setRangePips(rangePips);

                //Range too large for limit or stop order
                if ((rangeHigh - rangeLow) > ((context.getPercentageOfATRForMaxVolatility() / 100.00) * context.getATR()))
                {
                    context.addLogEntry(true, "10 bar range too BIG - Cancel Trade ",
                                              "Range established at: " + mql4.IntegerToString(rangePips), " micro pips (incl. 2* buffer) of ", mql4.DoubleToString(buffer, mql4.Digits), "\n",
                                              "HH=", mql4.DoubleToString(rangeHigh, mql4.Digits) + ", LL=" + mql4.DoubleToString(rangeLow, mql4.Digits), "\n",
                                              "Range (", mql4.IntegerToString(rangePips), " micro pips) is greater than ", mql4.DoubleToString(context.getPercentageOfATRForMaxVolatility(), 2), "% of ATR (" + mql4.IntegerToString(ATRPips), " micro pips)"
                                          );

                    //context.addLogEntry("Range (" + mql4.IntegerToString(rangePips) + " micro pips) is greater than " + mql4.DoubleToString(context.getPercentageOfATRForMaxVolatility(), 2) + "% of ATR (" + mql4.IntegerToString(ATRPips) + " micro pips)", true);
                    context.setState(new TradeClosed(context, mql4));
                }
                else
                {
                    double entryPrice = 0.0;
                    double stopLoss = 0.0;
                    double cancelPrice = 0.0;
                    int orderType = -1;
                    TradeState nextState = null;
                    double positionSize = 0;
                    double oneMicroPip = 1 / OrderManager.getPipConversionFactor(mql4);

                    //Range is less than max risk
                    if ((rangeHigh - rangeLow) < ((context.getPercentageOfATRForMaxRisk() / 100.00) * context.getATR()))
                    {

                        //write to log
                        context.addLogEntry(true, "10 bar range is less than max risk - Go ahead with stop order ",
                                              "Range established at: " + mql4.IntegerToString(rangePips), " micro pips (incl. 2* buffer) of ", mql4.DoubleToString(buffer, mql4.Digits), "\n",
                                              "HH=", mql4.DoubleToString(rangeHigh, mql4.Digits) + ", LL=" + mql4.DoubleToString(rangeLow, mql4.Digits), "\n",
                                              "Range (", mql4.IntegerToString(rangePips), " micro pips) is less than max risk ", mql4.DoubleToString(context.getPercentageOfATRForMaxRisk(), 2), "% of ATR (" + mql4.IntegerToString(ATRPips), " micro pips)"
                                          );

                        //context.addLogEntry("Range (" + mql4.IntegerToString(rangePips) + " micro pips) is less than max risk " + mql4.DoubleToString(context.getPercentageOfATRForMaxRisk(), 2) + "% of ATR (" + mql4.IntegerToString(ATRPips) + " micro pips)", true);

                        entryPrice = rangeLow;
                        stopLoss = rangeHigh;
                        cancelPrice = rangeHigh - buffer + oneMicroPip;
                        orderType = MqlApi.OP_SELLSTOP;
                        context.setOrderType("SELL_STOP");
                        nextState = new StopSellOrderOpened(context, mql4);
                    }
                    else
                    
                    //Range is above risk level, but below max volatility level. Current Bid price is less than entry level.  
                    if ((rangeHigh - rangeLow) < ((context.getPercentageOfATRForMaxVolatility() / 100.00) * context.getATR()))
                    {

                        //write to log
                        context.addLogEntry(true, "10 bar range is greater than max risk but less than 2*risk - Waiting for break above range high",
                                              "Range established at: " + mql4.IntegerToString(rangePips), " micro pips (incl. 2* buffer) of ", mql4.DoubleToString(buffer, mql4.Digits), "\n",
                                              "HH=", mql4.DoubleToString(rangeHigh, mql4.Digits) + ", LL=" + mql4.DoubleToString(rangeLow, mql4.Digits), "\n",
                                              "Range (" + mql4.IntegerToString(rangePips), " micro pips) is greater than max risk (", mql4.DoubleToString(context.getPercentageOfATRForMaxRisk(), 2), "% but less than max. volatility ", mql4.DoubleToString(context.getPercentageOfATRForMaxVolatility(), 2), "%) of ATR (", mql4.IntegerToString(ATRPips), " micro pips). Waiting for break above range high"
                                          );
                        //context.addLogEntry("Range (" + mql4.IntegerToString(rangePips) + " micro pips) is greater than max risk (" + mql4.DoubleToString(context.getPercentageOfATRForMaxRisk(), 2) +
                        //                    "% but less than max. volatility " + mql4.DoubleToString(context.getPercentageOfATRForMaxVolatility(), 2) + "%) of ATR (" + mql4.IntegerToString(ATRPips) + " micro pips). Waiting for break above range high", true);


                        entryPrice = rangeHigh - context.getATR() * (context.getPercentageOfATRForMaxRisk() / 100.00);
                        //mql4.Print("ATR is!!!! ", context.getATR());
                        //mql4.Print("Percent is!!: ", context.getPercentageOfATRForMaxRisk());
                        //mql4.Print("cal. diff is: ", context.getATR() * (context.getPercentageOfATRForMaxRisk() / 100.00));
                        //mql4.Print("EntryPrice is!!!! ", entryPrice);
                        stopLoss = rangeHigh;

                        
                        cancelPrice = rangeHigh - buffer + oneMicroPip - context.getATR() * (context.getPercentageOfATRForMaxVolatility() / 100.00); //cancel if above 20% of ATR
                        orderType = MqlApi.OP_SELLLIMIT;
                        context.setOrderType("SELL_LIMIT");
                        int riskPips = (int)(mql4.MathAbs(stopLoss - entryPrice) * factor);
                        /// TODO Parametrize Risk
                        double riskCapital = mql4.AccountBalance() * context.getMaxBalanceRisk(); ///Parametrize
                        positionSize = Math.Round(OrderManager.getLotSize(riskCapital, riskPips, mql4), context.getLotDigits(), MidpointRounding.AwayFromZero);
                        context.setState(new WaitForBreakToPlaceLimitOrder(context, orderType, rangeLow, rangeHigh, entryPrice, cancelPrice, positionSize, mql4));
                        return;
                    }
                    
                    //only place order if entryPrice was calculated. I.e., if any of the three previous if/else cases was exercised. 
                    if (entryPrice != 0.0)
                    {

                        int riskPips = (int)(mql4.MathAbs(stopLoss - entryPrice) * factor);
                        //TODO Parametrize risk (currently 0.75%)
                        double riskCapital = mql4.AccountBalance() * context.getMaxBalanceRisk();
                        positionSize = Math.Round(OrderManager.getLotSize(riskCapital, riskPips, mql4), context.getLotDigits(), MidpointRounding.AwayFromZero);

                        //context.addLogEntry("AccountBalance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2) + "; Risk Capital: $" + mql4.DoubleToString(riskCapital, 2) + "; Risk pips: " + mql4.DoubleToString(riskPips, 2) + " micro pips; Position Size: " + mql4.DoubleToString(positionSize, 2) + " lots; Pip value: " + mql4.DoubleToString(OrderManager.getPipValue(mql4), mql4.Digits), true);

                        //place Order
                        ErrorType result = context.Order.submitNewOrder(orderType, entryPrice, stopLoss, 0, cancelPrice, positionSize);
                        context.setStartingBalance(mql4.AccountBalance());
                        context.setOrderPlacedDate(mql4.TimeCurrent());
                        context.setSpreadOrderOpen((int)mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_SPREAD));
                        context.setAskPriceBeforeOrderEntry(mql4.Ask);
                        context.setBidPriceBeforeOrderEntry(mql4.Bid);
                        context.setCancelPrice(cancelPrice);
                        context.setPlannedEntry(entryPrice);
                        context.setStopLoss(stopLoss);
                        context.setOriginalStopLoss(stopLoss);
                        context.setTakeProfit(0);
                        context.setCancelPrice(cancelPrice);
                        context.setPositionSize(positionSize);

                        if (result == ErrorType.NO_ERROR)
                        {
                            context.setInitialProfitTarget(Math.Round(context.getPlannedEntry() + ((context.getPlannedEntry() - context.getStopLoss()) * (context.getMinProfitTarget())), mql4.Digits, MidpointRounding.AwayFromZero));
                            context.setState(nextState);
                            //context.addLogEntry("Order successfully placed. Initial Profit target is: " + mql4.DoubleToString(context.getInitialProfitTarget(), mql4.Digits) + " (" + mql4.IntegerToString((int)(mql4.MathAbs(context.getInitialProfitTarget() - context.getPlannedEntry()) * factor)) + " micro pips)" + " Risk is: " + mql4.IntegerToString((int)riskPips) + " micro pips", true);
                            context.addLogEntry(true, "Trade Details",
                                                      "AccountBalance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), "\n", 
                                                      "Risk Capital: $" + mql4.DoubleToString(riskCapital, 2), "\n", 
                                                      "Risk pips: " + mql4.DoubleToString(riskPips, 2) + " micro pips", "\n", 
                                                      "Pip value: " + mql4.DoubleToString(OrderManager.getPipValue(mql4), mql4.Digits), "\n",
                                                      "Initial Profit target is: " + mql4.DoubleToString(context.getInitialProfitTarget(), mql4.Digits) + "(" + mql4.IntegerToString((int)(mql4.MathAbs(context.getInitialProfitTarget() - context.getPlannedEntry()) * factor)) + " micro pips)"
                                                      );
                            return;
                        }
                        if ((result == ErrorType.RETRIABLE_ERROR) && (context.Order.OrderTicket == -1))
                        {
                            context.addLogEntry("Order entry failed. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Will re-try at next tick", true);

                            return;
                        }

                        //this should never happen...
                        if ((context.Order.OrderTicket != -1) && ((result == ErrorType.RETRIABLE_ERROR) || (result == ErrorType.NON_RETRIABLE_ERROR)))
                        {
                            context.addLogEntry("Error ocured but order is still open. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Continue with trade. Initial Profit target is: " + mql4.DoubleToString(context.getInitialProfitTarget(), mql4.Digits) + " (" + mql4.IntegerToString((int)(mql4.MathAbs(context.getInitialProfitTarget() - context.getPlannedEntry()) * factor)) + " micro pips)" + " Risk is: " + mql4.IntegerToString((int)riskPips) + " micro pips", true);
                            context.setInitialProfitTarget(Math.Round(context.getPlannedEntry() + ((context.getPlannedEntry() - context.getStopLoss()) * (context.getMinProfitTarget())), mql4.Digits, MidpointRounding.AwayFromZero));
                            context.setState(nextState);
                            return;
                        }

                        if ((result == ErrorType.NON_RETRIABLE_ERROR) && (context.Order.OrderTicket == -1))
                        {
                            context.addLogEntry("Non-recoverable error occurred. Errorcode: " + mql4.IntegerToString(mql4.GetLastError()) + ". Trade will be canceled", true);
                            context.setState(new TradeClosed(context, mql4));
                            return;
                        }
                    } //end of if that checks if entryPrice is 0.0
                } //end else (that checks for general trade eligibility)
            } //end if for range delay check
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