using APIClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace Blockchain_WebService.Models
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
                SenderID = 0,
                RecepientID = 0,
                Amount = 99999,
                Offset = 0,
                Hash = "",
                PrevHash = ""
            };

            chain.Add(MakeHash(genesis));
        }

        public void AddToChain(Block bl)
        {
            //bl.PrevHash = chain.Last().Hash;

            if (ValidateTransaction(bl) && ValidateHash(bl))
            {
                bl.ID = chain.Last().ID + 1;
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

        public float GetBalance(uint userID)
        {
            List<Block> temp = chain.FindAll(x => x.RecepientID == userID);
            float coinsReceived = 0.0F, coinsSent = 0.0F, total = 0.0f;

            foreach(Block b in chain)
            {
                if (b.RecepientID == userID)
                {
                    coinsReceived += b.Amount;
                }
                else if(b.SenderID == userID)
                {
                    coinsSent += b.Amount;
                }
            }

            total = coinsReceived - coinsSent;

            return total;
        }

        public Block MakeHash(Block bl)
        {
            string signature = null;

            while (!bl.Hash.StartsWith("12345"))
            {
                bl.Offset++;

                signature = bl.ID.ToString() + bl.SenderID.ToString() + bl.RecepientID.ToString()
                              + bl.Amount.ToString() + bl.Offset.ToString() + bl.PrevHash;

                Debug.WriteLine("Concat " + signature);
                
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] txtBytes = System.Text.Encoding.ASCII.GetBytes(signature);
                    byte[] hash = sha256Hash.ComputeHash(txtBytes);

                    bl.Hash = BitConverter.ToUInt32(hash, 0).ToString();
                }
                

                Debug.WriteLine("hash " + bl.Hash);
            }

            return bl;
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
            string  signature = bl.ID.ToString() + bl.SenderID.ToString() + bl.RecepientID.ToString()
                            + bl.Amount.ToString() + bl.Offset.ToString() + bl.PrevHash;

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
    }
}