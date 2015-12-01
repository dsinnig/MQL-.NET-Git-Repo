using System;
using System.Collections.Generic;
using NQuotes;

namespace biiuse
{
    class ATR_EA : NQuotes.MqlApi
    {
        [ExternVariable]
        public double maxBalanceRisk = 0.0075; //Max risk per trader relative to account balance (in %)
        [ExternVariable]
        public int sundayLengthInHours = 7; //Length of Sunday session in hours
        [ExternVariable]
        public int HHLL_Threshold = 60; //Time in minutes after last HH / LL before a tradeable HH/LL can occur
        [ExternVariable]
        public int lengthOfGracePeriod = 10; //Length in bars of Grace Period after a tradeable HH/LL occured
        [ExternVariable]
        public double rangeRestriction = 00; //Min range of Grace Period
        [ExternVariable]
        public int lookBackSessions = 1; //Number of sessions to look back for establishing a new HH/LL
        [ExternVariable]
        public int atrTypeInInt = 0; //Use intToATRType to convert to proper Enum ATR_Type
        [ExternVariable]
        public double maxRisk = 10; //Max risk (in percent of ATR)
        [ExternVariable]
        public double maxVolatility = 20; //Max volatility (in percent of ATR)
        [ExternVariable]
        public double minProfitTarget = 4; //Min Profit Target (in factors of the risk e.g., 3 = 3* Risk)
        [ExternVariable]
        public int rangeBuffer = 20; //Buffer in micropips for order opening and closing
        [ExternVariable]
        public int lotDigits = 1; //Lot size granularity (0 = full lots, 1 = mini lots, 2 = micro lots, etc).
        [ExternVariable]
        public int maxConsLoses = 99; //max consecutive losses before stop trading
        [ExternVariable]
        public double maxATROR = 0.5; //max percent value for ATR / OR
        [ExternVariable]
        public double minATROR = 0; //min percent value for ATR / OR
        [ExternVariable]
        public double maxDRATR = 0.35; //max percent value for ATR / OR
        [ExternVariable]
        public double minDRATR = 0; //min percent value for ATR / OR
        [ExternVariable]
        public int minATR = 0; //min ATR in MicroPips to take a trade
        [ExternVariable]
        public int maxATR = 10000; //max ATR in MicroPips to take a trade
        [ExternVariable]
        public bool cutLossesBeforeATRFilter = true; //min percent value for ATR / OR
        [ExternVariable]
        public string logFileName = "tradeLog.csv"; //path and filename for CSV trade log

        public override int init()
        {
            atrType = intToATR_Type(atrTypeInInt);
            sundayLengthInSeconds = 60 * 60 * sundayLengthInHours;
            trades = new List<Trade>();

            if (IsTesting())
            {
                //TODO Parameterize ATR_EA.log
                FileDelete("ATR_EA.log");
                FileDelete(logFileName);
            }

            return base.init();
        }

