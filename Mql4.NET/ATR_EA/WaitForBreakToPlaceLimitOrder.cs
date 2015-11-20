using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;

namespace biiuse
{
    class WaitForBreakToPlaceLimitOrder : TradeState
    {
        private ATRTrade trade;
        private int limitOrderType;
        private double rangeLow;
        private double rangeHigh;
        private double entryPrice;
        private double cancelPrice;
        private double positionSize;

        public WaitForBreakToPlaceLimitOrder(ATRTrade _trade, int _limitOrderType, double _rangeLow, double _rangeHigh, double _entryPrice, double _cancelPrice, double _positionSize, MqlApi mql4) : base(mql4)
        {
            trade = _trade;
            limitOrderType = _limitOrderType;
            rangeHigh = _rangeHigh;
            rangeLow = _rangeLow;
            cancelPrice = _cancelPrice;
            positionSize = _positionSize;
            entryPrice = _entryPrice;
        }

        public override void update()
        {
            ErrorType orderResult = ErrorType.NO_ERROR;
            bool orderPlaced = false;
            double stopLoss = 0.0;
            TradeState nextState = null;


            if (limitOrderType == MqlApi.OP_SELLLIMIT)
            {
                if (mql4.Bid < rangeLow)
                {
                    trade.addLogEntry(true, "Break below range low - Placing Sell Limit Order");
                    stopLoss = rangeHigh;
                    nextState = new SellLimitOrderOpened(trade, mql4);
                    orderResult = trade.Order.submitNewOrder(limitOrderType, entryPrice, stopLoss, 0, cancelPrice, positionSize);
                    orderPlaced = true;

                }
                if (mql4.Ask > rangeHigh)
                {
                    trade.addLogEntry(true, "Ask price above upper range - cancel trade");
                    trade.setState(new TradeClosed(trade, mql4));
                    return;
                }
            }

            if (limitOrderType == MqlApi.OP_BUYLIMIT)
            {
                if (mql4.Ask > rangeHigh)
                {
                    trade.addLogEntry(true, "Break above range high - Placing Buy Limit Order");
                    stopLoss = rangeLow;
                    nextState = new BuyLimitOrderOpened(trade, mql4);
                    orderResult = trade.Order.submitNewOrder(limitOrderType, entryPrice, stopLoss, 0, cancelPrice, positionSize);
                    orderPlaced = true;

                }
                if (mql4.Bid < rangeLow)
                {
                    trade.addLogEntry(true, "Bid price below lower range - cancel trade");
                    trade.setState(new TradeClosed(trade, mql4));
                    return;
                }
            }

            if (orderPlaced)
            {
                trade.setStartingBalance(mql4.AccountBalance());
                trade.setOrderPlacedDate(mql4.TimeCurrent());
                trade.setSpreadOrderOpen((int)mql4.MarketInfo(mql4.Symbol(), MqlApi.MODE_SPREAD));
                trade.setAskPriceBeforeOrderEntry(mql4.Ask);
                trade.setBidPriceBeforeOrderEntry(mql4.Bid);
                trade.setCancelPrice(cancelPrice);
                trade.setPlannedEntry(entryPrice);
                trade.setStopLoss(stopLoss);
                trade.setOriginalStopLoss(stopLoss);
                trade.setTakeProfit(0);
                trade.setCancelPrice(cancelPrice);
                trade.setPositionSize(positionSize);
                trade.setOriginalStopLoss(stopLoss);

                if (orderResult == ErrorType.NO_ERROR)
                {
                    trade.setInitialProfitTarget(Math.Round(trade.getPlannedEntry() + ((trade.getPlannedEntry() - trade.getStopLoss()) * (trade.getMinProfitTarget())), mql4.Digits, MidpointRounding.AwayFromZero));
                    //mql4.Print("Entry: ", trade.getPlannedEntry());
                    //mql4.Print("Stop loss: ", trade.getStopLoss());
                    //mql4.Print("MinProfit: ", trade.getMinProfitTarget());
                    //mql4.Print("Calc: ", trade.getPlannedEntry() + ((trade.getPlannedEntry() - trade.getStopLoss()) * (trade.getMinProfitTarget())));
                    trade.setState(nextState);

                    double riskCapital = mql4.AccountBalance() * trade.getMaxBalanceRisk();
                    int riskPips = (int)(mql4.MathAbs(stopLoss - entryPrice) * OrderManager.getPipConversionFactor(mql4));

                    trade.addLogEntry("Limit order successfully placed. Initial Profit target is: " + mql4.DoubleToString(trade.getInitialProfitTarget(), mql4.Digits) + " (" + (mql4.IntegerToString((int)(mql4.MathAbs(trade.getInitialProfitTarget() - trade.getPlannedEntry()) * OrderManager.getPipConversionFactor(mql4)))) + ") pips", true);

                    trade.addLogEntry(true, "Trade Details",
                                                      "AccountBalance: $" + mql4.DoubleToString(mql4.AccountBalance(), 2), "\n",
                                                      "Risk Capital: $" + mql4.DoubleToString(riskCapital, 2), "\n",
                                                      "Risk pips: " + mql4.DoubleToString(riskPips, 2) + " micro pips", "\n",
                                                      "Pip value: " + mql4.DoubleToString(OrderManager.getPipValue(mql4), mql4.Digits), "\n",
                                                      "Initial Profit target is: " + mql4.DoubleToString(trade.getInitialProfitTarget(), mql4.Digits) + "(" + mql4.IntegerToString((int)(mql4.MathAbs(trade.getInitialProfitTarget() - trade.getPlannedEntry()) * OrderManager.getPipConversionFactor(mql4))) + " micro pips)");



                    return;
                }
                if ((orderResult == ErrorType.RETRIABLE_ERROR) && (trade.Order.OrderTicket == -1))
                {
                    trade.addLogEntry("Order entry failed. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Will re-try at next tick", true);
                    return;
                }

                //this should never happen...
                if ((trade.Order.OrderTicket != -1) && ((orderResult == ErrorType.RETRIABLE_ERROR) || (orderResult == ErrorType.NON_RETRIABLE_ERROR)))
                {
                    trade.addLogEntry("Error ocured but order is still open. Error code: " + mql4.IntegerToString(mql4.GetLastError()) + ". Continue with trade. Initial Profit target is: " + mql4.DoubleToString(trade.getInitialProfitTarget(), mql4.Digits) + " (" + mql4.IntegerToString((int)(mql4.MathAbs(trade.getInitialProfitTarget() - trade.getPlannedEntry()) * OrderManager.getPipConversionFactor(mql4))) + " micro pips)", true);
                    trade.setInitialProfitTarget(Math.Round(trade.getPlannedEntry() + ((trade.getPlannedEntry() - trade.getStopLoss()) * (trade.getMinProfitTarget())), mql4.Digits, MidpointRounding.AwayFromZero));
                    trade.setState(nextState);
                    return;
                }

                if ((orderResult == ErrorType.NON_RETRIABLE_ERROR) && (trade.Order.OrderTicket == -1))
                {
                    trade.addLogEntry("Non-recoverable error occurred. Errorcode: " + mql4.IntegerToString(mql4.GetLastError()) + ". Trade will be canceled", true);
                    trade.setState(new TradeClosed(trade, mql4));
                    return;
                }

            }


        }


    }

}
