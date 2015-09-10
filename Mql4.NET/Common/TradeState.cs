namespace biiuse
{
    public abstract class TradeState : MQL4Wrapper
    {
        public TradeState(NQuotes.MqlApi mql4) : base(mql4) {}
        public abstract void update();
    }
}