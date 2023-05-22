using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class Block
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public int Size { get; set; } = 0;

        [JsonPropertyName("strippedsize")]
        public int StrippedSize { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 0;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 0;

        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonPropertyName("versionHex")]
        public string VersionHex { get; set; } = string.Empty;

        [JsonPropertyName("merkleroot")]
        public string Merkleroot { get; set; } = string.Empty;  

        [JsonPropertyName("tx")]
        public List<Transaction> Tx { get; set; } = new List<Transaction>();

        [JsonPropertyName("time")]
        public int Time { get; set; } = 0; 

        [JsonPropertyName("mediantime")]
        public int MedianTime { get; set; } = 0;

        [JsonPropertyName("nonce")]
        public long Nonce { get; set; } = 0;

        [JsonPropertyName("bits")]
        public string Bits { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public double Difficulty { get; set; } = 0;

        [JsonPropertyName("chainwork")]
        public string Chainwork { get; set; } = string.Empty;

        [JsonPropertyName("nTx")]
        public int NTx { get; set; } = 0;

        [JsonPropertyName("previousblockhash")]
        public string PreviousBlockHash { get; set; } = string.Empty;

        [JsonPropertyName("nextblockhash")]
        public string NextBlockHash { get; set; } = string.Empty;
    }
}
