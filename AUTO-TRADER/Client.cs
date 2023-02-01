using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Skender.Stock.Indicators;
using System.Threading;
using System.IO;
using System.Threading.Tasks;

namespace AUTO_TRADER
{
    class Client
    {
        private HttpClient client = new HttpClient();
        private string filePath = "..\\..\\..\\Data\\data.json";
        private string apiKey;
        private string epic;
        private string apiSecret;
        private string CST;

        public Client(string apiKey, string epic)
        {
            this.apiKey = apiKey;
            this.epic = epic;
        }

        private DateTime prepareDate(string date)
        {
            date = date.Substring(0, date.IndexOf(" "));
            date = date.Replace('/', '-');
            DateTime dt = DateTime.Parse(date);
            return dt;
        }
        private int GetWorkingDays(DateTime from, DateTime to)
        {
            var dayDifference = (int)to.Subtract(from).TotalDays;
            return Enumerable
                .Range(1, dayDifference)
                .Select(x => from.AddDays(x))
                .Count(x => x.DayOfWeek != DayOfWeek.Saturday);
        }
        public async Task<Quote[]> getNewMarketData(int days)
        {
            var market = client.GetAsync("https://demo-api.ig.com/gateway/deal/prices/" + epic + "/DAY/" + days.ToString());
            var res = await market.Result.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(res);
            Quote[] quotes = new Quote[days];
            for (var i = 0; i < days; i++)
            {
                Quote quote = new Quote();
                quote.Open = Convert.ToDecimal(json["prices"][i]["openPrice"]["bid"]);
                quote.Close = Convert.ToDecimal(json["prices"][i]["closePrice"]["bid"]);
                quote.High = Convert.ToDecimal(json["prices"][i]["highPrice"]["bid"]);
                quote.Low = Convert.ToDecimal(json["prices"][i]["lowPrice"]["bid"]);
                quote.Date = prepareDate(json["prices"][i]["snapshotTime"].ToString());
                quotes[i] = quote;
            }
            return quotes;
        }
        public void connect()
        {
            client.DefaultRequestHeaders.Add("X-IG-API-KEY", apiKey);
            client.DefaultRequestHeaders.Add("Version", "2");

            var json = JsonConvert.SerializeObject(new Login());
            StringContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var res = client.PostAsync("https://demo-api.ig.com/gateway/deal/session", content);
            foreach (KeyValuePair<string, IEnumerable<string>> line in res.Result.Headers)
            {
                if (line.Key == "CST" || line.Key == "X-SECURITY-TOKEN")
                {
                    client.DefaultRequestHeaders.Add(line.Key, line.Value.First());
                    if (line.Key == "CST")
                    {
                        CST = line.Value.First();
                    }
                    else if (line.Key == "X-SECURITY-TOKEN")
                    {
                        apiSecret = line.Value.First();
                    }
                }
            }
            Console.WriteLine("[REST] Połączenie zakończone z kodem: " + res.Result.StatusCode);
        }

        public void updateJsonFile()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string text = File.ReadAllText(filePath);
                    var list = JsonConvert.DeserializeObject<List<Quote>>(text);
                    var today = DateTime.Now;

                    if(list[list.Count - 1].Date.Date != today.Date)
                    {
                        int days = GetWorkingDays(list[list.Count - 1].Date.Date, today.Date);
                        Console.WriteLine("[DATA] Updating data file... [days: " + days + "]");
                        var data = getNewMarketData(days).Result;

                        for (int i = 0; i < days; i++)
                        {
                            list.RemoveAt(0);
                        }
                        var arr = list.ToArray().Concat(data).ToArray();
                        var json = JsonConvert.SerializeObject(arr);
                        File.WriteAllText(filePath, json.ToString());
                    }
                    else
                    {
                        Console.WriteLine("[DATA] Everything is up-to-date");
                    }
                }
                else
                {
                    Console.WriteLine("[DATA] File doesn't exists, creating new...");
                    var data = getNewMarketData(285).Result;
                    var json = JsonConvert.SerializeObject(data);
                    File.WriteAllText(filePath, json.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void openPosition(string direction, string stopLimit, string profitLimit,string currencyCode)
        {
            var json = JsonConvert.SerializeObject(new PositionNew(epic,direction,stopLimit, profitLimit, currencyCode));
            Console.WriteLine(json);
            StringContent content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var open = client.PostAsync("https://demo-api.ig.com/gateway/deal/positions/otc", content);
            Console.WriteLine("Position opened with code: " + open.Result.StatusCode);
            if(open.Result.StatusCode.ToString() == "OK")
            {
                Console.WriteLine("Stop Loss: " + stopLimit);
                Console.WriteLine("Profit Limit: "+ profitLimit);
            }
        }
        public Quote[] getDateFromJson()
        {
            string text = File.ReadAllText(filePath);
            var list = JsonConvert.DeserializeObject<List<Quote>>(text);
            return list.ToArray();
        }
        public ScottPlot.OHLC[] getOHCLArray(List<Quote> quotes)
        {
            ScottPlot.OHLC[] oHLCs = new ScottPlot.OHLC[quotes.Count];
            for (int i = 0; i < quotes.Count; i++)
            {
                oHLCs[i] = new ScottPlot.OHLC((double)quotes[i].Open, (double)quotes[i].High, (double)quotes[i].Low, (double)quotes[i].Close, quotes[i].Date.ToOADate());
            }
            return oHLCs;
        }
    }
}
