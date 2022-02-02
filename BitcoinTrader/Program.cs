using System;
using System.Threading;
using System.Threading.Tasks;
using Coinbase.Pro;
using System.IO;

namespace BitcoinTrader
{
    // temp global variable for the tickerdata so i can use it in multile functions
    static class Ticker
    {
        public static float[] tickerData = new float[17325];
    }
    class Program
    {
        static void Main(string[] args)
        {
            var client = new CoinbaseProClient(new Config
            {
                ApiKey = "asdf",
                Secret = "asdff",
                Passphrase = "asdfff",
                //Override the ApiUrl property to use Sandbox.
                ApiUrl = "https://api-public.sandbox.pro.coinbase.com"
            });

            //GetData(client).Wait();

            // temporaryily here so i don't do it every time in GetPrice
            string line;
            int i = 0;

            using (StreamReader reader = new StreamReader("../../../low.txt"))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    Ticker.tickerData[i] = Convert.ToSingle(line);
                    i++;
                }
            }

            ProcessData();
        }

        public static async Task GetData(CoinbaseProClient client)
        {
            var startString = "1/1/2021 00:00:00 AM";
            var endString = "1/4/2021 00:00:00 AM";
            var start = DateTime.Parse(startString, System.Globalization.CultureInfo.InvariantCulture);
            var end = DateTime.Parse(endString, System.Globalization.CultureInfo.InvariantCulture);

            //get market data for 3 day chunks in increments of 15 minutes(900 granularity)
            //then write to file for later use
            for (int i = 0; i < 60; i++)
            {
                var r = await client.MarketData.GetHistoricRatesAsync("BTC-USD", start, end, 900);

                //get low value
                using (StreamWriter outputFile = new StreamWriter("../../../low.txt", true))
                {
                    foreach (var ticker in r)
                    {
                        outputFile.WriteLine(ticker.Low);
                    }
                }

                //get high value, in a separate file for ease
                using (StreamWriter outputFile = new StreamWriter("../../../high.txt", true))
                {
                    foreach (var ticker in r)
                    {
                        outputFile.WriteLine(ticker.High);
                    }
                }
                // sleep for 1 second so that i don't get a 429 (too many requests)
                Thread.Sleep(1000);

                //Console.WriteLine("start: {0} end: {1}", start, end);

                start = end;
                end = end.AddDays(3);
            }
            Console.WriteLine("success");
            Console.ReadLine();
        }

        public static float GetPrice(int index)
        {
            // get current btc price through coinbase's api

            // take in index (which i won't need to do when using the api), get the ith line from the file and return it
            return Ticker.tickerData[index];
        }

        public static void Buy()
        {
            // buy a certain amount of btc through coinbase's api
        }

        public static void Sell()
        {
            // sell a certain amount of btc through coinbase's api
        }

        public static void ProcessData()
        {
            // read in ticker data from file
            // for each line, figure out what to do from there (is it higher or lower than the peak? if higher, update peak, if lower, how much lower? etc)
            // figure out when to "buy" and "sell" based off those prices
            // print out total gain/loss after end of file
            float peakPrice = 1;
            float buyPrice = 32804.37f;             // initialize to the first buy/sell prices, just for ease
            float sellPrice = 33290.25f;
            float percentDifference = 1.02f;        // look for 2% differences in price
            float startingCapital = 1000.00f;
            float finalCapital = startingCapital;
            int buycounter = 0;
            int sellcounter = 0;
            bool buyHappened = true;

            float currentPrice = 0;

            // this for loop will eventually become a while loop (infinite? or at least infinite until i press a certain key? and sleeping every x minutes?)
            for (int i = 0; i < 17325; i++)
            {
                currentPrice = GetPrice(i);

                if (buyHappened == false)
                {
                    // if current price is less than we sold it for (and less by a certain percentage or more), then buy
                    if ((currentPrice < sellPrice) && (sellPrice / currentPrice > percentDifference))
                    {
                        buyPrice = currentPrice;
                        //Console.WriteLine("bought at {0}", buyPrice);
                        // reset sell price until the next sell
                        sellPrice = 1;
                        peakPrice = 1;
                        buycounter++;
                        //Console.WriteLine("buy happened");
                        buyHappened = true;

                        /// now that i think about it, i should probably do a "reverse" peak price as well, for buying in
                        /// in other words, keep track of the price after a sell, and if it keeps gonig lower, then hold off on the buy until it's (percentDifference) higher than the lowest low
                    }
                }

                if (buyHappened == true)
                {
                    // update peak price if needed
                    if (currentPrice > peakPrice)
                    {
                        peakPrice = currentPrice;
                    }
                    else
                    {
                        // if the difference between peak and current price is more than the percent difference we're looking for (and also still higher than what we bought for), then sell
                        if (((peakPrice / currentPrice) > percentDifference) && (currentPrice / buyPrice > percentDifference))
                        //if (currentPrice / buyPrice > percentDifference)
                        //if (((peakPrice / currentPrice) > percentDifference) && (currentPrice > buyPrice))
                        {
                            sellPrice = currentPrice;
                            //Console.WriteLine("sold at: {0}", sellPrice);

                            sellcounter++;
                            buyHappened = false;

                            Console.WriteLine((currentPrice / buyPrice));
                            //Console.WriteLine("{0} {1} {2}", (currentPrice / buyPrice), percentDifference, percentDifference+1);
                            //Console.WriteLine(currentPrice - buyPrice);
                            //Console.WriteLine("{0}, {1}", buyPrice, sellPrice);
                            //Console.WriteLine("sell happened");

                            // find profit
                            float currentProfit = sellPrice / buyPrice;
                            finalCapital = (finalCapital * currentProfit) - (finalCapital * .0035f);        // the .0035 is the coinbase transaction fee (at least beteween $10k and $50k)

                            buyPrice = 1;
                            peakPrice = 1;
                        }
                    }
                }
            }
            String s = "12:01:00AM";
            char[] trim = {'A', 'P', 'M'};
            Console.WriteLine(s);
            Console.WriteLine(s.Trim(trim));
            String hour = s.Substring(0, 2);
            Console.WriteLine("yes {0}", Int32.Parse(hour) +12);
            hour = (Int32.Parse(hour) + 12).ToString();
            s = s.Remove(0, 2).Insert(0, hour);
            Console.WriteLine(s);

            Console.WriteLine(buycounter);
            Console.WriteLine(sellcounter);
            Console.WriteLine("final capital: {0}", finalCapital);
            Console.ReadLine();
        }
    }
}
