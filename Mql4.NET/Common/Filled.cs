using System;
using NQuotes;

namespace biiuse
{
    internal class Filled : OrderState
    {
        public Filled(Order context, MqlApi mql4) : base(context, mql4)
        {
        }

        public override void update()
        {
            //check if stoped out



        }
    }
}