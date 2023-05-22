using HistoricalNeo4jLoad.Models;
using Neo4j.Driver;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HistoricalNeo4jLoad
{
    internal class Program
    {
        private const string GENESIS_BLOCK = "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f";
        private const string RECENT_BLOCK = "000000000000000000013d1108a6a52495ac3900e091f44d889580875fa95de8";
        static async Task Main(string[] args)
        {
            try
            {
                List<Block> blocks = new List<Block>(); 
                using (var client = CreateHttpClient())
                {
                    blocks.Add(await GetBlock(client, GENESIS_BLOCK));
                }
                using(var session = CreateNeo4jSession())
                {
                    await CheckAndCreateConstraintsAsync(session);

                    await session.ExecuteWriteAsync(async tx =>
                    {
                        var result = await tx.RunAsync(CypherQueries.LoadBlockCypher,new { blocks });
                    });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var url = "http://127.0.0.1:8332";
            var client = new HttpClient();
            client.BaseAddress = new Uri(url);

            var rpcUsername = Environment.GetEnvironmentVariable("btcrpcuser");
            var rpcPassword = Environment.GetEnvironmentVariable("btcrpcpw");

            var byteArray = Encoding.ASCII.GetBytes($"{rpcUsername}:{rpcPassword}");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            return client;
        }

        private static async Task<Block> GetBlock(HttpClient client, string blockhash)
        {
            var content = CreateRPCRequestContent("getblock", new object[] { blockhash, 2 });
            var httpResp = await client.PostAsync("", content);
            var resp = await httpResp.Content.ReadFromJsonAsync<RPCBlockResponse>() ?? throw new Exception("No response from RPC");
            return resp.Result;
        }

        private static StringContent CreateRPCRequestContent(string method, object[] param)
        {
            var rpcRequest = new
            {
                jsonrpc = "1.0",
                id = method,
                method,
                @params = param
            };

            var content = new StringContent(
                JsonSerializer.Serialize(rpcRequest),
                Encoding.UTF8,
                "application/json"
            );
            return content;
        }

        private static IAsyncSession CreateNeo4jSession()
        {
            var driver = GraphDatabase.Driver(Environment.GetEnvironmentVariable("neo4juri"),
                             AuthTokens.Basic(Environment.GetEnvironmentVariable("neo4juser"),
                                              Environment.GetEnvironmentVariable("neo4jpw")));

            var session = driver.AsyncSession();
            return session;
        }

        public static async Task CheckAndCreateConstraintsAsync(IAsyncSession session)
        {
            var constraints = await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync(CypherQueries.ShowConstraints);
                return await result.ToListAsync();
            });

            var constraintsDict = constraints.ToDictionary(
                record => record["name"].As<string>(),
                record => record["ownedIndex"].As<string>());

            var newConstraints = CypherQueries.CreateConstraints;

            foreach (var constraint in newConstraints)
            {
                var constraintName = constraint.Split(' ')[2];

                if (!constraintsDict.ContainsKey(constraintName))
                {
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        await tx.RunAsync(constraint);
                    });
                }
            }
        }
    }
}