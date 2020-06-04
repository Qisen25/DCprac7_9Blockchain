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
        private static string pythonJobs = null;

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


        public string ReceiveTransaction(string inPythonList)
        {
            string outTrx = null;

            if (!string.IsNullOrEmpty(inPythonList)) 
            {
                pythonJobs = inPythonList;
            }
            else
            {
                outTrx = pythonJobs;
                pythonJobs = null;
            }

            return outTrx;
        }

        public void SetChain(List<Block> popularChain)
        {
            Block[] arr = new Block[popularChain.Count] ;
            popularChain.CopyTo(arr);

            chain.SetChain(arr.ToList<Block>());
        }

        public List<string[]> GetAnswers(List<string[]> inList, out bool found)
        {
            return chain.GetAnswers(inList, out found);
        }

        //public float GetWalletBalance(uint userID)
        //{
        //    return chain.GetBalance(userID);
        //}
    }
}
