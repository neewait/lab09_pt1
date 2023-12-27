using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LAB9_CS
{
    internal class Program
    {
        private static readonly object fileLock = new object();
        static async Task Main(string[] args)
        {
            List<string> tickers = ReadDataFromFile("C:\\vsc\\ticker.txt");
            List<Task> tasks = new List<Task>();

            foreach (string ticker in tickers)
            {
                tasks.Add(ProcessStock(ticker));
            }

            await Task.WhenAll(tasks);
        }

        static async Task ProcessStock(string ticker)
        {
            DateTime endDate = DateTime.Now;
            DateTime startDate = endDate.AddYears(-1);
            long startUnixTime = (long)(startDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            long endUnixTime = (long)(endDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string API_URL = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={startUnixTime}&period2={endUnixTime}&interval=1d&events=history&includeAdjustedClose=true";
            try
            {
                var data = await DownloadStockPrice(API_URL);

                decimal average = 0;
                if (data.Count > 0)
                {
                    average = data.Sum() / data.Count;
                }

                WriteToFile("C:\\vsc\\output.txt", $"{ticker}:{average}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error downloading stock data for {ticker}: {ex.Message}");
            }

            await Task.Delay(1000);
        }

        static async Task<List<int>> DownloadStockPrice(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                // Дабы не было ошибки 401
                client.DefaultRequestHeaders.Add("Authorization", "Bearer 5b46292ae01f2ba3b57add22869ad86ed5306bfe");
                string data = await client.GetStringAsync(url);
                string[] lines = data.Split('\n');
                List<int> prices = new List<int>();

                foreach (string line in lines.Skip(1))
                {
                    string[] columns = line.Split(',');

                    try
                    {
                        int high = Int32.Parse(columns[2].Split('.')[0]);
                        int low = Int32.Parse(columns[3].Split('.')[0]);
                        int average = high + low / 2;
                        prices.Add(average);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Error parsing value: {ex.Message}");
                    }
                }

                return prices;
            }
        }

        private static List<string> ReadDataFromFile(string path)
        {
            List<string> data = new List<string>();
            if (File.Exists(path))
            {
                data.AddRange(File.ReadAllLines(path));
            }
            else
            {
                Console.WriteLine("File does not exist");
            }

            return data;
        }

        private static void WriteToFile(string path, string data)
        {
            lock (fileLock)
            {
                File.AppendAllText(path, data + '\n');
            }
        }
    }
}