        public override int start()
        {

            //new bar?

            //the second condition is to skip the first couple of minutes of when the market opens on Sunday
            if (!bartime.Equals(Time[0]) && Time[0]-Time[2] < TimeSpan.FromHours(1))
            {
                //TODO verify that identity makes it still work
                bartime = Time[0];

                Session newSession = SessionFactory.getCurrentSession(sundayLengthInSeconds, HHLL_Threshold, lookBackSessions, atrType, this);
                if (currSession != newSession)
                {
                    currSession = newSession;

                    currSession.writeToCSV("session_atr.csv");
                    if (!currSession.tradingAllowed())
                    {
                        currSession.addLogEntry(true, "ATTENTION: Session could not be established", "\n", "Trading is disabled. Check log for details");
                    }
                    else
                    {

                        /*Print(TimeCurrent()," Start session: ", currSession.getName()," Start: ",currSession.getSessionStartTime()," End: ",currSession.getSessionEndTime()," ATR: ",currSession.getATR(), " (", (int) (currSession.getATR() * 100000), ") micro pips");
                        Print (" Ref: ",currSession.getHHLL_ReferenceDateTime(), " HH: ", currSession.getHighestHigh(), "@ ", currSession.getHighestHighTime(),
                               " LL: ", currSession.getLowestLow(), "@ ", currSession.getLowestLowTime());

                        */
                        double atr = currSession.getATR();
                        double tenDayHigh = currSession.getTenDayHigh();
                        double tenDayLow = currSession.getTenDayLow();
                        double ATR_OR = atr / (tenDayHigh - tenDayLow);
                        string sessionStatus = "";

                        if ((currSession.getATR() * OrderManager.getPipConversionFactor(this) < minATR) || (currSession.getATR() * OrderManager.getPipConversionFactor(this) > maxATR))
                        {
                            sessionStatus = ("ATR is not in range of: " + minATR + " - " + maxATR + ". No trades will be taken in this Session)");
                            currSession.tradingAllowed(false);
                        }
                        else if ((ATR_OR > maxATROR) || (ATR_OR < minATROR))
                        {
                            sessionStatus = ("ATR/OR is not in range of: " + minATROR.ToString("F2") + " - " + maxATROR.ToString("F2") + ". No trades will be taken in this Session)");
                            currSession.tradingAllowed(false);
                        }
                        else
                        {
                            sessionStatus = "ATR and ATR/OR are within range. Trades may be triggered in this sessions";
                        }

                        currSession.addLogEntry(true, "New Trading Session Established",
                                                      "Session name: ", currSession.getName(), "\n",
                                                      "Session start time: ", currSession.getSessionStartTime().ToString(), "\n",
                                                      "Reference date: ", currSession.getHHLL_ReferenceDateTime().ToString(), "\n",
                                                      "ATR: ", NormalizeDouble(atr, Digits), " (", (int)(currSession.getATR() * OrderManager.getPipConversionFactor(this)), " micro pips)", "\n",
                                                      "HH: ", currSession.getHighestHigh().ToString("F5"), "(", currSession.getHighestHighTime(), ") ", "LL: ", currSession.getLowestLow().ToString("F5"), "(", currSession.getLowestLowTime(), ") ", "\n",
                                                      "10 Day High is: ", tenDayHigh.ToString("F5"), " 10 Day Low is: ", tenDayLow.ToString("F5"), "\n",
                                                      "ATR / OR is: ", ATR_OR.ToString("F5"), "\n",
                                                      sessionStatus
                              );


                    }
                }
            }

            foreach (var trade in trades)
            {
                if (trade != null) trade.update();
            }



            if (currSession.tradingAllowed())
            {
                int updateResult = currSession.update((Bid + Ask) / 2);
                double atr = currSession.getATR();
                double curDailyRange = iHigh(null, MqlApi.PERIOD_D1, 0) - iLow(null, MqlApi.PERIOD_D1, 0);
                double DR_ATR = curDailyRange / atr;

                //if ((((ATR_OR < maxATROR) && (ATR_OR > minATROR)) || cutLossesBeforeATRFilter))


                if ((updateResult == 1) && (minATR < currSession.getATR() * OrderManager.getPipConversionFactor(this)) && (maxATR > currSession.getATR() * OrderManager.getPipConversionFactor(this)))
                {

                    string status = "";

                    bool go = false;
                    if ((DR_ATR < maxDRATR) && (DR_ATR > minDRATR))
                    {
                        go = true;
                        status = "DR/ATR is within range. Starting 10bar clock";
                    }
                    else
                    {
                        go = false;
                        status = "DR/ATR is too big. Trade is rejected";
                    }

                    currSession.addLogEntry(true, "Tradeable Highest High found",
                                                  "Highest high is: ", currSession.getHighestHigh().ToString("F5"), "\n",
                                                  "Time of highest high: ", currSession.getHighestHighTime().ToString(), "\n",
                                                  "Session high: ", iHigh(null, MqlApi.PERIOD_D1, 0).ToString("F5"), " Session low: ", iLow(null, MqlApi.PERIOD_D1, 0).ToString("F5"), "\n",
                                                  "DR / ATR is: ", DR_ATR.ToString("F5"), "\n",
                                                  status
                                                  );
                    if (go)
                    {
                        ATRTrade trade = new ATRTrade(false, lotDigits, logFileName, currSession.getHighestHigh(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), currSession, maxBalanceRisk, this);
                        trade.setState(new HighestHighReceivedEstablishingEligibilityRange(trade, this));
                        trades.Add(trade);
                    }

                }

                if ((updateResult == -1) && (minATR < currSession.getATR() * OrderManager.getPipConversionFactor(this)) && (maxATR > currSession.getATR() * OrderManager.getPipConversionFactor(this)))
                {

                    string status = "";

                    bool go = false;
                    if ((DR_ATR < maxDRATR) && (DR_ATR > minDRATR))
                    {
                        go = true;
                        status = "DR/ATR is within range. Starting 10bar clock";
                    }
                    else
                    {
                        go = false;
                        status = "DR/ATR is too big. Trade is rejected";
                    }

                    currSession.addLogEntry(true, "Tradeable Lowest Low found",
                                                  "Lowest low is: ", currSession.getLowestLow().ToString("F5"), "\n",
                                                  "Time of lowest low: ", currSession.getLowestLowTime().ToString(), "\n",
                                                  "Session high: ", iHigh(null, MqlApi.PERIOD_D1, 0).ToString("F5"), " Session low: ", iLow(null, MqlApi.PERIOD_D1, 0).ToString("F5"), "\n",
                                                  "DR / ATR is: ", DR_ATR.ToString("F5"), "\n",
                                                  status
                                                  );

                    if (go)
                    {
                        ATRTrade trade = new ATRTrade(false, lotDigits, logFileName, currSession.getLowestLow(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), currSession, maxBalanceRisk, this);
                        trade.setState(new LowestLowReceivedEstablishingEligibilityRange(trade, this));
                        trades.Add(trade);
                    }
                }
            }

            return base.start();
        }


