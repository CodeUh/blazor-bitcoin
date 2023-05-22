using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class ScriptPubKey
    {
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;

        [JsonPropertyName("reqSigs")]
        public int ReqSigs { get; set; } = 0;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("addresses")]
        public List<string> Addresses { get; set; } = new List<string>();
    }

}
