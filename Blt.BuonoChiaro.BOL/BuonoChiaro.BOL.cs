using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using PMessageBox.Contract.Conto;
using System.Xml.Linq;

namespace Blt.BuonoChiaro.BOL
{
    #region PARENT
    [XmlRoot("CRQ")]
    public class ParentReq
    {
        [XmlElement("IP")]
        public String IndirizzoIp { get; set; }
        [XmlElement("PORT")]
        public string Port { get; set; }
        [XmlElement("TIMEOUT")]
        public string Timeout { get; set; }
        [XmlElement("IDAWS")]
        public string CodiceIdentificativo { get; set; }
        [XmlElement("CODSCNT")]
        public string NumeroScontrino { get; set; }
        [XmlElement("CODCORP")]
        public string CODCORP { get; set; }
        [XmlElement("CODDEV")]
        public string CODDEV { get; set; }
        [XmlElement("CODPIC")]
        public string CODPIC { get; set; }
        [XmlElement("CODOPER")]
        public enumCodOper TipoOperazione { get; set; }
        [XmlElement("CODTR")]
        public string CodiceTransazione { get; set; }
        [XmlElement("NUMOP")]
        public string Progressivo { get; set; }

        Configuration config;
        public ParentReq()
        {
            string exeConfigPath = typeof(ParentReq).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }

        public void Init(ContrattoConto conto)
        {
            this.IndirizzoIp = config.AppSettings.Settings["IP"].Value;
            this.Port = config.AppSettings.Settings["PORT"].Value;
            this.Timeout = config.AppSettings.Settings["TIMEOUT"].Value;
            this.CodiceIdentificativo = config.AppSettings.Settings["IDAWS"].Value;
            this.CODCORP = config.AppSettings.Settings["CODCORP"].Value;
            this.CodiceTransazione = config.AppSettings.Settings["CODTR"].Value;

            this.CODDEV = (10000 + Convert.ToInt32(config.AppSettings.Settings["CODDEV"].Value.PadLeft(5, '0'))).ToString() + conto.PuntoCassa.PadLeft(5, '0');
            this.CODPIC = conto.CassiereLogin;
            //this.NumeroScontrino = conto.NumeroScontrinoFiscale;
            this.NumeroScontrino = conto.IdGestionale.Value.ToString();
            this.Progressivo = conto.IdGestionale.Value.ToString();
        }

        public string ToXML()
        {
            XmlSerializer xs = null;

            //These are the objects that will free us from extraneous markup.
            XmlWriterSettings settings = null;
            XmlSerializerNamespaces ns = null;

            //We use a XmlWriter instead of a StringWriter.
            XmlWriter xw = null;

            String outString = String.Empty;

            try
            {
                //To get rid of the xml declaration we create an 
                //XmlWriterSettings object and tell it to OmitXmlDeclaration.
                settings = new XmlWriterSettings();
                settings.OmitXmlDeclaration = true;

                //To get rid of the default namespaces we create a new
                //set of namespaces with one empty entry.
                ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                StringBuilder sb = new StringBuilder();

                xs = new XmlSerializer(this.GetType());

                //We create a new XmlWriter with the previously created settings 
                //(to OmitXmlDeclaration).
                xw = XmlWriter.Create(sb, settings);

                //We call xs.Serialize and pass in our custom 
                //XmlSerializerNamespaces object.
                xs.Serialize(xw, this, ns);

                xw.Flush();

                outString = sb.ToString() + Environment.NewLine;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (xw != null)
                {
                    xw.Close();
                }
            }
            return outString;
        }

    }
    [XmlRoot("CRQ")]

    public class ParentRes
    {
        public string COMRES { get; set; }
        [XmlIgnore]
        public enumCOMRES RISULTATO { get { if (this.COMRES == "OK") return enumCOMRES.OK; else return enumCOMRES.KO;  }}
        public string CODSCNT { get; set; }
        public string CODCORP { get; set; }
        public string CODDEV { get; set; }
        public string IDTR { get; set; }
        public string ERRMSG { get; set; }
        //[XmlArray("LIST")]
        //[XmlArrayItem("ELEM")]
        //public List<RowParentResponse> Rows { get; set; }

        public ParentRes()
        {
//            Rows = new List<RowParentResponse>();
        }
        public string ToString()
        {
            return this.RISULTATO.ToString();
        }

    }
    public class RowParentResponse
    {
        public string BC { get; set; }
        public string OPRES { get; set; }
        public string NUMOP { get; set; }
        public RowParentResponse() { }
    }
    #endregion

