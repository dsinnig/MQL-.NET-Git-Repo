using System;
using System.Collections.Generic;
using NQuotes;

namespace biiuse
{
    class ATR_EA : NQuotes.MqlApi
    {
        //TODO Add external paramters to wrapper mql4 file
        [ExternVariable]
        public int sundayLengthInHours = 7; //Length of Sunday session in hours
        [ExternVariable]
        public int HHLL_Threshold = 60; //Time in minutes after last HH / LL before a tradeable HH/LL can occur
        [ExternVariable]
        public int lengthOfGracePeriod = 10; //Length in 1M bars of Grace Period after a tradeable HH/LL occured
        [ExternVariable]
        public double rangeRestriction = 80; //Min range of Grace Period
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
        public int maxConsLoses = 4; //max consecutive losses before stop trading
       [ExternVariable]
        public string logFileName = "tradeLog.csv"; //path and filename for CSV trade log

        public override int init()
        {
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
            if (!bartime.Equals(Time[0]))
            {
                //TODO verify that identity makes it still work
                bartime = Time[0];

                Session newSession = SessionFactory.getCurrentSession(sundayLengthInSeconds, HHLL_Threshold, this);
                if (currSession != newSession)
                {
                    currSession = newSession;

                    if (!currSession.tradingAllowed())
                    {
                        Print(TimeCurrent(), " Start session: ", currSession.getName(), " NO NEW TRADES ALLOWED.");
                    }
                    else
                    {

                        /*Print(TimeCurrent()," Start session: ", currSession.getName()," Start: ",currSession.getSessionStartTime()," End: ",currSession.getSessionEndTime()," ATR: ",currSession.getATR(), " (", (int) (currSession.getATR() * 100000), ") micro pips");
                        Print (" Ref: ",currSession.getHHLL_ReferenceDateTime(), " HH: ", currSession.getHighestHigh(), "@ ", currSession.getHighestHighTime(),
                               " LL: ", currSession.getLowestLow(), "@ ", currSession.getLowestLowTime());

                        */

                        Print(TimeCurrent(), " Start session: ", currSession.getName(), " ATR: ", NormalizeDouble(currSession.getATR(), Digits), " (", (int)(currSession.getATR() * 100000), " micro pips)", ", HH: ", currSession.getHighestHigh(), ", LL: ", currSession.getLowestLow());

                        Print("Reference date: ", currSession.getHHLL_ReferenceDateTime());

                    }
                }
            }

            foreach (var trade in trades)
            {
                if (trade != null) trade.update();
            }

            
            int updateResult = currSession.update(Close[0]);

            if (currSession.tradingAllowed())
            {
                if (updateResult == 1)
                {
                    Print("Tradeable Highest High found: ", currSession.getHighestHigh(), " Time: ", currSession.getHighestHighTime());

                    ATRTrade trade = new ATRTrade(simOrder2(), lotDigits, logFileName, currSession.getHighestHigh(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), this);
                    trade.setState(new HighestHighReceivedEstablishingEligibilityRange(trade, this));
                    trades.Add(trade);
                }


                if (updateResult == -1)
                {
                    Print("Tradeable Lowest Low found: ", currSession.getLowestLow(), " Time: ", currSession.getLowestLowTime());
                    ATRTrade trade = new ATRTrade(simOrder2(), lotDigits, logFileName, currSession.getLowestLow(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), this);
                    trade.setState(new LowestLowReceivedEstablishingEligibilityRange(trade, this));
                    trades.Add(trade);
                 }
            }
            return base.start();
        }


        private bool simOrder(bool isLong)
        {
            int tradesAnalyzed = 0;
            int index = trades.Count - 1;
            while ((tradesAnalyzed < 10) && (index >= 0))
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
            if (clean_trades.Count-1 >= index)
            {
                Trade prevTrade = clean_trades[index];
                //find if it was concurrently open
                int i = index-1;
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
            return streak > maxConsLoses;
            

        }

        private bool simOrder()
        {

            List<Trade> clean_trades = new List<Trade>();

            foreach (var trade in trades)
            {
                if (!((trade.Order.getOrderProfit() == 0) && (trade.Order.OrderType == biiuse.OrderType.FINAL))) {
                    clean_trades.Add(trade);
                }
            }

            
            foreach (var trade in clean_trades)
            {
                if (trade.getTradeOpenedDate().Equals(new DateTime())) return true;
            }
                       

            int consLoses = 0;

            if (clean_trades.Count > 1) {
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
            foreach(var trade in trades)
            {
                //TODO Parametrize filenames
                trade.writeLogToFile("ATR_EA.log", true);
                trade.writeLogToHTML("ATR_EA.html", true);
            }
            return base.deinit();
        }

        private Session currSession = null;
        private static DateTime bartime = new DateTime();
        private int sundayLengthInSeconds;
        const int maxNumberOfTrades = 10000;
        List<Trade> trades;
    }
}
