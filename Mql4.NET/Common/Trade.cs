using System;
using NQuotes;

namespace biiuse
{
    public enum TradeType
    {
        FLAT,
        LONG,
        SHORT
    };

    public class Trade : MQL4Wrapper
    {
        protected string id;
        protected double startingBalance;
        protected double endingBalance;
        protected double plannedEntry;
        protected double actualEntry;
        protected double stopLoss;
        protected double originalStopLoss;
        protected double takeProfit;
        protected double cancelPrice;
        protected double actualClose;
        protected double initialProfitTarget;
        protected double positionSize;
        protected int lotDigits;
        protected double realizedPL;
        //protected double commission;
        //protected double swap;
        protected int spreadOrderOpen;
        protected int spreadOrderClose;
        protected TradeType tradeType;

        protected System.DateTime tradeOpenedDate;
        protected System.DateTime orderPlacedDate;
        protected System.DateTime orderFilledDate;
        protected System.DateTime tradeClosedDate;

        protected string logFileName;

        private TradeState state;
        private bool finalState;
        private string[] log = new string[500];
        private int logSize;
        private const int OFFSET = (-7) * 60 * 60;
        private Order order;

        public Order Order
        {
            get
            {
                return order;
            }

            set
            {
                order = value;
            }
        }

        public Trade(bool sim, int _lotDigits, string _logFileName, NQuotes.MqlApi mql4) : base(mql4)
        {
            this.logFileName = _logFileName;
            this.tradeType = TradeType.FLAT;
            this.startingBalance = mql4.AccountBalance();
            this.endingBalance = 0;
            this.lotDigits = _lotDigits;
            this.state = null;
            this.actualEntry = -1;
            this.actualClose = -1;
            this.takeProfit = 0;
            this.cancelPrice = 0;
            this.initialProfitTarget = 0;
            this.plannedEntry = 0;
            this.stopLoss = 0;
            this.originalStopLoss = 0;
            this.positionSize = 0;
            this.logSize = 0;
            this.realizedPL = 0.0;
            //this.commission = 0.0;
            //this.swap = 0.0;
            this.spreadOrderOpen = -1;
            this.spreadOrderClose = -1;

            this.tradeOpenedDate = mql4.TimeCurrent();
            this.orderPlacedDate = new DateTime();
            this.orderFilledDate = new DateTime();
            this.tradeClosedDate = new DateTime();

            this.finalState = false;

            

            if (sim)
            {
              
                this.order = new biiuse.SimOrder(this, mql4);
                this.id = "SIM_"+ mql4.Symbol() + (mql4.TimeCurrent() + TimeSpan.FromSeconds(OFFSET)).ToString();
            } else
            {
                this.order = new Order(this, mql4);
                this.id = mql4.Symbol() + (mql4.TimeCurrent() + TimeSpan.FromSeconds(OFFSET)).ToString();
            }
            
            if (!mql4.IsTesting())
            {
                string filename = mql4.Symbol() + "_" + mql4.TimeCurrent().ToString();
                int filehandle = mql4.FileOpen(filename, MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT);
                mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END);
                string output = "****Trade: " + this.id + " ****";
                mql4.FileWriteString(filehandle, output, output.Length);
                mql4.FileWriteString(filehandle, "\n", 1);
                mql4.FileClose(filehandle);
            }
        }


        /*

        public virtual ErrorType submitNewOrder(int orderType, double _entryPrice, double _stopLoss, double _takeProfit, double _cancelPrice, double _positionSize)
        {
            return OrderManager.submitNewOrder(orderType, _entryPrice, _stopLoss, _takeProfit, _cancelPrice, _positionSize, this, mql4);
        }

        public virtual ErrorType modifyOrder(double newOpenPrice, double newStopLoss, double newTakeProfit)
        {
            return OrderManager.modifyOrder(this.orderTicket, newOpenPrice, newStopLoss, newTakeProfit, this, mql4);
        }

        public virtual ErrorType deleteOrder()
        {
            return OrderManager.deleteOrder(this.orderTicket, this, mql4);
        }

        public virtual int getOrderStatus()  
        {
            if (mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET)) return mql4.OrderType();
            else return -1;
        }

        public virtual DateTime getOrderCloseTime()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderCloseTime();
        }

        public virtual double getOrderProfit()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderProfit();
        }

        public virtual double getOrderCommission()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderCommission();
        }

        public virtual double getOrderSwap()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderSwap();
        }


        public virtual double getOrderOpenPrice()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderOpenPrice();
        }

        public virtual double getOrderClosePrice()
        {
            mql4.OrderSelect(this.orderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderClosePrice();
        }

    */

