using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PMessageBox.Contract;

namespace HostPlugin
{
    public interface IPlugin
    {
        Messaggio RiceviMessaggio(Messaggio messaggio);
    }

    /// <summary>
    /// Dichiarazione di un plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class Plugin : Attribute
    {
        public string Nome { get; set; }
        public bool Attivo { get; set; }
        //public Type TypeClass { get; set; }
        public IPlugin Istanza { get; set; }
        public List<CanalePlugin> CanaliAbilitati { get; private set; }

        /// <summary>
        /// Dichiarazione di un plugin
        /// </summary>
        public Plugin()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Dichiarazione di un plugin
        /// </summary>
        /// <param name="nome">Nome</param>
        public Plugin(string nome)
        {
            Nome = nome;
            Attivo = false;
            Istanza = null;
            CanaliAbilitati = new List<CanalePlugin>();
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class CanalePlugin : Attribute
    {
        public string NomeCanale { get; set; }
        public bool AbilitaRichiestaDati { get; set; }
        public bool AbilitaInserimentoDati { get; set; }
        public bool AbilitaCancellazioneDati { get; set; }
        public bool AbilitaComandoImmediato { get; set; }

        public CanalePlugin(string nomeCanale, bool abilitaRichiestaDati, bool abilitaInserimentoDati, bool abilitaCancellazioneDati, bool abilitaComandoImmediato)
        {
            NomeCanale = nomeCanale;
            AbilitaCancellazioneDati = abilitaCancellazioneDati;
            AbilitaComandoImmediato = abilitaComandoImmediato;
            AbilitaInserimentoDati = abilitaInserimentoDati;
            AbilitaRichiestaDati = abilitaRichiestaDati;
        }
    }
}
