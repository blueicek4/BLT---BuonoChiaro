using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Blt.BuonoChiaro.BOL
{
    public class BltConfig
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AppSettingsSection Settings
        {
            get
            {
                return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BltConfig).Assembly.Location), "config")).AppSettings;
            }
        }

        public BltConfig()
        {
            Configuration config;

            string exeConfigPath = typeof(ParametriConto).Assembly.Location;
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exeConfigPath), "buonochiaro.config");
            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            else if (System.IO.File.Exists(exeConfigPath + ".setup" + ".config"))
            {
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = exeConfigPath + ".setup" + ".config";
                config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
                config.SaveAs(exeConfigPath + ".config");
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
                log.DebugFormat("File Config caricato da file di Setup");
            }

        }
    }
    public class BltSetup
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AppSettingsSection Settings
        {
            get
            {
                return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BltSetup).Assembly.Location), "setup")).AppSettings;
            }
        }

        public ConnectionStringsSection ConnectionStrings
        {
            get
            {
                return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BltSetup).Assembly.Location), "setup")).ConnectionStrings;
            }
        }
    }
    public class BltQuery
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AppSettingsSection Settings
        {
            get
            {
                return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BltQuery).Assembly.Location), "query")).AppSettings;
            }
        }

        public ConnectionStringsSection ConnectionStrings
        {
            get
            {
                return System.Configuration.ConfigurationManager.OpenExeConfiguration(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BltQuery).Assembly.Location), "query")).ConnectionStrings;
            }
        }
    }


}
