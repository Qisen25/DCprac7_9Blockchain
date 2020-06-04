using APIClass;
using BlockchainRemote;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
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
using System.Web.UI.WebControls;
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
        //private RestClient bankClient;
        //private RestClient minerClient;
        //private string bankUrl = "https://localhost:44346/";
        //private string minerUrl = "https://localhost:44360/";
        //private Dictionary<int, uint> existUsers;

        private string url;
        private RestClient webPool;
        private ClientDataStruct currClient, encodeClient;
        List<ClientDataStruct> clientList;
        private PeerProgram ourServ;
        private RemoteInterface ourRemoteThread;
        private List<APIClass.Block> ourBlockchain;
        private List<string[]> sendJobs, currJobsLogged;
        private List<string[]> clientJobResults;

        public MainWindow()
        {
            InitializeComponent();
            //bankClient = new RestClient(bankUrl);
            //minerClient = new RestClient(minerUrl);
            //existUsers = new Dictionary<int, uint>();
            sendJobs = new List<string[]>();
            clientJobResults = new List<string[]>();
            currJobsLogged = new List<string[]>();

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
                    //always update client list
                    RestRequest req = new RestRequest("api/Client/ip/" + encodeClient.ip + "/port/" + encodeClient.port + "/GetPeers");
                    IRestResponse resp = webPool.Get(req);
                    this.clientList = JsonConvert.DeserializeObject<List<ClientDataStruct>>(resp.Content);

                    //perform operations
                    BroadcastTransaction();
                    DoMining(minUtils);
                    SynchroniseChain();
                    GetResults();
                }
            });
        }

        /*
         * whenever there are 5 jobs, broadcast this to all nearby peers
         */
        private void BroadcastTransaction()
        {
            if (sendJobs.Count == 5)
            {
                string sendSnippet = JsonConvert.SerializeObject(sendJobs);

                ourRemoteThread.ReceiveTransaction(sendSnippet);

                foreach (ClientDataStruct c in clientList)
                {
                    Console.WriteLine(c.ip + " " + c.port);
                    RemoteInterface ri = ConnectToRemote(DecodeFrom64(c.ip), DecodeFrom64(c.port));

                    ri.ReceiveTransaction(sendSnippet);//broadcast to each client in pool
                }

                sendJobs.Clear();//clear list once transactions sent off
            }
        }

        /*
         * creating blocks
         */
        private void DoMining(MinerUtils minUtils)
        {
            string recvTransaction = ourRemoteThread.ReceiveTransaction(null);

            if (!string.IsNullOrEmpty(recvTransaction))
            {
                    //insert transaction details
                    APIClass.Block newBlock = new APIClass.Block();
                    newBlock.ID = ourBlockchain.Last().ID + 1;
                    newBlock.JsonStrList = DoPython(recvTransaction);
                    newBlock.Hash = "";

                    newBlock.PrevHash = ourRemoteThread.GetLatestBlock().Hash;

                    newBlock = minUtils.GenerateHash(newBlock);//create valid hash and add it to new block

                    ourRemoteThread.AddBlock(newBlock);
                    ourBlockchain = ourRemoteThread.GetCurrentChain();
            }
        }

        /*
         * execute python code
         */
        private string DoPython(string pythonJobs)
        {
            string finalList = null, funcName, varName;
            List<string[]> codeJobs = JsonConvert.DeserializeObject<List<string[]>>(pythonJobs);
            codeJobs = codeJobs.OrderBy(x => x[0]).ToList();//alphabetical sort

            foreach (string[] jb in codeJobs)
            {
                try
                {
                    string code = DecodeFrom64(jb[0]);
                    ScriptEngine engine = Python.CreateEngine();
                    ScriptScope scope = engine.CreateScope();
                    //var res = engine.Execute("def poo():\r\n return 1", scope);
                    var res = engine.Execute(code, scope);//remember to decode before executing python

                    if (IsAVar(code, out funcName, out varName))
                    {
                        dynamic func = scope.GetVariable(varName);
                        jb[1] = EncodeTo64(func.ToString());
                    }
                    else
                    {
                        dynamic func = scope.GetVariable(funcName);
                        var result = func();
                        jb[1] = EncodeTo64(result.ToString());
                    }
                    ;//encode result/response to send back to remote server
                }
                catch (Exception e)
                {
                    jb[1] = "Fail: " + e.Message;//if code is run unsuccessfully just log error in answer index of string array
                }
            }

            finalList = JsonConvert.SerializeObject(codeJobs);
            return finalList;
        }

        /*
         * checks if python code only has a function or a variable that is assigned a return value of a func created by user
         */
        private bool IsAVar(string code, out string fName, out string vName)
        {
            string funcName, varName = null;

            funcName = code.Substring(3, code.IndexOf("(") - 3).Trim();//get function name excluding def and open bracket
            if (code.Contains("return"))
            {
                string variableName = code.Substring(code.IndexOf("return"));
                if (variableName.Contains("="))//get variable name if a function call exists after return such as foo = someFunc(), below will get foo variable name
                {
                    variableName = code.Substring(code.IndexOf("return"), code.IndexOf("=") - code.IndexOf("return"));
                    string[] split = variableName.Split(new[] { ' ', '=', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    variableName = split[split.Length - 1];
                    varName = variableName;
                }
            }

            if (!string.IsNullOrEmpty(varName))//if code assigns a variable by a function call like abc = func(p, q) then the variable will have result
            {
                vName = varName;
                fName = funcName;
                return true;
            }
            else//otherwise just return from function
            {
                vName = varName;
                fName = funcName;
                return false;
            }
        }

        /*
         * Algo for synchronising chain with peers most popular chain
         * References: https://www.dotnetperls.com/common-elements-list
         */
        private void SynchroniseChain()
        {
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
                APIClass.Block currBlock = ourRemoteThread.GetLatestBlock();
                List<APIClass.Block> popularChain = null;
                bool found = false;
                //if latest block doesn't have the most popular hash and current block prev hash does not match popular hash then download popular chain
                if (!currBlock.Hash.Equals(maxHash) && !currBlock.PrevHash.Equals(maxHash))
                {
                    foreach (ClientDataStruct c in clientList)
                    {
                        ri = ConnectToRemote(DecodeFrom64(c.ip), DecodeFrom64(c.port));
                        popularChain = ri.GetCurrentChain();

                        if (ri.GetLatestBlock().Hash.Equals(maxHash))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        ourRemoteThread.SetChain(popularChain);
                        ourBlockchain = ourRemoteThread.GetCurrentChain();
                    }
                }
            }
        }

        /*
         * retrieve results for current client posted jobs
         */
        private void GetResults()
        {
            if (currJobsLogged.Count >= 5)
            {
                bool found = false;
                //send recent 5 jobs to access blockchain to get related answers and return back list with answers
                List<string[]> temp = ourRemoteThread.GetAnswers(currJobsLogged.OrderBy(x => x[0]).ToList(), out found);
                if (found)
                {
                    clientJobResults.AddRange(temp);
                    for (int i = 0; i < 5; i++)
                    {
                        currJobsLogged.RemoveAt(0);//remove the recent jobs once answers are successfuly retrieved
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
            NumBlocks.Text = "Number of Blocks: " + ourBlockchain.Count();
            YourNumJobs.Text = "Your amount of jobs: " + clientJobResults.Count;
            ListUsers.Items.Clear();

            int index = 1;
            foreach (string[] arr in clientJobResults)
            {
                ListUsers.Items.Add("Code " + index  + "\n" + DecodeFrom64(arr[0]) + "\nResult: " + DecodeFrom64(arr[1]));
                index++;
            }

            NumTaskLoaded.Text = currJobsLogged.Count + " tasks loaded in queue";
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
            if (currJobsLogged.Count < 5)//can only send 5 transactions at a time
            {
                if (!string.IsNullOrEmpty(PythonCodeText.Text))
                {
                    string[] inJob = new string[2];

                    inJob[0] = EncodeTo64(PythonCodeText.Text);

                    sendJobs.Add(inJob);
                    currJobsLogged.Add(inJob);

                    NumTaskLoaded.Text = currJobsLogged.Count + " tasks loaded in queue";
                }
                else
                {
                    MessageBox.Show("Please enter code");
                }
            }
            else
            {
                MessageBox.Show("Queue is full");
                NumTaskLoaded.Text = "Click get state to get status";
            }
        }

        /*
         * Refresh peer list
         */
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
         * Remove this client from peer pool when closing window
         */
        private void Window_Closed(object sender, EventArgs e)
        {
            RestRequest req = new RestRequest("api/Client/RemoveClient");
            req.AddJsonBody(encodeClient);

            webPool.Delete(req);
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
