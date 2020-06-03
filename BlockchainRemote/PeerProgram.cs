using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainRemote
{
    public class PeerProgram
    {
        private ServiceHost host;
        public string CreateServer(string ip, string port)
        {
            bool foundAvailable = false;

            while (!foundAvailable)
            {
                try
                {
                    Console.WriteLine("Who is in my server?");


                    NetTcpBinding tcp = new NetTcpBinding();

                    host = new ServiceHost(typeof(PeerServer));

                    host.AddServiceEndpoint(typeof(RemoteInterface), tcp, "net.tcp://" + ip + ":" + port + "/DataService");

                    host.Open();
                    Console.WriteLine("System online");
                    foundAvailable = true;
                }
                catch (InvalidOperationException)
                {
                    port = (new Random().Next(6000, 9999)).ToString("D4");
                }
            }

            return port;

        }

        public void Close()
        {
            Console.WriteLine("Closing server"); ;

            host.Close();
        }
    }
}
