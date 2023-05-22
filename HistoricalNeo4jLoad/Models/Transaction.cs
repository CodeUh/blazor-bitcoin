using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class Transaction
    {
        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;

        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public int Size { get; set; } = 0;

        [JsonPropertyName("vsize")]
        public int Vsize { get; set; } = 0;

        [JsonPropertyName("weight")]
        public int Weight { get; set; } = 0;

        [JsonPropertyName("version")]
        public int Version { get; set; } = 0;

        [JsonPropertyName("locktime")]
        public int Locktime { get; set; } = 0;

        [JsonPropertyName("vin")]
        public List<TransactionInput> Vin { get; set; } = new List<TransactionInput>();

        [JsonPropertyName("vout")]
        public List<TransactionOutput> Vout { get; set; } = new List<TransactionOutput>();

        [JsonPropertyName("blockhash")]
        public string BlockHash { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public long Time { get; set; } = 0;

        [JsonPropertyName("blocktime")]
        public long Blocktime { get; set; } = 0;

        [JsonPropertyName("fee")]
        public double Fee { get; set; } = 0.00;
    }

}
