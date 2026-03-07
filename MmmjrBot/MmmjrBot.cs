using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using log4net;

using Message = DreamPoeBot.Loki.Bot.Message;
using UserControl = System.Windows.Controls.UserControl;
using MmmjrBot.Lib.CommonTasks;
using System;
using MmmjrBot.Class;
using MmmjrBot.MapperBot;

public enum MapperBotState
{
    GetItems = 1,
    OpeningMap = 2,
    Mapping = 3,
    LeavingArea = 4,
    StoringItems = 5
}

namespace MmmjrBot
{
    public class MmmjrBot : IBot
    {
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private MmmjrGUI _gui;
        private Coroutine _coroutine;

        public static TaskManager _taskManager = new TaskManager();
        internal static bool IsOnRun;
        public static Stopwatch RequestPartySw = Stopwatch.StartNew();
        private OverlayWindow _overlay = new OverlayWindow(LokiPoe.ClientWindowHandle);
        private ChatParser _chatParser = new ChatParser();
        private Stopwatch _chatSw = Stopwatch.StartNew();
        public static bool CWDTTrigger;

        public static MapperBotState mapbot_state = new MapperBotState();
        public static bool[] ShaperMapperCompletion = new bool[4];

        public void Start()
        {
            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            Explorer.CurrentDelegate = user => CombatAreaCache.Current.Explorer.BasicExplorer;

            ComplexExplorer.ResetSettingsProviders();
            ComplexExplorer.AddSettingsProvider("MmmjrBot", MapBotExploration, ProviderPriority.Low);
            GlobalLog.IsDebugEnabled = false;
            CWDTTrigger = false;

            // Cache all bound keys.
            LokiPoe.Input.Binding.Update();

            // Since this bot will be performing client actions, we need to enable the process hook manager.
            LokiPoe.ProcessHookManager.Enable();
            CombatAreaCache.reset = false;

            _coroutine = null;

            ExilePather.BlockLockedDoors = FeatureEnum.Disabled;
            ExilePather.BlockLockedTempleDoors = FeatureEnum.Disabled;
            ExilePather.BlockTrialOfAscendancy = FeatureEnum.Disabled;

            ExilePather.Reload();

            _taskManager.Reset();

            AutoLogin.AutoLogin.Start();

            AddTasks();

            ExileRoutine.Initialize();
            MmmjrRoutine.Initialize();
            Events.Start();
            PluginManager.Start();
            RoutineManager.Start();
            _taskManager.Start();

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }

        }

        public static void OnChangeEnabledBots(int botId, bool enabled)
        {
            _taskManager.Reset();

            if(botId == 1 && enabled) //SextantBot
            {
                _taskManager.Add(new SextantTask());
            }
            else if(botId == 2 && enabled) //FarmingBot
            {
                mapbot_state = MapperBotState.OpeningMap;
                _taskManager.Add(new FarmingTask());
            }
        }

