using System.Net.Http.Headers;
using System.Net;
using Neo4j.Driver;
using System.Text;
using Newtonsoft.Json;
using BlazorBitcoin.Shared.Models;
using System.Text.Json;
using System.Diagnostics;

ServicePointManager.DefaultConnectionLimit = 12;
var options = new ParallelOptions { MaxDegreeOfParallelism = 12 };
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
var startingHeight = 60000;
var blockCount = 500000;

BlockResponse prevBlock = null;
var stopwatch = new Stopwatch();
stopwatch.Start();
var nextBlockHash = "";
for (int currentHeight = startingHeight; currentHeight < (startingHeight + blockCount); currentHeight++)
{
    try
    {
        dynamic rpcRequest;
        dynamic blockHash = new System.Dynamic.ExpandoObject(); ;
        if (string.IsNullOrEmpty(nextBlockHash))
        {
            //get the block hash for current height
            rpcRequest = new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockhash",
                @params = new object[] { currentHeight }
            };
            var hasContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            var hashResponse = await client.PostAsync("", hasContent);
            blockHash = JsonConvert.DeserializeObject<dynamic>(await hashResponse.Content.ReadAsStringAsync());
        } 
        else
        {
            blockHash.result = nextBlockHash;
        }

        //get the block with hash for current height
        rpcRequest = new
        {
            jsonrpc = "1.0",
            id = "2",
            method = "getblock",
            @params = new object[] { blockHash.result.ToString(), 2 }
        };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
        var response = await client.PostAsync("", content);
        var blockJson = await response.Content.ReadAsStringAsync();
        var block = System.Text.Json.JsonSerializer.Deserialize<BlockResponse>(blockJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        nextBlockHash = block.Result.NextBlockHash;
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

        //create the block HAS_CHAIN relationships
        if (prevBlock != null)
        {
            neo4jBlockResp = await session.ExecuteWriteAsync(
                async tx =>
                {
                    //TODO:Set properties on the relationship including the hash of the next or previous block
                    var result = await tx.RunAsync(
                        "MATCH (a:Block), (b:Block) " +
                        "WHERE a.hash = $block.Result.Hash AND b.hash = $prevBlock.Result.Hash " +
                        "CREATE (a)-[r:HAS_CHAIN]->(b) " +
                        "SET r.previousblockhash = $block.Result.PreviousBlockHash " +
                        "CREATE (b)-[r2:HAS_CHAIN]->(a) " +
                        "SET r2.nextblockhash = $block.Result.nextblockhash " +
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
            //todo, get rid of nesting
            
            var hasCoinbase = !string.IsNullOrEmpty(trx.Vin[0].Coinbase);
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
                        "a.hex = $trx.Hex " +
                        (hasCoinbase ? "CREATE (c:Coinbase) " +
                        "SET c.coinbase =  $trx.Vin[0].Coinbase, " +
                        "c.sequence =  $trx.Vin[0].Sequence " : " ") +
                        "WITH  a" +
                        (hasCoinbase ? ",c " : " ") +
                        "MATCH (b:Block) " +
                        "WHERE b.hash = $block.Result.Hash " +
                        "CREATE (a)-[r:IN_BLOCK]->(b) " +
                        (hasCoinbase ? "CREATE (b)-[r2:HAS_COINBASE]->(c) " +
                        "CREATE (c)-[r3:IN]->(a) " : " ") +
                        "RETURN a.hash + ', from node ' + id(a) ",
                        new { trx, block }); ;

                    var record = await result.SingleAsync();
                    return record[0].As<string>();
                });
            if (!hasCoinbase)
            {
                var ctVin = new CancellationToken();
                await Parallel.ForEachAsync(trx.Vin, options, async (input, ctVin) =>
                {
                    var vinSession = _driver.AsyncSession();
                    //Create relate the previous output to the input 
                    var neo4jInputResp = await vinSession.ExecuteWriteAsync(
                        async tx =>
                        {
                            var hasAddress = !string.IsNullOrEmpty(input.ScriptPubKey.Address);
                            var result = await tx.RunAsync(
                                "MATCH (a:Output {txid: $input.TxId, n: $input.Vout}) " +
                                (hasAddress ? "MERGE (b:Address {address:$input.ScriptPubKey.Address}) " +
                                "SET b.address = $input.ScriptPubKey.Address " +
                                "CREATE (a)-[r:UNLOCKED_BY]->(b) " : "") +
                                "WITH a " +
                                "MATCH (c: Transaction {txid:$trx.TxId}) " +
                                "CREATE (a)-[r:IN]->(c) " +
                                "SET r.hex = $input.ScriptPubKey.Hex, " +
                                "r.type = $input.ScriptPubKey.Type, " +
                                "r.asm = $input.ScriptPubKey.Asm " +
                                "RETURN type(r) ",
                                new { input, trx });

                            var record = await result.ConsumeAsync();
                            return "";
                        });
                });
            }
                
            var ctVout = new CancellationToken();
            //Iterate over the outputs in the transaction
            await Parallel.ForEachAsync(trx.Vout, options, async (output, ctVout) =>
            {
                var voutSession = _driver.AsyncSession();
                //Create the output node 
                var neo4jOutputResp = await voutSession.ExecuteWriteAsync(
                    async tx =>
                    {
                        var hasAddress = !string.IsNullOrEmpty(output.ScriptPubKey.Address);
                        var result = await tx.RunAsync(
                            "MERGE (a:Output {txid: $trx.TxId, n:$output.N}) " +
                            " " +
                            "SET a.value = $output.Value, " +
                            "a.n = $output.N " +
                            "WITH a " +
                            "MATCH (b:Transaction) " +
                            "WHERE b.txid = $trx.TxId " +
                            "CREATE (b)-[r:OUT]->(a) " +
                            "SET r.asm = $output.ScriptPubKey.Asm, " +
                            "r.hex = $output.ScriptPubKey.Hex, " +
                            "r.type = $output.ScriptPubKey.Type " +
                            (hasAddress ? "MERGE (c:Address {address:$output.ScriptPubKey.Address}) " + 
                            "SET c.address = $output.ScriptPubKey.Address " + 
                            "CREATE (a)-[d:LOCKED_BY]->(c) " : " ") +
                            "RETURN type(r) ",
                            new { output, trx });

                        var record = await result.ConsumeAsync();
                        return "";
                    });
                await voutSession.CloseAsync();
            });
        });

        prevBlock = block;
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}
Console.WriteLine($"total seconds: {stopwatch.Elapsed.TotalSeconds}");
Console.ReadLine();