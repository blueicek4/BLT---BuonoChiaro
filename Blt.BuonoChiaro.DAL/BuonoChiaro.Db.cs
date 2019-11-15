using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PMessageBox.Contract.Conto;
using Blt.BuonoChiaro.BOL;
using System.Net.Sockets;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Blt.BuonoChiaro.DAL
{
    public class BuonoChiaroDb
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TicketOpenRes ApriTicket(ContrattoConto conto)
        {
            try {
                ParametriConto par = new ParametriConto(conto);
                if (!String.IsNullOrEmpty(par.idBuonoChiaro))
                {
                    log.InfoFormat("Apri Ticket - Ticket già aperto - idBuonoChiaro: {0} - idConto {1} idUtente: {2}", par.idBuonoChiaro, par.IdConto, conto.CassiereLogin);
                    return new TicketOpenRes() { COMRES = enumCOMRES.OK.ToString(), ERRMSG = String.Empty, IDTR = par.idBuonoChiaro };

                }
                else
                {
                    TicketOpenReq apriTicket = new TicketOpenReq();
                    apriTicket.Init(conto);
                    string request = apriTicket.ToXML();
                    string logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                        , conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "_" + apriTicket.TipoOperazione + "_{0}_{1}.xml");
                    System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), request);
                    string response = InviaSocket(request);
                    System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), request);
                    TicketOpenRes res = (TicketOpenRes)new System.Xml.Serialization.XmlSerializer(typeof(TicketOpenRes)).Deserialize(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response)));
                    return res;
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Apri Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                return new TicketOpenRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message };
            }
        }
        public TicketValidationRes ValidaTicket(ContrattoConto conto, string buono)
        {
            try
            {
                TicketValidationReq validaTicket = new TicketValidationReq();
                validaTicket.Init(conto);
                validaTicket.CaricaBuono(buono);
                string request = validaTicket.ToXML();
                string logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                    , conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "_" + validaTicket.TipoOperazione + "_{0}_{1}.xml");
                System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), request);
                string response = InviaSocket(request);
                System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), response);
                TicketValidationRes res = (TicketValidationRes)new System.Xml.Serialization.XmlSerializer(typeof(TicketValidationRes)).Deserialize(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response)));
                return res;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Valida Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                return new TicketValidationRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message };
            }
        }
        public TicketVoidRes AnnullaTicket(ContrattoConto conto, string buono)
        {
            try
            {
                TicketVoidReq validaTicket = new TicketVoidReq();
                validaTicket.Init(conto);
                validaTicket.CaricaBuono(buono);
                string request = validaTicket.ToXML();
                string logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                    , conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "_" + validaTicket.TipoOperazione + "_{0}_{1}.xml");
                System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), request);
                string response = InviaSocket(request);
                System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), response);
                TicketVoidRes res = (TicketVoidRes)new System.Xml.Serialization.XmlSerializer(typeof(TicketVoidRes)).Deserialize(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response)));
                return res;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Valida Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                return new TicketVoidRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message };
            }
        }
        public List<TicketVoidRes> AnnullaTuttiTicket(ContrattoConto conto)
        {
            List<TicketVoidRes> res = new List<TicketVoidRes>();
            try
            {
                ParametriConto par = new ParametriConto(conto);
                foreach (var b in par.Codici)
                {
                    res.Add(AnnullaTicket(conto, b.CodiceABarre));
                }
                return res;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Valida Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                res.Add(new TicketVoidRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message });
                return res;
            }
        }
        public TicketVoidRes AnnullaTicketChiuso(ContrattoConto conto, string numeroConto, string buono)
        {
            try
            {
                TicketOpenReq apriTicket = new TicketOpenReq();
                apriTicket.Init(conto);
                apriTicket.NumeroScontrino = numeroConto;
                apriTicket.Progressivo = apriTicket.Progressivo.Replace(conto.IdGestionale.ToString(), numeroConto);
                string openrequest = apriTicket.ToXML();
                string logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                    , conto.DataGestione.Value.ToString("yyMMdd") + "_" + numeroConto + "_" + apriTicket.TipoOperazione + "_{0}_{1}.xml");
                System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), openrequest);
                string openresponse = InviaSocket(openrequest);
                System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), openresponse);


                TicketVoidAdminModeReq validaTicket = new TicketVoidAdminModeReq();
                validaTicket.Init(conto);
                validaTicket.CaricaBuono(buono);
                validaTicket.NumeroScontrino = numeroConto;
                validaTicket.Progressivo = apriTicket.Progressivo.Replace(conto.IdGestionale.ToString(), numeroConto);
                string request = validaTicket.ToXML();
                logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                    , conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "_" + validaTicket.TipoOperazione + "_{0}_{1}.xml");
                System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), request);
                string response = InviaSocket(request);
                System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), response);
                TicketVoidRes res = (TicketVoidRes)new System.Xml.Serialization.XmlSerializer(typeof(TicketVoidRes)).Deserialize(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response)));
                return res;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Valida Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                return new TicketVoidRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message };
            }

        }
        public TicketCloseRes ChiudiTicket(ContrattoConto conto)
        {
            try
            {
                TicketCloseReq apriTicket = new TicketCloseReq();
                apriTicket.Init(conto);
                string request = apriTicket.ToXML();
                string logFileName = System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["LogPath"]
                    , conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "_" + apriTicket.TipoOperazione + "_{0}_{1}.xml");
                System.IO.File.WriteAllText(String.Format(logFileName, "request", DateTime.Now.ToString("hhmmss")), request);
                string response = InviaSocket(request);
                System.IO.File.WriteAllText(String.Format(logFileName, "response", DateTime.Now.ToString("hhmmss")), request);
                TicketCloseRes res = (TicketCloseRes)new System.Xml.Serialization.XmlSerializer(typeof(TicketCloseRes)).Deserialize(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response)));
                return res;
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Errore Apri Ticket - Origine: {0} - Riga {1} Errore: {2}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex.ToString().Substring(ex.ToString().LastIndexOf(" ") + 1), ex.Message);
                return new TicketCloseRes() { COMRES = enumCOMRES.KO.ToString(), ERRMSG = ex.Message };
            }
        }

        public string InviaSocket(string request)
        {
            try
            {              
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //soc.
                System.Net.IPAddress ipAdd = System.Net.IPAddress.Parse(System.Configuration.ConfigurationManager.AppSettings["BuonoChiaroServer"]);
                System.Net.IPEndPoint remoteEP = new IPEndPoint(ipAdd, Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["BuonoChiaroPorta"]));
                soc.Connect(remoteEP);
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(request);
                soc.Send(byData);
                //

                //soc.Close();
                byte[] buffer = new byte[1024];
                int iRx = soc.Receive(buffer);
                char[] chars = new char[iRx];

                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
                String response = new System.String(chars);
                //System.String recv = new System.String(chars);                
                //XDocument doc = XDocument.Parse(recv);
                //String response = JsonConvert.SerializeXNode(doc);
                return response;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }
    }

}
