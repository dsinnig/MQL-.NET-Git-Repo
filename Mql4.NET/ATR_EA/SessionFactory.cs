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
        
        //This will only work for FXCM properly. Or any broker with exactly 5 trading days. 
        public static Session getCurrentSession(int aLengthOfSundaySession, int aHHLL_Threshold, int lookBackSessions, ATR_Type atrType, MqlApi mql4)
        {
            //TODO Change Session length determination logic
            int aLengthOfSundaySessionInHours = TimeSpan.FromSeconds(aLengthOfSundaySession).Hours;
            System.DateTime weekStartTime = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, detectWeekStartShift(mql4));
            System.DateTime currentTime = mql4.TimeCurrent();

            //System.DateTime startOfCurrentSession = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0);
            System.DayOfWeek weekday = currentTime.DayOfWeek;
            int weekEndDelay;
            if (weekendOverlap(weekday, lookBackSessions)) weekEndDelay = 2;
            else weekEndDelay = 0;

            //if daily bar is still in the old session - return. Waii until it updates.
            if ((currentSession != null) && (currentSession.getID() != 1) && (currentTime.DayOfWeek == mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0).DayOfWeek))
            {
                return currentSession;
            }

            switch (currentTime.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    {
                        if ((currentSession == null) || (currentSession.getID() != 1))
                        {
                            System.DateTime dailyBarStart = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0); 
                            ///Take out session end time. It's hard to calculate and not used currently. 
                            currentSession = new Session(1, "MONDAY", mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0), new DateTime(), mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0) - TimeSpan.FromDays(lookBackSessions+weekEndDelay), true, aHHLL_Threshold, atrType, mql4);
                        }
                        break;
                    }
                case DayOfWeek.Tuesday:
                    {
                        if ((currentSession == null) || (currentSession.getID() != 2))
                        {
                            ///Take out session end time. It's hard to calculate and not used currently. 
                            currentSession = new Session(2, "TUESDAY", mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0), new DateTime(), mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 1) - TimeSpan.FromDays(lookBackSessions + weekEndDelay-1), true, aHHLL_Threshold, atrType, mql4);
                        }
                        break;

                    }
                case DayOfWeek.Wednesday:
                    {
                        if ((currentSession == null) || (currentSession.getID() != 3))
                        {
                            ///Take out session end time. It's hard to calculate and not used currently. 
                            currentSession = new Session(3, "WEDNESDAY", mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0), new DateTime(), mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 1) - TimeSpan.FromDays(lookBackSessions + weekEndDelay-1), true, aHHLL_Threshold, atrType, mql4);
                        }
                        break;

                    }
                case DayOfWeek.Thursday:
                    {
                        if ((currentSession == null) || (currentSession.getID() != 4))
                        {
                            ///Take out session end time. It's hard to calculate and not used currently. 
                            currentSession = new Session(4, "THURSDAY", mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0), new DateTime(), mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 1) - TimeSpan.FromDays(lookBackSessions + weekEndDelay-1), true, aHHLL_Threshold, atrType, mql4);
                        }
                        break;

                    }
                case DayOfWeek.Friday:
                    {
                        if ((currentSession == null) || (currentSession.getID() != 5))
                        {
                            ///Take out session end time. It's hard to calculate and not used currently. 
                            currentSession = new Session(5, "FRIDAY", mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 0), new DateTime(), mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_D1, 1) - TimeSpan.FromDays(lookBackSessions + weekEndDelay-1), true, aHHLL_Threshold, atrType, mql4);
                        }
                            break;
                    }

                default:
                    {
                        if ((currentSession == null) || (currentSession.getID() != -1))
                        {
                            currentSession = new Session(-1, "UNKNOWN", new DateTime(), new DateTime(), new DateTime(), false, 0, atrType, mql4);
                        }
                            break;
                    }
            }
            return currentSession;
    }

static int weekDayToNumber(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return 0;
                case DayOfWeek.Tuesday: return 1;
                case DayOfWeek.Wednesday: return 2;
                case DayOfWeek.Thursday: return 3;
                case DayOfWeek.Friday: return 4;
                case DayOfWeek.Saturday: return 5;
                case DayOfWeek.Sunday: return 6;
            }
            return 6;
        }

static private bool weekendOverlap(DayOfWeek currentDay, int lookBackSessions)
        {
            return (weekDayToNumber(currentDay) - lookBackSessions) < 0;
        }

static private int detectWeekStartShift(MqlApi mql4)
{
    int i = 0;
    bool shiftDetected = false;
    while (i < 168)
    {
        if ((mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, i) - mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, i + 1) > TimeSpan.FromSeconds(4 * 60 * 60)) || (mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, i + 1).DayOfWeek == DayOfWeek.Sunday))
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