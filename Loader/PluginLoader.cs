using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;

namespace PluginLoader
{
    public class PluginLoader : IPlugin
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public PluginLoader()
        {
            Load();
        }

        private void Load()
        {
            var pluginLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");
            Log.Info("Plugins Path: " + pluginLocation);
            var plugins = Directory.GetFiles(pluginLocation, "*.dll");
            Log.Info($"[PluginLoader] Detected plugins: " +
                     "\n-----------------" +
                     $"\n\t{string.Join("\n\t", plugins)}" +
                     "\n-----------------");
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            foreach (var plugin in plugins)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(plugin);
                    var assemblyType = assembly.GetTypes().FirstOrDefault(t => t.Name == assembly.GetName().Name);
                    var instance = assembly.CreateInstance(assemblyType.ToString(), true);
                    if (instance is IAuthored authored)
                    {
                        Log.Info($"[[PluginLoader] Loaded: {authored.Name} v{authored.Version} <{authored.Author}> - {authored.Description}");
                    }
                    else
                    {
                        Log.Error($"[PluginLoader] {plugin} not authored");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[PluginLoader] error loading {plugin} \n{ex}");
                    if (ex is ReflectionTypeLoadException e)
                    {
                        foreach (var loaderException in e.LoaderExceptions)
                        {
                            Log.Error($"[PluginLoader] loaderException \n{loaderException}");
                        }
                    }
                }
            }
        }
        
        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == new AssemblyName(args.Name).Name);
        }

        #region Unused
        
        public void Initialize()
        {
        }
        public void Deinitialize()
        {
        }
        public void Disable()
        {
        }
        public void Enable()
        {
        }
        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }
        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
        public UserControl Control => null;
        public JsonSettings Settings => null;

        #endregion

        #region Author

        public string Author => "Lajt";
        public string Description => "PluginLoader";
        public string Name => "PluginLoader";
        public string Version => "1.0";

        #endregion
    }
}