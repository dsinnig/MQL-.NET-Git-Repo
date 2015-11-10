using NQuotes;
namespace biiuse
{
    internal class ATRTrade : Trade
    {


        private double tenDayRange;
        private double rangeHigh;
        private double rangeLow;
        private int rangePips;
        private double newHHLL;
        private double atr;
        private int lengthIn1MBarsOfWaitingPeriod;
        private double percentageOfATRForMaxRisk;
        private double percentageOfATRForMaxVolatility;
        private double minProfitTarget;
        private int rangeBufferInMicroPips;
        // to be added
        private double rangeRestriction;
        private double askPriceBeforeOrderEntry;
        private double bidPriceBeforeOrderEntry;
        private double RSI;
        private double RSI_5M;
        private double RSI_15M;
        private double RSI_30M;
        private double momentum;
        private double momentum_5M;
        private double momentum_15M;
        private double momentum_30M;
        private double stochastics;
        private double stochastics5M;
        private double stochastics15M;
        private double stochastics30M;
        private bool movAveragePriceCrossOver;
        private bool movAverageCrossOver;
        private string orderType;
        private double currentDailyRange;       
        private Session referenceSession;

        public ATRTrade(bool sim, int _lotDigits, string _logFileName, double _newHHLL, double _ATR, int _lengthIn1MBarsOfWaitingPeriod, double _percentageOfATRForMaxRisk, double _percentageOfATRForMaxVolatility,
            double _minProfitTarget, int _rangeBufferInMicroPips, double _rangeRestriction, double _tenDayRange, Session referenceSession, MqlApi mql4) : base(sim, _lotDigits, _logFileName, mql4)
        {
            this.newHHLL = _newHHLL;
            this.atr = _ATR;
            this.lengthIn1MBarsOfWaitingPeriod = _lengthIn1MBarsOfWaitingPeriod;
            this.percentageOfATRForMaxRisk = _percentageOfATRForMaxRisk;
            this.percentageOfATRForMaxVolatility = _percentageOfATRForMaxVolatility;
            this.minProfitTarget = _minProfitTarget;
            this.rangeBufferInMicroPips = _rangeBufferInMicroPips;
            this.rangeRestriction = _rangeRestriction;

            this.rangeHigh = 0;
            this.rangeLow = 0;
            this.rangePips = 0;
            this.newHHLL = _newHHLL;
            this.tenDayRange = _tenDayRange;
            this.referenceSession = referenceSession;
            this.currentDailyRange = mql4.iHigh(null, MqlApi.PERIOD_D1, 0) - mql4.iLow(null, MqlApi.PERIOD_D1, 0);
            mql4.Print("CurrentDailyRange Daily range is: ", currentDailyRange * OrderManager.getPipConversionFactor(mql4));
        }

        public double getATR()
        {
            return atr;
        }

        public int getLengthIn1MBarsOfWaitingPeriod()
        {
            return this.lengthIn1MBarsOfWaitingPeriod;
        }

        public double getPercentageOfATRForMaxRisk()
        {
            return this.percentageOfATRForMaxRisk;
        }

        public double getPercentageOfATRForMaxVolatility()
        {
            return this.percentageOfATRForMaxVolatility;
        }

        public double getMinProfitTarget()
        {
            return minProfitTarget;
        }

        public int getRangeBufferInMicroPips()
        {
            return rangeBufferInMicroPips;
        }

        public double getRangeRestriction()
        {
            return this.rangeRestriction;
        }

        public void setRangeHigh(double _high)
        {
            this.rangeHigh = _high;
        }
        public double getRangeHigh()
        {
            return this.rangeHigh;
        }
        public void setRangeLow(double _low)
        {
            this.rangeLow = _low;
        }
        public double getRangeLow()
        {
            return this.rangeLow;
        }
        public void setNewHHLL(double _newHHLL)
        {
            this.newHHLL = _newHHLL;
        }
        public double getNewHHLL()
        {
            return this.newHHLL;
        }

        public void setRangePips(int _pips)
        {
            this.rangePips = _pips;
        }

        public int getRangePips()
        {
            return this.rangePips;
        }

        public void setAskPriceBeforeOrderEntry(double _price)
        {
            this.askPriceBeforeOrderEntry = _price;
        }

        public double getAskPriceBeforeOrderEntry()
        {
            return this.askPriceBeforeOrderEntry;
        }

        public void setBidPriceBeforeOrderEntry(double _price)
        {
            this.bidPriceBeforeOrderEntry = _price;
        }

        public double getBidPriceBeforeOrderEntry()
        {
            return this.bidPriceBeforeOrderEntry;
        }

        public void setRSI(double _RSI)
        {
            this.RSI = _RSI;
        }
        public double getRSI()
        {
            return this.RSI;
        }

        public void setRSI5M(double _RSI)
        {
            this.RSI_5M = _RSI;
        }

        public double getRSI5M()
        {
            return this.RSI_5M;
        }

        public void setRSI15M(double _RSI)
        {
            this.RSI_15M = _RSI;
        }
        public double getRSI15M()
        {
            return this.RSI_15M;
        }

        public void setRSI30M(double _RSI)
        {
            this.RSI_30M = _RSI;
        }
        public double getRSI30M()
        {
            return this.RSI_30M;
        }

        public void setMomentum(double _momentum)
        {
            this.momentum = _momentum;
        }
        public double getMomentum()
        {
            return this.momentum;
        }

        public void setMomentum5M(double _momentum)
        {
            this.momentum_5M = _momentum;
        }

        public double getMomentum5M()
        {
            return this.momentum_5M;
        }

        public void setMomentum15M(double _momentum)
        {
            this.momentum_15M = _momentum;
        }