        private bool simOrder(bool isLong)
        {
            int tradesAnalyzed = 0;
            int index = trades.Count - 1;
            while ((tradesAnalyzed < maxATROR) && (index >= minATROR))
            {
                if (isLong)
                {
                    if ((trades[index].getTradeClosedDate().Equals(new DateTime())) && ((trades[index].Order.OrderType == biiuse.OrderType.SELL) || (trades[index].Order.OrderType == biiuse.OrderType.BUY)))
                    {
                        TimeSpan span = TimeCurrent() - trades[index].getTradeOpenedDate();
                        if (span.TotalSeconds > 7200) return false;
                    }
                }
                else
                {
                    if ((trades[index].getTradeClosedDate().Equals(new DateTime())) && ((trades[index].Order.OrderType == biiuse.OrderType.BUY) || (trades[index].Order.OrderType == biiuse.OrderType.SELL)))
                    {
                        TimeSpan span = TimeCurrent() - trades[index].getTradeOpenedDate();
                        if (span.TotalSeconds > 7200) return false;
                    }
                }
                index--;
            }
            return true;
        }


        private bool wasConcurrentlyOpen(int index, List<Trade> clean_trades)
        {
            bool prevTradeWasConcurOpen = false;
            if (clean_trades.Count - 1 >= index)
            {
                Trade prevTrade = clean_trades[index];
                //find if it was concurrently open
                int i = index - 1;
                while (i >= 0)
                {
                    if ((clean_trades[i].getTradeOpenedDate().Equals(new DateTime())) || (prevTrade.getTradeOpenedDate() < clean_trades[i].getTradeOpenedDate()))
                    {
                        prevTradeWasConcurOpen = true;
                        break;
                    }
                    i--;
                }
            }
            return prevTradeWasConcurOpen;
        }



        //alternative method to find out wether a real order a sim order should be placed
        private bool simOrder2()
        {
            double atr = currSession.getATR();
            double tenDayHigh = currSession.getTenDayHigh();
            double tenDayLow = currSession.getTenDayLow();
            double ATR_OR = atr / (tenDayHigh - tenDayLow);


            if (cutLossesBeforeATRFilter)
            {
                if (!((ATR_OR < maxATROR) && (ATR_OR > minATROR)))
                {
                    return true;
                }
            }

            List<Trade> clean_trades = new List<Trade>();

            foreach (var trade in trades)
            {
                if (!((trade.Order.getOrderProfit() == 0) && (trade.Order.OrderType == biiuse.OrderType.FINAL)))
                {
                    clean_trades.Add(trade);
                }
            }

            /*Trade lastTrade = null; 
            if (clean_trades.Count > 0) {
                lastTrade = clean_trades[clean_trades.Count - 1];
                */



            int streak = 0;
            foreach (var trade in clean_trades)
            {

                if (wasConcurrentlyOpen(clean_trades.IndexOf(trade), clean_trades))
                {
                    streak++;
                    Print("TRADE: " + trade.getId() + " is concurrently open. Increasing index \n");
                    continue;
                }


                if (((trade.Order.OrderType == biiuse.OrderType.FINAL) && (trade.Order.getOrderProfit() < 0)) || (trade.getTradeClosedDate().Equals(new DateTime())))
                {
                    streak++;
                    Print("TRADE: " + trade.getId() + " is done and a loser. Increasing index \n");
                    continue;
                }

                streak = 0;

            }


            bool amIConcurOpen = false;

            foreach (var trade in clean_trades)
            {
                if (trade.getTradeOpenedDate().Equals(new DateTime())) { amIConcurOpen = true; break; }
            }


            if (amIConcurOpen) streak++;

            Print("STREAK IS: " + streak + "\n");
            return streak >= maxConsLoses;


        }

