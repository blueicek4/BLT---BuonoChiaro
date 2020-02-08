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
using System.Security.Cryptography;

namespace Blt.BuonoChiaro.BOL
{
    public class ParametriConto
    {
        public string idBuonoChiaro { get; set; }
        public Int32? IdConto { get; set; }
        public string idScontrinoElettronico { get; set; }
        public string IdChiamata { get; set; }
        public string UltimaDomanda { get; set; }
        public string UltimaRisposta { get; set; }
        public StatoConto Stato { get; set; }
        public Decimal? Totale { get; set; }
        public List<BuonoPasto> Codici { get { return this._codici; } }
        private List<BuonoPasto> _codici { get; set; }
        public string CategoriaPagamento { get; set; }
        public string CodicePagamento { get; set; }
        public enumCodOper UltimoComandoBuonoChiaro { get; set; }
        private Int32?  _indiceParametri {get;set;}
        public EnumTipoBuonoPasto TipoBuonoPasto { get; set; }
        Configuration config;
        public Decimal GetTotale()
        {
            try
            {
                return _codici.Sum(b => b.ValoreTotale);
            }
            catch
            {
                return 0;
            }

        }
        public Decimal GetUltimo()
        {
            try
            {

                if (Codici.Count > 0)
                    return Codici.Last().ValoreTotale;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }
        public ParametriConto()
        {
            _codici = new List<BuonoPasto>();
            string exeConfigPath = typeof(ParametriConto).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
        }
        public String ToXml()
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter tw = null;

            XmlSerializer serializer = new XmlSerializer(this.GetType());
            tw = new XmlTextWriter(sw);
            serializer.Serialize(tw, this);
            sw.Close();
            return sw.ToString();
        }

