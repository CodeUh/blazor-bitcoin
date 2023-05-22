using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoricalNeo4jLoad.Models
{
    public class RPCBlockResponse 
    {
        public string Jsonrpc { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public Block Result { get; set; } = new Block();
        public string Error { get; set; } = string.Empty;
    }
}
