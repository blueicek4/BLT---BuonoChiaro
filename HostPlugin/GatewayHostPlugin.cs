using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PMessageBox;
using PMessageBox.Kernel;
using PMessageBox.Contract;

using PMessageBox.Contract.Cliente;
using PMessageBox.Contract.Fornitore;
using PMessageBox.Contract.Articolo;
using PMessageBox.Contract.Magazzino;
using PMessageBox.Contract.Appuntamento;
using PMessageBox.Contract.Comanda;
using PMessageBox.Contract.Conto;
using PMessageBox.Contract.PrenotazioneMenu;
using PMessageBox.Contract.Chiusure;
using PMessageBox.Contract.PrenotazioneWelcome;
using PMessageBox.Contract.Common;
using System.IO;
using System.Reflection;
using PMessageBox.Contract.Configuration;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]


namespace HostPlugin
{
    public class GatewayHostPlugin : Gateway
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static List<Plugin> mPluginAttivi;

        /// <summary>
        /// ID Gestionale Ho.Re.Ca. collegato
        /// </summary>
        public static string IDGestionale { get; private set; }

        public static GatewayHostPlugin Istanza
        {
            get
            {
                return ToolSviluppo.GatewayAttivo as GatewayHostPlugin;
            }
        }

        public static void AvviaHost(string cartellaInstallazione, out string _ipaddress, out string _port, out List<string> _plugins)
        {
            AvviaHost(false, cartellaInstallazione, out _ipaddress, out _port, out _plugins);
        }

        internal static void AvviaHost(bool IIS, string cartellaInstallazione, out string _ipaddress, out string _port, out List<string> _plugins)
        {
            string[] canaliAbilitati = new string[]
            {
                ContrattoArticolo.NomeContratto,
                ContrattoCliente.NomeContratto,
                ContrattoFornitore.NomeContratto,
                ContrattoDocumentoMagazzino.NomeContratto,
                ContrattoAppuntamento.NomeContratto,
                ContrattoDisponibilitaRisorsa.NomeContratto,
                ContrattoComanda.NomeContratto,
                ContrattoPrenotazioneMenu.NomeContratto,
                ContrattoConto.NomeContratto,
                ContrattoCamera.NomeContratto,
                ContrattoPrenotazioneWelcome.NomeContratto,
                ContrattoTessera.NomeContratto,
                ContrattoChiusuraGiornaliera.NomeContratto,
                ContrattoAddebitoWelcome.NomeContratto,
            };

            AppSettingsReader reader = new AppSettingsReader();
            string idMB = (string)reader.GetValue("IDMessageBox", typeof(string));
            //IDGestionale = (string)reader.GetValue("IDGestionale", typeof(string));
            string porta;
            if (!IIS)
                porta = (string)reader.GetValue("PortaMessageBox", typeof(string));
            else
                porta = "80";

            _port = porta;
            string stringaConnessioneDB = (string)reader.GetValue("ConnessioneDB", typeof(string));
            string pluginConfigurati = (string)reader.GetValue("PluginAttivi", typeof(string));
            if (String.IsNullOrEmpty(idMB))
                throw new Exception("ID Messagebox non valido");
            if (String.IsNullOrEmpty(porta))
                throw new Exception("Porta Messagebox non valida");
            if (String.IsNullOrEmpty(stringaConnessioneDB))
                throw new Exception("Connessione DB non valida");

            log.InfoFormat("AvviaHost - Avvio Host Messagebox in corso con i seguenti parametri:\nID: {0}\nPorta: {1}\nPlugins: {2}\nConnessioneDB: {3}", idMB, porta, pluginConfigurati, stringaConnessioneDB);
           ToolSviluppo.Avvia(idMB, porta, typeof(GatewayHostPlugin), stringaConnessioneDB, canaliAbilitati, IIS, cartellaInstallazione);

            CaricaPlugin(cartellaInstallazione, pluginConfigurati);

            _plugins = mPluginAttivi.Select(p => p.Nome).ToList();
            if (mPluginAttivi.Count==0)
                log.WarnFormat("AvviaHost - Avvio Host Messagebox completato! - Nessun plugin attivo", "HostPlugin attenzione: non è stato attivato nessun plugin nel file di configurazione");
            else
                log.InfoFormat("AvviaHost - Avvio Host Messagebox completato! - Caricati i seguenti Plugins: {0}", String.Join(" - " ,mPluginAttivi.Select(p => p.Nome).ToArray()));

            _ipaddress = "";
        }

