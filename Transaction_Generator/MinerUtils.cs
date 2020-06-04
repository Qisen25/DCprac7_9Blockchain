using APIClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Transaction_Generator
{
    /*
     * Class for storing miner utility such as hashing
     */
    public class MinerUtils
    {
        public Block GenerateHash(Block bl)
        {
            string signature = null;

            while (!bl.Hash.StartsWith("12345"))
            {
                bl.Offset++;

                signature = bl.ID.ToString() + bl.JsonStrList + bl.Offset.ToString() + bl.PrevHash;

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

        public bool ValidateTransaction(int sender, int receiver, float amount)
        {
            bool isValid = false;

            isValid = amount > 0.0F && sender > -1 && receiver > -1;

            return isValid;
        }
    }
}
