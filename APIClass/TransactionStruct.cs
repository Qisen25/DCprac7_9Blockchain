using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIClass
{
    public class TransactionStruct
    {
        public float amount { get; set; }
        public int sender { get; set; }
        public int receiver { get; set; }
    }
}
