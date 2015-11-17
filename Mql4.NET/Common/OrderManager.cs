using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;

namespace biiuse
{
    public enum ErrorType
    { ///Rename to OrderManager
        NO_ERROR,
        RETRIABLE_ERROR,
        NON_RETRIABLE_ERROR
    };

    class OrderManager
    {

        public static double getPipValue(MqlApi mql4)
        {
            double point;
            if (mql4.Digits == 5)
                point = mql4.Point;
            else
                point = mql4.Point / 10.0;

            return (mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_TICKVALUE) * point) / mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_TICKSIZE);
        }

        public static double getLotSize(double riskCapital, int riskPips, MqlApi mql4)
        {
            double pipValue = OrderManager.getPipValue(mql4);
            return riskCapital / ((double) riskPips * pipValue);
        }

        public static double getPipConversionFactor(MqlApi mql4)
        {
            //multiplier depending on YEN or non YEN pairs
            if (mql4.Digits == 5)
                return 100000.00;
            else
                return 10000.00;
        }

        public static ErrorType submitNewOrder(int orderType, double _entryPrice, double _stopLoss, double _takeProfit, double _cancelPrice, double _positionSize, Trade trade, MqlApi mql4)
        {
            int maxSlippage = 4;
            int magicNumber = 0;
            System.DateTime expiration = new DateTime();
            System.Drawing.Color arrowColor = System.Drawing.Color.Red;

            double entryPrice = (double) Decimal.Round((decimal)_entryPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double stopLoss = (double) Decimal.Round((decimal)_stopLoss, mql4.Digits, MidpointRounding.AwayFromZero);
            double takeProfit = (double) Decimal.Round((decimal) _takeProfit, mql4.Digits, MidpointRounding.AwayFromZero);
            double cancelPrice = (double) Decimal.Round((decimal) _cancelPrice, mql4.Digits, MidpointRounding.AwayFromZero);
            double positionSize = (double) Decimal.Round((decimal) _positionSize, 2, MidpointRounding.AwayFromZero);

            string orderTypeStr;
            string entryPriceStr = "";
            string stopLossStr = "";
            string takeProfitStr = "";
            string cancelPriceStr = "";
            string positionSizeStr = "";

            switch (orderType)
            {
                case MqlApi.OP_BUY: { orderTypeStr = "BUY Market Order"; break; }
                case MqlApi.OP_SELL: { orderTypeStr = "SELl Market Order"; break; }
                case MqlApi.OP_BUYLIMIT:
                    {
                        orderTypeStr = "BUY Limit Order";
                        if (mql4.Ask - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                        }
                        break;
                    }
                case MqlApi.OP_SELLLIMIT:
                    {
                        orderTypeStr = "SELL Limit Order";
                        if (entryPrice - mql4.Bid < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                        }
                        break;
                    }
                case MqlApi.OP_BUYSTOP:
                    {
                        orderTypeStr = "BUY Stop Order";
                        //check if entryPrice is too close to market price and adjust accordingly

                        if (entryPrice - mql4.Ask < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Ask of " + mql4.DoubleToString(mql4.Ask, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Ask + mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                        }
                        break;
                    }
                case MqlApi.OP_SELLSTOP:
                    {
                        orderTypeStr = "SELL Stop Order";
                        if (mql4.Bid - entryPrice < mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL))
                        {
                            trade.addLogEntry("Desired entry price of " + mql4.DoubleToString(entryPrice, mql4.Digits) + " is too close to current Bid of " + mql4.DoubleToString(mql4.Bid, mql4.Digits) + " Adjusting to " + mql4.DoubleToString(mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL), mql4.Digits), true);
                            entryPrice = mql4.Bid - mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_STOPLEVEL);
                        }
                        break;
                    }
                default: { trade.addLogEntry("Invalid Order Type. Abort Trade", true); return ErrorType.NON_RETRIABLE_ERROR; }
            }


            if (entryPrice != 0) entryPriceStr = "; entry price: " + mql4.DoubleToString(entryPrice, mql4.Digits);
            if (stopLoss != 0) stopLossStr = "; stop loss: " + mql4.DoubleToString(stopLoss, mql4.Digits);
            if (takeProfit != 0) takeProfitStr = "; take profit: " + mql4.DoubleToString(takeProfit, mql4.Digits);
            if (cancelPrice != 0) cancelPriceStr = "; cancel price: " + mql4.DoubleToString(cancelPrice, mql4.Digits);

            positionSizeStr = "; position size: " + mql4.DoubleToString(positionSize, 2) + " lots";

            trade.addLogEntry("Attemting to place " + orderTypeStr + entryPriceStr + stopLossStr + takeProfitStr + cancelPriceStr + positionSizeStr, true);

            int ticket = mql4.OrderSend(mql4.Symbol(), orderType, positionSize, entryPrice, maxSlippage, stopLoss, takeProfit, trade.getId(), magicNumber, expiration, arrowColor);

            ErrorType result = analzeAndProcessResult(trade, mql4);

            if (result == ErrorType.NO_ERROR)
            {
                trade.setPlannedEntry(entryPrice);
                trade.setStopLoss(stopLoss);
                trade.setOriginalStopLoss(stopLoss);
                trade.setTakeProfit(takeProfit);
                trade.setCancelPrice(cancelPrice);
                trade.setPositionSize(positionSize);
                trade.Order.OrderTicket = ticket;
            }
            return result;
        }


        public static ErrorType modifyOrder(int orderTicket, double newOpenPrice, double newStopLoss, double newTakeProfit, Trade trade, MqlApi mql4)
        {
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

            trade.addLogEntry("Attemting to modify order to: NewOpenPrice: " + newOpenPriceStr + " NewStopLoss: " + newStopLossStr + " NewTakeProfit: " + newTakeProfit, true);

            bool res = mql4.OrderModify(orderTicket, newOpenPrice, newStopLoss, newTakeProfit, expiration, arrowColor);

            ErrorType result = analzeAndProcessResult(trade, mql4);

            if (result == ErrorType.NO_ERROR)
            {
                trade.setPlannedEntry(newOpenPrice);
                trade.setStopLoss(newStopLoss);
                trade.setTakeProfit(newTakeProfit);
            }

            return result;

        }



        public static ErrorType deleteOrder(int orderTicket, Trade trade, MqlApi mql4)
        {
            trade.addLogEntry("Attemting to delete Order (ticket number: " + mql4.IntegerToString(orderTicket) + ")", true);
            mql4.ResetLastError();
            bool success = mql4.OrderDelete(orderTicket, System.Drawing.Color.Red);
            return analzeAndProcessResult(trade, mql4);
        }


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
    }
}
