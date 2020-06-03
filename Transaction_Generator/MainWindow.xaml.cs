using APIClass;
using BlockchainRemote;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Transaction_Generator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RestClient bankClient;
        private RestClient minerClient;
        private string bankUrl = "https://localhost:44346/";
        private string minerUrl = "https://localhost:44360/";
        private Dictionary<int, uint> existUsers;

        private string url;
        private RestClient webPool;
        private ClientDataStruct currClient, encodeClient;
        List<ClientDataStruct> clientList;
        private PeerProgram ourServ;
        private RemoteInterface ourRemoteThread;
        private List<APIClass.Block> ourBlockchain;

        public MainWindow()
        {
            InitializeComponent();
            bankClient = new RestClient(bankUrl);
            minerClient = new RestClient(minerUrl);
            existUsers = new Dictionary<int, uint>();

            url = "https://localhost:44370/";
            webPool = new RestClient(url);

            //create current client connection, AKA this block is the server thread
            currClient = new ClientDataStruct();
            currClient.ip = "localhost";
            currClient.port = (new Random().Next(6000, 9999)).ToString("D4");//random generation of port number
            ourServ = new PeerProgram();
            currClient.port = ourServ.CreateServer(currClient.ip, currClient.port);
            ourRemoteThread = ConnectToRemote(currClient.ip, currClient.port);

            JoinPool();
            ourBlockchain = ourRemoteThread.GetCurrentChain();

            ourAddrText.Text = "Your Client Address is " + currClient.ip + ":" + currClient.port;
            MinerThread();
        }

        private void JoinPool()//join pool via web service
        {
            RestRequest req = new RestRequest("api/Client/AddClient");
            encodeClient = new ClientDataStruct();
            encodeClient.ip = EncodeTo64(currClient.ip);
            encodeClient.port = EncodeTo64(currClient.port);
            req.AddJsonBody(encodeClient);
            webPool.Post(req);
        }

        /*
         * Mining Thread
         */
        private async void MinerThread()
        {
            MinerUtils minUtils = new MinerUtils();
            await Task.Run(() =>
            {
                while (true)
                {
                    DoMining(minUtils);
                    SynchroniseChain();
                }
            });
        }

        private void DoMining(MinerUtils minUtils)
        {
            TransactionStruct newTrx = ourRemoteThread.ReceiveTransaction(null);

            if (newTrx != null)
            {
                if (minUtils.ValidateTransaction(newTrx.sender, newTrx.receiver, newTrx.amount))
                {
                    float senderBalance = ourRemoteThread.GetWalletBalance(Convert.ToUInt32(newTrx.sender));

                    if (senderBalance >= newTrx.amount)
                    {
                        //insert transaction details
                        APIClass.Block newBlock = new APIClass.Block();
                        newBlock.ID = ourBlockchain.Last().ID + 1;
                        newBlock.Amount = newTrx.amount;
                        newBlock.SenderID = Convert.ToUInt32(newTrx.sender);
                        newBlock.RecepientID = Convert.ToUInt32(newTrx.receiver);
                        newBlock.Hash = "";

                        newBlock.PrevHash = ourRemoteThread.GetLatestBlock().Hash;

                        newBlock = minUtils.GenerateHash(newBlock);//create valid hash and add it to new block

                        ourRemoteThread.AddBlock(newBlock);
                        ourBlockchain = ourRemoteThread.GetCurrentChain();
                    }
                }
            }
        }

        /*
         * Algo for synchronising chain with peers most popular chain
         * References: https://www.dotnetperls.com/common-elements-list
         */
        private void SynchroniseChain()
        {
            RestRequest req = new RestRequest("api/Client/ip/" + encodeClient.ip + "/port/" + encodeClient.port + "/GetPeers");
            IRestResponse resp = webPool.Get(req);
            clientList = JsonConvert.DeserializeObject<List<ClientDataStruct>>(resp.Content);

            Dictionary<string, int> hashCount = new Dictionary<string, int>();
            RemoteInterface ri = null;

            foreach (ClientDataStruct c in clientList)
            {
                //Console.WriteLine(c.ip + " " + c.port);
                ri = ConnectToRemote(DecodeFrom64(c.ip), DecodeFrom64(c.port));

                string daHash = ri.GetLatestBlock().Hash;

                if(hashCount.TryGetValue(daHash, out int count))
                {
                    hashCount[daHash] = count + 1;
                }
                else //if hash not found yet add it to hashCount
                {
                    if (!daHash.Equals(ourBlockchain.First().Hash))
                    {
                        hashCount.Add(daHash, 1);
                    }
                }
            }

            
            //if our latest block hash is not the same as most popular then download the most popular chain
            if (hashCount.Count > 0)
            {
                string maxHash = hashCount.OrderBy(x => x.Value).Last().Key;//find hash with highest population count
                List<APIClass.Block> popularChain = null;
                bool found = false;
                //if max hash is not found 
                if (!ourRemoteThread.GetLatestBlock().Hash.Equals(maxHash))
                {
                    foreach (ClientDataStruct c in clientList)
                    {
                        ri = ConnectToRemote(DecodeFrom64(c.ip), DecodeFrom64(c.port));
                        popularChain = ri.GetCurrentChain();

                        if (!ri.GetLatestBlock().Hash.Equals(maxHash))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        ourRemoteThread.SetChain(popularChain);
                        //ourBlockchain = ourRemoteThread.GetCurrentChain();
                    }
                }
            }
        }

        private RemoteInterface ConnectToRemote(string ip, string port)//connect to remote server
        {
            ChannelFactory<RemoteInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();

            string URL = "net.tcp://" + ip + ":" + port + "/DataService";
            foobFactory = new ChannelFactory<RemoteInterface>(tcp, URL);
            RemoteInterface foob = foobFactory.CreateChannel();

            return foob;
        }

        private void GetState_Click(object sender, RoutedEventArgs e)
        {
            //int count = 0;
            //RestRequest req = new RestRequest("api/Bank/GetCurrentChain");
            //IRestResponse resp = bankClient.Get(req);
            //List<APIClass.Block> currChain = JsonConvert.DeserializeObject<List<APIClass.Block>>(resp.Content);

            //NumBlocks.Text = "Number of Blocks: " + currChain.Count();
            //ListUsers.Items.Clear();
            //foreach(APIClass.Block b in currChain)
            //{
            //    req = new RestRequest("api/Bank/GetBalance/" + b.RecepientID);
            //    resp = bankClient.Get(req);

            //    if (!existUsers.ContainsValue(b.RecepientID))
            //    {
            //        existUsers.Add(count, b.RecepientID);
            //    }

            //    try
            //    {
            //        if (!ListUsers.Items.IsEmpty && ListUsers.Items.GetItemAt(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key) != null)
            //        {
            //            ListUsers.Items.RemoveAt(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key);
            //        }
            //    }
            //    catch(ArgumentOutOfRangeException)
            //    {
            //        Debug.WriteLine("This is first time entry this is added");
            //    }
            //    ListUsers.Items.Insert(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key, b.RecepientID + " - Balance: " + resp.Content);

            //    count++;
            //}

            NumBlocks.Text = "Number of Blocks: " + ourBlockchain.Count();

            Dictionary<uint, float> wallets = new Dictionary<uint, float>();
            ListUsers.Items.Clear();
            foreach (APIClass.Block b in ourBlockchain)
            {
                if (wallets.TryGetValue(b.RecepientID, out _))
                {
                    wallets[b.RecepientID] = ourRemoteThread.GetWalletBalance(b.RecepientID);
                }
                else
                {
                    wallets.Add(b.RecepientID, ourRemoteThread.GetWalletBalance(b.RecepientID));
                }
            }

            foreach(KeyValuePair<uint, float> entry in wallets)
            {
                ListUsers.Items.Add("Wallet-" + entry.Key + " Balance: " + entry.Value);
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            //RestRequest req = new RestRequest("api/Miner/SubmitTransaction");
            //req.AddJsonBody(new APIClass.TransactionStruct()
            //{
            //    amount = float.Parse(AmountText.Text),
            //    sender = Int32.Parse(SenderIDText.Text),
            //    receiver = Int32.Parse(ReceiverIDText.Text)
            //});

            //IRestResponse resp = minerClient.Post(req);

            RestRequest req = new RestRequest("api/Client/ip/" + encodeClient.ip + "/port/" + encodeClient.port + "/GetPeers");
            IRestResponse resp = webPool.Get(req);
            clientList = JsonConvert.DeserializeObject<List<ClientDataStruct>>(resp.Content);

            TransactionStruct inTrx = new TransactionStruct()
            {
                amount = int.Parse(AmountText.Text),
                sender = int.Parse(SenderIDText.Text),
                receiver = int.Parse(ReceiverIDText.Text)
            };

            ourRemoteThread.ReceiveTransaction(inTrx);

            foreach (ClientDataStruct c in clientList)
            {
                Console.WriteLine(c.ip + " " + c.port);
                RemoteInterface ri = ConnectToRemote(DecodeFrom64(c.ip), DecodeFrom64(c.port));

                ri.ReceiveTransaction(inTrx) ;//broadcast to each client in pool
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if(clientList != null || !(clientList.Count > 0))
            {
                PeerList.Items.Clear();

                foreach(ClientDataStruct c in clientList)
                {
                    PeerList.Items.Add("IP: " + DecodeFrom64(c.ip) + " Port: " + DecodeFrom64(c.port));
                }
            }
        }

        /*
         * Encoding functions
         */

        private string EncodeTo64(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            byte[] txtBytes = System.Text.Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(txtBytes);
        }

        private string DecodeFrom64(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            byte[] encodeBytes = Convert.FromBase64String(str);
            return System.Text.Encoding.UTF8.GetString(encodeBytes);
        }

        private byte[] MakeHash(string data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                return sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            }
        }
    }
}