        public Boolean AggiungiBuono( TicketValidationRes buono, Boolean force = false)
        {
            BuonoPasto bp = new BuonoPasto(buono);
            if (force)
            {
                this._codici.Add(bp);
                return true;
            }
            if (!Convert.ToBoolean(config.AppSettings.Settings["isAbilitaResto"].Value))
            {
                if (this.GetTotale() + bp.ValoreTotale <= this.Totale)
                {
                    this._codici.Add(bp);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                this._codici.Add(bp);
                return true;
            }
        }
        public Boolean AggiungiBuono(BuonoPasto buonoPasto)
        {
            if (!Convert.ToBoolean(config.AppSettings.Settings["isAbilitaResto"].Value))
            {
                if (this.GetTotale() + buonoPasto.ValoreTotale <= this.Totale)
                {
                    this._codici.Add(buonoPasto);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                this._codici.Add(buonoPasto);
                return true;
            }
        }
        public List<BuonoPasto> GetRiepilogo()
        {
            List<BuonoPasto> lbp = _codici.GroupBy(c => new { c.ValoreTotale , c.Fornitore })
                .Select(bp => new BuonoPasto() { CodiceABarre = bp.Count().ToString(), ValoreTotale = bp.First().ValoreTotale, Fornitore = bp.First().Fornitore }).ToList();
            return lbp;
        }
        public ParametriConto(string xml)
        {
            string exeConfigPath = typeof(ParametriConto).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            try
            {
                StringReader strReader = null;
                XmlSerializer serializer = null;
                XmlTextReader xmlReader = null;
                ParametriConto obj = null;
                try
                {
                    strReader = new StringReader(xml);
                    serializer = new XmlSerializer(this.GetType());
                    xmlReader = new XmlTextReader(strReader);
                    obj = (ParametriConto)serializer.Deserialize(xmlReader);
                }
                catch (Exception exp)
                {
                    //Handle Exception Code
                }
                finally
                {
                    if (xmlReader != null)
                    {
                        xmlReader.Close();
                    }
                    if (strReader != null)
                    {
                        strReader.Close();
                    }
                }
                this.idBuonoChiaro = obj.idBuonoChiaro;
                this._codici = obj.Codici;
                this.IdChiamata = obj.IdChiamata;
                this.idScontrinoElettronico = obj.idScontrinoElettronico;
                this.IdConto = obj.IdConto;
                this.Stato = obj.Stato;
                this.Totale = obj.Totale;
                this.UltimaDomanda = obj.UltimaDomanda;
                this.UltimaRisposta = obj.UltimaRisposta;
                this.CategoriaPagamento = obj.CategoriaPagamento;
                this.CodicePagamento = obj.CodicePagamento;
            }
            catch(Exception ex)
            {
            }
        }
        public ParametriConto(ContrattoConto conto)
        {
            string exeConfigPath = typeof(ParametriConto).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            if (conto.Tool_Parametri == null)
            {
                this.idScontrinoElettronico = conto.NumeroScontrinoFiscale;
                this.IdConto = conto.IdGestionale;
                this.IdChiamata = conto.Tool_IdTastoCustom;
                this.Stato = StatoConto.inizio;
                this.CategoriaPagamento = config.AppSettings.Settings["CategoriaPagamento"].Value;
                this.CodicePagamento = config.AppSettings.Settings["CodicePagamento"].Value;
                this._indiceParametri = null;
                this._codici = new List<BuonoPasto>();                
            }
            else if (conto.Tool_Parametri[0] == null)
            {
                this.idScontrinoElettronico = conto.NumeroScontrinoFiscale;
                this.IdConto = conto.IdGestionale;
                this.IdChiamata = conto.Tool_IdTastoCustom;
                this.Stato = StatoConto.inizio;
                this.CategoriaPagamento = config.AppSettings.Settings["CategoriaPagamento"].Value;
                this.CodicePagamento = config.AppSettings.Settings["CodicePagamento"].Value;
                this._indiceParametri = 0;
                this._codici = new List<BuonoPasto>();
            }
            else
            {
                for (int i = 0; i < conto.Tool_Parametri.Length; i++)
                {
                    ParametriConto par = new ParametriConto((conto.Tool_Parametri[i] ?? String.Empty).ToString());
                    if (par.IdConto != null)
                    {
                        this.idBuonoChiaro = par.idBuonoChiaro;
                        this._codici = par.Codici;
                        this.IdChiamata = par.IdChiamata;
                        this.idScontrinoElettronico = par.idScontrinoElettronico;
                        this.IdConto = par.IdConto;
                        this.Stato = par.Stato;
                        this.Totale = par.Totale;
                        this.UltimaDomanda = par.UltimaDomanda;
                        this.UltimaRisposta = par.UltimaRisposta;
                        this.CategoriaPagamento = par.CategoriaPagamento;
                        this.CodicePagamento = par.CodicePagamento;
                        this._indiceParametri = i;
                    }
                }
            }

        }
        public ContrattoConto Reset(ContrattoConto conto)
        {
            if(conto.Tool_Parametri.Length <= 1)
            {
                conto.Tool_Parametri = new object[1];
            }
            else
            {
                var list = conto.Tool_Parametri.ToList();
                list.RemoveAt(this._indiceParametri.Value);
                conto.Tool_Parametri = list.ToArray();

            }
            return conto;
        }
        public ContrattoConto ToConto(ContrattoConto conto)
        {
            if(this._indiceParametri!=null)
            {
                conto.Tool_Parametri[this._indiceParametri.Value] = this.ToXml();
            }
            else if(conto.Tool_Parametri != null)
            {
                var p = conto.Tool_Parametri.ToList();
                p.Add(this.ToXml());
                conto.Tool_Parametri = p.ToArray();
            }
            else
            {
                conto.Tool_Parametri = new object[1] { this.ToXml() };
            }
            return conto;
        }
    }

    public class BuonoPasto
    {
        public string CodiceABarre { get; set; }
        public decimal ValoreTotale { get; set; }
        public DateTime Scadenza { get; set; }
        public string Fornitore { get; set;}
        public string CodiceTransazione { get; set; }
        public Int32 Quantita { get; set; }
        public Decimal Valore { get; set; }
        //public BuonoPasto(string codice)
        //{
        //    this.CodiceABarre = codice;
        //    this.Valore = Convert.ToDecimal(codice.Substring(codice.Length - 5)) / 100;
        //    this.Scadenza = DateTime.ParseExact(codice.Substring(codice.Length - 9, 4) + DateTime.DaysInMonth(2000 + Convert.ToInt32(codice.Substring(codice.Length - 9, 2)), Convert.ToInt32(codice.Substring(codice.Length - 7, 2))), "yyMMdd", CultureInfo.InvariantCulture);
        //}
        public BuonoPasto(TicketValidationRes validationResponse)
        {
            this.CodiceABarre = validationResponse.Rows[0].BC;
            this.Fornitore = validationResponse.Rows[0].COMPANY.ToString();
            this.ValoreTotale = Convert.ToDecimal(validationResponse.Rows[0].VB, new CultureInfo("en-US"));
            this.Scadenza = DateTime.ParseExact(validationResponse.Rows[0].DTSCAD, "yyyyMMdd", CultureInfo.InvariantCulture);
            this.CodiceTransazione = validationResponse.IDTR;
        }
        public BuonoPasto() { }
    }
    
    public enum EnumTipoBuonoPasto
    {
        CARTACEO = 0,
        ELETTRONICO = 1
    }
    public enum StatoConto
    {
        inizio,
        leggi,
        gestione,
        erroreppt,
        errorebc,
        massimobuoni,
        massimoraggiunto,
        erroreapertura,
        errorevalidazione,
        errorechiusura,
        erroreannullamento,
        sceltazione,
        annullaBuonoPasto,
        azzerabpinizio,
        azzerabppassword,
        azzerabpscontrino,
        azzerabpbuono,
        azzerabpesegui,
        fine,
        azzerascontrino
    }

    public static class Helper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static DataTable SqlSelect(string query, object[] pars, string connectionName = null)
        {
            try
            {
                if (String.IsNullOrEmpty(connectionName))
                {
                    connectionName = System.Configuration.ConfigurationManager.ConnectionStrings["Bluetech"].Name;
                }
                //cmd1.SelectCommand.CommandTimeout = 300;
                SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[connectionName].ConnectionString);

                DataTable sqlquery = new DataTable();
                SqlDataAdapter cmd1 = new SqlDataAdapter();
                SqlCommand cmd = new SqlCommand(query, con);
                cmd1.SelectCommand = cmd;
                if (pars != null)
                {
                    int i = 0;

                    foreach (object par in pars)
                    {
                        SqlParameter gp = new SqlParameter("@param" + i.ToString(), par);
                        cmd.Parameters.Add(gp);

                        i++;
                    }
                }
                //cmd1.SelectCommand.CommandTimeout = Convert.ToInt32(config.AppSettings.Settings["SqlTimeout"].Value);

                cmd1.Fill(sqlquery);

                sqlquery.TableName = "SqlQuery";

                return sqlquery;
            }
            catch (Exception e)
            {
                log.ErrorFormat(@"{0} - Eccezione! - Riga: {1}\nStringa: {2}\nParametri: {3}\nConnessione: {4}\nErrore: {5}", System.Reflection.MethodBase.GetCurrentMethod().Name, e.ToString().Substring(e.ToString().LastIndexOf(" ") + 1), query, pars.ToList().ToString(), connectionName, e.Message);
                return new DataTable();
            }

        }

