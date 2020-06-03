using APIClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace P2P_WebService.Models
{
    public class P2PModel
    {
        public List<ClientDataStruct> clients;
        private static P2PModel instance = null;

        public static P2PModel GetInstance()
        {
            if (instance == null)
            {
                instance = new P2PModel();
            }

            return instance;
        }

        private P2PModel()
        {
            clients = new List<ClientDataStruct>();
        }

        public void AddClient(ClientDataStruct cl)
        {
            clients.Add(cl);
            ShuffleList();
        }

        public void RemoveClient(ClientDataStruct cl)
        {
            clients.Remove(clients.Find(x => x.ip == cl.ip && x.port == cl.port));
        }

        public List<ClientDataStruct> GetPeers(string ip, string port)
        {
            List<ClientDataStruct> result = new List<ClientDataStruct>(clients);
            result.Remove(clients.Find(x => x.ip == ip && x.port == port));

            return result;
        }

        /*
         * Algo to shuffle list to create fairness for peer connection
         * Reference: https://www.dotnetperls.com/fisher-yates-shuffle
         */
        private void ShuffleList()
        {
            Random rand = new Random();

            for (int i = clients.Count - 1; i > 0; i--)
            {
                int rnd = rand.Next(0, i);
                ClientDataStruct val = clients[rnd];
                clients[rnd] = clients[i];
                clients[i] = val;
            }
        }
    }
}