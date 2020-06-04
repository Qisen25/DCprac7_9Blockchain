using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace APIClass
{
    public class Block
    {
        public uint ID { get; set; }
        public string JsonStrList { get; set; }
        public uint Offset { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }

        public bool ValidateData()
        {
            bool isValid = (Hash.ToString().StartsWith("12345"));

            return isValid;
        }
    }
}