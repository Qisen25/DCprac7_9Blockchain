using APIClass;
using Miner_WebService.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;

namespace Miner_WebService.Controllers
{
    public class MinerController : ApiController
    {
        private string url = "https://localhost:44346/";
        private static Semaphore pool;
        private static Thread mineThread = null;
        private MinerUtils minUtils = new MinerUtils();
        private static Queue<TransactionStruct> transQueue = new Queue<TransactionStruct>();

        // GET: api/Miner/
        //[Route("api/Miner/GetCurrentChain")]
        //[HttpGet]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        // POST: api/Miner
        [Route("api/Miner/SubmitTransaction")]
        [HttpPost]
        public void Post([FromBody]TransactionStruct value)
        {
            transQueue.Enqueue(value);

            if (pool == null)
            {
                pool = new Semaphore(1, 1);
            }

            if (mineThread == null || !mineThread.IsAlive)
            {             
                mineThread = new Thread(DoMining);

                mineThread.Start();
            }
        }

        // PUT: api/Miner/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Miner/5
        public void Delete(int id)
        {
        }

        private void DoMining()
        {
            pool.WaitOne();
            RestClient client = new RestClient(url);

            while(transQueue.Count > 0)
            {
                TransactionStruct inTrx = transQueue.Dequeue();
                if (minUtils.ValidateTransaction(inTrx.sender, inTrx.receiver, inTrx.amount))
                {
                    RestRequest req = new RestRequest("api/Bank/GetBalance/" + inTrx.sender);                  
                    IRestResponse resp = client.Get(req);

                    float senderBalance = float.Parse(resp.Content);

                    if (senderBalance >= inTrx.amount)
                    {
                        //insert transaction details
                        Block newBlock = new Block();
                        newBlock.Amount = inTrx.amount;
                        newBlock.SenderID = Convert.ToUInt32(inTrx.sender);
                        newBlock.RecepientID = Convert.ToUInt32(inTrx.receiver);
                        newBlock.Hash = "";

                        //get last block
                        req = new RestRequest("api/Bank/GetLastBlock");
                        resp = client.Get(req);
                        Block lastBlock = JsonConvert.DeserializeObject<Block>(resp.Content);
                        newBlock.PrevHash = lastBlock.Hash;

                        newBlock = minUtils.GenerateHash(newBlock);//create valid hash and add it to new block

                        req = new RestRequest("api/Bank/SubmitBlock");
                        req.AddJsonBody(newBlock);

                        client.Post(req);//send off
                    }
                }
            }

            pool.Release();
        }
    }
}
