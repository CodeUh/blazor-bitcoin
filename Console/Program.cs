using System.Net.Http.Headers;
using System.Net;
using Neo4j.Driver;
using System;
using System.Text;
using Newtonsoft.Json;
using BlazorBitcoin.Shared.Models;
using System.Text.Json;
using System.Runtime.CompilerServices;

ServicePointManager.DefaultConnectionLimit = 10;
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
var startingHeight = 50;
var blockCount = 50;
BlockResponse prevBlock = null;
for(int currentHeight = startingHeight; currentHeight < (startingHeight + blockCount); currentHeight++)
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
                        "CREATE (a)-[r:PREV_BLOCK]->(b) " +
                        "CREATE (b)-[r2:NEXT_BLOCK]->(a) " +
                        "RETURN type(r)",
                         new { prevBlock, block });

                    var record = await result.ConsumeAsync();
                    return "";
                });
        }

        //Iterate over the transactions in the block
        foreach(var trx in block.Result.Transactions)
        {
            //check if the transaction is a coinbase transaction
            if (!string.IsNullOrEmpty(trx.Vin[0].Coinbase))
            {
                //Create the coinbase node
                var neo4jTransResp = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "CREATE (a:Coinbase) " +
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
                neo4jTransResp = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "MATCH (a:Block), (b:Coinbase) " +
                            "WHERE a.hash = $block.Result.Hash AND b.txid = $trx.TxId " +
                            "CREATE (b)-[r:IN_BLOCK]->(a) " +
                            "RETURN type(r)",
                             new { trx, block });

                        var record = await result.ConsumeAsync();
                        return "";
                    });

                //Iterate over the outputs in the coinbase transaction
                foreach (var output in trx.Vout)
                {
                    //Create the output node 
                    var neo4jOutputResp = await session.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "CREATE (a:Output) " +
                                "SET a.value = $output.Value, " +
                                "a.n = $output.N " +
                                "WITH a " +
                                "MATCH (b:Coinbase) " +
                                "WHERE b.txid = $trx.TxId " +
                                "CREATE (a)-[r:OUTPUT_OF]->(b) " +
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
                                Console.WriteLine("ADDR:"+output.ScriptPubKey.Address+" on block" + block.Result.Height);
                            }
                            return "";
                        });
                }
            }
            else
            {
                //Create the coinbase node
                var neo4jTransResp = await session.ExecuteWriteAsync(
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

                //Create the coinbase IN_BLOCK relationship
                neo4jTransResp = await session.ExecuteWriteAsync(
                    async tx =>
                    {
                        var result = await tx.RunAsync(
                            "MATCH (a:Block), (b:Coinbase) " +
                            "WHERE a.hash = $block.Result.Hash AND b.txid = $trx.TxId " +
                            "CREATE (b)-[r:IN_BLOCK]->(a) " +
                            "RETURN type(r)",
                             new { trx, block });

                        var record = await result.ConsumeAsync();
                        return "";
                    });

                foreach (var input in trx.Vin)
                {
                    //Create relate the previous output to the input 
                    var neo4jInputResp = await session.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "MATCH (a:Output),(b:Address) " +
                                "WHERE a.txid = $input.TxId AND b.address = $input.ScriptPubKey.Address " +
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
                }

                //Iterate over the outputs in the coinbase transaction
                foreach (var output in trx.Vout)
                {
                    //Create the output node 
                    var neo4jOutpuResp = await session.ExecuteWriteAsync(
                        async tx =>
                        {
                            var result = await tx.RunAsync(
                                "CREATE (a:Output) " +
                                "SET a.value = $output.Value, " +
                                "a.n = $output.N " +
                                "WITH a " +
                                "MATCH (b:Coinbase) " +
                                "WHERE b.txid = $trx.TxId " +
                                "CREATE (a)-[r:OUTPUT_OF]->(b) " +
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
                }
            }
        }

        prevBlock = block;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}