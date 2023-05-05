using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBitcoin.Shared.Models
{
    public class RpcRequest
    {
        public string Method { get; set; }
        public object[] Params { get; set; }
        public string Id { get; set; } = "1";
        public string Jsonrpc { get; set; } = "1.0";
    }
}
