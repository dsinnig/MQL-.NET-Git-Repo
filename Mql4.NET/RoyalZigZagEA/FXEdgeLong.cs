using NQuotes;
using System;

namespace biiuse
{
    class FXEdgeLong : TradeState
    {
        public FXEdgeLong(RoyalZigZagTrade _context, MqlApi mql4) : base(mql4)
        {
            this.context = _context;
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

            //check if this actually works
            if (!mql4.OrderCloseTime().Equals(new System.DateTime()))
            {
                double pips = mql4.MathAbs(mql4.OrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);
                string logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
                context.addLogEntry("Stop loss triggered @" + mql4.DoubleToString(mql4.OrderClosePrice(), mql4.Digits) + " " + logMessage, true);
                context.addLogEntry("P/L: $" + mql4.DoubleToString(mql4.OrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(mql4.OrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(mql4.OrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);

                context.setRealizedPL(mql4.OrderProfit());
                context.setCommission(mql4.OrderCommission());
                context.setSwap(mql4.OrderSwap());

                context.setActualClose(mql4.OrderClosePrice());
                context.setState(new TradeClosed(context, mql4));
                return;
            }

            //if new bar, check if previous bar printed a high arrow
            if (isNewBar())
            {

                string[] parameters =
    {
                    "144", "89", "55"
                };
                string paramsPack = String.Join("|", parameters);

                double high = mql4.iCustom(null, 0, "AK_ZigZag ROYAL Pointer NET", paramsPack, 1, 1);
                if (high != 0)
                {
                    bool successClose = mql4.OrderClose(context.getOrderTicket(), context.getPositionSize(), mql4.Bid, 10);
                    double pips = mql4.MathAbs(mql4.OrderClosePrice() - context.getActualEntry()) * OrderManager.getPipConversionFactor(mql4);
                    string logMessage = "Loss of " + mql4.DoubleToString(pips, 1) + " micro pips.";
                    context.addLogEntry("High Arrow found - Closing trade @" + mql4.DoubleToString(mql4.OrderClosePrice(), mql4.Digits) + " " + logMessage, true);
                    context.addLogEntry("P/L: $" + mql4.DoubleToString(mql4.OrderProfit(), 2) + "; Commission: $" + mql4.DoubleToString(mql4.OrderCommission(), 2) + "; Swap: $" + mql4.DoubleToString(mql4.OrderSwap(), 2) + "; New Account balance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), true);

                    context.setRealizedPL(mql4.OrderProfit());
                    context.setCommission(mql4.OrderCommission());
                    context.setSwap(mql4.OrderSwap());

                    context.setActualClose(mql4.OrderClosePrice());
                    context.setState(new TradeClosed(context, mql4));
                }
            }
        }

        private RoyalZigZagTrade context;

        private System.DateTime lastbar = new System.DateTime();
        private bool isNewBar()
        {
            System.DateTime curbar = mql4.Time[0];
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
    };
}