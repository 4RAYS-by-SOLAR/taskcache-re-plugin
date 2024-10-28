using RegistryPluginBase.Classes;
using RegistryPluginBase.Interfaces;
using System.ComponentModel;
using System;
using Registry.Abstractions;
using System.Collections.Generic;

namespace TaskCachePlugin
{
    public class TaskCachePlugin : IRegistryPluginGrid
    {
        private readonly BindingList<ValuesOut> _values;

        public TaskCachePlugin()
        {
            _values = new BindingList<ValuesOut>();
            Errors = new List<string>();
        }

        public string InternalGuid => "00000000-0000-0000-1111-347261797301";

        public List<string> KeyPaths => new List<string>(new[]
        {
            @"Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache"
        });

        public string ValueName => null;
        public string AlertMessage { get; private set; } = null;
        public RegistryPluginType.PluginType PluginType => RegistryPluginType.PluginType.Grid;
        public string Author => "SOLAR 4RAYS";
        public string Email => "4rays@rt-solar.ru";
        public string Phone => string.Empty;
        public string PluginName => "4RAYS TaskCache";
        public string ShortDescription => "Displays values from TaskCache keys with Actions-subkey parse";
        public string LongDescription => ShortDescription;

        public double Version => 1;
        public List<string> Errors { get; }

        public void ProcessValues(RegistryKey key)
        {
            _values.Clear();
            Errors.Clear();

            try
            {
                var result = TaskCacheKeyParser.Parse(key);
                foreach (var item in result)
                    _values.Add(new ValuesOut(item));
            }
            catch (Exception ex)
            {
                Errors.Add($"Error processing TaskCache: {ex.Message}");
            }

            if (Errors.Count > 0)
            {
                AlertMessage = "Errors detected. See Errors information in lower right corner of plugin window";
            }
        }

        public IBindingList Values => _values;
    }
}