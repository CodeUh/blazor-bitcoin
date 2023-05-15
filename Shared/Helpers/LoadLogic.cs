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
using static System.Collections.Specialized.BitVector32;

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
                var result = await tx.RunAsync(
                    "MERGE (b:Block {hash:$block.Result.Hash}) " +
                    "SET b.hash = $block.Result.Hash, " +
                    "b.confirmations = $block.Result.Confirmations, " +
                    "b.height = $block.Result.Height, " +
                    "b.version = $block.Result.Version, " +
                    "b.versionHex = $block.Result.VersionHex, " +
                    "b.merkleroot = $block.Result.MerkleRoot, " +
                    "b.time = $block.Result.Time, " +
                    "b.mediantime = $block.Result.MedianTime, " +
                    "b.nonce = $block.Result.Nonce, " +
                    "b.bits = $block.Result.Bits, " +
                    "b.difficulty = $block.Result.Difficulty, " +
                    "b.nTx = $block.Result.NTx, " +
                    "b.strippedsize = $block.Result.StrippedSize, " +
                    "b.size = $block.Result.Size, " +
                    "b.weight = $block.Result.Weight " +
                    "WITH b " +
                    "MERGE (prev: Block {hash: $block.Result.PreviousBlockHash}) " +
                    "ON CREATE SET prev.hash = $block.Result.PreviousBlockHash " +
                    "MERGE (b)-[r:CHAIN]->(prev) " +
                    "SET r.hash = prev.hash ",
                    new { block });

                var record = await result.ConsumeAsync();
                return "";
            });
        }
    }
}