    #region OPEN
    [XmlRoot("CRQ")]
    public class TicketOpenReq : ParentReq
    {
        public TicketOpenReq()
        {
            this.TipoOperazione = enumCodOper.APERTURASCONTRINO;
        }
    }
    [XmlRoot("CRQ")]
    public class TicketOpenRes : ParentRes
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowParentResponse> Rows { get; set; }
        public TicketOpenRes()
        {
            Rows = new List<RowParentResponse>();
        }

    }

    #endregion

    #region VALIDATION
    [XmlRoot("CRQ")]
    public class TicketValidationReq : ParentReq
    {

        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowValidationRequest> Rows { get; set; }
        public TicketValidationReq()
        {
            this.TipoOperazione = enumCodOper.VALIDAZIONE;
            Rows = new List<RowValidationRequest>();
        }
        public void CaricaBuono(string buono)
        {
            this.Rows.Add(new RowValidationRequest() { Line = buono });
        }

    }

    public class RowValidationRequest
    {
        [XmlElement("BC")]
        public string Line { get; set; }
    }
    [XmlRoot("CRQ")]
    public class TicketValidationRes : ParentRes
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowValidationResponse> Rows { get; set; }

        public TicketValidationRes()
        {
            Rows = new List<RowValidationResponse>();
        }

    }
    public class RowValidationResponse
    {
        public string BC { get; set; }
        public string OPRES { get; set; }
        public string NUMOP { get; set; }
        public string EM { get; set; }
        public string DP { get; set; }
        public string VB { get; set; }
        public string TB { get; set; }
        public string DTSCAD { get; set; }
        public string DTSCADEFF { get; set; }
        public string PIVA { get; set; }
        public string PSCRP { get; set; }
        public string VLRIM { get; set; }
        public string PARAGG { get; set; }
        public string PARMSG { get; set; }
        public string SBTYPECOD { get; set; }
        public string MVBACK { get; set; }
        public string CIG { get; set; }
        public string CIGD { get; set; }
        public string COMPREFUND { get; set; }
        public enumCompRefund COMPANY { get
            {
                if (String.IsNullOrEmpty(this.COMPREFUND))
                    return enumCompRefund.DEFAULT;
                else
                    return (enumCompRefund)Enum.Parse(typeof(enumCompRefund), Convert.ToInt32(this.COMPREFUND).ToString()); 
            }
        }
        public string ISSUERCOMPANY { get; set; }

        public RowValidationResponse() { }
    }
    #endregion

    #region VOID
    [XmlRoot("CRQ")]
    public class TicketVoidReq : ParentReq
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowVoidRequest> Rows { get; set; }

        public TicketVoidReq()
        {
            this.TipoOperazione =enumCodOper.STORNO;
            Rows = new List<RowVoidRequest>();
        }
        public void CaricaBuono(string buono)
        {
            this.Rows.Add(new RowVoidRequest() { Line = buono });
        }
    }
    public class RowVoidRequest
    {
        [XmlElement("BC")]
        public string Line { get; set; }
    }
    [XmlRoot("CRQ")]
    public class TicketVoidRes : ParentRes
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowParentResponse> Rows { get; set; }

        public TicketVoidRes()
        {
            Rows = new List<RowParentResponse>();
        }

    }

    [XmlRoot("CRQ")]
    public class TicketVoidAdminModeReq : ParentReq
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowVoidRequest> Rows { get; set; }

        public TicketVoidAdminModeReq()
        {
            this.TipoOperazione = enumCodOper.SUPERVISORVOID;
            Rows = new List<RowVoidRequest>();
        }
        public void CaricaBuono(string buono)
        {
            this.Rows.Add(new RowVoidRequest() { Line = buono });
        }
    }

    #endregion

    #region CLOSE
    [XmlRoot("CRQ")]
    public class TicketCloseReq :ParentReq
    {
        public TicketCloseReq()
        {
            this.TipoOperazione = enumCodOper.CHIUSURASCONTRINO;
        }
    }


    [XmlRoot("CRQ")]
    public class TicketCloseRes : ParentRes
    {

    }
    #endregion CLOSE

    #region MASTERVOID

    [XmlRoot("CRQ")]
    public class MasterVoidReq : ParentReq
    {
        [XmlArray("LIST")]
        [XmlArrayItem("ELEM")]
        public List<RowValidationRequest> Rows { get; set; }
        public MasterVoidReq()
        {
            this.TipoOperazione = enumCodOper.SUPERVISORVOID;
            Rows = new List<RowValidationRequest>();
        }
    }
    #endregion

    public enum enumSceltaAzione
    {
        leggi,
        annulla,
        azzera,
        supervisor
    }
    public enum enumCompRefund
    {
        EDENRED = 1,
        DAY = 2,
        SODEXO = 3,
        PELLEGRINI = 4,
        RISTOMAT = 5,
        QUI = 6,
        CIR = 7,
        JAKALA = 8,
        MIG = 9,
        SODEXOGIFT = 10,
        DEFAULT = 99
    }

    public static class HelperDizionario
    {

        public static string rispostaToKey(string tipo, string valore)
        {
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
            String risposta = valore;
            XmlDocument doc = new XmlDocument();
            doc.Load(config.AppSettings.Settings["dizionarioSceltaAzioni"].Value);
            var nodes = doc.DocumentElement.SelectNodes(tipo);
            if (nodes != null && nodes.Count > 0)
            {
                foreach (XmlNode n in nodes)
                {
                    if (n.Attributes["value"]?.InnerText == valore)
                        risposta = n.Attributes["key"]?.InnerText;
                }
            }
            return risposta;
        }
        public static Dictionary<string, string> risposteSceltaAzione
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                Dictionary<string, string> domandaSceltaAzione = new Dictionary<string, string>();
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioSceltaAzioni"].Value);
                var nodes = doc.DocumentElement.SelectNodes("SceltaAzione");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        domandaSceltaAzione.Add(n.Attributes["key"]?.InnerText, n.Attributes["value"]?.InnerText);
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static Dictionary<string, string> dizionarioErroreAnnullamento
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                var doc = XDocument.Load(config.AppSettings.Settings["dizionarioErroreAnnullamento"].Value);
                var rootNodes = doc.Root.DescendantNodes().OfType<XElement>();
                return rootNodes.ToDictionary(n => n.Name.ToString(), n => n.Value);
            }
        }
        public static string domandaInizio
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "inizio")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaLeggi
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "leggi")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaGestione
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "gestione")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErroreppt
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "erroreppt")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErrorebc
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "errorebc")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaMassimobuoni
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "massimobuoni")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaMassimoraggiunto
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "massimoraggiunto")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErroreapertura
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "erroreapertura")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErrorevalidazione
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "errorevalidazione")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErrorechiusura
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "errorechiusura")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErroreannullamento
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "erroreannullamento")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaErroreresto
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "erroreresto")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }
        public static string domandaSceltazione
        {
            get
            {
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
                string domandaSceltaAzione = String.Empty;
                XmlDocument doc = new XmlDocument();
                doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
                var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (XmlNode n in nodes)
                    {
                        if (n.Attributes["key"]?.InnerText == "sceltazione")
                            domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                    }
                }
                return domandaSceltaAzione;
            }
        }

        public static string domanda(string domanda, params object[] parameters)
        {
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
            string domandaSceltaAzione = String.Empty;
            XmlDocument doc = new XmlDocument();
            doc.Load(config.AppSettings.Settings["dizionarioDomande"].Value);
            var nodes = doc.DocumentElement.SelectNodes("TestoDomanda");
            if (nodes != null && nodes.Count > 0)
            {
                foreach (XmlNode n in nodes)
                {
                    if (n.Attributes["key"]?.InnerText == domanda)
                        domandaSceltaAzione = n.Attributes["value"]?.InnerText;
                }
            }
            if (parameters.Length > 0)
                domandaSceltaAzione = String.Format(domandaSceltaAzione, parameters);
            return domandaSceltaAzione;
        }
        public static Dictionary<string, string> risposte(string domanda)
        {
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);
            Dictionary<string, string> domandaSceltaAzione = new Dictionary<string, string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(config.AppSettings.Settings["dizionarioSceltaAzioni"].Value);
            var nodes = doc.DocumentElement.SelectNodes(domanda);
            if (nodes != null && nodes.Count > 0)
            {
                foreach (XmlNode n in nodes)
                {
                    domandaSceltaAzione.Add(n.Attributes["key"]?.InnerText, n.Attributes["value"]?.InnerText);
                }
            }
            return domandaSceltaAzione;
        }

    }
    public enum enumCodOper
    {
        APERTURASCONTRINO = 0,
        VALIDAZIONE = 1,
        STORNO = 3,
        CHIUSURASCONTRINO = 5,
        SUPERVISORVOID = 9
    }

    public enum enumCOMRES
    {
        OK,
        KO
    }
    
    public enum enumErroreValidazioneRisposta
    {
        Leggi,
        Chiudi,
        Annulla

    }

    public enum enumErroreAnnullamentoRisposta
    {
        Riprova,
        Mantieni,
        Interrompi
    }
    
}