        public virtual void update()
        {
            if ((order.OrderType != OrderType.INIT) && (order.OrderType != OrderType.FINAL)) order.update();
            if (state != null)
                state.update();
        }

        public void addLogEntry(string entry, bool print)
        {
            this.log[logSize] = (mql4.TimeCurrent() + TimeSpan.FromSeconds(OFFSET)).ToString() + ": " + entry;
            logSize++;

            if (!mql4.IsTesting())
            {
                //write to file
                string filename = mql4.Symbol() + "_" + mql4.TimeToStr(mql4.TimeCurrent(), MqlApi.TIME_DATE);
                int filehandle = mql4.FileOpen(filename, MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT);
                mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END);
                string output = (mql4.TimeCurrent() + TimeSpan.FromSeconds(OFFSET)).ToString() + ": " + entry;
                mql4.FileWriteString(filehandle, output, output.Length);
                mql4.FileWriteString(filehandle, "\n", 1);
                mql4.FileClose(filehandle);

            }
            //enable this only when in Not Testmode or in DEBUG mode
            if (print) 
            mql4.Print(mql4.TimeToStr(mql4.TimeCurrent(), MqlApi.TIME_DATE | MqlApi.TIME_SECONDS) + ": TradeID: " + this.id + " " + entry);
        }

        public void printLog()
        {
            mql4.Print("****Trade: " + this.id, " ****");
            for (int i = 0; i < logSize; ++i)
            {
                mql4.Print(log[i]);
            }
        }

        public void writeLogToFile(string filename, bool append)
        {
            mql4.ResetLastError();
            int openFlags;
            if (append)
                openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT;
            else
                openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_TXT;

            int filehandle = mql4.FileOpen(filename, openFlags);
            if (append)
                mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END);
            string output = "****Trade: " + this.id + " ****";
            mql4.FileWriteString(filehandle, output, output.Length);
            mql4.FileWriteString(filehandle, "\n", 1);
            for (int i = 0; i < logSize; ++i)
            {
                mql4.FileWriteString(filehandle, log[i], log[i].Length);
                mql4.FileWriteString(filehandle, "\n", 1);
            }
            mql4.FileClose(filehandle);
        }

        public void writeLogToHTML(string filename, bool append)
        {
            mql4.ResetLastError();
            int openFlags;
            if (append)
                openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT;
            else
                openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_TXT;

            int filehandle = mql4.FileOpen(filename, openFlags);
            if (append)
            {
                mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END);
            }
            string output = "<b>****Trade: " + this.id + " **** </b>";
            mql4.FileWriteString(filehandle, output, output.Length);
            mql4.FileWriteString(filehandle, "\n", 1);
            output = "<ul>";
            mql4.FileWriteString(filehandle, output, output.Length);
            for (int i = 0; i < logSize; ++i)
            {
                output = "<li>" + log[i] + "</li>";
                mql4.FileWriteString(filehandle, output, output.Length);
                mql4.FileWriteString(filehandle, "\n", 1);
            }
            output = "<ul>";
            mql4.FileWriteString(filehandle, output, output.Length);
            mql4.FileClose(filehandle);
        }


        

        public virtual void writeLogToCSV()
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
                output = "TRADE_ID, ORDER_TICKET, TRADE_TYPE, SYMBOL, TRADE_OPENED_DATE, ORDER_PLACED_DATE, STARTING_BALANCE, PLANNED_ENTRY, ORDER_FILLED_DATE, ACTUAL_ENTRY, SPREAD_ORDER_OPEN, INITIAL_STOP_LOSS, REVISED_STOP_LOSS, INITIAL_TAKE_PROFIT, REVISED TAKE_PROFIT, CANCEL_PRICE, ACTUAL_CLOSE, SPREAD_ORDER_CLOSE, POSITION_SIZE, REALIZED PL, COMMISSION, SWAP, ENDING_BALANCE, TRADE_CLOSED_DATE";
                mql4.FileWriteString(filehandle, output, output.Length);
            }
            output = this.id + ", " + this.Order.OrderTicket + ", " + this.tradeType + ", " + mql4.Symbol() + ", " + ExcelUtil.datetimeToExcelDate(this.tradeOpenedDate) + ", " + ExcelUtil.datetimeToExcelDate(this.orderPlacedDate) + ", " + this.startingBalance + ", " + this.plannedEntry + ", " + ExcelUtil.datetimeToExcelDate(this.orderFilledDate) + ", " + this.actualEntry + ", " + this.spreadOrderOpen + ", " + this.originalStopLoss + ", " + this.stopLoss + ", " + this.initialProfitTarget + ", " + this.takeProfit + ", " + this.cancelPrice + ", " + this.actualClose + ", " + this.spreadOrderClose + ", " + this.positionSize + ", " + this.realizedPL + ", " + this.Order.getOrderCommission() + ", " + this.Order.getOrderSwap() + ", " + this.endingBalance + ", " + ExcelUtil.datetimeToExcelDate(this.tradeClosedDate);
            mql4.FileWriteString(filehandle, "\n", 1);
            mql4.FileWriteString(filehandle, output, output.Length);
            mql4.FileClose(filehandle);
        }


        public void setState(TradeState aState)
        {
            this.state = aState;
        }

        public string getId()
        {
            return id;
        }

        public void setPlannedEntry(double entry)
        {
            this.plannedEntry = entry;
        }

        public double getPlannedEntry()
        {
            return this.plannedEntry;
        }

        public void setActualEntry(double entry)
        {
            this.actualEntry = entry;
        }

        public double getActualEntry()
        {
            return this.actualEntry;
        }

        public void setStopLoss(double sL)
        {
            this.stopLoss = sL;
        }

        public double getStopLoss()
        {
            return this.stopLoss;
        }

        public void setOriginalStopLoss(double sL)
        {
            this.originalStopLoss = sL;
        }

        public double getOriginalStopLoss()
        {
            return this.originalStopLoss;
        }

        public void setTakeProfit(double tP)
        {
            this.takeProfit = tP;
        }

        public double getTakeProfit()
        {
            return this.takeProfit;
        }

        public void setCancelPrice(double cP)
        {
            this.cancelPrice = cP;
        }

        public double getCancelPrice()
        {
            return this.cancelPrice;
        }

        public void setPositionSize(double p)
        {
            this.positionSize = p;
        }

        public double getPositionSize()
        {
            return this.positionSize;
        }

        public void setInitialProfitTarget(double target)
        {
            this.initialProfitTarget = target;
        }

        public double getInitialProfitTarget()
        {
            return this.initialProfitTarget;
        }

        public void setActualClose(double close)
        {
            this.actualClose = close;
        }

        public double getActualClose()
        {
            return this.actualClose;
        }

        public void setLotDigits(int _lotDigits)
        {
            this.lotDigits = _lotDigits;
        }

        public int getLotDigits()
        {
            return lotDigits;
        }

        public void setRealizedPL(double _realizedPL)
        {
            this.realizedPL = _realizedPL;
        }
        public double getRealizedPL()
        {
            return this.realizedPL;
        }

        public void setStartingBalance(double _balance)
        {
            this.startingBalance = _balance;
        }

        public double getStartingBalance()
        {
            return startingBalance;
        }

        public void setEndingBalance(double _balance)
        {
            this.endingBalance = _balance;
        }

        public double getEndingBalance()
        {
            return endingBalance;
        }

        public void setTradeOpenedDate(System.DateTime _date)
        {
            this.tradeOpenedDate = _date;
        }

        public System.DateTime getTradeOpenedDate()
        {
            return this.tradeOpenedDate;
        }

        public void setOrderPlacedDate(System.DateTime _date)
        {
            this.orderPlacedDate = _date;
        }

        public System.DateTime getOrderPlacedDate()
        {
            return this.orderPlacedDate;
        }

        public void setOrderFilledDate(System.DateTime _date)
        {
            this.orderFilledDate = _date;
        }

        public System.DateTime getOrderFilledDate()
        {
            return this.orderFilledDate;
        }

        public void setTradeClosedDate(System.DateTime _date)
        {
            this.tradeClosedDate = _date;
        }

        public System.DateTime getTradeClosedDate()
        {
            return this.tradeClosedDate;
        }

        public void setSpreadOrderOpen(int _spread)
        {
            this.spreadOrderOpen = _spread;
        }

        public int getSpreadOrderOpen()
        {
            return this.spreadOrderOpen;
        }

        public void setSpreadOrderClose(int _spread)
        {
            this.spreadOrderClose = _spread;
        }

        public int getSpreadOrderClose()
        {
            return this.spreadOrderClose;
        }

        public void setTradeType(TradeType _type)
        {
            this.tradeType = _type;
        }

        public TradeType getTradeType()
        {
            return this.tradeType;
        }

        public bool isInFinalState()
        {
            return this.finalState;
        }

        public void setFinalStateFlag()
        {
            this.finalState = true;
        }
    }
}
