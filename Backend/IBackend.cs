using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace Backend
{
    [ServiceContract]
    public interface IBackend
    {
        [OperationContract(IsOneWay = true)]
        void Send(string path, string text, int numberOfKey);
        [OperationContract(IsOneWay = true)]
        void Receive(CompositeType data);
        [OperationContract(IsOneWay = true)]
        void ServerSend(string request);
        [OperationContract(IsOneWay = true)]
        void ClientRespond(string request, int channel, string addr, string port);
        [OperationContract(IsOneWay = true)]
        void ServerHandle(string respond, int channel);
    }

    [DataContract]
    public class CompositeType
    {
        [DataMember]
        public string path;
        [DataMember]
        public string text;

        public CompositeType(string m, string n)
        {
            path = m;
            text = n;
        }
    }

    public delegate void GetFileDelegate(CompositeType path);
}
