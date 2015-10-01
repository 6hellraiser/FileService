using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Security.Cryptography;
using System.Configuration;
using System.ServiceModel.Configuration;
using System.Web.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Collections.Specialized;


namespace Backend
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Backend : IBackend
    {
        public static List<DiffieHellman> servers;
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        static GetFileDelegate _getFileDelegate;
        [DataMember]
        public DiffieHellman _server;
        [DataMember]
        public DiffieHellman _client;

        public static string path;
        public static string text;

        public Backend(GetFileDelegate gfd)
        {
            _getFileDelegate = gfd;
            StartService();
        }

        public static int counter = 0;

        public void ServerSend(string request)
        {
            if (counter == 0)
            {
                counter++;
                ClientSection clientSection = (ClientSection)WebConfigurationManager.GetSection("system.serviceModel/client");
                NetPeerTcpBinding binding = new NetPeerTcpBinding("Wimpy");

                string port = "";
                for (int i = 0; i < clientSection.Endpoints.Count; i++)
                {
                    string[] chars = new string[] { "//", "/", ":" };
                    string address = clientSection.Endpoints[i].Address.ToString();
                    string[] res = address.Split(chars, StringSplitOptions.None);
                    if (res.Length > 4)
                        port = res[3];
                    channels.Add(new ChannelFactory<IBackend>(binding, address).CreateChannel());
                }

                DiffieHellman.keys = new List<byte[]>();
                for (int i = 0; i < channels.Count; i++)
                {
                    DiffieHellman.keys.Add(new byte[8]);
                }
                for (int i = 0; i < channels.Count; i++)
                {
                    channels[i].ClientRespond(request, i, GetLocalIPAddress(), port);
                }
            }
            else counter = 0;
        }

        public void ClientRespond(string request, int channel, string addr, string port)
        {
            NameValueCollection settings =
            ConfigurationManager.GetSection("listenAddressGroup/listenAddress")
            as System.Collections.Specialized.NameValueCollection;

            string address = "";
            if (settings != null)
            {
                address = settings.AllKeys[0];
            }
            string[] res = address.Split(new string[] { ":" }, StringSplitOptions.None);
            NetPeerTcpBinding binding = new NetPeerTcpBinding("Wimpy");
            if ((port != "") && String.Compare(address, addr + ":" + port) == 0)
            {
                _channel = new ChannelFactory<IBackend>(binding, "net.p2p://" + addr + ":" + port + "/FileService").CreateChannel();
            }
            else if ((port == "") && (res.Length == 1) && String.Compare(address, addr) == 0)
            {
                _channel = new ChannelFactory<IBackend>(binding, "net.p2p://" + addr + "/FileService").CreateChannel();
            }
            _client = new DiffieHellman(64).GenerateResponse(request);
            _channel.ServerHandle(_client.ToString(), channel);
        }

        public void ServerHandle(string respond, int channel)
        {
            servers[0].HandleResponse(respond, channel);
            Send(path, text, channel);
        }

        public void Receive(CompositeType data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (_getFileDelegate != null)
            {
                SymmetricAlgorithm algorithm = DES.Create();
                algorithm.Key = _client.Key;
                algorithm.IV = _client.Key;
                ICryptoTransform transform = algorithm.CreateDecryptor();
                byte[] inputbuffer_p = Convert.FromBase64String(data.path);
                byte[] inputbuffer_t = Convert.FromBase64String(data.text);
                byte[] outputBuffer_p = transform.TransformFinalBlock(inputbuffer_p, 0, inputbuffer_p.Length);
                byte[] outputBuffer_t = transform.TransformFinalBlock(inputbuffer_t, 0, inputbuffer_t.Length);
                data.path = Encoding.Unicode.GetString(outputBuffer_p);
                data.text = Encoding.Unicode.GetString(outputBuffer_t);
                _getFileDelegate(data);
            }
        }

        private ServiceHost host = null;
        private ChannelFactory<IBackend> channelFactory = null;
        private IBackend _channel;
        static List<IBackend> channels;
        ServicesSection serviceSection;

        public void Send(string path, string text, int numberOfKey)
        {
            SymmetricAlgorithm algorithm = DES.Create();
            algorithm.Key = DiffieHellman.keys[numberOfKey];
            algorithm.IV = DiffieHellman.keys[numberOfKey];
            ICryptoTransform transform = algorithm.CreateEncryptor();
            byte[] inputbuffer_p = Encoding.Unicode.GetBytes(path);
            byte[] inputbuffer_t = Encoding.Unicode.GetBytes(text);
            byte[] outputBuffer_p = transform.TransformFinalBlock(inputbuffer_p, 0, inputbuffer_p.Length);
            byte[] outputBuffer_t = transform.TransformFinalBlock(inputbuffer_t, 0, inputbuffer_t.Length);
            channels[numberOfKey].Receive(new CompositeType(Convert.ToBase64String(outputBuffer_p), Convert.ToBase64String(outputBuffer_t)));
            servers.Clear();
            DiffieHellman.keys.Clear();
            channels.RemoveAt(numberOfKey);
        }

        private Backend() { }



        private void StartService()
        {
            serviceSection = (ServicesSection)WebConfigurationManager.GetSection("system.serviceModel/services");
            host = new ServiceHost(this);
            host.Open();

            channels = new List<IBackend>();
            servers = new List<DiffieHellman>();
        }

        public void StopService()
        {
            if (host != null)
            {
                servers.Clear();
                if (host.State != CommunicationState.Closed)
                {
                    channelFactory.Close();
                    host.Close();
                }
            }
        }
    }
}
