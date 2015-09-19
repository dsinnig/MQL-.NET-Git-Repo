using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;

namespace biiuse
{
    class SimOrder : Order 
    {
        public SimOrder(Trade trade, MqlApi mql4) : base (trade, mql4)
        {

        }

        //placing fake order
        public override ErrorType submitNewOrder(int mql4OrderType, double _entryPrice, double _stopLoss, double _takeProfit, double _cancelPrice, double _positionSize)
        {
            if (this.OrderType != OrderType.INIT)
            {
                this.Trade.addLogEntry("Order already submitted", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }

            System.Drawing.Color arrowColor = System.Drawing.Color.Red;

            double entryPrice = (double)Decimal.Round((decimal)_entryPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double stopLoss = (double)Decimal.Round((decimal)_stopLoss, mql4.Digits, MidpointRounding.AwayFromZero);
            double takeProfit = (double)Decimal.Round((decimal)_takeProfit, mql4.Digits, MidpointRounding.AwayFromZero);
            double cancelPrice = (double)Decimal.Round((decimal)_cancelPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double positionSize = (double)Decimal.Round((decimal)_positionSize, 2, MidpointRounding.AwayFromZero);

            OrderType orderType;

            string orderTypeStr;
            string entryPriceStr = "";
            string stopLossStr = "";
            string takeProfitStr = "";
            string cancelPriceStr = "";
            string positionSizeStr = "";

            switch (mql4OrderType)
            {
                case MqlApi.OP_BUY: { orderTypeStr = "SIM BUY Market Order"; orderType = OrderType.BUY; break; }
                case MqlApi.OP_SELL: { orderTypeStr = "SIM SELl Market Order"; orderType = OrderType.SELL; break; }
                case MqlApi.OP_BUYLIMIT:
                    {
                        orderTypeStr = "SIM BUY Limit Order";
                        orderType = OrderType.BUY_LIMIT;
                        if (mql4.Ask - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            this.Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);

                        }
                        break;
                    }
                case MqlApi.OP_SELLLIMIT:
                    {
                        orderTypeStr = "SIM SELL Limit Order";
                        orderType = OrderType.SELL_LIMIT;
                        if (entryPrice - mql4.Bid < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            this.Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);

                        }
                        break;
                    }
                case MqlApi.OP_BUYSTOP:
                    {
                        orderTypeStr = "SIM BUY Stop Order";
                        orderType = OrderType.BUY_STOP;
                        //check if entryPrice is too close to market price and adjust accordingly

                        if (entryPrice - mql4.Ask < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            this.Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);

                        }
                        break;
                    }
                case MqlApi.OP_SELLSTOP:
                    {
                        orderTypeStr = "SIM SELL Stop Order";
                        orderType = OrderType.SELL_STOP;
                        if (mql4.Bid - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            this.Trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);

                        }
                        break;
                    }
                default: { this.Trade.addLogEntry("Invalid Order Type. Abort Trade", true); return ErrorType.NON_RETRIABLE_ERROR; }
            }

            if (entryPrice != 0) entryPriceStr = "; entry price: " + mql4.DoubleToString(entryPrice, mql4.Digits);
            if (stopLoss != 0) stopLossStr = "; stop loss: " + mql4.DoubleToString(stopLoss, mql4.Digits);
            if (takeProfit != 0) takeProfitStr = "; take profit: " + mql4.DoubleToString(takeProfit, mql4.Digits);
            if (cancelPrice != 0) cancelPriceStr = "; cancel price: " + mql4.DoubleToString(cancelPrice, mql4.Digits);

            positionSizeStr = "; position size: " + mql4.DoubleToString(positionSize, 2) + " lots";

            this.Trade.addLogEntry("Attemting to place SIM Order: " + orderTypeStr + entryPriceStr + stopLossStr + takeProfitStr + cancelPriceStr + positionSizeStr, true);

            this.OrderTicket = -1;
            this.EntryPrice = entryPrice;
            this.PositionSize = positionSize;
            this.StopLoss = stopLoss;
            this.TakeProfit = takeProfit;
            this.CancelPrice = cancelPrice;
            this.OrderType = orderType;
            this.State = new SIM_Pending(this, mql4);

            return ErrorType.NO_ERROR;
        }


        public override ErrorType deleteOrder()
        {
            if ((this.OrderType != OrderType.SELL_LIMIT) && (this.OrderType != OrderType.SELL_STOP) && (this.OrderType != OrderType.BUY_LIMIT) && (this.OrderType == OrderType.SELL_STOP))
            {
                Trade.addLogEntry("Filled orded cannot be delete, it must be closed", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }


            Trade.addLogEntry("Attemting to delete Sim Order (ticket number: " + this.OrderTicket + ")", true);
            this.State = new Final(this, mql4);
            this.OrderType = OrderType.FINAL;
            return analzeAndProcessResult(Trade, mql4);
        }

        public override ErrorType modifyOrder(double newOpenPrice, double newStopLoss, double newTakeProfit)
        {
            if ((this.OrderType == OrderType.INIT) || (OrderType == OrderType.FINAL))
            {
                Trade.addLogEntry("Order not open", true);
                return ErrorType.NON_RETRIABLE_ERROR;
            }

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

            this.EntryPrice = newOpenPrice;
            this.StopLoss = newStopLoss;
            this.TakeProfit = newTakeProfit;
            this.Profit = 100;
            return ErrorType.NO_ERROR;

        }

        public override DateTime getOrderCloseTime()
        {
            return this.CloseTime;
        }

        public override double getOrderProfit()
        {
            switch (this.OrderType)
            {
                case OrderType.BUY:
                    {
                        if (this.EntryPrice < mql4.Bid)
                        {
                            return 50;
                        }
                        else
                        {
                            return -50;
                        }
                    }
                case OrderType.SELL:
                    {
                        if (this.EntryPrice > mql4.Ask)
                        {
                            return 50;
                        }
                        else
                        {
                            return -50;
                        }
                    }

                default: return this.Profit;
            }
        }

        public override double getOrderCommission()
        {
            return this.Commission;
        }

        public override double getOrderSwap()
        {
            return this.Swap;
        }


        public override double getOrderOpenPrice()
        {
            return this.OpenPrice;
        }

        public override double getOrderClosePrice()
        {
            return this.ClosePrice;
        }

        private DateTime closeTime = new DateTime();
        private double profit = 0;
        private double commission = 0;
        private double swap = 0;
        private double openPrice = 0;
        private double closePrice = 0;

        public DateTime CloseTime
        {
            get
            {
                return closeTime;
            }

            set
            {
                closeTime = value;
            }
        }

        public double Profit
        {
            get
            {
                return profit;
            }

            set
            {
                profit = value;
            }
        }

        public double Commission
        {
            get
            {
                return commission;
            }

            set
            {
                commission = value;
            }
        }

        public double Swap
        {
            get
            {
                return swap;
            }

            set
            {
                swap = value;
            }
        }

        public double OpenPrice
        {
            get
            {
                return openPrice;
            }

            set
            {
                openPrice = value;
            }
        }

        public double ClosePrice
        {
            get
            {
                return closePrice;
            }

            set
            {
                closePrice = value;
            }
        }
    }
}
