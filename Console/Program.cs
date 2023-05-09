using System.Net.Http.Headers;
using System.Net;
using Neo4j.Driver;
using System;
using System.Text;
using Newtonsoft.Json;
using BlazorBitcoin.Shared.Models;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

ServicePointManager.DefaultConnectionLimit = 12;
var options = new ParallelOptions { MaxDegreeOfParallelism = 32 };
var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:8332/");
var authValue = new AuthenticationHeaderValue(
    "Basic", Convert.ToBase64String(
        System.Text.ASCIIEncoding.ASCII.GetBytes(
            $"{Environment.GetEnvironmentVariable("btcrpcuser")}:{Environment.GetEnvironmentVariable("btcrpcpw")}")));
client.DefaultRequestHeaders.Authorization = authValue;

IDriver _driver;
var uri = Environment.GetEnvironmentVariable("neo4juri");

_driver = GraphDatabase.Driver(uri, AuthTokens.Basic(Environment.GetEnvironmentVariable("neo4juser"), Environment.GetEnvironmentVariable("neo4jpw")));

await using var session = _driver.AsyncSession();

//var startingHeight = 788802;
var startingHeight = 0;
var blockCount = 2000;
BlockResponse prevBlock = null;
var stopwatch = new Stopwatch();
stopwatch.Start();
for (int currentHeight = startingHeight; currentHeight < (startingHeight + blockCount); currentHeight++)
{
    try
    {
        //get the block hash for current height
        var rpcRequest = new
        {
            jsonrpc = "1.0",
            id = "1",
            method = "getblockhash",
            @params = new object[] { currentHeight }
        };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
        var response = await client.PostAsync("", content);
        var blockHash = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

        //get the block with hash for current height
        rpcRequest = new
        {
            jsonrpc = "1.0",
            id = "2",
            method = "getblock",
            @params = new object[] { blockHash.result.ToString(), 2 }
        };
        content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
        response = await client.PostAsync("", content);
        var blockJson = await response.Content.ReadAsStringAsync();
        var block = System.Text.Json.JsonSerializer.Deserialize<BlockResponse>(blockJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        //create the block node
        var neo4jBlockResp = await session.ExecuteWriteAsync(
            async tx =>
            {
                var result = await tx.RunAsync(
                    "CREATE (a:Block) " +
                    "SET a.hash = $block.Result.Hash " +
                    "SET a.confirmations = $block.Result.Confirmations " +
                    "SET a.height = $block.Result.Height " +
                    "SET a.version = $block.Result.Version " +
                    "SET a.versionHex = $block.Result.VersionHex " +
                    "SET a.merkleroot = $block.Result.MerkleRoot " +
                    "SET a.time = $block.Result.Time " +
                    "SET a.mediantime = $block.Result.MedianTime " +
                    "SET a.nonce = $block.Result.Nonce " +
                    "SET a.bits = $block.Result.Bits " +
                    "SET a.difficulty = $block.Result.Difficulty " +
                    "SET a.nTx = $block.Result.NTx " +
                    "SET a.strippedsize = $block.Result.StrippedSize " +
                    "SET a.size = $block.Result.Size " +
                    "SET a.weight = $block.Result.Weight " +
                    "RETURN a.hash + ', from node ' + id(a)",
                    new { block });

                var record = await result.SingleAsync();
                return record[0].As<string>();
            });

        //create the block PREV_BLOCK and NEXT_BLOCK relationships
        if (prevBlock != null)
        {
            neo4jBlockResp = await session.ExecuteWriteAsync(
                async tx =>
                {
                    var result = await tx.RunAsync(
                        "MATCH (a:Block), (b:Block) " +
                        "WHERE a.hash = $block.Result.Hash AND b.hash = $prevBlock.Result.Hash " +
                        "CREATE (a)-[r:HAS_CHAIN]->(b) " +
                        "CREATE (b)-[r2:HAS_CHAIN]->(a) " +
                        "RETURN type(r)",
                         new { prevBlock, block });

                    var record = await result.ConsumeAsync();
                    return "";
                });
        }
        var ct = new CancellationToken();
        //Iterate over the transactions in the block
        await Parallel.ForEachAsync(block.Result.Transactions, options, async (trx,ct) =>
        {
            var trxSession = _driver.AsyncSession();
            //check if the transaction is a coinbase transaction
            if (!string.IsNullOrEmpty(trx.Vin[0].Coinbase))
            {
                //Create the coinbase node
                var neo4jTransResp = await trxSession.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "CREATE (a:Transaction) " +
                            "SET a.txid = $trx.TxId, " +
                            "a.hash = $trx.Hash, " +
                            "a.version = $trx.Version, " +
                            "a.size = $trx.Size, " +
                            "a.vsize = $trx.VSize, " +
                            "a.weight = $trx.Weight, " +
                            "a.locktime = $trx.LockTime, " +
                            "a.hex = $trx.Hex, " +
                            "a.coinbase = $trx.Vin[0].Coinbase, " +
                            "a.sequence = $trx.Vin[0].Sequence " +
                            "RETURN a.hash + ', from node ' + id(a)",
                            new { trx });

                        var record = await result.SingleAsync();
                        return record[0].As<string>();
                    });

                //Create the coinbase IN_BLOCK relationship
                neo4jTransResp = await trxSession.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "MATCH (a:Block), (b:Transaction) " +
                            "WHERE a.hash = $block.Result.Hash AND b.txid = $trx.TxId " +
                            "CREATE (b)-[r:IN_BLOCK]->(a) " +
                            "CREATE (a)-[r2:HAS_COINBASE]->(b) " +
                            "RETURN type(r)",
                             new { trx, block });

                        var record = await result.ConsumeAsync();
                        return "";
                    });
                var ctVout = new CancellationToken();
                //Iterate over the outputs in the transaction
                await Parallel.ForEachAsync(trx.Vout, options, async (output, ctVout) =>
                {
                    var voutSession = _driver.AsyncSession();
                    //Create the output node 
                    var neo4jOutputResp = await voutSession.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "CREATE (a:Output) " +
                                "SET a.value = $output.Value, " +
                                "a.n = $output.N " +
                                "WITH a " +
                                "MATCH (b:Transaction) " +
                                "WHERE b.txid = $trx.TxId " +
                                "CREATE (a)-[r:HAS_OUTPUT]->(b) " +
                                "CREATE (c:Address) " +
                                "SET c.asm = $output.ScriptPubKey.Asm, " +
                                "c.hex = $output.ScriptPubKey.Hex, " +
                                "c.type = $output.ScriptPubKey.Type, " +
                                "c.address = $output.ScriptPubKey.Address " +
                                "CREATE (a)-[d:LOCKED_BY]->(c) " +
                                "RETURN type(r) ",
                                new { output, trx });

                            var record = await result.ConsumeAsync();
                            if (!string.IsNullOrEmpty(output.ScriptPubKey.Address))
                            {
                                Console.WriteLine("ADDR:" + output.ScriptPubKey.Address + " on block" + block.Result.Height);
                            }
                            return "";
                        });
                    await voutSession.CloseAsync();
                });
            }
            else
            {
                trxSession = _driver.AsyncSession();
                //Create the transaction node
                var neo4jTransResp = await trxSession.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "CREATE (a:Transaction) " +
                            "SET a.txid = $trx.TxId, " +
                            "a.hash = $trx.Hash, " +
                            "a.version = $trx.Version, " +
                            "a.size = $trx.Size, " +
                            "a.vsize = $trx.VSize, " +
                            "a.weight = $trx.Weight, " +
                            "a.locktime = $trx.LockTime, " +
                            "a.hex = $trx.Hex " +
                            "RETURN a.hash + ', from node ' + id(a)",
                            new { trx });

                        var record = await result.SingleAsync();
                        return record[0].As<string>();
                    });

                //Create the transaction IN_BLOCK relationship
                neo4jTransResp = await trxSession.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "MATCH (a:Block), (b:Transaction) " +
                            "WHERE a.hash = $block.Result.Hash AND b.txid = $trx.TxId " +
                            "CREATE (b)-[r:IN_BLOCK]->(a) " +
                            "RETURN type(r)",
                             new { trx, block });

                        var record = await result.ConsumeAsync();
                        return "";
                    });
                var ctVin = new CancellationToken();
                await Parallel.ForEachAsync(trx.Vin, options, async (input, ctVin) =>
                {
                    var vinSession = _driver.AsyncSession();
                    //Create relate the previous output to the input 
                    var neo4jInputResp = await vinSession.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "MATCH (a:Output) " +
                                "WHERE a.txid = $input.TxId  " +
                                "MERGE (b:Address {address:$input.ScriptPubKey.Address}) " +
                                "ON CREATE SET b.asm = $input.ScriptPubKey.Asm, " +
                                "b.hex = $input.ScriptPubKey.Hex, " +
                                "b.type = $input.ScriptPubKey.Type, " +
                                "b.address = $input.ScriptPubKey.Address " +
                                "CREATE (a)-[r:UNLOCKED_BY]->(b) " +
                                "RETURN type(r) ",
                                new { input, trx });

                            var record = await result.ConsumeAsync();
                            if (!string.IsNullOrEmpty(input.ScriptPubKey.Address))
                            {
                                Console.WriteLine("ADDR:" + input.ScriptPubKey.Address + " on block" + block.Result.Height);
                            }
                            return "";
                        });
                });
                var ctVout = new CancellationToken();
                //Iterate over the outputs in the coinbase transaction
                await Parallel.ForEachAsync(trx.Vout, options, async (output, ctVout) =>
                {
                    var voutSession = _driver.AsyncSession();
                    //Create the output node 
                    var neo4jOutpuResp = await voutSession.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "CREATE (a:Output) " +
                                "SET a.value = $output.Value, " +
                                "a.n = $output.N " +
                                "WITH a " +
                                "MATCH (b:Transaction) " +
                                "WHERE b.txid = $trx.TxId " +
                                "CREATE (a)-[r:HAS_OUTPUT]->(b) " +
                                "MERGE (c:Address) " +
                                "ON CREATED SET c.asm = $output.ScriptPubKey.Asm, " +
                                "c.hex = $output.ScriptPubKey.Hex, " +
                                "c.type = $output.ScriptPubKey.Type, " +
                                "c.address = $output.ScriptPubKey.Address " +
                                "CREATE (a)-[d:LOCKED_BY]->(c) " +
                                "RETURN type(r) ",
                                new { output, trx });

                            var record = await result.ConsumeAsync();
                            if (!string.IsNullOrEmpty(output.ScriptPubKey.Address))
                            {
                                Console.WriteLine("ADDR:" + output.ScriptPubKey.Address + " on block" + block.Result.Height);
                            }
                            return "";
                        });
                });
            }
        });

        prevBlock = block;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}
Console.WriteLine($"total seconds: {stopwatch.Elapsed.TotalSeconds}");
Thread.Sleep(10000);