        private bool simOrder()
        {

            List<Trade> clean_trades = new List<Trade>();

            foreach (var trade in trades)
            {
                if (!((trade.Order.getOrderProfit() == 0) && (trade.Order.OrderType == biiuse.OrderType.FINAL)))
                {
                    clean_trades.Add(trade);
                }
            }


            foreach (var trade in clean_trades)
            {
                if (trade.getTradeOpenedDate().Equals(new DateTime())) return true;
            }


            int consLoses = 0;

            if (clean_trades.Count > 1)
            {
                int index = clean_trades.Count - 1;

                while (index >= 0)
                {
                    if (wasConcurrentlyOpen(index, clean_trades))
                    {
                        consLoses++;
                        index--;
                        Print(clean_trades[index].getId() + " count as loss as it was concurrently open\n");
                        continue;
                    }

                    if (((clean_trades[index].Order.getOrderProfit() <= 0)))
                    {
                        consLoses++;
                        index--;
                        Print(clean_trades[index].getId() + " count as loss as it a loser");
                        continue;
                    }

                    /*if (((clean_trades[index].getTradeClosedDate().Equals(new DateTime()))))
                    {
                        consLoses++;
                        index--;
                        continue;
                    }*/

                    if ((clean_trades[index].Order.OrderType == biiuse.OrderType.FINAL) && (clean_trades[index].Order.getOrderProfit() > 0))
                    {
                        break;
                    }

                    index--;
                    Print("WARNING!!!!: should never happen");

                }





            }

            return (consLoses > maxConsLoses);

            //bool result = true;
            //int tradesAnalyzed = 0;
            //int index = trades.Count-1;
            //while ((tradesAnalyzed < maxConsLoses) && (index >= 0))
            //{
            //    if ((trades[index].Order.getOrderProfit() > 0) || (trades[index].Order.OrderType == biiuse.OrderType.SELL) || (trades[index].Order.OrderType == biiuse.OrderType.BUY))
            //    {
            //        return false;
            //    }

            //    if (trades[index].Order.getOrderProfit() == 0)
            //    {
            //        index--;
            //        continue;
            //    }
            //    if (trades[index].Order.getOrderProfit() < 0)
            //    {
            //        index--;
            //        tradesAnalyzed++;
            //        continue;
            //    }

            //}
            //if (tradesAnalyzed < maxConsLoses) return false;
            //else return result;
        }

        public override int deinit()
        {
            foreach (var trade in trades)
            {
                //TODO Parametrize filenames
                trade.writeLogToFile("ATR_EA.log", true);
                trade.writeLogToHTML("ATR_EA.html", true);
            }
            return base.deinit();
        }


        private ATR_Type intToATR_Type(int atr)
        {
            switch (atr)
            {
                case 0: return ATR_Type.DAY_TRADE_ORIG;
                case 1: return ATR_Type.DAY_TRADE_2_DAYS;
                case 2: return ATR_Type.SWING_TRADE_5_DAYS;
            }
            return ATR_Type.DAY_TRADE_2_DAYS;
        }


        private Session currSession = null;
        private static DateTime bartime = new DateTime();
        private int sundayLengthInSeconds;
        const int maxNumberOfTrades = 10000;
        List<Trade> trades;
        private ATR_Type atrType;
    }


}
