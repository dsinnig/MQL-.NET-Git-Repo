using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NQuotes;

namespace biiuse
{
    class SessionFactory
    {

        public static Session getCurrentSession(int aLengthOfSundaySession, int aHHLL_Threshold, MqlApi mql4)
        {
            //TODO Change Session length determination logic
            int aLengthOfSundaySessionInHours = TimeSpan.FromSeconds(aLengthOfSundaySession).Hours;
            System.DateTime weekStartTime = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, detectWeekStartShift(mql4));
            System.DateTime previousFridayStartTime = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, detectWeekStartShift(mql4) + (24 - aLengthOfSundaySessionInHours));
            System.DateTime currentTime = mql4.TimeCurrent();
            int fullDayInSeconds = 60 * 60 * 24;
            TimeSpan timeElapsedSinceWeekStart = currentTime - weekStartTime;

            if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession))
            {
                if ((currentSession == null) || (currentSession.getID() != 0))
                {
                    currentSession = new Session(0, "SUNDAY", weekStartTime, weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession), new DateTime(), false, 0, mql4);
                }
                return currentSession;
            }
            else if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession + 1 * fullDayInSeconds))
            {
                if ((currentSession == null) || (currentSession.getID() != 1))
                {
                    currentSession = new Session(1, "MONDAY", weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + fullDayInSeconds), previousFridayStartTime, true, aHHLL_Threshold, mql4);
                }
                return currentSession;
            }
            else if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession + 2 * fullDayInSeconds))
            {
                if ((currentSession == null) || (currentSession.getID() != 2))
                {
                    currentSession = new Session(2, "TUESDAY", weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 2 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession), true, aHHLL_Threshold, mql4);
                }
                return currentSession;
            }
            else if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession + 3 * fullDayInSeconds))
            {
                if ((currentSession == null) || (currentSession.getID() != 3))
                {
                    currentSession = new Session(3, "WEDNESDAY", weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 2 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 3 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + fullDayInSeconds), true, aHHLL_Threshold, mql4);
                }
                return currentSession;
            }
            else if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession + 4 * fullDayInSeconds))
            {
                if ((currentSession == null) || (currentSession.getID() != 4))
                {
                    currentSession = new Session(4, "THURSDAY", weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 3 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 4 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 2 * fullDayInSeconds), true, aHHLL_Threshold, mql4);
                }
                return currentSession;
            }
            else if (timeElapsedSinceWeekStart < TimeSpan.FromSeconds(aLengthOfSundaySession + 4 * fullDayInSeconds + (fullDayInSeconds - aLengthOfSundaySession)))
            {
                if ((currentSession == null) || (currentSession.getID() != 5))
                {
                    currentSession = new Session(5, "FRIDAY", weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 4 * fullDayInSeconds), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 4 * fullDayInSeconds + (fullDayInSeconds - aLengthOfSundaySession)), weekStartTime + TimeSpan.FromSeconds(aLengthOfSundaySession + 3 * fullDayInSeconds), true, aHHLL_Threshold, mql4);
                }
                return currentSession;
            }
            else
            {
                if ((currentSession == null) || (currentSession.getID() != -1))
                {
                    currentSession = new Session(-1, "UNKNOWN", new DateTime(), new DateTime(), new DateTime(), false, 0, mql4);

                }
                return currentSession;
            }
        }

        static private int detectWeekStartShift(MqlApi mql4)
        {
            int i = 0;
            bool shiftDetected = false;
            while (i < 168)
            {
                if (mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, i) - mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, i + 1) > TimeSpan.FromSeconds(4 * 60 * 60))
                {
                    shiftDetected = true;
                    break;
                }
                ++i;
            }

            if (shiftDetected) return i;
            else
            {
                mql4.Print("Unable to detect weekstart - abort");
                return -1;
            }
        }

        private static Session currentSession = null;
    }
}