        public static Int32 SqlUpdate(string query, object[] pars, string connectionName = null)
        {
            int res = 0;
            try
            {
                if (String.IsNullOrEmpty(connectionName))
                {
                    connectionName = System.Configuration.ConfigurationManager.ConnectionStrings["Bluetech"].Name;
                }
                //cmd1.SelectCommand.CommandTimeout = 300;
                SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[connectionName].ConnectionString);
                con.Open();

                SqlCommand cmd = new SqlCommand(query, con);
                if (pars != null)
                {
                    int i = 0;

                    foreach (object par in pars)
                    {
                        SqlParameter gp = new SqlParameter("@param" + i.ToString(), par);
                        cmd.Parameters.Add(gp);

                        i++;
                    }
                }

                res = cmd.ExecuteNonQuery();
                con.Close();

            }
            catch (Exception e)
            {
                log.ErrorFormat("{0} - Aggiornamento Dati - Eccezione - Riga: {1} - Errore: {2}\nQuery: {3}\nParametri: {4}", System.Reflection.MethodBase.GetCurrentMethod().Name, e.ToString().Substring(e.ToString().LastIndexOf(" ") + 1), e.Message, query, String.Join(" - ", (pars ?? new object[] { }).ToList().Select(p => p.ToString()).ToArray()));
                res = -1;
            }
            return res;
        }
        public static Int32 SqlInsert(string query, object[] pars, string connectionName = null)
        {
            int res = 0;
            try
            {
                if (String.IsNullOrEmpty(connectionName))
                {
                    connectionName = System.Configuration.ConfigurationManager.ConnectionStrings["Bluetech"].Name;
                }
                //cmd1.SelectCommand.CommandTimeout = 300;
                SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings[connectionName].ConnectionString);
                con.Open();

                SqlCommand cmd = new SqlCommand(query, con);
                if (pars != null)
                {
                    int i = 0;

                    foreach (object par in pars)
                    {
                        SqlParameter gp = new SqlParameter("@param" + i.ToString(), par);
                        cmd.Parameters.Add(gp);

                        i++;
                    }
                }

                res = cmd.ExecuteNonQuery();
                con.Close();

            }
            catch (Exception e)
            {
                log.ErrorFormat("{0} - Aggiornamento Dati - Eccezione - Riga: {1} - Errore: {2}\nQuery: {3}\nParametri: {4}", System.Reflection.MethodBase.GetCurrentMethod().Name, e.ToString().Substring(e.ToString().LastIndexOf(" ") + 1), e.Message, query, String.Join(" - ", (pars ?? new object[] { }).ToList().Select(p => p.ToString()).ToArray()));
                res = -1;
            }
            return res;
        }

        public static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static Boolean VerificaLicenza()
        {
            try
            {
                return true;
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap() { ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(HelperDizionario).Assembly.Location), "buonochiaro.config") }, ConfigurationUserLevel.None);

                string percorso = System.IO.Path.Combine(System.IO.Directory.GetParent(System.IO.Path.GetDirectoryName(typeof(Helper).Assembly.Location)).ToString(), "FileConfigurazione.xml");
                XmlDocument doc = new XmlDocument();
                doc.Load(percorso);
                var nodoXml = doc.GetElementsByTagName("CodiceContratto")[0];
                string codiceContratto = nodoXml.Attributes["path"].Value;
                codiceContratto = System.IO.Path.GetFileNameWithoutExtension(codiceContratto);

                string codiceLicenza = codiceContratto + "8858c0cfa1c3888ad816e62dc35afb484fe8c5b7";

                if (Hash(codiceLicenza) == config.AppSettings.Settings["CodiceLicenza"].Value)
                    return true;
                else
                    return false;
            }          
            catch(Exception ex)
            {
                log.ErrorFormat(ex.Message);
                return false;
            }

        }
    }
}
