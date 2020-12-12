using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Microsoft.Win32;


namespace LogSync
{
    /// <summary>
    /// Just an adapter to hide some of the System.Configuration cumbersomeness.
    /// </summary>
    public interface IConfigAdapter
    {
        string Read(string key);
        void Write(string key, string value);
        void WriteDate(string key, DateTime value);
    }

    /// <summary>
    /// XML file backed config.
    /// </summary>
    public class XmlConfigAdapter : IConfigAdapter
    {
        private Configuration config;

        public XmlConfigAdapter()
        {
            // .net core apps seem to run in a temp folder (at least a when publishing as a single file?)
            // this means it creates a fresh config every time
            // Application.ExecutablePath is the temp file path
            // Process.GetCurrentProcess().MainModule.FileName is the real path
            config = ConfigurationManager.OpenExeConfiguration(Process.GetCurrentProcess().MainModule.FileName);
        }

        public void Write(string key, string value)
        {
            config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, value);
            config.Save(ConfigurationSaveMode.Modified);
        }

        public string Read(string key)
        {
            return config.AppSettings.Settings[key]?.Value;
        }

        public void WriteDate(string key, DateTime value)
        {
            Write(key, value.ToString("o"));
        }
    }

    /// <summary>
    /// Windows Registry backed config.
    /// </summary>
    public class RegConfigAdapter : IConfigAdapter
    {
        private readonly RegistryKey reg;

        public RegConfigAdapter()
        {
            //var builder = new ConfigurationBuilder();
            //config = builder.AddJsonFile("appsettings.json").Build();
            reg = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\RaidLootParser");
        }

        public string Read(string key)
        {
            return reg.GetValue(key)?.ToString();
        }

        public void Write(string key, string value)
        {
            reg.SetValue(key, value);
        }

        public void WriteDate(string key, DateTime value)
        {
            reg.SetValue(key, value.ToString("o"));
        }
    }

}