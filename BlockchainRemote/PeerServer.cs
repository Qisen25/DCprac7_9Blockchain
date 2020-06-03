using APIClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainRemote
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, UseSynchronizationContext = false)]
    internal class PeerServer : RemoteInterface
    {
        private static Blockchain chain = Blockchain.GetInstance();
        private static Queue<TransactionStruct> newTrx = new Queue<TransactionStruct>();

        public List<Block> GetCurrentChain()
        {
            return chain.GetCurrentChain();
        }

        public void AddBlock(Block block)
        {
            chain.AddToChain(block);
        }

        public Block GetLatestBlock()
        {
            return chain.GetLastBlock();
        }


        public TransactionStruct ReceiveTransaction(TransactionStruct tr)
        {
            TransactionStruct outTrx = null;

            if (tr != null)
            {
                newTrx.Enqueue(tr);
            }
            else if(newTrx.Count > 0)
            {
                outTrx = newTrx.Dequeue();
            }

            return outTrx;
        }

        public void SetChain(List<Block> popularChain)
        {
            Block[] arr = new Block[popularChain.Count] ;
            popularChain.CopyTo(arr);

            chain.SetChain(arr.ToList<Block>());
        }

        public float GetWalletBalance(uint userID)
        {
            return chain.GetBalance(userID);
        }
    }
}
