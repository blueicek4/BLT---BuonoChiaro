//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.ServiceModel.Activation;
//using System.ServiceModel;
//using PMessageBox;
//using System.Reflection;
//using System.IO;

//namespace HostPlugin
//{
//    /// <summary>
//    /// Attivare Non-http Activation oltre a ASP.NET -> http://msdn.microsoft.com/en-us/library/ms731053.aspx
//    /// Nel sito in IIS, impostazionie avanzate, impostare nei protocollo abilitati http,net.tcp
//    /// URI tcp standard: net.tcp://indirizzo/NomeSito/Adapter.svc - esempio: net.tcp://localhost/HostPluginIIS/Adapter.svc
//    /// URI tcp sicuro: net.tcp://indirizzo/NomeSito/Adapter.svc/Sec - esempio: net.tcp://localhost/HostPluginIIS/Adapter.svc/Sec
//    /// URI http Web Service: http://indirizzo/NomeSito/Adapter.svc - esempio: http://localhost/HostPluginIIS/Adapter.svc
//    /// </summary>
//    public class HostFactory : ServiceHostFactoryBase
//    {
//        static ServiceHost mHost;

//        public override ServiceHostBase CreateServiceHost(
//          string constructorString, Uri[] baseAddresses)
//        {
//            //System.Threading.Thread.Sleep(20000);
//            if (mHost == null)
//            {
//                string percorso = AssemblyDirectory;

//                GatewayHostPlugin.AvviaHost(true, percorso);

//                Assembly amb = Assembly.LoadFile(percorso + "\\MessageBox.dll");
//                Type service = amb.GetType(constructorString);
//                mHost = new ServiceHost(service, baseAddresses);
//#if DEBUG
//                mHost.Opening += new EventHandler(host_Opening);
//                mHost.Closing += new EventHandler(host_Closing);
//#endif

//                ToolSviluppo.InitHostIIS(mHost);
//            }

//            return mHost;
//        }

//        static public string AssemblyDirectory 
//        { 
//            get 
//            { 
//                string codeBase = Assembly.GetExecutingAssembly().CodeBase; 
//                UriBuilder uri = new UriBuilder(codeBase); 
//                string path = Uri.UnescapeDataString(uri.Path); 
//                return Path.GetDirectoryName(path); 
//            } 
//        }

//        void host_Opening(object sender, EventArgs e)
//        {
//            MBLog.Scrivi(EnumTipoLog.Info, "Apertura canale IIS", String.Empty);
//        }

//        void host_Closing(object sender, EventArgs e)
//        {
//            MBLog.Scrivi(EnumTipoLog.Info, "Chiusura canale IIS", String.Empty);
//        }
//    }
//}
