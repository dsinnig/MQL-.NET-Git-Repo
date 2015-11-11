
#property copyright "Daniel Sinnig, BIIUSE Consulting and Investments"
#property link "http://www.biiuse.com"

#import "nquotes/nquoteslib.ex4"
	int nquotes_setup(string className, string assemblyName);
	int nquotes_init();
	int nquotes_start();
	int nquotes_deinit();

	int nquotes_set_property_bool(string name, bool value);
	int nquotes_set_property_int(string name, int value);
	int nquotes_set_property_double(string name, double value);
	int nquotes_set_property_datetime(string name, datetime value);
	int nquotes_set_property_color(string name, color value);
	int nquotes_set_property_string(string name, string value);
	int nquotes_set_property_adouble(string name, double& value[], int count=WHOLE_ARRAY, int start=0);

	bool nquotes_get_property_bool(string name);
	int nquotes_get_property_int(string name);
	double nquotes_get_property_double(string name);
	datetime nquotes_get_property_datetime(string name);
	color nquotes_get_property_color(string name);
	string nquotes_get_property_string(string name);
	int nquotes_get_property_array_size(string name);
	int nquotes_get_property_adouble(string name, double& value[]);
#import

input double maxBalanceRisk = 0.75; //Max risk per trader relative to account balance (in %)
input int sundayLengthInHours=7; //Length of Sunday session in hours
input int HHLL_Threshold=60; //Time in minutes after last HH / LL before a tradeable HH/LL can occur
input int lengthOfGracePeriod=10; //Length in bars of Grace Period after a tradeable HH/LL occured
input double rangeRestriction=80; //Min range of Grace Period
input int lookBackSessions = 1; //Number of sessions to look back for establishing a new HH/LL
input double maxRisk=10; //Max risk (in percent of ATR)
input double maxVolatility=20; //Max volatility (in percent of ATR)
input double minProfitTarget=4; //Min Profit Target (in factors of the risk e.g., 3 = 3* Risk)
input int rangeBuffer=20; //Buffer in micropips for order opening and closing
input int lotDigits=1; //Lot size granularity (0 = full lots, 1 = mini lots, 2 = micro lots, etc).
input int maxConsLoses=99; //Lot size granularity (0 = full lots, 1 = mini lots, 2 = micro lots, etc).
input double maxATROR = 0.5; //max percent value for ATR / OR
input double minATROR = 0; //min percent value for ATR / OR
input double maxDRATR = 0.35; //max percent value for ATR / OR
input double minDRATR = 0; //min percent value for ATR / OR
input bool cutLossesBeforeATRFilter=true; //Flag whether the losing streak filter should take into account trades with invalid ATR OT 
input string logFileName="tradeLog.csv"; //path and filename for CSV trade log
   


int init()
{
	nquotes_setup("biiuse.ATR_EA", "ATR_EA");
	nquotes_set_property_double("maxBalanceRisk",maxBalanceRisk); 
	nquotes_set_property_int("sundayLengthInHours",sundayLengthInHours); 
   nquotes_set_property_int("HHLL_Threshold",HHLL_Threshold); 
   nquotes_set_property_int("lengthOfGracePeriod",lengthOfGracePeriod); 
   nquotes_set_property_double("rangeRestriction",rangeRestriction); 
   nquotes_set_property_int("lookBackSessions",lookBackSessions); 
   nquotes_set_property_double("maxRisk",maxRisk); 
   nquotes_set_property_double("maxVolatility",maxVolatility); 
   nquotes_set_property_double("minProfitTarget",minProfitTarget); 
   nquotes_set_property_int("rangeBuffer",rangeBuffer); 
   nquotes_set_property_int("lotDigits",lotDigits); 
   nquotes_set_property_int("maxConsLoses",maxConsLoses);
   nquotes_set_property_double("maxATROR",maxATROR); 
   nquotes_set_property_double("minATROR",minATROR);  
   nquotes_set_property_double("maxDRATR",maxDRATR); 
   nquotes_set_property_double("minDRATR",minDRATR);  
   nquotes_set_property_bool("cutLossesBeforeATRFilter", cutLossesBeforeATRFilter);
   nquotes_set_property_string("logFileName",logFileName); 
   
   
   
   
   
   

	return (nquotes_init());
}

int start()
{
	return (nquotes_start());
}

int deinit()
{
	return (nquotes_deinit());
}
