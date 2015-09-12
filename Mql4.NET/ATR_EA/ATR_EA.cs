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
                    ATRTrade trade = new ATRTrade(lotDigits, logFileName, currSession.getHighestHigh(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), this);
                    trade.setState(new HighestHighReceivedEstablishingEligibilityRange(trade, this));
                    trades.Add(trade);
                }


                if (updateResult == -1)
                {
                    Print("Tradeable Lowest Low found: ", currSession.getLowestLow(), " Time: ", currSession.getLowestLowTime());
                    ATRTrade trade = new ATRTrade(lotDigits, logFileName, currSession.getLowestLow(), currSession.getATR(), lengthOfGracePeriod, maxRisk, maxVolatility, minProfitTarget, rangeBuffer, rangeRestriction, currSession.getTenDayHigh() - currSession.getTenDayLow(), this);
                    trade.setState(new LowestLowReceivedEstablishingEligibilityRange(trade, this));
                    trades.Add(trade);
                 }
            }


            return base.start();


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
