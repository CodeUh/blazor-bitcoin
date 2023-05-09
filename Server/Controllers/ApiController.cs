using BlazorBitcoin.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorBitcoin.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class BlockchainController : ControllerBase
    {
        private readonly HttpClient _client;

        public BlockchainController(HttpClient client)
        {
            _client = client;
        }

        [HttpGet("blockchaininfo")]
        public async Task<BlockchainInfoResponse> GetBlockchainInfo()
        {
             
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
            catch(Exception ex)
            {
                return blockchainInfo;
            }
            
        }

        [HttpGet("block/{height}")]
        public async Task<ActionResult<BlockResponse>> GetBlock(int height = 0)
        {
            
            var rpcRequest = new 
            {
                jsonrpc = "1.0",
                id = "1",
                method = "getblockhash",
                @params = new object[] { height }
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            var response = await _client.PostAsync("", content);
            var blockHash = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

            rpcRequest = new
            {
                jsonrpc = "1.0",
                id = "2",
                method = "getblock",
                @params = new object[] { blockHash.result.ToString(), 1 }
            };
            content = new StringContent(System.Text.Json.JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json-rpc");
            response = await _client.PostAsync("", content);
            var blockJson = await response.Content.ReadAsStringAsync();
            var block = System.Text.Json.JsonSerializer.Deserialize<BlockResponse>(blockJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return block;
        }
    }
}

