using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BlazorBitcoin.Shared
{
    public class RpcResponse<T>
    {
        [JsonProperty("result")][JsonPropertyName("result")]public T Result { get; set; } = default(T);

        [JsonProperty("error")][JsonPropertyName("error")]public RpcError Error { get; set; } = new RpcError();

        [JsonProperty("id")][JsonPropertyName("id")]public string Id { get; set; } = string.Empty;
    }

    public class RpcError
    {
        [JsonProperty("code")][JsonPropertyName("code")]public int Code { get; set; } = 0;

        [JsonProperty("message")][JsonPropertyName("message")]public string Message { get; set; } = string.Empty;

        [JsonProperty("data")][JsonPropertyName("data")]public object Data { get; set; } = new object();
    }
}
