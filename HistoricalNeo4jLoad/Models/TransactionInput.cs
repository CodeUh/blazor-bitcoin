using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class TransactionInput
    {
        [JsonPropertyName("coinbase")]
        public string Coinbase { get; set; } = string.Empty;
        
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        [JsonPropertyName("vout")]
        public int Vout { get; set; } = 0;

        [JsonPropertyName("value")]
        public double Value { get; set; } = 0.00;

        [JsonPropertyName("scriptSig")]
        public ScriptSig ScriptSig { get; set; } = new ScriptSig();

        [JsonPropertyName("sequence")]
        public long Sequence { get; set; } = 0;

        [JsonPropertyName("txinwitness")]
        public List<string> Txinwitness { get; set; } = new List<string>();
    }

}
