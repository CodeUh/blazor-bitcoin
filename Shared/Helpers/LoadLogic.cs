using BlazorBitcoin.Shared.Models;
using Neo4j.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorBitcoin.Shared.Models;

namespace BlazorBitcoin.Shared
{
    public class LoadLogic
    {
        private HttpClient _client;
        private IDriver _driver;
        public LoadLogic()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:8332/");
            var authValue = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        $"{Environment.GetEnvironmentVariable("btcrpcuser")}:{Environment.GetEnvironmentVariable("btcrpcpw")}")));
            _client.DefaultRequestHeaders.Authorization = authValue;

            _driver = GraphDatabase.Driver(Environment.GetEnvironmentVariable("neo4juri"),
                AuthTokens.Basic(Environment.GetEnvironmentVariable("neo4juser"),
                Environment.GetEnvironmentVariable("neo4jpw")));
        }

        public async Task<BlockchainInfoResponse> GetBlockchainInfo()
        {
            //TODO: think about how to handle this error and not swallow and return a default
            var blockchainInfo = new BlockchainInfoResponse() { Result = new BlockchainInfoResult() { Blocks = 1 } };
            try
            {
                var request = new
                {
                    jsonrpc = "1.0",
                    id = "1",
                    method = "getblockchaininfo",
                    @params = new object[] { }
                };
                var requestJson = JsonConvert.SerializeObject(request);
                var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var blockchainInfoResponse = JsonConvert.DeserializeObject<BlockchainInfoResponse>(responseContent);
                return blockchainInfoResponse;
            }
            catch (Exception ex)
            {
                return blockchainInfo;
            }
        }

        public async Task<List<BlockResult>> GetBlocksWithoutCoinbase(int batchSize)
        {
            var results = new List<BlockResult>();
            try
            {
                var session = _driver.AsyncSession();
                results = await session.ExecuteReadAsync(async tx =>
                {
                    var cypher = $"MATCH (b:Block) WHERE NOT (b)-[:COINBASE]->() AND b.height > 0 RETURN b ORDER BY b.height ASC LIMIT $batchSize";
                    var cursor = await tx.RunAsync(cypher, new { batchSize });
                    var records = await cursor.ToListAsync();
                    foreach(var record in records)
                    {
                        results.Add(new BlockResult()
                        {
                            Hash = record["b"].As<INode>().Properties["hash"].As<string>(),
                            Height = record["b"].As<INode>().Properties["height"].As<int>(),
                            Version = record["b"].As<INode>().Properties["version"].As<int>(),
                            VersionHex = record["b"].As<INode>().Properties["versionHex"].As<string>(),
                            MerkleRoot = record["b"].As<INode>().Properties["merkleroot"].As<string>(),
                            Time = record["b"].As<INode>().Properties["time"].As<int>(),
                            MedianTime = record["b"].As<INode>().Properties["mediantime"].As<int>(),
                            Nonce = record["b"].As<INode>().Properties["nonce"].As<long>(),
                            Bits = record["b"].As<INode>().Properties["bits"].As<string>(),
                            Difficulty = record["b"].As<INode>().Properties["difficulty"].As<double>(),
                            //ChainWork = record["b"].As<INode>().Properties["chainwork"].As<string>(),
                            NTx = record["b"].As<INode>().Properties["nTx"].As<int>(),
                            StrippedSize = record["b"].As<INode>().Properties["strippedsize"].As<int>(),
                            Size = record["b"].As<INode>().Properties["size"].As<int>(),
                            Weight = record["b"].As<INode>().Properties["weight"].As<int>()
                        }) ;
                    }
                    return results;
                });
                await session.CloseAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return results;
        }

        public async Task<int> GetDBHeight()
        {
            var session = _driver.AsyncSession();
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cypher = "MATCH (b:Block) RETURN max(b.height) AS height";
                var cursor = await tx.RunAsync(cypher);
                var record = await cursor.SingleAsync();
                return record["height"].As<int>();
            });
            await session.CloseAsync();
            return result;
        }

        public async Task<Transaction> GetTransaction(string txid)
        {
            var transaction = new Transaction();
            try
            {
                var rpcRequest = new
                {
                    jsonrpc = "1.0",
                    id = "2",
                    method = "getrawtransaction",
                    @params = new object[] { txid, true }
                };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
                var response = await _client.PostAsync("", content);
                var blockJson = await response.Content.ReadAsStringAsync();
                transaction = System.Text.Json.JsonSerializer.Deserialize<TransactionResponse>(blockJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }).Result;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return transaction;
        }

        public async Task LoadBlock(int height)
        {
            var rpcRequest = new
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockhash",
                @params = new object[] { height }
            };
            var hasContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            var hashResponse = await _client.PostAsync("", hasContent);
            var blockHash = JsonConvert.DeserializeObject<dynamic>(await hashResponse.Content.ReadAsStringAsync());

            if (blockHash == null || string.IsNullOrEmpty(blockHash.result.ToString())) { throw new Exception("Error getting block height in LoadBlock"); }

            rpcRequest = new
            {
                jsonrpc = "1.0",
                id = "2",
                method = "getblock",
                @params = new object[] { blockHash.result.ToString(), 1 }
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            var response = await _client.PostAsync("", content);
            var blockJson = await response.Content.ReadAsStringAsync();
            var block = System.Text.Json.JsonSerializer.Deserialize<BlockResponse>(blockJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var previousBlockHash = block.Result.PreviousBlockHash;
            var nextBlockHash = block.Result.NextBlockHash;

            var session = _driver.AsyncSession();
            var neo4jBlockResp = await session.ExecuteWriteAsync(
            async tx =>
            {
                var cypher = @"
                    MERGE (b:Block {hash:$block.Result.Hash})
                    SET b.hash = $block.Result.Hash,
                    b.height = $block.Result.Height,
                    b.version = $block.Result.Version,
                    b.versionHex = $block.Result.VersionHex,
                    b.merkleroot = $block.Result.MerkleRoot,
                    b.time = $block.Result.Time,
                    b.nonce = $block.Result.Nonce,
                    b.bits = $block.Result.Bits,
                    b.difficulty = $block.Result.Difficulty,
                    b.nTx = $block.Result.NTx,
                    b.strippedsize = $block.Result.StrippedSize,
                    b.size = $block.Result.Size,
                    b.weight = $block.Result.Weight
                    WITH b
                    MERGE (prev: Block {hash: $block.Result.PreviousBlockHash})
                    ON CREATE SET prev.hash = $block.Result.PreviousBlockHash
                    MERGE (b)-[r:CHAIN]->(prev)
                    SET r.hash = prev.hash
                ";
                var result = await tx.RunAsync(cypher, new { block });

                var record = await result.ConsumeAsync();
                return "";
            });
        }

        public async Task LoadCoinbase(BlockResult b)
        {
            var rpcRequest = new
            {
                jsonrpc = "1.0",
                id = "2",
                method = "getblock",
                @params = new object[] { b.Hash, 1 }
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            var response = await _client.PostAsync("", content);
            var blockJson = await response.Content.ReadAsStringAsync();
            var block = System.Text.Json.JsonSerializer.Deserialize<BlockResponse>(blockJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            //Get Coinbase tx
            var coinbaseTx = await GetTransaction(block.Result.Transactions[0]);

            var session = _driver.AsyncSession();
            var neo4jBlockResp = await session.ExecuteWriteAsync(
            async tx =>
            {
                var cypher = $@"
                    MERGE (t:Transaction {{txid:$coinbaseTx.TxId}}) 
                    SET  t.txid = $coinbaseTx.TxId, 
                    t.hash = $coinbaseTx.Hash, 
                    t.version = $coinbaseTx.Version, 
                    t.size = $coinbaseTx.Size, 
                    t.vsize = $coinbaseTx.VSize, 
                    t.weight = $coinbaseTx.Weight, 
                    t.locktime = $coinbaseTx.LockTime, 
                    t.hex = $coinbaseTx.Hex 
                    CREATE (c:Coinbase) 
                    SET c.coinbase = $coinbaseTx.Vin[0].Coinbase, 
                    c.sequence = $coinbaseTx.Vin[0].Sequence 
                    CREATE (t)<-[r:IN]-(c)
                    WITH t, c 
                    MATCH (b:Block {{hash: $block.Result.Hash}}) 
                    MERGE (t)-[r1:BLOCK]->(b) 
                    CREATE (b)-[r2:COINBASE]->(c) 
                    WITH t, c, b, r1, r2
                    UNWIND $coinbaseTx.Vout AS output
                    MERGE (o:Output {{txid: t.txid, n:output.N}})
                    SET o.value = output.Value,
                    o.n = output.N,
                    o.txid = t.txid
                    CREATE (t)-[r3:OUT]->(o)
                    SET r3.asm = output.ScriptPubKey.Asm,
                    r3.hex = output.ScriptPubKey.Hex,
                    r3.type = output.ScriptPubKey.Type
                    RETURN output.ScriptPubKey.Address AS address,t.txid AS txid, o.n AS n
                ";
                
                var result = await tx.RunAsync(cypher, new { coinbaseTx, block });

                var records = await result.ToListAsync();
                
                foreach(var record in records)
                {
                    var address = record.Values["address"].As<string>();

                    if (string.IsNullOrEmpty(address)) { return ""; }

                    var txid = record.Values["txid"].As<string>();
                    var n = record.Values["n"].As<int>();
                    cypher = $@"
                        MERGE (a:Address {{address: $address}})
                        SET a.address = $address
                        WITH a
                        MATCH (o:Output {{txid: $txid, n: $n}})
                        CREATE (a)-[r:LOCKED_BY]->(o)
                        RETURN a.address
                    ";
                    result = await tx.RunAsync(cypher, new { address,txid,n });

                    var resp = await result.ConsumeAsync();
                }
                return "";
            });
        }
    }
}
