using APIClass;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace BlockchainRemote
{
    public class Blockchain
    {
        private static Blockchain instance = null;
        private List<Block> chain;

        public static Blockchain GetInstance()
        {
            if(instance == null)
            {
                instance = new Blockchain();
            }

            return instance;
        }

        private Blockchain()
        {
            chain = new List<Block>();
            Block genesis = new Block
            {
                ID = 0,
                JsonStrList = "",
                Offset = 52968,
                Hash = "1234542389",
                PrevHash = ""
            };

            chain.Add(genesis);
        }

        public void AddToChain(Block bl)
        {
            //bl.PrevHash = chain.Last().Hash;

            if (ValidateTransaction(bl) && ValidateHash(bl))
            {
                //bl.ID = chain.Last().ID + 1;
                chain.Add(bl);
            }
        }

        public Block GetLastBlock()
        {
            return chain.Last();
        }

        public List<Block> GetCurrentChain()
        {
            return chain;
        }

        public List<string[]> GetAnswers(List<string[]> clientList, out bool found)
        {
            found = false;
            foreach(Block b in chain)
            {
                List<string[]> blockJson = JsonConvert.DeserializeObject<List<string[]>>(b.JsonStrList);
                if (blockJson != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        string[] arr1 = blockJson[i];
                        string[] arr2 = clientList[i];
                        if (arr1[0].Equals(arr2[0]) && string.IsNullOrEmpty(arr2[1]))//check if code matches and answer is empty
                        {
                            arr2[1] = arr1[1];//add answer
                            clientList[i] = arr2;
                            found = true;
                        }
                    }
                }
            }

            return clientList;
        }

        public void SetChain(List<Block> popularChain)
        {
            chain = popularChain;
        }

        public bool ValidateTransaction(Block bl)
        {
            bool isValid = false;

            if (bl.PrevHash.Equals(chain.Last().Hash))
            {
                isValid = bl.ValidateData();
            }

            return isValid;
        }

        public bool ValidateHash(Block bl)
        {
            string  signature = bl.ID.ToString() + bl.JsonStrList + bl.Offset.ToString() + bl.PrevHash;

            Debug.WriteLine("Concat " + signature);

            string tempHash;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] txtBytes = System.Text.Encoding.ASCII.GetBytes(signature);
                byte[] hash = sha256Hash.ComputeHash(txtBytes);

                tempHash = BitConverter.ToUInt32(hash, 0).ToString();
            }

            Debug.WriteLine("hash " + bl.Hash);

            return bl.Hash.Equals(tempHash);
        }

        //public float GetBalance(uint userID)
        //{
        //    List<Block> temp = chain.FindAll(x => x.RecepientID == userID);
        //    float coinsReceived = 0.0F, coinsSent = 0.0F, total = 0.0f;

        //    foreach(Block b in chain)
        //    {
        //        if (b.RecepientID == userID)
        //        {
        //            coinsReceived += b.Amount;
        //        }
        //        else if(b.SenderID == userID)
        //        {
        //            coinsSent += b.Amount;
        //        }
        //    }

        //    total = coinsReceived - coinsSent;

        //    return total;
        //}

        //public Block MakeHash(Block bl)
        //{
        //    string signature = null;

        //    while (!bl.Hash.StartsWith("12345"))
        //    {
        //        bl.Offset++;

        //        signature = bl.ID.ToString() + bl.SenderID.ToString() + bl.RecepientID.ToString()
        //                      + bl.Amount.ToString() + bl.Offset.ToString() + bl.PrevHash;

        //        Debug.WriteLine("Concat " + signature);

        //        using (SHA256 sha256Hash = SHA256.Create())
        //        {
        //            byte[] txtBytes = System.Text.Encoding.ASCII.GetBytes(signature);
        //            byte[] hash = sha256Hash.ComputeHash(txtBytes);

        //            bl.Hash = BitConverter.ToUInt32(hash, 0).ToString();
        //        }


        //        Debug.WriteLine("hash " + bl.Hash);
        //    }

        //    return bl;
        //}
    }
}