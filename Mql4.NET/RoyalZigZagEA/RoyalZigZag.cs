using System;
using NQuotes;

namespace biiuse
{
    public class RoyalZigZag : NQuotes.MqlApi
    {

        [ExternVariable]
        public double stopLossPips = 250.0;
        [ExternVariable]
        public double lotSize = 0.3;
        [ExternVariable]
        public string logFileName = "tradeLogRoyalZigZag.csv";


        private int lotDigits;
        private RoyalZigZagTrade currentLongTrade;
        private RoyalZigZagTrade currentShortTrade;
        private static System.DateTime lastbar = new System.DateTime();

        public override int init()
        {
            lotDigits = 1;
            currentLongTrade = null;
            currentShortTrade = null;
            return base.init();
        }

        private bool isNewBar()
        {
            System.DateTime curbar = Time[0];
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

        public override int start()
        {
            int hour = TimeCurrent().Hour;
            int day = TimeCurrent().Day;
            int minute = TimeCurrent().Minute;
            
            
            //check of trade is closed. If yes, destroy object and set pointer back to NULL
            if (currentLongTrade != null)
            {
                if (currentLongTrade.isInFinalState())
                {
                    Print("Long Trade closed, object deleted");
                    currentLongTrade = null;
                }
                else currentLongTrade.update();
            }
            if (currentShortTrade != null)
            {
                if (currentShortTrade.isInFinalState())
                {
                    Print("Short Trade closed, object deleted");
                    currentShortTrade = null;
                }
                else currentShortTrade.update();
            }
            if (isNewBar())
            {
                string[] parameters = { "144", "89", "55" };
                string paramsPack = String.Join("|", parameters);

                double low = iCustom(Symbol(), 0, "AK_ZigZag ROYAL Pointer NET", paramsPack, 0, 1);
                double high = iCustom(Symbol(), 0, "AK_ZigZag ROYAL Pointer NET", paramsPack, 1, 1);

                if (low != 0.0)
                {
                    //Print("New low arrow printed at ", NormalizeDouble(low, 5));
                    //no long trade established - go long at market
                    if (currentLongTrade == null)
                    {
                        //place Order
                        Print("Going Long...");
                        double factor = OrderManager.getPipConversionFactor(this);
                        double stopLoss = Ask - stopLossPips / factor;
                        currentLongTrade = new RoyalZigZagTrade(lotDigits, logFileName, this);
                        ErrorType result = OrderManager.submitNewOrder(OP_BUY, Ask, stopLoss, 0, 0, lotSize, currentLongTrade, this);
                        currentLongTrade.setState(new FXEdgeLong(currentLongTrade, this));
                    }
                }
                if (high != 0.0)
                {
                    //rint("New high arrow printed at ", NormalizeDouble(high, 5));
                    //no short trade established - go short at market
                    if (currentShortTrade == null)
                    {
                        //place Order
                        Print("Going Short....");
                        double factor = OrderManager.getPipConversionFactor(this);
                        double stopLoss = Bid + stopLossPips / factor;
                        currentShortTrade = new RoyalZigZagTrade(lotDigits, logFileName, this);
                        ErrorType result = OrderManager.submitNewOrder(OP_SELL, Bid, stopLoss, 0, 0, lotSize, currentShortTrade, this);
                        currentShortTrade.setState(new FXEdgeShort(currentShortTrade, this));
                    }
                }

            }
            return base.start();
        }
    }



}
