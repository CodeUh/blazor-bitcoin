using BlazorBitcoin.Shared;
using System.Diagnostics;
using System.Net;

var logic = new LoadLogic();
ServicePointManager.DefaultConnectionLimit = 64;

var pollTime = 0;
var batchSize = 12;
var stopwatch = new Stopwatch();
var cnt = 0;
try
{
    stopwatch.Start();
    while (true)
    {
       

        var missingCoinbase = await logic.GetBlocksWithoutCoinbase(batchSize);
        if (missingCoinbase.Count > 0)
        {
            Console.WriteLine($"Found {missingCoinbase.Count} blocks missing coinbase transactions. coinbase/s: {cnt / stopwatch.Elapsed.TotalSeconds}");

            List<Task> tasks = new();

            foreach (var block in missingCoinbase)
            {
                tasks.Add(logic.LoadCoinbase(block));
                cnt++;
                    
            }
            await Task.WhenAll(tasks);
        }
        await Task.Delay(pollTime);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
}
finally
{
    Console.ReadLine();
}