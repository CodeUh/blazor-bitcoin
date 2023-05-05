namespace BlazorBitcoin.Shared.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json;

    public class BlockchainInfoResponse
    {
        [JsonProperty("result")]
        [JsonPropertyName("result")]
        public BlockchainInfoResult Result { get; set; } = new BlockchainInfoResult();

        [JsonProperty("error")]
        [JsonPropertyName("error")]
        public object Error { get; set; } = new object();

        [JsonProperty("id")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class BlockchainInfoResult
    {
        [JsonProperty("chain")]
        [JsonPropertyName("chain")]
        public string Chain { get; set; } = string.Empty;

        [JsonProperty("blocks")]
        [JsonPropertyName("blocks")]
        public int Blocks { get; set; } = 0;

        [JsonProperty("headers")]
        [JsonPropertyName("headers")]
        public int Headers { get; set; } = 0;

        [JsonProperty("bestblockhash")]
        [JsonPropertyName("bestblockhash")]
        public string BestBlockHash { get; set; } = string.Empty;

        [JsonProperty("difficulty")]
        [JsonPropertyName("difficulty")]
        public double Difficulty { get; set; } = 0;

        
        //public int MedianTime { get; set; } = 0;

        private int medianTimeUnix;

        [JsonProperty("mediantime")]
        [JsonPropertyName("mediantime")]

        public int MedianTimeUnix
        {
            get 
            { 
                return medianTimeUnix; 
            }
            set 
            { 
                medianTimeUnix = value; 
                MedianDateTime = Helpers.UnixTimeHelper.UnixTimeToDateTime(value);
            }
        }

        public DateTime MedianDateTime { get; set; }


        [JsonProperty("verificationprogress")]
        [JsonPropertyName("verificationprogress")]
        public double VerificationProgress { get; set; } = 0;

        [JsonProperty("initialblockdownload")]
        [JsonPropertyName("initialblockdownload")]
        public bool InitialBlockDownload { get; set; } = false;

        [JsonProperty("chainwork")]
        [JsonPropertyName("chainwork")]
        public string ChainWork { get; set; } = string.Empty;

        [JsonProperty("size_on_disk")]
        [JsonPropertyName("size_on_disk")]
        public long SizeOnDisk { get; set; } = 0;

        [JsonProperty("pruned")]
        [JsonPropertyName("pruned")]
        public bool Pruned { get; set; } = false;

        [JsonProperty("softforks")]
        [JsonPropertyName("softforks")]
        public Softforks Softforks { get; set; } = new Softforks();

        [JsonProperty("warnings")]
        [JsonPropertyName("warnings")]
        public string Warnings { get; set; } = string.Empty;
    }

    public class Softforks
    {
        [JsonProperty("bip34")]
        [JsonPropertyName("bip34")]
        public Bip Bip34 { get; set; } = new Bip();

        [JsonProperty("bip66")]
        [JsonPropertyName("bip66")]
        public Bip Bip66 { get; set; } = new Bip();

        [JsonProperty("bip65")]
        [JsonPropertyName("bip65")]
        public Bip Bip65 { get; set; } = new Bip();

        [JsonProperty("csv")]
        [JsonPropertyName("csv")]
        public Bip Csv { get; set; } = new Bip();

        [JsonProperty("segwit")]
        [JsonPropertyName("segwit")]
        public Bip Segwit { get; set; } = new Bip();

        [JsonProperty("taproot")]
        [JsonPropertyName("taproot")]
        public Taproot Taproot { get; set; } = new Taproot();
    }

    public class Bip
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("active")]
        [JsonPropertyName("active")]
        public bool Active { get; set; } = false;

        [JsonProperty("height")]
        [JsonPropertyName("height")]
        public int Height { get; set; } = 0;
    }

    public class Taproot
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("bip9")]
        [JsonPropertyName("bip9")]
        public Bip9 Bip9 { get; set; } = new Bip9();

        [JsonProperty("active")]
        [JsonPropertyName("active")]
        public bool Active { get; set; } = false;
    }

    public class Bip9
    {
        [JsonProperty("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("start_time")]
        [JsonPropertyName("start_time")]
        public int StartTime { get; set; } = 0;

        [JsonProperty("timeout")]
        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 0;

        [JsonProperty("since")]
        [JsonPropertyName("since")]
        public int Since { get; set; } = 0;

        [JsonProperty("min_activation_height")]
        [JsonPropertyName("min_activation_height")]
        public int MinActivationHeight { get; set; } = 0;
    }




}
