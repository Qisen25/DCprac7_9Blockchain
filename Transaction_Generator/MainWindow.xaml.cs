using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public MainWindow()
        {
            InitializeComponent();
            bankClient = new RestClient(bankUrl);
            minerClient = new RestClient(minerUrl);
            existUsers = new Dictionary<int, uint>();
        }

        private void GetState_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            RestRequest req = new RestRequest("api/Bank/GetCurrentChain");
            IRestResponse resp = bankClient.Get(req);
            List<APIClass.Block> currChain = JsonConvert.DeserializeObject<List<APIClass.Block>>(resp.Content);

            NumBlocks.Text = "Number of Blocks: " + currChain.Count();
            ListUsers.Items.Clear();
            foreach(APIClass.Block b in currChain)
            {
                req = new RestRequest("api/Bank/GetBalance/" + b.RecepientID);
                resp = bankClient.Get(req);

                if (!existUsers.ContainsValue(b.RecepientID))
                {
                    existUsers.Add(count, b.RecepientID);
                }

                try
                {
                    if (!ListUsers.Items.IsEmpty && ListUsers.Items.GetItemAt(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key) != null)
                    {
                        ListUsers.Items.RemoveAt(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key);
                    }
                }
                catch(ArgumentOutOfRangeException)
                {
                    Debug.WriteLine("This is first time entry this is added");
                }
                ListUsers.Items.Insert(existUsers.FirstOrDefault(x => x.Value.Equals(b.RecepientID)).Key, b.RecepientID + " - Balance: " + resp.Content);

                count++;
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            RestRequest req = new RestRequest("api/Miner/SubmitTransaction");
            req.AddJsonBody(new APIClass.TransactionStruct()
            {
                amount = float.Parse(AmountText.Text),
                sender = Int32.Parse(SenderIDText.Text),
                receiver = Int32.Parse(ReceiverIDText.Text)
            });

            IRestResponse resp = minerClient.Post(req);
        }
    }
}