        public void Tick()
        {
            if (MmmjrBotSettings.Instance.EnableAutoLogin)
            {
                AutoLogin.AutoLogin.Tick();
            }

            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            ExilePather.Reload();
            Events.Tick();
            CombatAreaCache.Tick();
            _taskManager.Tick();
            PluginManager.Tick();
            RoutineManager.Tick();
            PlayerMoverManager.Tick();

            if (_chatSw.ElapsedMilliseconds > 250)
            {
                _chatParser.Update();
                _chatSw.Restart();
            }
            // Check to see if the coroutine is finished. If it is, stop the bot.
            if (_coroutine.IsFinished)
            {
                BotManager.Stop();
                return;
            }

            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        public void Stop()
        {
            _taskManager.Stop();
            PluginManager.Stop();
            RoutineManager.Stop();

            // When the bot is stopped, we want to remove the process hook manager.
            LokiPoe.ProcessHookManager.Disable();

            // Cleanup the coroutine.
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }

        private async Task MainCoroutine()
        {
            while (true)
            {
                if (LokiPoe.IsInLoginScreen)
                {
                    if(MmmjrBotSettings.Instance.EnableAutoLogin)
                    {
                        await AutoLogin.AutoLogin.Login();
                    }
                    else
                    {
                        // Offload auto login logic to a plugin.
                        var logic = new Logic("hook_login_screen", this);
                        foreach (var plugin in PluginManager.EnabledPlugins)
                        {
                            if (await plugin.Logic(logic) == LogicResult.Provided)
                                break;
                        }
                    }
                }
                else if (LokiPoe.IsInCharacterSelectionScreen)
                {
                    if (MmmjrBotSettings.Instance.EnableAutoLogin)
                    {
                        await AutoLogin.AutoLogin.CharacterSelection();
                    }
                    else
                    {
                        // Offload character selection logic to a plugin.
                        var logic = new Logic("hook_character_selection", this);
                        foreach (var plugin in PluginManager.EnabledPlugins)
                        {
                            if (await plugin.Logic(logic) == LogicResult.Provided)
                                break;
                        }
                    }
                }
                else if (LokiPoe.IsInGame)
                {
                    // To make things consistent, we once again allow user coorutine logic to preempt the bot base coroutine logic.
                    // This was supported to a degree in 2.6, and in general with our bot bases. Technically, this probably should
                    // be at the top of the while loop, but since the bot bases offload two sets of logic to plugins this way, this
                    // hook is being placed here.
                    var hooked = false;
                    var logic = new Logic("hook_ingame", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            hooked = true;
                            break;
                        }
                    }

                    if (!hooked)
                    {
                        // Wait for game pause
                        if (LokiPoe.InstanceInfo.IsGamePaused)
                        {
                            Log.Debug("Waiting for game pause");
                        }
                        // Resurrect character if it is dead
                        else if (LokiPoe.Me.IsDead)
                        {
                            await ResurrectionLogic.Execute();
                            if(mapbot_state == MapperBotState.Mapping && MapExplorationTask.MapCompleted)
                            {
                                FarmingTask.FinalDeath = true;
                            }
                        }
                        // What the bot does now is up to the registered tasks.
                        else
                        {
                            await _taskManager.Run(TaskGroup.Enabled, RunBehavior.UntilHandled);
                        }
                    }
                }
                else
                {
                    // Most likely in a loading screen, which will cause us to block on the executor, 
                    // but just in case we hit something else that would cause us to execute...
                    await Coroutine.Sleep(1000);
                    continue;
                }

                // End of the tick.
                await Coroutine.Yield();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            var id = message.Id;

            if (id == BotStructure.GetTaskManagerMessage)
            {
                message.AddOutput(this, _taskManager);
                handled = true;
            }
            else if (id == Messages.GetIsOnRun)
            {
                message.AddOutput(this, IsOnRun);
                handled = true;
            }
            else if (id == Messages.SetIsOnRun)
            {
                var value = message.GetInput<bool>();
                GlobalLog.Info($"[MmmjrBot] SetIsOnRun: {value}");
                IsOnRun = value;
                handled = true;
            }
            else if (message.Id == Events.Messages.AreaChanged)
            {
                handled = true;
            }

            Events.FireEventsFromMessage(message);

            var res = _taskManager.SendMessage(TaskGroup.Enabled, message);
            if (res == MessageResult.Processed)
                handled = true;

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }


        public TaskManager GetTaskManager()
        {
            return _taskManager;
        }

        public async void Initialize()
        {
            BotManager.OnBotChanged += BotManagerOnOnBotChanged;
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            _overlay.Start();
        }

        public void Deinitialize()
        {
            BotManager.OnBotChanged -= BotManagerOnOnBotChanged;
        }

        private void BotManagerOnOnBotChanged(object sender, BotChangedEventArgs botChangedEventArgs)
        {
            if (botChangedEventArgs.New == this)
            {
                ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            }
        }

        private void AddTasks()
        {
        }

        private static ExplorationSettings MapBotExploration()
        {
            if (!World.CurrentArea.IsMap)
                return new ExplorationSettings();

            OnNewMapEnter();

            return new ExplorationSettings(tileSeenRadius: TileSeenRadius);
        }

        private static void OnNewMapEnter()
        {
            var areaName = World.CurrentArea.Name;
            Log.Info($"[MmmjrBot] New map has been entered: {areaName}.");
            IsOnRun = true;
            Utility.BroadcastMessage(null, Messages.NewMapEntered, areaName);
        }

        private static int TileSeenRadius
        {
            get
            {
                if (TileSeenDict.TryGetValue(World.CurrentArea.Name, out int radius))
                    return radius;

                return ExplorationSettings.DefaultTileSeenRadius;
            }
        }

        private static readonly Dictionary<string, int> TileSeenDict = new Dictionary<string, int>
        {
            [MapNames.MaoKun] = 3,
            [MapNames.Arena] = 3,
            [MapNames.CastleRuins] = 3,
            [MapNames.UndergroundRiver] = 3,
            [MapNames.TropicalIsland] = 3,
            [MapNames.Beach] = 5,
            [MapNames.Strand] = 5,
            [MapNames.Port] = 5,
            [MapNames.Alleyways] = 5,
            [MapNames.Phantasmagoria] = 5,
            [MapNames.Wharf] = 5,
            [MapNames.Cemetery] = 5,
            [MapNames.MineralPools] = 5,
            [MapNames.Temple] = 5,
            [MapNames.Malformation] = 5,
        };

        public static class Messages
        {
            public const string NewMapEntered = "MB_new_map_entered_event";
            public const string MapFinished = "MB_map_finished_event";
            public const string MapTrialEntered = "MB_map_trial_entered_event";
            public const string GetIsOnRun = "MB_get_is_on_run";
            public const string SetIsOnRun = "MB_set_is_on_run";
        }

        public string Name => "MmmjrBot";
        public string Author => "MMMJR, thanks to Alcor75 for hardwork in DPB";
        public string Description => "Sextant Bot.";
        public string Version => "0.1.0.3";
        public UserControl Control => _gui ?? (_gui = new MmmjrGUI());
        public JsonSettings Settings => MmmjrBotSettings.Instance;
        public override string ToString() => $"{Name}: {Description}";
    }
}
