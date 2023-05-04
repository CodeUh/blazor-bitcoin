using BlazorBitcoin.Shared;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BlazorBitcoin.Server.Controllers
{
    [ApiController]
    [Route("api")]
    public class BlockchainController : ControllerBase
    {
        private readonly HttpClient _client;

        public BlockchainController()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:8332/");
            var authValue = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(
                    System.Text.ASCIIEncoding.ASCII.GetBytes(
                        $"{Environment.GetEnvironmentVariable("btcrpcuser")}:{Environment.GetEnvironmentVariable("btcrpcuser")}")));
            _client.DefaultRequestHeaders.Authorization = authValue;
        }

        [HttpGet("blockchaininfo")]
        public async Task<BlockchainInfo> GetBlockchainInfo()
        {
            //TODO:Hack to get around working api
            var blockchainInfo = new BlockchainInfo() { Blocks=1 };
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
                if (!response.IsSuccessStatusCode)
                {
                }

                blockchainInfo = JsonConvert.DeserializeObject<BlockchainInfo>(await response.Content.ReadAsStringAsync());
                return blockchainInfo;
            }
            catch(Exception ex)
            {
                //TODO:Hack to get around working api

                return blockchainInfo;
            }
            
        }
    }
}
