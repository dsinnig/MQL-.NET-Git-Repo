
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
#importC:\Users\dsinnig\Desktop\Dropbox\documents\professional\DayTrading\MQ4\MQL .NET Git Repo\Mql4.NET\RoyalZigZagEA\mql4\ea_root\ROYAL_ZIG_ZAG_EA_NET.mq4

input double stopLossPips = 250.0; //Stop loss in micro pips
input double lotSize = 0.3; //Position size in lots
input string logFileName = "tradeLogRoyalZigZag.csv"; //Log file name

int init()
{
	nquotes_setup("RoyalZigZagEA.RoyalZigZag", "RoyalZigZagEA");
	nquotes_set_property_double("stopLossPips", stopLossPips);
	nquotes_set_property_double("lotSize", lotSize);
	nquotes_set_property_string("logFileName", logFileName);
	
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
