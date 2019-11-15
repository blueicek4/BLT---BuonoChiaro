﻿

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PMessageBox.Contract;
using PMessageBox.Contract.Common;
using PMessageBox.Kernel;
using PMessageBox.Contract.Cliente;
using PMessageBox.Contract.Conto;
using HostPlugin;
using Blt.BuonoChiaro.BOL;
using System.Data;
using PMessageBox.Contract.Buoni;

namespace Blt.BuonoChiaro.API
{
    [Plugin("BuonoChiaro")]
    [CanalePlugin(ContrattoConto.NomeContratto, false, true, true, false)]
    public class BltBuonoChiaro : IPlugin
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Messaggio RiceviMessaggio(Messaggio messaggio)
        {

            ContrattoConto conto = messaggio.Body.FirstOrDefault() as ContrattoConto;
            if (messaggio.Canale.Nome == ContrattoConto.NomeContratto)
            {
                if (messaggio.ComandoEnum == EnumComando.InserimentoDati)
                {
                    //Riceve un conto in uno degli eventi dichiarati sul gestionale, 
                    //imposta la promozione e restituisce il conto al gestionale
                    //if(conto.Tool_IdTastoCustom == "BUONOCHIARO")
                    switch(conto.Tool_Evento)
                    {
                        case EnumToolEvento.TastoCustom:
                            switch(conto.Tool_IdTastoCustom)
                            {
                                case "BUONOCHIARO":
                                    conto = InizioBuoniPasto(conto);
                                    break;
                                case "GESTIONECHIARO":
                                    conto = SceltaAzioneBuonoChiaro(conto);
                                    break;
                                default:
                                    conto = GestioneBuonoChiaro(conto);
                                    break;
                            }
                            break;
                        case EnumToolEvento.PreChiusura:
                            break;
                        case EnumToolEvento.PostChiusura:
                                conto = ScriviBuonoChiaro(conto);
                                //conto = ScriviBuonoPasto(conto);
                            break;
                        case EnumToolEvento.Annullamento:
                            conto = AzzeraBuonoChiaro(conto);
                            break;
                        default:
                            break;
                    }
                }
            }
            Messaggio risposta = new Messaggio();
            risposta.Body.Add(conto);
            return risposta;
        }
        public ContrattoConto SceltaAzioneBuonoChiaro(ContrattoConto conto)
        {
            if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta))
            {
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.SceltaMultipla;
                conto.Tool_Domanda = HelperDizionario.domandaSceltazione;
                conto.Tool_ValoriRisposta = HelperDizionario.risposteSceltaAzione.Select(d => d.Value).ToArray();
                var par = new ParametriConto(conto);
                par.Stato = StatoConto.sceltazione;
                conto = par.ToConto(conto);
            }
            else
            {
                var par = new ParametriConto(conto);
                var domSceltaAzione = HelperDizionario.risposteSceltaAzione.First(d => d.Value == conto.Tool_RispostaAScelta);
                enumSceltaAzione enumSA = (enumSceltaAzione)Enum.Parse(typeof(enumSceltaAzione), domSceltaAzione.Key);
                switch(enumSA)
                {
                    case enumSceltaAzione.leggi:
                        par.Stato = StatoConto.leggi;
                        conto = par.ToConto(conto);
                        return GestioneBuonoChiaro(conto);
                    case enumSceltaAzione.annulla:
                        return AnnullaBuonoChiaro(conto, null);
                    case enumSceltaAzione.azzera:
                        return AzzeraBuonoChiaro(conto);
                    case enumSceltaAzione.supervisor:

                        break;
                    default:
                        break;
                }
            }

            return conto;
        }

        public ContrattoConto GestioneBuonoChiaro(ContrattoConto conto)
        {
            ParametriConto par;
            string buonoPasto = String.Empty;
            BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
            string a = "a";
            string b = "b";
            if (a == b)
            {
                //bcDb.AnnullaTicketChiuso(conto, conto.Tool_RispostaAScelta);
            }

            if (conto.Tool_Parametri == null)
            {
                return InizioBuoniPasto(conto);
            }
            else
            {
                par = new ParametriConto(conto);
                par.Totale = conto.TotaleDaPagare;
                if ("1"=="1")//!String.IsNullOrEmpty(conto.Tool_RispostaAScelta))
                {
                    switch (par.Stato)
                    {
                        case StatoConto.inizio:
                        case StatoConto.leggi:
                            if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta) && conto.Tool_RispostaFissa == null)
                            {
                                if(ControllaSuperatoMassimoImporto(conto, par, out conto))
                                {
                                    return conto;
                                }
                                else if (ControllaSuperatoMassimoNumero(conto, par, out conto))
                                {
                                    return conto;
                                }
                                else
                                    return ChiediAncora(conto);
                            }
                                buonoPasto = conto.Tool_RispostaAScelta;
                            /*if (par.Codici.Exists(c => c.CodiceABarre == buonoPasto) && Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isGestisciDuplicatiLocalmente"]))
                            {
                                par.Stato = StatoConto.erroreppt;
                                return DoppioBuonoPasto(conto, buonoPasto);
                            }
                            else
                            {*/
                                log.InfoFormat("Valido Ticket Buono Chiaro - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4} - Ticket: {5}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare, buonoPasto);
                                var res = bcDb.ValidaTicket(conto, buonoPasto);
                            if (res.RISULTATO == enumCOMRES.OK)
                            {
                                if (!par.AggiungiBuono(res))
                                {
                                    conto.Tool_Domanda = HelperDizionario.domandaErroreresto;
                                    conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                                    conto = par.ToConto(conto);
                                    var resA = bcDb.AnnullaTicket(conto, buonoPasto);
                                    if (resA.RISULTATO == enumCOMRES.OK)
                                    {
                                        return ScriviPagamento(conto, par);
                                    }
                                    else
                                    {
                                        par.AggiungiBuono(res, true);
                                        conto = par.ToConto(conto);
                                        conto = ErroreAnnullaTicketResto(conto, resA);
                                        return ScriviPagamento(conto, par);
                                    }
                                }
                                else
                                {
                                    if (ControllaSuperatoMassimoImporto(conto, par, out conto))
                                    {
                                        return conto;
                                    }
                                    else if (ControllaSuperatoMassimoNumero(conto, par, out conto))
                                    {
                                        return conto;
                                    }
                                    else
                                    {
                                        conto = par.ToConto(conto);
                                        
                                        return LeggiBuoniPasto(ScriviPagamento(conto, par));
                                    }
                                }
                            }
                            else
                            {
                                conto= ErroreValidaTicket(conto, res);
                                par = new ParametriConto(conto);
                                return ScriviPagamento(conto, par);
                            }
                            //}
                            conto = par.ToConto(conto);
                            break;
                        case StatoConto.sceltazione:
                            string risposta = HelperDizionario.rispostaToKey("SceltaAzione", conto.Tool_RispostaAScelta);
                            switch(risposta)
                            {
                                case "leggi":
                                    par.Stato = StatoConto.leggi;
                                    conto = par.ToConto(conto);
                                    return LeggiBuoniPasto(conto);
                                    break;
                                case "annulla":
                                    //par.Stato = StatoConto.annullaBuonoPasto;
                                    conto = par.ToConto(conto);
                                    return AnnullaBuonoChiaro(conto, null);
                                    break;
                                case "azzera":
                                    par.Stato = StatoConto.azzerascontrino;
                                    return AnnullaScontrino(conto);
                                    break;
                                case "supervisor":
                                    par.Stato = StatoConto.azzerabpinizio;
                                    conto = par.ToConto(conto);
                                    return AzzeraBuonoChiaro(conto);
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case StatoConto.annullaBuonoPasto:
                            buonoPasto = conto.Tool_RispostaAScelta;
                            return AnnullaBuonoChiaro(conto, buonoPasto);
                        case StatoConto.azzerabpinizio:
                        case StatoConto.azzerabppassword:
                        case StatoConto.azzerabpscontrino:
                        case StatoConto.azzerabpbuono:
                        case StatoConto.azzerabpesegui:
                            return AzzeraBuonoChiaro(conto);
                        case StatoConto.errorevalidazione:
                            return LeggiBuoniPasto(conto);
                        default:
                            break;
                    }

                }
            }
            if (Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isModalitaVeloce"]))
            {
                if (par.Stato == StatoConto.inizio || par.Stato == StatoConto.leggi)
                {
                    if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta) && conto.Tool_RispostaFissa == null)
                    {
                        par.Stato = StatoConto.gestione;
                    }
                    else if (Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxNumeroBuoni"]) <= par.Codici.Count)
                    {
                        par.Stato = StatoConto.massimobuoni;
                    }
                    else if (conto.TotaleDaPagare <= par.GetTotale())
                    {
                        par.Stato = StatoConto.massimoraggiunto;
                    }
                }
                switch (par.Stato)
                {
                    case StatoConto.leggi:
                    case StatoConto.inizio:
                        return LeggiBuoniPasto(conto);
                    case StatoConto.gestione:
                        if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta) && conto.Tool_RispostaFissa == null)
                        {
                            return ChiediAncora(conto);
                        }
                        else
                        {
                            switch (conto.Tool_RispostaFissa)
                            {
                                case EnumToolRisposta.No:
                                    return LeggiBuoniPasto(conto);
                                case EnumToolRisposta.Si:
                                    return ScriviPagamento(conto, par);
                                case EnumToolRisposta.Annulla:
                                    conto.Tool_Parametri = null;
                                    if (conto.Pagamenti.Any(p => p.Tipo.Codice == par.CodicePagamento))
                                        conto.Pagamenti.First(p => p.Tipo.Codice == par.CodicePagamento).Importo = 0;
                                    bcDb.AnnullaTuttiTicket(conto);
                                    par = new ParametriConto();
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case StatoConto.erroreppt:
                        switch (conto.Tool_RispostaFissa)
                        {
                            case EnumToolRisposta.Si:
                                return LeggiBuoniPasto(conto);
                            case EnumToolRisposta.No:
                                return conto;
                            case EnumToolRisposta.Annulla:
                                conto.Tool_Parametri = null;
                                if (conto.Pagamenti.Any(p => p.Tipo.Codice == par.CodicePagamento))
                                    conto.Pagamenti.First(p => p.Tipo.Codice == par.CodicePagamento).Importo = 0;
                                bcDb.AnnullaTuttiTicket(conto);
                                par = new ParametriConto();
                                break;
                            default:
                                break;
                        }
                        break;
                    case StatoConto.massimobuoni:
                        if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta) && conto.Tool_RispostaFissa == null)
                        {
                            return MassimoBuoni(conto);
                        }
                        else
                        {
                            switch (conto.Tool_RispostaAScelta)
                            {
                                case "Annulla":
                                    return conto;
                                case "Chiudi":
                                    return ScriviPagamento(conto, par);
                                case "Azzera Buoni":
                                    conto.Tool_Parametri = null;
                                    if (conto.Pagamenti.Any(p => p.Tipo.Codice == par.CodicePagamento))
                                        conto.Pagamenti.First(p => p.Tipo.Codice == par.CodicePagamento).Importo = 0;
                                    bcDb.AnnullaTuttiTicket(conto);
                                    par = new ParametriConto();
                                    break;
                                default:
                                    if (conto.Tool_IdTastoCustom == System.Configuration.ConfigurationManager.AppSettings["IdTastoCustom"])
                                        return MassimoBuoni(conto);
                                    conto.Tool_Domanda = String.Format(HelperDizionario.domandaMassimobuoni, par.Codici.Count, par.GetTotale());
                                    conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                                    return ScriviPagamento(conto, par);
                                    break;
                            }
                        }
                        break;
                    case StatoConto.massimoraggiunto:
                        if (conto.Tool_IdTastoCustom == System.Configuration.ConfigurationManager.AppSettings["IdTastoCustom"])
                            return MassimoBuoni(conto);
                        conto.Tool_Domanda = string.Format(HelperDizionario.domandaMassimoraggiunto, conto.TotaleDaPagare, par.GetTotale());
                        conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                        conto = par.ToConto(conto);
                        return ScriviPagamento(conto, par);

                    default:
                        break;
                }
            }
            else
            {
                switch (par.Stato)
                {
                    case StatoConto.leggi:
                    case StatoConto.inizio:
                        return ChiediAncora(conto);
                    case StatoConto.gestione:
                        switch (conto.Tool_RispostaFissa)
                        {
                            case EnumToolRisposta.Si:
                                return LeggiBuoniPasto(conto);
                            case EnumToolRisposta.No:
                                return conto;
                            case EnumToolRisposta.Annulla:
                                conto.Tool_Parametri = null;
                                if (conto.Pagamenti.Any(p => p.Tipo.Codice == par.CodicePagamento))
                                    conto.Pagamenti.First(p => p.Tipo.Codice == par.CodicePagamento).Importo = 0;
                                bcDb.AnnullaTuttiTicket(conto);
                                par = new ParametriConto();
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            return conto;
        }
        public ContrattoConto ScriviBuonoChiaro(ContrattoConto conto)
        {
            log.DebugFormat("Chiudi Scontrino BuonoChiaro - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare);
            ParametriConto par = new ParametriConto(conto);
            conto.Tool_Parametri = new object[1];
            BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
            var res = bcDb.ChiudiTicket(conto);
            if(res.RISULTATO == enumCOMRES.OK)
            {
                foreach (var file in System.IO.Directory.GetFiles(System.Configuration.ConfigurationManager.AppSettings["LogPath"], "*" + conto.DataGestione.Value.ToString("yyMMdd") + "_" + conto.IdGestionale + "*"))
                    System.IO.File.Move(file, System.IO.Path.Combine(System.Configuration.ConfigurationManager.AppSettings["SuccessFolder"], System.IO.Path.GetFileName(file)));
            }
            else
            {
                log.ErrorFormat("Errore Chiusura BuonoChiaro - Errore: {0} - Messaggio: {1} - ID Transazione: {2} - ID Scontrino: {3}", res.RISULTATO, res.ERRMSG, res.IDTR, res.CODSCNT);
                conto.Tool_Domanda = String.Format(HelperDizionario.domandaErrorechiusura, res.ERRMSG);
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.SiNo;
                
            }

            return par.ToConto(conto);
        }
        public ContrattoConto AzzeraBuonoChiaro(ContrattoConto conto)
        {
            ParametriConto par = new ParametriConto(conto);
            switch(par.Stato)
            {
                case StatoConto.azzerabpinizio:
                    conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                    conto.Tool_Domanda = HelperDizionario.domanda("azzerabpinizio");
                    par.Stato = StatoConto.azzerabppassword;
                    conto = par.ToConto(conto);
                    break;
                case StatoConto.azzerabppassword:
                    if(!String.IsNullOrEmpty(conto.Tool_RispostaAScelta))
                    {
                        if(conto.Tool_RispostaAScelta == "blt.1980")
                        {
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                            conto.Tool_Domanda = HelperDizionario.domanda("azzerabppassword");
                            par.Stato = StatoConto.azzerabpscontrino;
                            conto = par.ToConto(conto);
                            return conto;
                        }
                        else
                        {
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                            conto.Tool_Domanda = HelperDizionario.domanda("azzerabppassworderrore");
                            par.Stato = StatoConto.azzerabppassword;
                            conto = par.ToConto(conto);
                            return conto;

                        }
                    }
                    else
                    {
                        conto = par.Reset(conto);
                        return conto;
                    }
                    break;
                case StatoConto.azzerabpscontrino:
                    if (!String.IsNullOrEmpty(conto.Tool_RispostaAScelta))
                    {
                        BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
                        //ParametriConto par = new ParametriConto(conto);
                        par.IdConto = Convert.ToInt32(conto.Tool_RispostaAScelta);
                        par.Stato = StatoConto.azzerabpbuono;
                        conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                        conto.Tool_Domanda = HelperDizionario.domanda("azzerabpbuono");
                        conto = par.ToConto(conto);

                    }
                    else
                    {
                        conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                        conto.Tool_Domanda = HelperDizionario.domanda("azzerabpscontrino");
                        par.Stato = StatoConto.azzerabpscontrino;
                        conto = par.ToConto(conto);
                        return conto;
                    }
                    break;
                case StatoConto.azzerabpbuono:
                    if (!String.IsNullOrEmpty(conto.Tool_RispostaAScelta))
                    {
                        BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
                        //ParametriConto par = new ParametriConto(conto);
                        par.Codici.Add(new BuonoPasto() { CodiceABarre = conto.Tool_RispostaAScelta });
                        par.Stato = StatoConto.azzerabpesegui;
                        var res = bcDb.AnnullaTicketChiuso(conto, par.IdConto.ToString(), par.Codici[0].CodiceABarre);
                        if (res.RISULTATO == enumCOMRES.OK)
                        {
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                            conto.Tool_Domanda = HelperDizionario.domanda("azzerabpok");
                            conto = par.Reset(conto);
                        }
                        else
                        {
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                            conto.Tool_Domanda = HelperDizionario.domanda("azzerabpko", par.Codici[0].CodiceABarre, res.ERRMSG);
                            //conto.Tool_BloccaChiusura = true;
                            conto = par.Reset(conto);
                        }
                        return conto;
                    }
                    else
                    {
                        conto = par.Reset(conto);
                        return conto;
                    }

                    break;
                default:
                    par.Stato = StatoConto.inizio;
                    conto = par.ToConto(conto);
                    break;
            }
            if (String.IsNullOrEmpty(conto.Tool_RispostaAScelta) || par.Stato == StatoConto.sceltazione)
            {
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                conto.Tool_Domanda = HelperDizionario.domanda("azzerabpinizio");
                par.Stato = StatoConto.azzerabpinizio;
                conto = par.ToConto(conto);
            }
            return conto;
        }
        public ContrattoConto AnnullaScontrino(ContrattoConto conto)
        {
            ParametriConto par = new ParametriConto(conto);
            if (par.Stato != StatoConto.annullaBuonoPasto)
                conto.Tool_RispostaAScelta = string.Empty;

            Boolean result = true;
            List<String> Barcodes = new List<string>();
            BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
            if (!String.IsNullOrEmpty(par.idBuonoChiaro))
            {
                List<string> codici = par.Codici.Select(b => b.CodiceABarre).ToList();
                foreach (var buonoPasto in codici)
                {
                    var resA = bcDb.AnnullaTicket(conto, buonoPasto);
                    if (resA.RISULTATO == enumCOMRES.KO)
                    {
                        if (resA.Rows[0].OPRES == "KOVNU")
                        {
                            par.Codici.RemoveAll(c => c.CodiceABarre == resA.Rows[0].BC);
                        }
                        else
                        {
                            Barcodes.Add(resA.Rows[0].BC);
                            result = false;
                        }
                    }
                    else
                    {
                        par.Codici.RemoveAll(c => c.CodiceABarre == resA.Rows[0].BC);
                    }
                }
                if (result)
                {
                    conto.Tool_Domanda = HelperDizionario.domanda("annullascontrinook");

                }
                else
                {
                    conto.Tool_Domanda = HelperDizionario.domanda("annullascontrinoerrore", String.Join(";", Barcodes.ToArray()));

                }
            }
            else
            {
                conto.Tool_Domanda = HelperDizionario.domanda("erroreapertura");
            }
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
            par.Stato = StatoConto.fine;
            conto = par.ToConto(conto);
            conto = ScriviPagamento(conto, par);
            return conto;
        }
        public ContrattoConto AnnullaBuonoChiaro(ContrattoConto conto, string buonoPasto)
        {

            ParametriConto par = new ParametriConto(conto);
            if (par.Stato != StatoConto.annullaBuonoPasto)
                conto.Tool_RispostaAScelta = string.Empty;
            string risposta = HelperDizionario.rispostaToKey("ErroreAnnullaBuono", conto.Tool_RispostaAScelta);
            switch (risposta)
            {
                case "":
                case null:
                    conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                    conto.Tool_Domanda = HelperDizionario.domanda("annullaBuonoPasto");
                    par.Stato = StatoConto.annullaBuonoPasto;
                    conto = par.ToConto(conto);
                    return conto;
                case "annulla":
                    par.Codici.RemoveAll(c => c.CodiceABarre == buonoPasto);
                    return par.ToConto(conto);
                case "mantieni":
                    return par.ToConto(conto);
                case "riprova":
                default:
                    BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
                    if (!String.IsNullOrEmpty(buonoPasto))
                    {
                        var resA = bcDb.AnnullaTicket(conto, buonoPasto);
                        if (resA.RISULTATO == enumCOMRES.OK)
                        {
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                            conto.Tool_Domanda = HelperDizionario.domanda("annullabpok");
                            par.Codici.RemoveAll(c => c.CodiceABarre == buonoPasto);
                            conto = par.ToConto(conto);
                            return ScriviPagamento(conto, par);
                        }
                        else
                        {
                            if(resA.Rows[0].OPRES == "KOVNU")
                            {
                                par.Codici.RemoveAll(c => c.CodiceABarre == resA.Rows[0].BC);
                            }
                            //par.AggiungiBuono(resA, true);
                            conto = par.ToConto(conto);
                            conto = ErroreAnnullaTicket(conto, resA);
                            return ScriviPagamento(conto, par);
                        }
                    }
                    break;
            }
            return conto;
        }
        public ContrattoConto InizioBuoniPasto(ContrattoConto conto)
        {
            log.DebugFormat("Inizio Gestione Buoni Pasto - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare);
            ParametriConto par = new ParametriConto(conto);
            if (ControllaSuperatoMassimoImporto(conto, par, out conto))
            {
                return conto;
            }
            else if (ControllaSuperatoMassimoNumero(conto, par, out conto))
            {
                return conto;
            }

            BuonoChiaro.DAL.BuonoChiaroDb bcDb = new DAL.BuonoChiaroDb();
            log.InfoFormat("Apro Ticket Buono Chiaro - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare);
            var res = bcDb.ApriTicket(conto);
            if (res.RISULTATO == enumCOMRES.OK)
            {
                par.idBuonoChiaro =  res.IDTR;
                log.DebugFormat("Apertura Ticket Buono Chiaro OK - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4} - Response: {5} - IdChiamata: {6}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare, res.RISULTATO, res.IDTR);
                String domanda = String.Format(HelperDizionario.domandaLeggi, conto.TotaleDaPagare, par.Codici.Count, par.GetTotale(), par.GetUltimo());
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;
                conto.Tool_Domanda = domanda;
                par.UltimaDomanda = conto.Tool_Domanda;
                par.Stato = StatoConto.leggi;
                conto = par.ToConto(conto);//.Tool_Parametri[0] = par.ToXml();
            }
            else
            {
                log.ErrorFormat("Errore Apertura Buono Chiaro - Conto: {0} - Operatore: {1} - Cassa/Tavolo: {2} - Reparto/Sala: {3} - Totale Conto: {4} - Id Transazione: {5} - Errore: {6}", conto.IdGestionale, conto.CassiereLogin, conto.PuntoCassa, conto.Reparto, conto.TotaleDaPagare, res.IDTR, res.ERRMSG);
                conto = ErroreApriTicket(conto, res);
            }
            return conto;

        }
        public ContrattoConto DoppioBuonoPasto(ContrattoConto conto, string buonoPasto)
        {
            var par = new ParametriConto(conto);
            string domanda = String.Format(HelperDizionario.domandaErroreppt, buonoPasto, par.GetTotale());
            par.UltimaDomanda = conto.Tool_Domanda = domanda;
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SiNoAnnulla;
            par.Stato = StatoConto.erroreppt;
            conto = par.ToConto(conto);
            return conto;

        }
        public ContrattoConto LeggiBuoniPasto(ContrattoConto conto)
        {
            var par = new ParametriConto(conto);
            String domanda = String.Format(HelperDizionario.domandaLeggi, conto.TotaleDaPagare, par.Codici.Count, par.GetTotale(), par.GetUltimo());
            par.UltimaDomanda = conto.Tool_Domanda = domanda;
            par.Stato = StatoConto.leggi;
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Input;

            conto = par.ToConto(conto);

            return conto;
        }
        public ContrattoConto ChiediAncora(ContrattoConto conto)
        {
            var par = new ParametriConto(conto);
            string buonoPasto = String.Empty;
            //if (conto.Tool_RispostaAScelta.Length > 0)
            //{
            //    buonoPasto = conto.Tool_RispostaAScelta;
            //    par.Codici.Add(buonoPasto);
            //}
            string domanda = String.Format(HelperDizionario.domandaGestione, par.GetUltimo(), par.GetTotale());
            par.UltimaDomanda = conto.Tool_Domanda = domanda;
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SiNoAnnulla;
            par.Stato = StatoConto.gestione;
            conto = par.ToConto(conto);
            return conto;
        }
        public ContrattoConto MassimoBuoni(ContrattoConto conto)
        {
            var par = new ParametriConto(conto);
            string domanda = String.Format(HelperDizionario.domandaMassimobuoni, par.Codici.Count, par.GetTotale());
            par.UltimaDomanda = conto.Tool_Domanda = domanda;
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SceltaTripla;
            conto.Tool_ValoriRisposta = new string[] { "Annulla", "Azzera Buoni", "Chiudi" };
            par.Stato = StatoConto.massimobuoni;
            conto = par.ToConto(conto);
            return conto;
        }
        public ContrattoConto ScriviPagamento(ContrattoConto conto, ParametriConto par)
        {
            Decimal buonipasto = par.GetTotale();
            if (buonipasto == 0)
                return conto;
            string pIvaEmettitore = String.Empty;
            if (Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["IsDecodificaEmettitori"]))
            {

            }
            

            //var pg = conto.Pagamenti; // = new frwbo2.OggettoEntitaCollection<PMBRigaPagamento>();
            foreach (var p in conto.Pagamenti)
            {
                p.Importo = 0;
                p.ListaBuoni = new List<PMBRigaPagamento.BuonoPastoXPagamento>();
            }
            /*
            PMBRigaPagamento pagamento = new PMBRigaPagamento();

            pagamento.Numero = 1;
                    pagamento.Importo = par.GetTotale();
                    pagamento.Tipo = new PMBTipoPagamento() { Categoria = par.CategoriaPagamento, Codice = par.CodicePagamento };
                    pagamento.ListaBuoni = new List<PMBRigaPagamento.BuonoPastoXPagamento>();
            foreach (var pag in par.Codici)
            {
                pagamento.ListaBuoni.Add(new PMBRigaPagamento.BuonoPastoXPagamento() {
                    Buono = pag.Fornitore,
                    BuonoAgenziaPIVA = pIvaEmettitore,// par.Codici.First().CodiceABarre,
                    BuonoValore = 0,
                    BuonoValoreRimborsato = 0,
                    Importo = pag.Valore,
                    Numero = 1
                });
            }
            conto.Pagamenti.Add(pagamento);
            */
            var pag = conto.Pagamenti.Where(p => p.Tipo.Categoria == par.CategoriaPagamento).ToList();
            if(pag.Count > 0 && !par.Codici.Select(c=>c.CodiceABarre).All(c=> pag.First().ListaBuoni.Any(b=>b.Buono == c)))
            {
                foreach(var c in par.Codici)
                {
                    if(!pag.First().ListaBuoni.Any(b=>b.Buono == c.CodiceABarre))
                    {
                        conto.Pagamenti.Where(p => p.Tipo.Categoria == par.CategoriaPagamento && p.Tipo.Codice == par.CodicePagamento).First().ListaBuoni.Add(
                            new PMBRigaPagamento.BuonoPastoXPagamento()
                            { Buono = c.Fornitore, BuonoValoreRimborsato = 0, BuonoAgenziaPIVA = pIvaEmettitore, BuonoValore = 0, Importo = c.Valore, Numero = 1});
                    }
                }
            }
            else
            {
                conto.Pagamenti.Add(
                    new PMBRigaPagamento()
                    {
                        Numero = 2,
                        Importo = par.GetTotale(),
                        Tipo = new PMBTipoPagamento() { Categoria = par.CategoriaPagamento, Codice = par.CodicePagamento },
                        ListaBuoni = new List<PMBRigaPagamento.BuonoPastoXPagamento>() {
                            new PMBRigaPagamento.BuonoPastoXPagamento() {
                                Buono = par.Codici.First().Fornitore,
                                BuonoAgenziaPIVA = pIvaEmettitore,// par.Codici.First().CodiceABarre,
                                BuonoValore = 0,
                                BuonoValoreRimborsato = 0,
                                Importo = par.Codici.First().Valore,
                                Numero = 1
                            }
                        }
                    });

            }
            if(par.GetTotale() <= conto.TotaleDaPagare )
            {
                conto.Pagamenti.First(p => p.Tipo.Categoria.ToLower() == "contanti").Importo = conto.TotaleDaPagare.Value - par.GetTotale();
            }
            else
            {
                conto.Pagamenti.First(p => p.Tipo.Categoria == "BuonoSconto").Importo = par.GetTotale()- conto.TotaleDaPagare.Value;
            }
            int n = 2;
            for(int p=0; p< conto.Pagamenti.Count; p++)
            {
                if(conto.Pagamenti.ElementAt(p).Tipo.Categoria == par.CategoriaPagamento && conto.Pagamenti.ElementAt(p).Tipo.Codice == par.CodicePagamento)
                {
                    conto.Pagamenti.ElementAt(p).Numero = 1;
                }
                else
                {
                    conto.Pagamenti.ElementAt(p).Numero = n;
                    n++;
                }
            }
            #region GESTIONE BUONI PASTO DA FATTURARE

            /*
            if (!Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isGestisciComeIncassati"]))
            #region GESTIONE COME INCASSATI
            {
                
                if (conto.Pagamenti.Count != 0)
                {
                    if (conto.Pagamenti.Where(pg => pg.Tipo.Categoria != par.CategoriaPagamento).Sum(pg => pg.Importo) + buonipasto > conto.TotaleDaPagare)
                    {
                        Decimal residuo = (Decimal)(conto.Pagamenti.Where(pg => pg.Tipo.Categoria != par.CategoriaPagamento).Sum(pg => pg.Importo) + buonipasto - conto.TotaleDaPagare);
                        foreach (var pagamento in conto.Pagamenti)
                        {
                            if (pagamento.Tipo.Categoria == par.CategoriaPagamento)
                            {
                                isBuoniPastoInserito = true;
                                pagamento.Importo = buonipasto;
                                pagamento.Numero = 2;
                                par.CodicePagamento = pagamento.Tipo.Codice;
                            }
                            else
                            {
                                if (residuo > 0)
                                {
                                    if (pagamento.Importo >= residuo)
                                    {
                                        pagamento.Importo = pagamento.Importo - residuo;
                                        residuo = 0;
                                    }
                                    else
                                    {
                                        residuo = residuo - pagamento.Importo;
                                        pagamento.Importo = 0;
                                    }
                                    if (pagamento.Numero == 2)
                                    {
                                        pagamento.Numero = conto.Pagamenti.Where(pg => pg.Tipo.Categoria != par.CategoriaPagamento).Max(b => b.Numero);
                                    }
                                }
                            }
                        }
                    }
                    if (conto.Pagamenti.Any(p => p.Tipo.Categoria == par.CategoriaPagamento))
                    {
                        var p = conto.Pagamenti.First(pg => pg.Tipo.Categoria == par.CategoriaPagamento);
                        p.Importo = par.GetTotale();
                        p.Numero = 2;


                        //return conto;
                    }
                    if (conto.Pagamenti.Count > 1)
                    {
                        foreach (var p in conto.Pagamenti)
                        {
                            if (p.Numero > 1)
                            {

                            }
                        }
                    }
                }
            }
            #endregion
            else
            {
                if (conto.Pagamenti.Count != 0)
                {
                    if (conto.Pagamenti.Where(pg => pg.Tipo.Categoria != par.CategoriaPagamento).Sum(pg => pg.Importo) + buonipasto > conto.TotaleDaPagare)
                    {
                        Decimal residuo = (Decimal)(conto.Pagamenti.Where(pg => pg.Tipo.Codice != par.CodicePagamento).Sum(pg => pg.Importo) + buonipasto - conto.TotaleDaPagare);
                        foreach (var pagamento in conto.Pagamenti)
                        {
                            if (pagamento.Tipo.Codice == par.CodicePagamento)
                            {
                                isBuoniPastoInserito = true;
                                pagamento.Importo = buonipasto;
                                pagamento.Numero = 2;
                            }
                            else
                            {
                                if (residuo > 0)
                                {
                                    if (pagamento.Importo >= residuo)
                                    {
                                        pagamento.Importo = pagamento.Importo - residuo;
                                        residuo = 0;
                                    }
                                    else
                                    {
                                        residuo = residuo - pagamento.Importo;
                                        pagamento.Importo = 0;
                                    }
                                    if (pagamento.Numero == 2)
                                    {
                                        pagamento.Numero = conto.Pagamenti.Where(pg => pg.Tipo.Codice != par.CodicePagamento).Max(b => b.Numero);
                                    }
                                }
                            }
                        }
                    }
                    if (conto.Pagamenti.Any(p => p.Tipo.Codice == par.CodicePagamento))
                    {
                        var p = conto.Pagamenti.First(pg => pg.Tipo.Codice == par.CodicePagamento);
                        p.Importo = par.GetTotale();
                        p.Numero = 2;


                        //return conto;
                    }
                    if (conto.Pagamenti.Count > 1)
                    {
                        foreach (var p in conto.Pagamenti)
                        {
                            if (p.Numero > 1)
                            {

                            }
                        }
                    }
                }
            }

            if (!isBuoniPastoInserito)
            {
                PMBRigaPagamento pag = new PMBRigaPagamento();
                PMBTipoPagamento tipo = new PMBTipoPagamento();
                tipo.Categoria = par.CategoriaPagamento;
                tipo.Codice = par.CodicePagamento;
                pag.Numero = 2;
                pag.Tipo = tipo;
                pag.Importo = par.GetTotale();
                pag.ListaBuoni.Add(new PMBRigaPagamento.BuonoPastoXPagamento() { Buono });
                conto.Pagamenti.Add(pag);
            }

            if(conto.TotaleDaPagare < par.GetTotale() && Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["isAbilitaResto"]))
            {
                conto = CreaBuonoSconto(conto);                
            }
            conto = par.ToConto(conto);*/
            #endregion
            return conto;
        }
        public ContrattoConto AzzeraPagamento(ContrattoConto conto)
        {
            return conto;
        }
        public ContrattoConto ScriviBuonoPasto(ContrattoConto conto)
        {
            ParametriConto par = new ParametriConto(conto);
            if (par.GetTotale() > 0)
            {
                string strSQL = @"  SELECT Pagamento.id 
                                FROM Pagamento 
                                INNER JOIN TipoPagamento ON idTipoPagamento = TipoPagamento.id 
                                WHERE Pagamento.idConto = @param0 AND TipoPagamento.nome = @param1 AND Pagamento.importo = @param2";
                DataTable dt = Blt.BuonoChiaro.BOL.Helper.SqlSelect(strSQL, new object[] { conto.IdGestionale, par.CodicePagamento, par.GetTotale() });
                if (dt.Rows.Count == 1)
                {
                    foreach (var b in par.GetRiepilogo())
                    {
                        string strSQLInsert = @"INSERT INTO BuonoPastoXPagamento
                                        (selettore, utenteCreazione, dataCreazione, utenteUltimaModifica, dataUltimaModifica, timeStmp, idBuonoPasto, idPagamento, numero, isIncassato, importo)
                                        VALUES ( 'MetaBuonoPastoXPagamento', 1, GETDATE(),1,GETDATE(),1,@param0,@param1,@param2,'False',@param3)";
                        string idPagamento = dt.Rows[0][0].ToString();
                        string idBuonoPasto = System.Configuration.ConfigurationManager.AppSettings["idBuonoPasto"];
                        //string numero = "1";
                        var res = Helper.SqlInsert(strSQLInsert, new object[] { idBuonoPasto, idPagamento, b.CodiceABarre, b.Valore });
                        if (res != 1)
                        {
                            conto.Tool_Domanda = "Errore in registrazione Buono Pasto";
                            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                            break;
                        }
                    }
                }
            }
            return conto;
        }
        public ContrattoConto CreaBuonoSconto(ContrattoConto conto)
        {
            ParametriConto par = new ParametriConto(conto);
            ContrattoBuonoSconto buono = new ContrattoBuonoSconto();
            buono.Importo = par.GetTotale() - conto.TotaleDaPagare.Value;
            buono.Codice = conto.IdGestionale.Value;
            buono.DataCreazione = DateTime.Now;
            buono.DataUltimaModifica = DateTime.Now;
            //Spire.Barcode.BarcodeSettings bcsett = new Spire.Barcode.BarcodeSettings();
            //bcsett.Type = Spire.Barcode.BarCodeType.EAN13;
            //bcsett.UseChecksum = Spire.Barcode.CheckSumMode.ForceEnable;
            //bcsett.Data = "95" + conto.IdGestionale.Value.ToString().PadLeft(10, '0');
            
            //Spire.Barcode.BarCodeGenerator bc = new Spire.Barcode.BarCodeGenerator(bcsett);
            //bc.ToString();
            string code = "95" + conto.IdGestionale.Value.ToString().PadLeft(10, '0');
            buono.CodiceABarre = code + _checksum_ean13(code).ToString();
            buono.Descrizione = "Tallon";
            buono.Emissione = conto.DataGestione.Value;
            //buono.Scadenza = conto.DataGestione.Value.AddMonths(6);
            buono.TipoEnum = TipoBuonoSconto.ScontoEuro;
            conto.BuonoDaEmettere = buono;
            return conto;

        }
        public ContrattoConto ErroreApriTicket(ContrattoConto conto, TicketOpenRes response)
        {
            conto.Tool_BloccaChiusura = true;
            conto.Tool_Domanda = String.Format(HelperDizionario.domandaErroreapertura, response.ERRMSG);
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SiNo;
            ParametriConto par = new ParametriConto(conto);
            par.Stato = StatoConto.erroreapertura;
            conto = par.ToConto(conto);
            return conto;
        }
        public ContrattoConto ErroreValidaTicket(ContrattoConto conto, TicketValidationRes response)
        {
            conto.Tool_BloccaChiusura = true;
            if (response.Rows.Count > 0)
                conto.Tool_Domanda = String.Format(HelperDizionario.domanda("errorevalidazione"), response.Rows.FirstOrDefault().BC, response.ERRMSG);
            else
                conto.Tool_Domanda = String.Format(HelperDizionario.domanda("errorevalidazione"), String.Empty, response.ERRMSG);
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SiNo;
            conto.Tool_IdTastoCustom = "COMANDASMART";
            //conto.Tool_ValoriRisposta = Enum.GetNames(typeof(enumErroreValidazioneRisposta));
            ParametriConto par = new ParametriConto(conto);
            par.Stato = StatoConto.errorevalidazione;
            conto = par.ToConto(conto);
            return conto;
        }
        public Boolean ControllaSuperatoMassimoImporto(ContrattoConto conto,ParametriConto par, out ContrattoConto Rconto)
        {
            if (conto.TotaleDaPagare <= par.GetTotale())
            {
                conto.Tool_Domanda = HelperDizionario.domanda("massimoraggiunto", par.Codici.Count, par.GetTotale());
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                par.Stato = StatoConto.fine;
                conto = par.ToConto(conto);
                Rconto = ScriviPagamento(conto, par);
                return true;
            }
            else
            {
                Rconto = conto;
                return false;
            }

        }
        public Boolean ControllaSuperatoMassimoNumero(ContrattoConto conto, ParametriConto par, out ContrattoConto Rconto)
        {
            if (Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxNumeroBuoni"]) <= par.Codici.Count)
            {
                conto.Tool_Domanda = HelperDizionario.domanda("massimoraggiunto", conto.TotaleDaPagare, par.GetTotale());
                conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
                par.Stato = StatoConto.fine;
                conto = par.ToConto(conto);
                Rconto = ScriviPagamento(conto, par);
                return true;
            }
            else
            {
                Rconto = conto;
                return false;
            }
        }
        public ContrattoConto ErroreAnnullaTicket(ContrattoConto conto, TicketVoidRes response)
        {
            conto.Tool_BloccaChiusura = true;
            conto.Tool_Domanda = String.Format(HelperDizionario.domanda("erroreannullamento"), response.Rows.First().BC, response.ERRMSG);
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SceltaTripla;
            conto.Tool_ValoriRisposta = HelperDizionario.risposte("erroreannullamento").Select(v => v.Value).ToArray();// Enum.GetNames(typeof(enumErroreValidazioneRisposta));
            ParametriConto par = new ParametriConto(conto);
            par.Stato = StatoConto.erroreannullamento;
            conto = par.ToConto(conto);
            return conto;
        }
        public ContrattoConto ErroreAnnullaTicketResto(ContrattoConto conto, TicketVoidRes response)
        {
            conto.Tool_BloccaChiusura = true;
            conto.Tool_Domanda = String.Format(HelperDizionario.domanda("erroreannullamentoresto"), response.Rows.First().BC, response.ERRMSG);
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.Info;
            ParametriConto par = new ParametriConto(conto);
            par.Stato = StatoConto.fine;
            conto = par.ToConto(conto);
            return conto;
        }
        public ContrattoConto ErroreChiudiTicket(ContrattoConto conto, TicketOpenRes response)
        {
            conto.Tool_BloccaChiusura = true;
            conto.Tool_Domanda = String.Format(HelperDizionario.domandaErrorechiusura, response.ERRMSG);
            conto.Tool_TipoDomanda = EnumToolTipoDomanda.SceltaTripla;
            conto.Tool_ValoriRisposta = Enum.GetNames(typeof(enumErroreValidazioneRisposta));
            ParametriConto par = new ParametriConto(conto);
            par.Stato = StatoConto.erroreapertura;
            conto = par.ToConto(conto);
            return conto;
        }
        static int _checksum_ean13(String data)
        {
            // Test string for correct length
            if (data.Length != 12 && data.Length != 13)
                return -1;

            // Test string for being numeric
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < 0x30 || data[i] > 0x39)
                    return -1;
            }

            int sum = 0;

            for (int i = 11; i >= 0; i--)
            {
                int digit = data[i] - 0x30;
                if ((i & 0x01) == 1)
                    sum += digit;
                else
                    sum += digit * 3;
            }
            int mod = sum % 10;
            return mod == 0 ? 0 : 10 - mod;
        }
    }
}