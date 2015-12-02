using System;
using NQuotes;

namespace biiuse
{
    internal class WaitForRetracement : TradeState
    {
        private ATRTrade trade;
        private int stopOrderType;
        private double retracementLevel;
        private double stopLoss;
        private double cancelPrice;
        private double positionSize;
        
        public WaitForRetracement(ATRTrade _trade, int _stopOrderType, double _retracementLevel, double _stopLoss, double _cancelPrice, double _positionSize, MqlApi mql4) : base(mql4)
        {
            this.trade = _trade;
            this.stopOrderType = _stopOrderType;
            this.retracementLevel = _retracementLevel;
            this.stopLoss = _stopLoss;
            this.cancelPrice = _cancelPrice;
            this.positionSize = _positionSize;
        }

        public override void update()
        {
            ErrorType orderResult = ErrorType.NO_ERROR;
            bool orderPlaced = false;
            double entryPrice = 0.0;
            double initialProfitTarget = 0.0;
            TradeState nextState = null;

            if (stopOrderType == MqlApi.OP_SELLSTOP)
            {
                if (mql4.Bid > retracementLevel)
                {
                    trade.addLogEntry(true, "Retracement complete - placing SELL stop order");
                    nextState = new StopSellOrderOpened(trade, mql4);
                    entryPrice = retracementLevel - ((stopLoss- retracementLevel) * trade.getEntryLevel());
                    initialProfitTarget = retracementLevel - ((stopLoss - retracementLevel) * trade.getMinProfitTarget());


                    orderResult = trade.Order.submitNewOrder(stopOrderType, entryPrice, stopLoss, 0, stopLoss, positionSize);
                    orderPlaced = true;
                    trade.setCancelPrice(stopLoss);
                }

                if (mql4.Bid < cancelPrice)
                {
                    trade.addLogEntry(true, "Bid price went below cancel level - close trade");
                    trade.setState(new TradeClosed(trade, mql4));
                    return;
                }
            }
            if (stopOrderType == MqlApi.OP_BUYSTOP)
            {

                if (mql4.Ask < retracementLevel)
                {
                    trade.addLogEntry(true, "Retracement complete - placing BUY stop order");
                    nextState = new StopBuyOrderOpened(trade, mql4);
                    entryPrice = retracementLevel + ((retracementLevel - stopLoss) * trade.getEntryLevel());
                    initialProfitTarget = retracementLevel + ((retracementLevel - stopLoss)) * trade.getMinProfitTarget();
                    orderResult = trade.Order.submitNewOrder(stopOrderType, entryPrice, stopLoss, 0, stopLoss, positionSize);
                    orderPlaced = true;
                    trade.setCancelPrice(stopLoss);
                }

                if (mql4.Ask > cancelPrice)
                {
                    trade.addLogEntry(true, "Ask price went above cancel level - close trade");
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
                trade.setCancelPrice(stopLoss);
                trade.setPlannedEntry(entryPrice);
                trade.setStopLoss(stopLoss);
                trade.setOriginalStopLoss(stopLoss);
                trade.setTakeProfit(0);
                trade.setPositionSize(positionSize);
                trade.setOriginalStopLoss(stopLoss);

                if (orderResult == ErrorType.NO_ERROR)
                {
                    trade.setInitialProfitTarget(initialProfitTarget);
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