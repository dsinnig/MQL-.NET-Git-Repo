using System;
using NQuotes;
namespace biiuse
{
    internal class Session : MQL4Wrapper
    {
        public Session(int aSessionID, string aSessionName, DateTime aSessionStartDateTime, DateTime aSessionEndDateTime, DateTime aHHLL_ReferenceDateTime, bool tradingFlag, int aHHLLThreshold, MqlApi mql4) : base(mql4)
        {
            this.sessionID = aSessionID;
            this.sessionName = aSessionName;
            this.sessionStartDateTime = aSessionStartDateTime;
            this.sessionEndDateTime = aSessionEndDateTime;
            this.HHLL_ReferenceDateTime = aHHLL_ReferenceDateTime;
            this.isTradingAllowed = tradingFlag;
            this.HHLL_Threshold = aHHLLThreshold;
            this.highestHigh = -1;
            this.lowestLow = 9999999999;

            initialize();
        }

        private void initialize()
        {

            this.highestHigh = -1;
            this.lowestLow = 999999;
            this.dateOfHighestHigh = new DateTime();
            this.dateOfLowestLow = new DateTime();
            this.atr = 0;

            if (this.isTradingAllowed)
            {
                int indexOfReferenceStart = mql4.iBarShift(mql4.Symbol(), MqlApi.PERIOD_H1, this.HHLL_ReferenceDateTime, true);

                if (indexOfReferenceStart == -1)
                {
                    mql4.Print("Could not find Shift of first 1H bar of reference period");
                    return;
                }

                int indexOfHighestHigh = mql4.iHighest(mql4.Symbol(), MqlApi.PERIOD_H1, MqlApi.MODE_HIGH, indexOfReferenceStart, 0);

                mql4.Print("Index of HH is: ", indexOfHighestHigh);

                int indexOfLowestLow = mql4.iLowest(mql4.Symbol(), MqlApi.PERIOD_H1, MqlApi.MODE_LOW, indexOfReferenceStart, 0);

                if ((indexOfHighestHigh == -1) || (indexOfLowestLow == -1))
                {
                    mql4.Print("Could not find highest high or lowest low for reference period");
                    return;
                }

                this.highestHigh = mql4.iHigh(mql4.Symbol(), MqlApi.PERIOD_H1, indexOfHighestHigh);

                mql4.Print("Highest High is: ", this.highestHigh);

                this.dateOfHighestHigh = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, indexOfHighestHigh);

                mql4.Print("Date of highest high is: ", this.dateOfHighestHigh);

                this.lowestLow = mql4.iLow(mql4.Symbol(), MqlApi.PERIOD_H1, indexOfLowestLow);
                this.dateOfLowestLow = mql4.iTime(mql4.Symbol(), MqlApi.PERIOD_H1, indexOfLowestLow);

                //check if new High / Low happened in last 100 minutes - if yes update dateOfLowestLow / dateOfHighestHigh with accurate timestamp
                int i = HHLL_Threshold; //paratrize
                while (i > 0)
                {
                    if (mql4.Low[i] == lowestLow)
                    {
                        dateOfLowestLow = mql4.Time[i];
                    }
                    if (mql4.High[i] == highestHigh)
                    {
                        dateOfHighestHigh = mql4.Time[i];
                    }
                    i--;
                }

                //get shift for session start
                int indexOfSessionStart = mql4.iBarShift(mql4.Symbol(), MqlApi.PERIOD_H1, this.sessionStartDateTime, true);

                decimal sum = 0;
                decimal _tenDayHigh = 0;
                decimal _tenDayLow = 9999;


                for (i = 0; i < 10; ++i)
                {

                    decimal periodHigh = (decimal) mql4.iHigh(mql4.Symbol(), MqlApi.PERIOD_H1, mql4.iHighest(mql4.Symbol(), MqlApi.PERIOD_H1, MqlApi.MODE_HIGH, 24, (indexOfSessionStart + i * 24) + 1));
                    decimal periodLow = (decimal) mql4.iLow(mql4.Symbol(), MqlApi.PERIOD_H1, mql4.iLowest(mql4.Symbol(), MqlApi.PERIOD_H1, MqlApi.MODE_LOW, 24, (indexOfSessionStart + i * 24) + 1));

                    if (periodHigh > _tenDayHigh) _tenDayHigh = periodHigh;
                    if (periodLow < _tenDayLow) _tenDayLow = periodLow;

                    sum = sum + (periodHigh - periodLow);

                }

                this.atr = (double) sum / 10.0d;
                this.tenDayHigh = (double) _tenDayHigh;
                this.tenDayLow = (double) _tenDayLow;

            } //end if TradingAllowed
        }

