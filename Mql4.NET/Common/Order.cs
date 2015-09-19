using System;
using System.Collections.Generic;
using System.Text;
using NQuotes;

namespace biiuse
{

    public enum OrderType
    {
        INIT, BUY_LIMIT, SELL_LIMIT, BUY_STOP, SELL_STOP, BUY, SELL, FINAL
    }

    public class Order : MQL4Wrapper
    {
        public Order(Trade trade, MqlApi mql4) : base(mql4)
        {
            this.Trade = trade;
            this.orderType = OrderType.INIT;
        }
        public virtual void update()
        {
            State.update();
        }

        public virtual ErrorType submitNewOrder(int mql4OrderType, double _entryPrice, double _stopLoss, double _takeProfit, double _cancelPrice, double _positionSize)
        {
            if (this.orderType != OrderType.INIT)
            {
                Trade.addLogEntry("Order already submitted", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }

            int maxSlippage = 4;
            int magicNumber = 0;
            System.DateTime expiration = new DateTime();
            System.Drawing.Color arrowColor = System.Drawing.Color.Red;

            double entryPrice = (double)Decimal.Round((decimal)_entryPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double stopLoss = (double)Decimal.Round((decimal)_stopLoss, mql4.Digits, MidpointRounding.AwayFromZero);
            double takeProfit = (double)Decimal.Round((decimal)_takeProfit, mql4.Digits, MidpointRounding.AwayFromZero);
            double cancelPrice = (double)Decimal.Round((decimal)_cancelPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double positionSize = (double)Decimal.Round((decimal)_positionSize, 2, MidpointRounding.AwayFromZero);

            //will be override later on
            OrderType orderType;

            string orderTypeStr;
            string entryPriceStr = "";
            string stopLossStr = "";
            string takeProfitStr = "";
            string cancelPriceStr = "";
            string positionSizeStr = "";

            switch (mql4OrderType)
            {
                case MqlApi.OP_BUY: { orderTypeStr = "BUY Market Order"; orderType = OrderType.BUY;  break; }
                case MqlApi.OP_SELL: { orderTypeStr = "SELl Market Order"; orderType = OrderType.SELL; break; }
                case MqlApi.OP_BUYLIMIT:
                    {
                        orderTypeStr = "BUY Limit Order";
                        orderType = OrderType.BUY_LIMIT;
                        if (mql4.Ask - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                            
                        }
                        break;
                    }
                case MqlApi.OP_SELLLIMIT:
                    {
                        orderTypeStr = "SELL Limit Order";
                        orderType = OrderType.SELL_LIMIT;
                        if (entryPrice - mql4.Bid < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                            
                        }
                        break;
                    }
                case MqlApi.OP_BUYSTOP:
                    {
                        orderTypeStr = "BUY Stop Order";
                        orderType = OrderType.BUY_STOP;
                        //check if entryPrice is too close to market price and adjust accordingly

                        if (entryPrice - mql4.Ask < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                            
                        }
                        break;
                    }
                case MqlApi.OP_SELLSTOP:
                    {
                        orderTypeStr = "SELL Stop Order";
                        orderType = OrderType.SELL_STOP;
                        if (mql4.Bid - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                            
                        }
                        break;
                    }
                default: { Trade.addLogEntry("Invalid Order Type. Abort Trade", true); return ErrorType.NON_RETRIABLE_ERROR; }
            }


            if (entryPrice != 0) entryPriceStr = "; entry price: " + mql4.DoubleToString(entryPrice, mql4.Digits);
            if (stopLoss != 0) stopLossStr = "; stop loss: " + mql4.DoubleToString(stopLoss, mql4.Digits);
            if (takeProfit != 0) takeProfitStr = "; take profit: " + mql4.DoubleToString(takeProfit, mql4.Digits);
            if (cancelPrice != 0) cancelPriceStr = "; cancel price: " + mql4.DoubleToString(cancelPrice, mql4.Digits);

            positionSizeStr = "; position size: " + mql4.DoubleToString(positionSize, 2) + " lots";

            Trade.addLogEntry("Attemting to place " + orderTypeStr + entryPriceStr + stopLossStr + takeProfitStr + cancelPriceStr + positionSizeStr, true);

            int ticket = mql4.OrderSend(mql4.Symbol(), mql4OrderType, positionSize, entryPrice, maxSlippage, stopLoss, takeProfit, Trade.getId(), magicNumber, expiration, arrowColor);

            ErrorType result = analzeAndProcessResult(Trade, mql4);

            if (result == ErrorType.NO_ERROR)
            {
                this.OrderTicket = ticket;
                this.entryPrice = entryPrice;
                this.positionSize = positionSize;
                this.stopLoss = stopLoss;
                this.takeProfit = takeProfit;
                this.cancelPrice = cancelPrice;
                this.orderType = orderType;
                this.state = new Pending(this, mql4);
            }
            return result;
        }

        public virtual ErrorType deleteOrder()
        {
            if ((orderType != OrderType.SELL_LIMIT) && (orderType != OrderType.SELL_STOP) && (orderType != OrderType.BUY_LIMIT) && (orderType == OrderType.SELL_STOP))
            {
                Trade.addLogEntry("Filled orded cannot be delete, it must be closed", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }


            Trade.addLogEntry("Attemting to delete Order (ticket number: " + mql4.IntegerToString(OrderTicket) + ")", true);
            mql4.ResetLastError();
            bool success = mql4.OrderDelete(OrderTicket, System.Drawing.Color.Red);
            //TODO include error handling here
            this.state = new Final(this, mql4);
            this.OrderType = OrderType.FINAL;
            return analzeAndProcessResult(Trade, mql4);
        }

        //TODO add OrderClose method

        public virtual ErrorType modifyOrder(double newOpenPrice, double newStopLoss, double newTakeProfit)
        {
            if ((orderType == OrderType.INIT) || (OrderType == OrderType.FINAL))
            {
                Trade.addLogEntry("Order not open", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }

            System.DateTime expiration = new DateTime();
            System.Drawing.Color arrowColor = System.Drawing.Color.Red;

            newOpenPrice = (double)Decimal.Round((decimal)newOpenPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            newStopLoss = (double)Decimal.Round((decimal)newStopLoss, mql4.Digits, MidpointRounding.AwayFromZero);
            newTakeProfit = (double)Decimal.Round((decimal)newTakeProfit, mql4.Digits, MidpointRounding.AwayFromZero);

            string newOpenPriceStr = "";
            string newStopLossStr = "";
            string newTakeProfitStr = "";

            if (newOpenPrice != 0.0) newOpenPriceStr = "; entry price: " + mql4.DoubleToString(newOpenPrice, mql4.Digits);
            if (newStopLoss != 0.0) newStopLossStr = "; stop loss: " + mql4.DoubleToString(newStopLoss, mql4.Digits);
            if (newTakeProfit != 0.0) newTakeProfitStr = "; take profit: " + mql4.DoubleToString(newTakeProfit, mql4.Digits);

            Trade.addLogEntry("Attemting to modify order to: NewOpenPrice: " + newOpenPriceStr + " NewStopLoss: " + newStopLossStr + " NewTakeProfit: " + newTakeProfit, true);

            bool res = mql4.OrderModify(OrderTicket, newOpenPrice, newStopLoss, newTakeProfit, expiration, arrowColor);

            ErrorType result = analzeAndProcessResult(Trade, mql4);

            if (result == ErrorType.NO_ERROR)
            {
                this.entryPrice = newOpenPrice;
                this.stopLoss = newStopLoss;
                this.takeProfit = newTakeProfit;
            }

            return result;

        }

        public virtual DateTime getOrderCloseTime()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderCloseTime();
        }

        public virtual double getOrderProfit()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderProfit();
        }

        public virtual double getOrderCommission()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderCommission();
        }

        public virtual double getOrderSwap()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderSwap();
        }


        public virtual double getOrderOpenPrice()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderOpenPrice();
        }

        public virtual double getOrderClosePrice()
        {
            mql4.OrderSelect(this.OrderTicket, MqlApi.SELECT_BY_TICKET);
            return mql4.OrderClosePrice();
        }


        private OrderState state;
        private Trade trade;

        //private int orderType;
        private double entryPrice;
        private double stopLoss;
        private double takeProfit;
        private double cancelPrice;
        private double positionSize;
        private OrderType orderType;
        private int orderTicket;


        public static ErrorType analzeAndProcessResult(Trade trade, MqlApi mql4)
        {
            int result = mql4.GetLastError();
            switch (result)
            {
                //No Error
                case 0: return (ErrorType.NO_ERROR);
                // Not crucial errors                  
                case 4:
                    mql4.Alert("Trade server is busy");
                    trade.addLogEntry("Trade server is busy. Waiting 3000ms and then re-try", true);
                    mql4.Sleep(3000);
                    return (ErrorType.RETRIABLE_ERROR);
                case 135:
                    mql4.Alert("Price changed. Refreshing Rates");
                    trade.addLogEntry("Price changed. Refreshing Rates and retry", true);
                    mql4.RefreshRates();
                    return (ErrorType.RETRIABLE_ERROR);
                case 136:
                    mql4.Alert("No prices. Refreshing Rates and retry");
                    trade.addLogEntry("No prices. Refreshing Rates and retry", true);
                    while (mql4.RefreshRates() == false)
                        mql4.Sleep(1);
                    return (ErrorType.RETRIABLE_ERROR);
                case 137:
                    mql4.Alert("Broker is busy");
                    trade.addLogEntry("Broker is busy. Waiting 3000ms and then re-try", true);
                    mql4.Sleep(3000);
                    return (ErrorType.RETRIABLE_ERROR);
                case 146:
                    mql4.Alert("Trading subsystem is busy.");
                    trade.addLogEntry("Trade system is busy. Waiting 500ms and then re-try", true);
                    mql4.Sleep(500);
                    return (ErrorType.RETRIABLE_ERROR);
                // Critical errors      
                case 2:
                    mql4.Alert("Common error.");
                    trade.addLogEntry("Common error. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                case 5:
                    mql4.Alert("Old terminal version.");
                    trade.addLogEntry("Old terminal version. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                case 64:
                    mql4.Alert("Account blocked.");
                    trade.addLogEntry("Account blocked. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                case 133:
                    mql4.Alert("Trading forbidden.");
                    trade.addLogEntry("Trading forbidden. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                case 134:
                    mql4.Alert("Not enough money to execute operation.");
                    trade.addLogEntry("Not enough money to execute operation. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                case 4108:
                    mql4.Alert("Order ticket was not found. Abort trade");
                    trade.addLogEntry("Order ticket was not found. Abort trade", true);
                    return (ErrorType.NON_RETRIABLE_ERROR);
                default:
                    mql4.Alert("Unknown error, error code: ", result);
                    return (ErrorType.NON_RETRIABLE_ERROR);
            } //end of switch
        }

        public OrderType OrderType
        {
            get
            {
                return orderType;
            }

            set
            {
                orderType = value;
            }
        }

        public double EntryPrice
        {
            get
            {
                return entryPrice;
            }

            set
            {
                entryPrice = value;
            }
        }

        public double StopLoss
        {
            get
            {
                return stopLoss;
            }

            set
            {
                stopLoss = value;
            }
        }

        public double TakeProfit
        {
            get
            {
                return takeProfit;
            }

            set
            {
                takeProfit = value;
            }
        }

        public double CancelPrice
        {
            get
            {
                return cancelPrice;
            }

            set
            {
                cancelPrice = value;
            }
        }

        public double PositionSize
        {
            get
            {
                return positionSize;
            }

            set
            {
                positionSize = value;
            }
        }

        public int OrderTicket
        {
            get
            {
                return orderTicket;
            }

            set
            {
                orderTicket = value;
            }
        }

        public OrderState State
        {
            get
            {
                return state;
            }

            set
            {
                state = value;
            }
        }

        public Trade Trade
        {
            get
            {
                return trade;
            }

            set
            {
                trade = value;
            }
        }
    }
}