        public double getMomentum15M()
        {
            return this.momentum_15M;
        }

        public void setMomentum30M(double _momentum)
        {
            this.momentum_30M = _momentum;
        }

        public double getMomentum30M()
        {
            return this.momentum_30M;
        }


        public void setMovAveCrossedOverPrice(bool _hasCrossedOver)
        {
            this.movAveragePriceCrossOver = _hasCrossedOver;
        }
        public bool hasMovAveCrossedOverPrice()
        {
            return this.movAveragePriceCrossOver;
        }

        public void setMovAveCrossOver(bool _hasCrossedOver)
        {
            this.movAverageCrossOver = _hasCrossedOver;
        }

        public bool hasMovAveCrossOver()
        {
            return this.movAverageCrossOver;
        }

        public void setStochastics(double _stoch)
        {
            this.stochastics = _stoch;
        }
        public double getStochastics()
        {
            return stochastics;
        }

        public void setStochastics5M(double _stoch)
        {
            this.stochastics5M = _stoch;
        }

        public double getStochastics5M()
        {
            return stochastics5M;
        }

        public void setStochastics15M(double _stoch)
        {
            this.stochastics15M = _stoch;
        }

        public double getStochastics15()
        {
            return stochastics15M;
        }

        public void setStochastics30M(double _stoch)
        {
            this.stochastics30M = _stoch;
        }

        public double getStochastics30M()
        {
            return stochastics30M;
        }

        public void setOrderType(string _orderType)
        {
            this.orderType = _orderType;
        }
        public string getOrderType()
        {
            return this.orderType;
        }

        public double getCurrentDailyRange()
        {
            return this.currentDailyRange;
        }

        public override void writeLogToCSV()
        {
            mql4.ResetLastError();
            int openFlags;
            openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT;
            int filehandle = mql4.FileOpen(this.logFileName, openFlags);
            mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END); //go to the end of the file

            string output;
            //if first entry, write column headers
            if (mql4.FileTell(filehandle) == 0)
            {
                output = "TRADE_ID, ORDER_TICKET, TRADE_TYPE, SYMBOL, ATR, HH/LL, REF_PRICE, TRADE_OPENED_DATE, RANGE_HIGH, RANGE_LOW, RANGE_PIPS, ORDER_PLACED_DATE, ORDER_TYPE, ASK_MARKET_PRICE_AT_ORDER_PLACEMENT, BID_MARKET_PRICE_AT_ORDER_PLACEMENT, 10_DAY_RANGE, CUR_DAILY_RANGE, RSI, RSI_5M, RSI_15M, RSI_30M, MOMENTUM, MOMENTUM_5M, MOMENTUM_15, MOMENTUM_30, STOCH, STOCH_5M, STOCH_15, STOCH_30, STARTING_BALANCE, PLANNED_ENTRY, ORDER_FILLED_DATE, ACTUAL_ENTRY, SPREAD_ORDER_OPEN, INITIAL_STOP_LOSS, REVISED_STOP_LOSS, INITIAL_TAKE_PROFIT, REVISED TAKE_PROFIT, CANCEL_PRICE, ACTUAL_CLOSE, SPREAD_ORDER_CLOSE, POSITION_SIZE, REALIZED PL, COMMISSION, SWAP, ENDING_BALANCE, TRADE_CLOSED_DATE";

                mql4.FileWriteString(filehandle, output, output.Length);
            }

            int indexOfReferenceStart = mql4.iBarShift(mql4.Symbol(), MqlApi.PERIOD_H1, this.referenceSession.getHHLL_ReferenceDateTime(), true);
            double priceAtRefStart = mql4.iClose(mql4.Symbol(), MqlApi.PERIOD_H1, indexOfReferenceStart);

            output = this.id + ", " + this.orderTicket + ", " + tradeType + ", " + mql4.Symbol() + ", " + this.atr * OrderManager.getPipConversionFactor(mql4) + ", " + this.newHHLL + ", " + priceAtRefStart + "," + ExcelUtil.datetimeToExcelDate(this.tradeOpenedDate) + ", " + this.rangeHigh + ", " + this.rangeLow + ", " + this.rangePips + "," + ExcelUtil.datetimeToExcelDate(this.orderPlacedDate) + ", " + this.orderType + ", " + this.askPriceBeforeOrderEntry + ", " + this.bidPriceBeforeOrderEntry + ", " + this.tenDayRange * OrderManager.getPipConversionFactor(mql4) + ", " + this.currentDailyRange * OrderManager.getPipConversionFactor(mql4) + ", " + this.RSI + ", " + this.RSI_5M + ", " + this.RSI_15M + ", " + this.RSI_30M + ", " + this.momentum + ", " + this.momentum_5M + ", " + this.momentum_15M + ", " + this.momentum_30M + ", " + this.stochastics + ", " + this.stochastics5M + ", " + this.stochastics15M + ", " + this.stochastics30M + ", " + this.startingBalance + ", " + this.plannedEntry + "," + ExcelUtil.datetimeToExcelDate(this.orderFilledDate) + ", " + this.actualEntry + ", " + this.spreadOrderOpen + ", " + this.originalStopLoss + ", " + this.stopLoss + ", " + this.initialProfitTarget + ", " + this.takeProfit + ", " + this.cancelPrice + ", " + this.actualClose + ", " + this.spreadOrderClose + ", " + this.positionSize + ", " + this.realizedPL + ", " + this.commission + ", " + this.swap + ", " + this.endingBalance + "," + ExcelUtil.datetimeToExcelDate(this.tradeClosedDate);

            mql4.FileWriteString(filehandle, "\n", 1);
            mql4.FileWriteString(filehandle, output, output.Length);


            mql4.FileClose(filehandle);
        }
    }
}

