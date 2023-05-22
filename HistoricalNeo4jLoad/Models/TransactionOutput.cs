using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class TransactionOutput
    {
        [JsonPropertyName("txid")]
        public string TxId { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public double Value { get; set; } = 0.00;

        [JsonPropertyName("n")]
        public int N { get; set; } = 0;

        [JsonPropertyName("scriptPubKey")]
        public ScriptPubKey ScriptPubKey { get; set; } = new ScriptPubKey();
    }

}
