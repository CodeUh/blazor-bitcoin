using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBitcoin.Shared.Models
{
    public class IndexState
    {
        public IndexState()
        {
            BlockChainInfo = new BlockchainInfoResponse();
        }
        public BlockchainInfoResponse BlockChainInfo { get; set; }
    }
}
