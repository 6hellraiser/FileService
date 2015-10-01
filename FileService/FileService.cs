using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FileService
{
    public partial class FileService : ServiceBase
    {
        string path = "C:\\Service";
        private Backend.Backend _backend;

        public FileService()
        {
            InitializeComponent();
            
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            if (e.FullPath != null)
            {
                System.Threading.Thread.Sleep(1000);
                string readFile = File.ReadAllText(e.FullPath);
                Backend.Backend.path = e.FullPath;
                Backend.Backend.text = readFile;
                _backend._server = new Backend.DiffieHellman(64).GenerateRequest();
                Backend.Backend.servers.Add(_backend._server);
                _backend.ServerSend(_backend._server.ToString());
            }
        }

        public void ReceiveFile(Backend.CompositeType data)
        {
            if (!File.Exists(data.path))
            {
                string[] res = data.path.Split(new string[] { "\\" }, StringSplitOptions.None);
                string local_path = path + "\\" + res.Last();
                File.WriteAllText(local_path, data.text);
            }

        }

        protected override void OnStart(string[] args)
        {
            _backend = new Backend.Backend(this.ReceiveFile);
            FileSystemWatcher watcher = new FileSystemWatcher(path, "*.txt");
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.EnableRaisingEvents = true;
        }

        protected override void OnStop()
        {
            _backend.StopService();
            CanStop = true;
            Stop();
        }
    }
}
