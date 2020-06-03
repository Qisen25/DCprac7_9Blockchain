using APIClass;
using P2P_WebService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace P2P_WebService.Controllers
{
    public class ClientController : ApiController
    {
        private P2PModel pool = P2PModel.GetInstance();

        // GET: api/Client
        [Route("api/Client/ip/{ip}/port/{port}/GetPeers")]
        [HttpGet]
        public List<ClientDataStruct> Get(string ip, string port)
        {
            return pool.GetPeers(ip, port);
        }

        // POST: api/Client/AddClient
        [Route("api/Client/AddClient")]
        [HttpPost]
        public void Post([FromBody]ClientDataStruct cl)
        {
            pool.AddClient(cl);
        }

        // DELETE: api/Client/RemoveClient
        [Route("api/Client/RemoveClient")]
        [HttpDelete]
        public void Delete([FromBody]ClientDataStruct cl)
        {
            pool.RemoveClient(cl);
        }
    }
}