        private static void CaricaPlugin(string cartellaInstallazione, string pluginConfigurati)
        {
            mPluginAttivi = new List<Plugin>();
            string[] files = Directory.GetFiles(cartellaInstallazione);
            foreach (string file in files)
            {
                if (file.EndsWith("dll", StringComparison.OrdinalIgnoreCase) || file.EndsWith("exe", StringComparison.OrdinalIgnoreCase))
                {
                    Type[] tipi=null;
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(file);
                        tipi = assembly.GetTypes();
                    }
                    catch
                    {
                        MBLog.Scrivi(EnumTipoLog.Trace1, "Errore caricamento file", "Errore caricamento file:" + file);
                    }
                    if (tipi != null)
                    {
                        foreach (Type tipo in tipi)
                        {
                            if (tipo.IsClass && tipo.GetInterface("IPlugin") != null)
                            {
                                object[] attrPlugin = tipo.GetCustomAttributes(typeof(Plugin), true);
                                if (attrPlugin != null && attrPlugin.Length == 1)
                                {
                                    Plugin plugin = (Plugin)attrPlugin[0];
                                    if (String.IsNullOrEmpty(plugin.Nome))
                                        plugin.Nome = tipo.Name;
                                    if (pluginConfigurati.IndexOf(plugin.Nome) > -1)
                                        plugin.Attivo = true;
                                    ConstructorInfo cinfo = tipo.GetConstructor(new Type[0]);
                                    if (cinfo == null)
                                        throw new Exception("Plugin " + plugin.Nome + " non ha costruttore senza parametri");
                                    plugin.Istanza = (IPlugin)cinfo.Invoke(new object[0]);
                                    object[] attrCanali = tipo.GetCustomAttributes(typeof(CanalePlugin), true);
                                    if (attrPlugin != null && attrPlugin.Length > 0)
                                    {
                                        foreach (CanalePlugin canale in attrCanali)
                                            plugin.CanaliAbilitati.Add(canale);
                                    }
                                    if (System.Configuration.ConfigurationManager.AppSettings["PluginAttivi"].Split(',').ToList().Any(p => p == plugin.Nome))
                                    {
                                        mPluginAttivi.Add(plugin);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public GatewayHostPlugin()
            : base()
        {
        }

        //protected override void Inizializzazione()
        //{
        //    base.Inizializzazione();
        //}

        protected override void Login(Messaggio messaggio)
        {
            //fa entrare tutti
            //messaggio.InfoLogin = "miainfo";
        }

        protected override Messaggio RiceviMessaggio(Messaggio messaggio)
        {
            log.DebugFormat("RiceviMessaggio - Inizio - Ricevuto nuovo messaggio - Tipo Canale: {0}", messaggio.Canale.Nome);
            if (messaggio.Canale == null)
            {
                return null;//non considero messaggi vuoti
            }
            else if (messaggio.Canale.Nome == CostantiMB.NomeCanaleSistema
                || messaggio.Canale.Nome == ContrattoConfigurazioneMB.NomeContratto)
            {
                return base.RiceviMessaggio(messaggio);
            }
            else
            {
                //List<Contratto> cntRisposta = new List<Contratto>();
                 Messaggio risposta = null;
                foreach (Plugin plugin in mPluginAttivi)
                {
                    bool daSmistare = false;
                    foreach (CanalePlugin canalePlugin in plugin.CanaliAbilitati)
                    {
                        if (messaggio.Canale.Nome == canalePlugin.NomeCanale)
                        {
                            switch (messaggio.ComandoEnum)
                            {
                                case EnumComando.RichiestaDati:
                                    if (canalePlugin.AbilitaRichiestaDati)
                                        daSmistare = true;
                                    break;
                                case EnumComando.InserimentoDati:
                                    if (canalePlugin.AbilitaInserimentoDati)
                                        daSmistare = true;
                                    break;
                                case EnumComando.CancellazioneDati:
                                    if (canalePlugin.AbilitaCancellazioneDati)
                                        daSmistare = true;
                                    break;
                                case EnumComando.ComandoImmediato:
                                    if (canalePlugin.AbilitaComandoImmediato)
                                        daSmistare = true;
                                    break;
                                default:
                                    break;
                            }
                            if (daSmistare)
                            {
                                //Contratto[] risp=plugin.Istanza.RiceviMessaggio(messaggio);
                                //if (risp != null && risp.Length > 0)
                                //    cntRisposta.AddRange(risp);
                                risposta = plugin.Istanza.RiceviMessaggio(messaggio);
                                break;
                            }
                        }
                    }
                }
                return risposta;
                //if (cntRisposta.Count > 0)
                //{
                //    Messaggio risposta = new Messaggio(null);
                //    foreach (Contratto cnt in cntRisposta)
                //        risposta.Body.Add(cnt);
                //    return risposta;
                //}
                //else
                //    return null;
            }
        }

        //protected override void CompletaContratto(Messaggio messaggio, Contratto contratto)
        //{
        //    base.CompletaContratto(messaggio, contratto);
        //}

    }

}
