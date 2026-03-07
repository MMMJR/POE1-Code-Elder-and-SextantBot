using System.Threading.Tasks;
using System.Windows.Controls;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using log4net;
using MmmjrBot.Lib.Global;
using settings = MmmjrBot.Lib.Settings;

namespace MmmjrBot.Lib
{
    public class EXtensions : IContent, IUrlProvider
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();

        public static void AbandonCurrentArea()
        {
            var botName = BotManager.Current.Name;
            if (botName.Contains("QuestBot"))
            {
                Travel.RequestNewInstance(World.CurrentArea);
            }
        }

        #region Unused interface methods

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public void Initialize()
        {
            Log.DebugFormat("[EXtensions] Initialize");
        }

        public void Deinitialize()
        {
            Log.DebugFormat("[EXtensions] Deinitialize");
        }

        public string Name => "EXtensions";
        public string Description => "Global logic used by bot bases.";
        public string Author => "ExVault";
        public string Version => "1.3";
        public JsonSettings Settings => settings.Instance;
        public UserControl Control => null;
        public string Url => "";

        #endregion
    }
}