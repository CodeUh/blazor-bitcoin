using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BlazorBitcoin.Shared
{
    public class BlockchainInfo
    {
        [JsonProperty("chain")]
        [JsonPropertyName("chain")]
        public string Chain { get; set; } = string.Empty;

        [JsonProperty("blocks")][JsonPropertyName("blocks")]public int Blocks { get; set; } = 0;

        [JsonProperty("headers")][JsonPropertyName("headers")]public int Headers { get; set; } = 0;

        [JsonProperty("bestblockhash")][JsonPropertyName("bestblockhash")]public string BestBlockHash { get; set; } = string.Empty;

        [JsonProperty("difficulty")][JsonPropertyName("difficulty")]public double Difficulty { get; set; } = 0.0;

        [JsonProperty("mediantime")][JsonPropertyName("mediantime")]public int MedianTime { get; set; } = 0;

        [JsonProperty("verificationprogress")][JsonPropertyName("verificationprogress")]public double VerificationProgress { get; set; } = 0.0;

        [JsonProperty("initialblockdownload")][JsonPropertyName("initialblockdownload")]public bool InitialBlockDownload { get; set; } = false;

        [JsonProperty("chainwork")][JsonPropertyName("chainwork")]public string ChainWork { get; set; } = string.Empty;

        [JsonProperty("size_on_disk")][JsonPropertyName("size_on_disk")]public long SizeOnDisk { get; set; } = 0;

        [JsonProperty("pruned")][JsonPropertyName("pruned")]public bool Pruned { get; set; } = false;

        [JsonProperty("softforks")][JsonPropertyName("softforks")]public List<Softfork> Softforks { get; set; } = new List<Softfork>();

        [JsonProperty("warnings")][JsonPropertyName("warnings")]public string Warnings { get; set; } = string.Empty;
    }

    public class Softfork
    {
        [JsonProperty("id")][JsonPropertyName("id")]public string Id { get; set; } = string.Empty;

        [JsonProperty("version")][JsonPropertyName("version")]public int Version { get; set; } = 0;

        [JsonProperty("reject")][JsonPropertyName("reject")]public SoftforkReject Reject { get; set; }  = new SoftforkReject();
    }

    public class SoftforkReject
    {
        [JsonProperty("status")][JsonPropertyName("status")] public bool Status { get; set; } = false;

        [JsonProperty("found")][JsonPropertyName("found")] public int Found { get; set; } = new int();

        [JsonProperty("required")][JsonPropertyName("required")] public int Required { get; set; } = new int();
    }
}
