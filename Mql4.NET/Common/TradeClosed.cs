using NQuotes;

namespace biiuse
{
    //Generic TradeState
    class TradeClosed : TradeState
    {


        public TradeClosed(Trade aContext, MqlApi mql4) : base(mql4)
        {
            this.context = aContext;
            this.context.Order.OrderType = OrderType.FINAL;
            context.setEndingBalance(mql4.AccountBalance());
            context.setTradeClosedDate(mql4.TimeCurrent());
            context.setSpreadOrderClose((int)mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_SPREAD));
            context.addLogEntry("Trade is closed", true);

            context.writeLogToCSV();

            context.setFinalStateFlag();

        }
        public override void update()
        {

        }

        private Trade context;
    }
}

