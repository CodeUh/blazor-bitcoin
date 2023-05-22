using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class ScriptSig
    {
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;
    }

}
