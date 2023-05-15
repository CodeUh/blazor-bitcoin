using System.Net.Http.Headers;
using System.Net;
using Neo4j.Driver;
using System.Text;
using Newtonsoft.Json;
using BlazorBitcoin.Shared.Models;
using System.Text.Json;
using System.Diagnostics;
using BlazorBitcoin.Shared;

var logic = new LoadLogic();
var pollTime = 5000;
try
{
    while (true)
    {
        var blockChainInfo = await logic.GetBlockchainInfo();
        var bestBlockHeight = (blockChainInfo.Result.Blocks);
        var dbHeight = await logic.GetDBHeight();
        if (bestBlockHeight > dbHeight)
        {
            Console.WriteLine($"behind {(bestBlockHeight - dbHeight)} blocks. Loading them now.");
            for (var i = dbHeight;i<= bestBlockHeight; i++)
            {
                await logic.LoadBlock(i);
            }
        }
        await Task.Delay(pollTime);
    }
    
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
}
finally
{
    Console.ReadLine();
}
