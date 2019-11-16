using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Configuration;
using HostPlugin;
using Blt.BuonoChiaro.BOL;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Blt.BuonoChiaro.Con
{
    public class Program
    {
        public Configuration config;
        public Program()
        {
            string exeConfigPath = typeof(Program).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            Run();
        }

        public static void Run()
        {
            string ipaddress;
            string port;
            List<string> plugins;
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["isReplicaDatiAttivo"]))
            {

                log.InfoFormat(String.Format(@"Avvio Host in corso su porta: {0} con ID: {1} per Plugin: {2}", ConfigurationManager.AppSettings["PortaMessageBox"], ConfigurationManager.AppSettings["IDMessageBox"], ConfigurationManager.AppSettings["PluginAttivi"]));

                GatewayHostPlugin.AvviaHost(AppDomain.CurrentDomain.BaseDirectory, out ipaddress, out port, out plugins);
                log.InfoFormat(String.Format(@"Host Avviato!"));
            }

            string input = String.Empty;

            while (input.ToUpper() != "EXIT")
            {
                Console.WriteLine("");
                Console.WriteLine(@"Digita ""EXIT"" per interrompere");
                input = Console.ReadLine();
                switch (input.ToUpper())
                {
                    case "EXIT":
                        
                    break;
                    default:
                        break;
                }
            }
        }
    }
}

