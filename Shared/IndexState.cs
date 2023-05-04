using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorBitcoin.Shared
{
    public class IndexState
    {
        public IndexState()
        {
            BlockChainInfo = new BlockchainInfo();
        }
        public BlockchainInfo BlockChainInfo { get; set; }
    }
}
