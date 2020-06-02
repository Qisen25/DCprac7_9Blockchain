using APIClass;
using Blockchain_WebService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Blockchain_WebService.Controllers
{
    public class BlockchainController : ApiController
    {
        private Blockchain blockchain = Blockchain.GetInstance();

        // GET: api/GetCurrentChain
        [Route("api/Bank/GetCurrentChain")]
        [HttpGet]
        public List<Block> GetCurrentChain()
        {
            lock (blockchain)
            {
                return blockchain.GetCurrentChain();
            }
        }

        [Route("api/Bank/GetLastBlock")]
        [HttpGet]
        public Block GetLastBlock()
        {
            lock (blockchain)
            {
                return blockchain.GetLastBlock();
            }
        }

        [Route("api/Bank/GetBalance/{id}")]
        [HttpGet]
        public float GetBalance(uint id)
        {
            lock (blockchain)
            {
                return blockchain.GetBalance(id);
            }
        }

        // POST: api/Blockchain
        [Route("api/Bank/SubmitBlock")]
        [HttpPost]
        public void Post([FromBody]Block value)
        {
            lock (blockchain)
            {
                blockchain.AddToChain(value);
            }
        }

        // PUT: api/Blockchain/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Blockchain/5
        public void Delete(int id)
        {
        }
    }
}
