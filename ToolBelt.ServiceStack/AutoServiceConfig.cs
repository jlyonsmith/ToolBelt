using System;
using System.Configuration;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.Logging;
using ToolBelt;
using System.IO;
using System.Collections.Specialized;

namespace ToolBelt.ServiceStack
{
    public abstract class AutoServiceConfig : IProcessCommandLine, IProcessAppSettings
    {
        ILog log = LogManager.GetLogger(typeof(AutoServiceConfig));

        #region IProcessCommandLine

        public void ProcessCommandLine(string[] args)
        {
            var parser = new CommandLineParser(this);
                
            parser.ParseAndSetTarget(args);
        }

        #endregion

        #region IProcessAppSettings

        public void ProcessAppSettings(NameValueCollection collection)
        {
            var parser = new AppSettingsParser(this, flags: AppSettingsParserFlags.CaseSensitive);

            parser.ParseAndSetTarget(ConfigurationManager.AppSettings);

            var appSettingsKeys = ConfigurationManager.AppSettings.AllKeys;

            // Check that all the app.config settings have a property and warn for those that don't
            foreach (var key in appSettingsKeys)
            {
                if (!key.StartsWith("servicestack:") && parser.ArgumentCollection.FirstOrDefault(a => a.Name == key) == null)
                {
                    log.Warn("{0}.config contains unused setting '{1}'".CultureFormat(
                        Path.GetFileName(Assembly.GetEntryAssembly().Location), key));
                }
            }

            // Check that all arguments have an app.config setting and warn for those that don't
            foreach (var arg in parser.ArgumentCollection)
            {
                if (appSettingsKeys.FirstOrDefault(k => k == arg.Name) == null)
                {
                    log.Warn("{0}.config does not contain setting for '{1}'".CultureFormat(
                        Path.GetFileName(Assembly.GetEntryAssembly().Location), arg.Name));
                }
            }
        }

        #endregion

        #region Methods
        public void Log()
        {
            var sb = new StringBuilder();

            foreach (var propInfo in this.GetType().GetProperties())
            {
                if (propInfo.GetCustomAttribute<AppSettingsArgumentAttribute>() == null)
                    continue;

                var value = propInfo.GetValue(this);
                var enumerable = value as IEnumerable<object>;

                if (enumerable != null)
                {
                    var list = String.Join<object>(",", enumerable);

                    sb.AppendFormat("\n  {0}={1}", propInfo.Name, list);
                }
                else
                {
                    sb.AppendFormat("\n  {0}={1}", propInfo.Name, value);
                }
            }

            log.Info("Service configuration:" + sb.ToString());
        }
        #endregion
    }
}