        public int update(double price)
        {
            if (this.isTradingAllowed)
            {
                if (price > this.highestHigh)
                {
                    bool validHighestHigh = false;
                    if ((mql4.TimeCurrent() - this.dateOfHighestHigh) > TimeSpan.FromSeconds((HHLL_Threshold * 60)))
                    {
                        validHighestHigh = true;
                    }
                    this.highestHigh = price;
                    this.dateOfHighestHigh = mql4.TimeCurrent();
                    if (validHighestHigh) return 1;
                }

                if (price < this.lowestLow)
                {
                    bool validLowestLow = false;
                    if ((mql4.TimeCurrent() - this.dateOfLowestLow) > TimeSpan.FromSeconds(HHLL_Threshold * 60))
                    {
                        validLowestLow = true;
                    }
                    this.lowestLow = price;
                    this.dateOfLowestLow = mql4.TimeCurrent();
                    if (validLowestLow) return -1;
                }
            }
            return 0;



        }

        public void writeToCSV(string filename)
        {
            mql4.ResetLastError();
            int openFlags;
            openFlags = MqlApi.FILE_WRITE | MqlApi.FILE_READ | MqlApi.FILE_TXT;
            int filehandle = mql4.FileOpen(filename, openFlags);
            mql4.FileSeek(filehandle, 0, MqlApi.SEEK_END); //go to the end of the file

            string output;
            //if first entry, write column headers
            if (mql4.FileTell(filehandle) == 0)
            {
                output = "START_DATE, ATR, TEN_DAY_HIGH, TEN_DAY_LOW";

                mql4.FileWriteString(filehandle, output, output.Length);
            }
            output = ExcelUtil.datetimeToExcelDate(this.getSessionStartTime()) + ", " + this.getATR() + ", " + this.tenDayHigh + ", " + this.tenDayLow;

            mql4.FileWriteString(filehandle, "\n", 1);
            mql4.FileWriteString(filehandle, output, output.Length);


            mql4.FileClose(filehandle);




        }

        public DateTime getSessionStartTime()
        {
            return sessionStartDateTime;
        }

        public DateTime getSessionEndTime()
        {
            return sessionEndDateTime;
        }

        public DateTime getHHLL_ReferenceDateTime()
        {
            return HHLL_ReferenceDateTime;
        }

        public string getName()
        {
            return sessionName;
        }

        public double getHighestHigh()
        {
            return highestHigh;
        }

        public DateTime getHighestHighTime()
        {
            return dateOfHighestHigh;
        }

        public double getLowestLow()
        {
            return lowestLow;
        }

        public DateTime getLowestLowTime()
        {
            return dateOfLowestLow;
        }

        public double getATR()
        {
            return this.atr;
        }

        public int getID()
        {
            return this.sessionID;
        }

        public bool tradingAllowed()
        {
            return this.isTradingAllowed;
        }

        public double getTenDayHigh()
        {
            return this.tenDayHigh;
        }

        public double getTenDayLow()
        {
            return this.tenDayLow;
        }



        private int sessionID;
        private string sessionName;
        private DateTime sessionStartDateTime;
        private DateTime sessionEndDateTime;
        private DateTime HHLL_ReferenceDateTime;
        private double highestHigh;
        private DateTime dateOfHighestHigh;
        private double lowestLow;
        private DateTime dateOfLowestLow;
        private double atr;
        private int HHLL_Threshold;
        private bool isTradingAllowed;
        private double tenDayLow;
        private double tenDayHigh;
    }
}