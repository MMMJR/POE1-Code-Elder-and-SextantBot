using System;
using System.Threading.Tasks;
using log4net;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archnemesis.Models;
using DreamPoeBot.Loki.Coroutine;
using Newtonsoft.Json;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;

namespace Archnemesis
{
    public class Archnemesis : IPlugin, ITask
    {
        // Adjust these to your needs
        #region Configuration

        private static bool Verbose => ArchnemesisSettings.Instance.VerboseLogging;
        
        // Name of the task we add ourself in front of (see Start())
        const string AddBeforeTaskName = "LootItemTask";

        // Target recipes
        // "Mathematically proven"
        static readonly List<string> SampleTargetRecipe = new List<string>() { "Innocence-touched", "Brine King-touched", "Kitava-touched", "Abberath-touched" };
        // Grimro
        static readonly List<string> GrimroTargetRecipe = new List<string>() { "Mirror Image", "Treant Horde", "Assassin", "Rejuvenating" };

        private static List<ArchnemesisMod> TargetRecipe => 
            ArchnemesisSettings.Instance.TargetRecipe.Select(GetRecipe).ToList();
        private static List<ArchnemesisMod> TargetDisposableComponents =>
            ArchnemesisSettings.Instance.TargetDisposableComponents.Select(GetRecipe).ToList();

        // Todo: Implement extra recipe for disposable components of tier 2+
        //private static List<ArchnemesisMod> DisposableRecipe1 => new List<ArchnemesisMod>
        //{
        //    GetRecipe("")
        //};

        private static List<ComponentWrapper> DisposableComponents = new List<ComponentWrapper>();

        #endregion
        
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private Gui _instance;
        private static List<ArchnemesisMod> _modDB;

        private static readonly Interval ScanInterval = new Interval(1000);
        
        private static List<string> _modsToPickup = new List<string>();
        private bool _shouldUpdate = false;
        

        #region Recipe Altars

        // Call this function in your pickup evaluation
        public static bool ShouldPickup(string name)
        {
            AddModDropStats(name);
            return _modsToPickup.Contains(name);
        }

        private static void AddModDropStats(string name)
        {
            var area = LokiPoe.CurrentWorldArea.Name;
            if (!ArchnemesisSettings.Instance.ItemFinds.TryGetValue(name, out var locations))
            {
                locations = new Dictionary<string, int>();
            }

            if (!locations.ContainsKey(area))
                locations[area] = 0;

            locations[area]++;
            ArchnemesisSettings.Instance.ItemFinds[name] = locations;
            ArchnemesisSettings.Instance.Save();
        }

        public static ArchnemesisMod GetRecipe(string name)
        {
            return _modDB.Find(m => m.Name == name);
        }
        
        private static bool ModInInv(string modName)
        {
            return LokiPoe.InstanceInfo.Archnemesis.AvailableArchnemesisMods.Exists(invComp => invComp.ArchnemesisMod.Mod.DisplayName == modName);
        }

        private static int ModsInInv(string modName)
        {
            return LokiPoe.InstanceInfo.Archnemesis.AvailableArchnemesisMods.Count(invComp => invComp.ArchnemesisMod.Mod.DisplayName == modName);
        }

        public static bool IsT1(LokiPoe.InstanceInfo.Archnemesis.ArchnemesisMods i)
        {
            var mod = GetRecipe(i.ArchnemesisMod.Mod.DisplayName);
            return mod.ComponentNames.Count == 0;
        }
        
        private static void UpdateRecipes()
        {
            // Run completable check
            if (CachedData.Completable == null) 
            {
                // Update completable status for main recipe.
                CachedData.Completable = CompletableRecipe(TargetRecipe);

                if (CachedData.Completable == null && TargetDisposableComponents?.Any(dc => dc.Tier() > 1) == true)
                {
                    CachedData.Completable = CompletableRecipe(TargetDisposableComponents.Where(dc => dc.Tier() > 1).ToList());
                }
            }
            else if (CachedData.CanFitMore)
            {
                // Todo: Refactor and move this.
                if (Verbose)
                {
                    Log.Debug($"[Archnemesis] Can fit more components after completed recipe.");
                }

                // We have finished the main recipe and should update secondary completable status.
                if (CachedData.Completable2?.Any() != true && CachedData.UsedComponents.Count == 2)
                {
                    var log = "";

                    // We have room for another 2 component recipe but we need to check if we have a completable one not using
                    // any of the already used components this instance.

                    // First check any of the planned components.
                    var plannedRecipesWithComponentsThatAreNotUsedThisInstance =
                        TargetRecipe.Where(tr =>
                            tr.ComponentNames.Count == 2 &&
                            tr.ComponentNames.All(ModInInv) &&
                            !tr.ComponentNames.Intersect(CachedData.UsedComponents).Any());
                    if (plannedRecipesWithComponentsThatAreNotUsedThisInstance.Any())
                    {
                        CachedData.Completable2 = 
                            plannedRecipesWithComponentsThatAreNotUsedThisInstance.First().ComponentNames;

                        log = $"[Archnemesis] Can fit 2 more components after completed recipe. " +
                                $"Adding recipe {string.Join(", ", CachedData.Completable2)} ";
                    }
                    else
                    {
                        log = $"[Archnemesis] Can fit 2 more components after completed recipe. " +
                                $"Did not find any suitable recipe.";
                    }

                    if (Verbose)
                    {
                        Log.Debug(log);
                    }
                }
                
                if (CachedData.Completable2?.Any() != true)
                {
                    var remainingSlots = 4 - CachedData.UsedComponents.Count;

                    var log = $"[Archnemesis] Can fit {remainingSlots} more components after completed recipe but no disposable components found.";

                    // Try to find any disposable component to use.
                    // Todo: Implement list of disposable components to pickup and priority.
                    var availableDisposableComponents = 
                        DisposableComponents.Where(dc => !CachedData.UsedComponents.Any(uc => uc == dc.Name));
                    if (availableDisposableComponents.Any())
                    {
                        // Prioritize certain components.
                        availableDisposableComponents = availableDisposableComponents
                            .OrderByDescending(dc => dc.Name == "Opulent")
                            .ThenByDescending(dc => dc.Tier)
                            .ThenByDescending(dc => dc.CurrentQuantity);
                        CachedData.Completable2 = availableDisposableComponents.Take(remainingSlots).Select(dc => dc.Name).ToList();

                        log = $"[Archnemesis] Can fit {remainingSlots} more components after completed recipe. " +
                                $"Adding disposable {string.Join(", ", CachedData.Completable2)} ";
                    }

                    if (Verbose)
                    {
                        Log.Debug(log);
                    }
                }
            }

            // re-calc needed items
            UpdateNeededComps();
        }
        
        [Obsolete]
        private static List<string> CompletableRecipe(List<string> components)
        {
            if (components.All(ModInInv))
                return new List<string>(components);

            foreach (var component in components)
            {
                // do not build components we already own
                if (ModInInv(component))
                    continue;
            
                List<string> completable;
                if ((completable = CompletableRecipe(GetRecipe(component).ComponentNames)) != null && completable.Any())
                {
                    return completable;
                }
            }

            return null;
        }

        private static List<string> CompletableRecipe(List<ArchnemesisMod> components)
        {
            // If we have all components we can complete the recipe.
            if (components.All(c => ModInInv(c.Name)))
            {
                return components.Select(c => c.Name).ToList();
            }

            // Order them so that we consider the components we have the least of first (In case of shared sub components).
            components = components.OrderBy(c => ModsInInv(c.Name)).ToList();

            // We don't have all the components in the inventory so we will see if we can build any of them.
            foreach (var component in components)
            {
                // Do not build the component if we are already at max.
                if (ModsInInv(component.Name) >= ArchnemesisSettings.Instance.MaxT2Reserve + 1)
                    continue;

                // We consider if we can complete this component.
                var completable = CompletableRecipe(component.Children());
                if (completable != null && completable.Any())
                {
                    return completable;
                }
            }

            // No completable recipes found.
            return null;
        }

        /// <summary>
        /// Wrapper to help keep track of a components role.
        /// </summary>
        private class ComponentWrapper
        {
            public ComponentWrapper(ArchnemesisMod component)
            {
                Name = component.Name;
                Tier = component.Tier();
            }

            public string Name { get; set; }
            /// <summary>
            /// This component is disposable and will be used after other recipes if possible.
            /// </summary>
            public bool Disposable { get; set; }
            /// <summary>
            /// This is a component of a disposable parent and will be kept for building Disposable mods.
            /// </summary>
            public bool DisposableComponent { get; set; }

            public int Tier { get; set; }

            public int RequiredQuantity { get; set; }
            public int MaxQuantity { get; set; }
            public int CurrentQuantity { get; set; }
        }

        private static void UpdateNeededComps()
        {
            _modsToPickup.Clear();
            DisposableComponents.Clear();

            List<ComponentWrapper> _components = new List<ComponentWrapper>();

            var inventory = LokiPoe.InstanceInfo.Archnemesis.AvailableArchnemesisMods;
            var freeSpace = /*inventory.Capacity for some reason this retured 32 when count was low !?*/64 - inventory.Count;

            // Identify all components.
            foreach (var component in TargetRecipe)
            {
                _components.Add(new ComponentWrapper(component));
                _components.AddRange(
                    component.GetAllComponents()
                    .Select(c => new ComponentWrapper(c)));
            }
            foreach (var component in TargetDisposableComponents)
            {
                _components.Add(new ComponentWrapper(component) { Disposable = true });
                _components.AddRange(
                    component.GetAllComponents()
                    .Select(c => new ComponentWrapper(c) { DisposableComponent = true }));
            }

            // Check target count and current count of components.
            foreach (var component in _components)
            {
                component.RequiredQuantity = _components.Count(c => c.Name == component.Name);
                component.CurrentQuantity = ModsInInv(component.Name);

                if (component.DisposableComponent)
                {
                    component.MaxQuantity = ArchnemesisSettings.Instance.MaxT1ReserveDisposable + component.RequiredQuantity;
                }
                else if (component.Disposable)
                {
                    component.MaxQuantity = ArchnemesisSettings.Instance.MaxT2ReserveDisposable + component.RequiredQuantity;
                }
                else
                {
                    component.MaxQuantity = component.Tier == 1 ?
                        ArchnemesisSettings.Instance.MaxT1Reserve + component.RequiredQuantity :
                        ArchnemesisSettings.Instance.MaxT2Reserve + component.RequiredQuantity;
                }

                if (component.CurrentQuantity < component.MaxQuantity)
                {
                    if (component.Tier == 1 &&
                        component.CurrentQuantity >= component.RequiredQuantity && 
                        freeSpace < ArchnemesisSettings.Instance.MinFreeSpaceForT1Reserve)
                    {
                        Log.Warn($"[Archnemesis] We won't keep any more reserves since free space ({freeSpace}) is less than " +
                                 $"minimum free space for picking up reserves " +
                                 $"({ArchnemesisSettings.Instance.MinFreeSpaceForT1Reserve})");
                    }
                    else
                    {
                        _modsToPickup.Add(component.Name);
                        if (Verbose)
                        {
                            var log = $"[Archnemesis] [Current]:{component.CurrentQuantity} [Req]:{component.RequiredQuantity} " +
                                $"[Max]:{component.MaxQuantity}{(component.Disposable ? " [Disposable] " : "")}" +
                                $"{(component.DisposableComponent ? " [DisposableComponent] " : "")}" +
                                $"- Looking for {(component.Tier == 1 ? "T1" : "T2+")} {component.Name}";

                            if (component.Tier == 1 && component.CurrentQuantity < component.RequiredQuantity)
                                Log.Error(log);
                            else if (component.Tier == 1)
                                Log.Debug(log);
                            else
                                Log.Info(log);
                        }
                    }
                }
                else
                {
                    if (component.Tier != 1)
                    {
                        // Add this to pickup just in case (if we forget when completing secondary recipe etc.).
                        _modsToPickup.Add(component.Name);
                    }
                }
            }

            // Todo: Check inventory capacity.
            if (ArchnemesisSettings.Instance.PickupOpulent)
                _modsToPickup.Add("Opulent");
            foreach (var additionalPickup in ArchnemesisSettings.Instance.AdditionalPickup)
            {
                _modsToPickup.Add(additionalPickup);
            }

            // Redundant mods in inventory.
            if (Verbose)
            {
                Log.Info("[Archnemesis] The following mods found in inventory are not used by any recipes:");
            }

            // Add non-disposable components as disposable if we keep reserve and reserve is full:
            var fullyStockedReserve = _components.Where(c => !c.Disposable && 
                c.CurrentQuantity >= c.MaxQuantity && 
                c.MaxQuantity - c.RequiredQuantity > 0);
            foreach (var g in fullyStockedReserve.Distinct())
            {
                DisposableComponents.Add(g);
                if (Verbose) Log.Info($"[Archnemesis] Marking {g.Name} as disposable since we are full ({g.CurrentQuantity}/{g.MaxQuantity})");
            }

            // Add any copmponent in inventory or components list that is not used by any recipes as disposable:
            var notUsed = inventory.FindAll(i =>
                 _components.All(c => c.Disposable || c.Name != i.ArchnemesisMod.Mod.DisplayName))
                .Select(i =>
                {
                    var existing = _components.Find(c => c.Name == i.ArchnemesisMod.Mod.DisplayName);
                    if (existing != null) return existing;
                    return new ComponentWrapper(GetRecipe(i.ArchnemesisMod.Mod.DisplayName))
                    {
                        CurrentQuantity = ModsInInv(i.ArchnemesisMod.Mod.DisplayName)
                    };
                }).ToList();
            foreach (var g in notUsed.Distinct())
            {
                DisposableComponents.Add(g);
                if (Verbose) Log.Info($"[Archnemesis] Disposable in inventory: {g.Name}: {g.CurrentQuantity}");
            }
        }

        [Obsolete("Keeping this in case we want to add a switch")]
        private static void UpdateNeededCompsOld()
        {
            _modsToPickup.Clear();

            // find all does not work for duplicates
            var missingMods = new List<ArchnemesisMod>();
            foreach (var child in SampleTargetRecipe)
            {
                missingMods.Add(GetRecipe(child));
            }

            bool onlyT1Left = false;
            var keepMods = new List<LokiPoe.InstanceInfo.Archnemesis.ArchnemesisMods>();
            while (!onlyT1Left)
            {
                // remove all that we have
                foreach (var i in LokiPoe.InstanceInfo.Archnemesis.AvailableArchnemesisMods)
                {
                    int foundIdx;
                    // FIXME: this will remove have mods multiple times!
                    if (!keepMods.Contains(i)
                        && (foundIdx = missingMods.FindIndex(m => m.Name == i.ArchnemesisMod.Mod.DisplayName)) != -1)
                    {
                        keepMods.Add(i);
                        missingMods.RemoveAt(foundIdx);
                    }
                }

                // assume we are done, revert it later
                onlyT1Left = true;

                // add child mods
                var children = new List<String>();
                // we want to remove T2+ mods as we can't find them anyways and we don't want to check them again in this loop
                var parents = new List<ArchnemesisMod>();
                foreach (var mm in missingMods)
                {
                    // is T2+?
                    if (mm.ComponentNames.Count > 0)
                    {
                        children.AddRange(mm.ComponentNames);
                        parents.Add(mm);
                        
                        onlyT1Left = false;
                    }
                }

                // remove parents
                foreach (var p in parents)
                {
                    missingMods.Remove(p);
                    // but please pick them up
                    _modsToPickup.Add(p.Name);
                }
                
                // add children
                foreach (var child in children)
                {
                    missingMods.Add(GetRecipe(child));
                }
            }

            if (Verbose)
            {
                Log.Info($"Missing count: {missingMods.Count}, have count: {keepMods.Count}");

                var destroy = LokiPoe.InstanceInfo.Archnemesis.AvailableArchnemesisMods
                    .FindAll(i => !keepMods.Contains(i) && IsT1(i));
                var grouped = destroy
                    .GroupBy(i => i.ArchnemesisMod.Mod.DisplayName)
                    .OrderByDescending(i => i.Count());
                foreach (var g in grouped)
                {
                    Log.Info($"Get rid of: {g.Key}: {g.Count()}");
                }

                foreach (var g in missingMods.GroupBy(i => i.Name)
                    .OrderByDescending(i => i.Count()))
                {
                    Log.Info(
                        $"Missing: {g.Key}: {g.Count()} " +
                        $"Maps: {string.Join(", ", GetRecipe(g.Key).Maps.Select(map => map.Name))}");
                }
            }

            _modsToPickup.AddRange(missingMods.Select(m => m.Name).ToList());
            
            if (ArchnemesisSettings.Instance.PickupOpulent)
                _modsToPickup.Add("Opulent");
        }
        #endregion

        #region Tangle Altars

        // We will choose the option with the higher score that is > 0
        private static int OptionScore(TangleAltar.Option option)
        {
            // List of mods:
            // https://poedb.tw/us/Siege_of_the_Atlas#PrimordialAltar
            
            // negative
            var badMods = new string[]
            {
                "Penetrates", "Steal", "Chaos Resistance", "Scorch", "Meteor", "Extra Fire", "Extra Lightning", "Extra Cold",
                "Implicit Modifier"
            };
            if (option.Text.Any(t => badMods.Any(t.Contains)))
                return -1;
            
            // positive
            // just weight boss less here
            var score = GetModType(option) == ModTypes.Boss ? 1 : 2;
            
            return score;
        }

        private static ModTypes GetModType(TangleAltar.Option option)
        {
            var line0 = option.Text.First();
            if (line0.Contains("Map boss gains"))
                return ModTypes.Boss;
            if (line0.Contains("Eldritch Minions gain"))
                return ModTypes.Eldrich;
            if (line0.Contains("Player gains:"))
                return ModTypes.Player;
            
            Log.Error($"[Archnemesis] Unknown tangle mod type: {line0}");
            return ModTypes.Unknown;
        }
        #endregion
        

        #region Meat

        /// <summary> The plugin start callback. Do any initialization here. </summary>
        public void Start()
        {
            Log.DebugFormat("[Archnemesis] Start");
            
            // TODO: check recipe typos
            if (false)
            {
                Log.Fatal("[Archnemesis] Unknown recipe component. Check your target recipe!");
                BotManager.Stop();
            }

            if (ArchnemesisSettings.Instance.DoCombine)
            {
                CombatAreaCache.AddPickupItemEvaluator(
                    "Archnemesis",
                    i => 
                        i.Components.ArchnemesisModComponent != null 
                        && ShouldPickup(i.Components.ArchnemesisModComponent.ModWrapper.DisplayName)
                    );
            }
            
            UpdateNeededComps();

            var taskManager = BotStructure.TaskManager;
            taskManager.AddBefore(this, AddBeforeTaskName);
        }

        /// <summary> The plugin tick callback. Do any update logic here. </summary>
        public void Tick()
        {
            if (!LokiPoe.IsInGame || !LokiPoe.CurrentWorldArea.IsCombatArea)
                return;

            // find altars and tangle shrines
            if (ScanInterval.Elapsed)
            {
                UpdateRecipes();

                // Mobs to insert items into
                var trapped = LokiPoe.ObjectManager.GetObjectsByType<ArchnemesisTrappedMonster>()
                    .Where(m => !CachedData.UseableAltars.ContainsKey(m.Id) && !CachedData.IgnoredAltars.ContainsKey(m.Id));
                foreach (var m in trapped)
                {
                    CachedData.UseableAltars.Add(m.Id, new CachedObject(m));
                }

                // "Remnants"
                foreach (var a in LokiPoe.ObjectManager.GetObjectsByType<TangleAltar>().Where(a => !CachedData.Tangles.ContainsKey(a.Id)))
                {
                    CachedData.Tangles.Add(a.Id, new CachedObject(a));
                }
            }

            // if (CachedData.Completable != null)
            //     Hud.Overlay.Log("currRecp", $"[ArchNem] current recipe: {String.Join(", ", CachedData.Completable)}");
        }

        public async Task<bool> Run()
        {
            var cache = CachedData;
            
            // activate altars
            var nextTangle = CachedData.Tangles.Where(a => !a.Value.Ignored).OrderBy(a => a.Value.Position.DistanceSqr).FirstOrDefault().Value;
            if (ArchnemesisSettings.Instance.ActivateRemnants && nextTangle != null)
            {
                // come close and make sure we are standing still
                if (nextTangle.Position.Distance > 20)
                {
                    if (!nextTangle.Position.TryCome())
                    {
                        nextTangle.Ignored = true;
                    }
                    return true;
                }
                await Coroutines.FinishCurrentAction();
                if (nextTangle.Position.Distance > 20)
                {
                    nextTangle.Position.TryCome();
                    return true;
                }

                var ta = nextTangle.Object as TangleAltar;
                var scores = ta.Options.Select(OptionScore).ToList();
                
                nextTangle.Ignored = scores.All(s => s <= 0);
                
                if (!nextTangle.Ignored)
                {
                    var best = scores.IndexOf(scores.Max());
                    ta.Options[best].Activate();
                    nextTangle.Ignored = true;
                    await Coroutine.Sleep(500);
                }
            }
            
            // Recipes
            if (ArchnemesisSettings.Instance.DoCombine && 
                (cache.Completable?.Any() == true || cache.Completable2?.Any() == true) && 
                cache.UseableAltars.Any())
            {
                // TODO ignore bad monsters

                List<string> _completable;
                if (cache.CanFitMore)
                {
                    _completable = CachedData.Completable2;
                }
                else
                {
                    _completable = CachedData.Completable;
                }

                var nextAltar = cache.UseableAltars
                    .OrderBy(a => a.Value.Position.DistanceSqr)
                    .FirstOrDefault().Value;

                if (nextAltar.Position.Distance > 20)
                {
                    nextAltar.Position.TryCome();
                    return true;
                }

                await Coroutines.FinishCurrentAction();

                if (nextAltar.Position.Distance > 20)
                {
                    nextAltar.Position.TryCome();
                    return true;
                }

                // Log
                Log.Debug($"[Archnemesis] Target: {nextAltar.Id} {nextAltar.Object.WalkablePosition()}");

                // open
                if (!(nextAltar.Object as ArchnemesisTrappedMonster).OpenMenu())
                {
                    Log.Error("[Archnemesis] Failed to open archnemesis ritual menu");
                    cache.IgnoredAltars.Add(nextAltar.Id, nextAltar);
                    cache.UseableAltars.Remove(nextAltar.Id);
                    return false;
                }

                await Coroutines.LatencyWait();

                // place
                if (!LokiPoe.InGameState.ArchnemesisEncounterUi.IsOpened)
                {
                    Log.Error("[Archnemesis] Failed to open archnemesis ritaul menu 2");
                    cache.IgnoredAltars.Add(nextAltar.Id, nextAltar);
                    cache.UseableAltars.Remove(nextAltar.Id);
                    return false;
                }

                Log.Info($"[Archnemesis] Now inserting: {_completable.First()} into {nextAltar.Object.Name}");

                var nextItem = LokiPoe.InGameState.ArchnemesisInventoryUi.AvailableArchnemesisMods
                    .FirstOrDefault(m => m.ArchnemesisMod.Mod.DisplayName == _completable.First());
                if (!LokiPoe.InGameState.ArchnemesisInventoryUi.FastMove(nextItem))
                {
                    Log.Error("[Archnemesis] Failed to move mod");
                    cache.IgnoredAltars.Add(nextAltar.Id, nextAltar);
                    cache.UseableAltars.Remove(nextAltar.Id);
                    return false;
                }

                // We are done with this item so register the usage.
                cache.RegisterUsedComponent(_completable.First(), _completable);
                await Coroutines.LatencyWait(3);

                if (!LokiPoe.InGameState.ArchnemesisEncounterUi.Begin())
                {
                    Log.Error("[Archnemesis] Failed to start encounter");
                    cache.IgnoredAltars.Add(nextAltar.Id, nextAltar);
                    cache.UseableAltars.Remove(nextAltar.Id);
                    return false;
                }

                // this altar is done
                Log.Info("[Archnemesis] Encounter started!");
                cache.IgnoredAltars.Add(nextAltar.Id, nextAltar);
                cache.UseableAltars.Remove(nextAltar.Id);

                // track the mob se we really kill it
                // Attention: this does not work if we die/chicken or go to HO
                // maybe add it to kill boss logic? PR welcome
                //TrackMobLogic.AddValuable(next);

                await Coroutine.Sleep(3000);
            }

            return false;
        }

        /// <summary> The plugin stop callback. Do any pre-dispose cleanup here. </summary>
        public void Stop()
        {
            Log.DebugFormat("[Archnemesis] Stop");
        }

        #endregion

       
        #region Implementation of IEnableable

        /// <summary> The plugin is being enabled.</summary>
        public void Enable()
        {
            Log.DebugFormat("[Archnemesis] Enable");
        }

        /// <summary> The plugin is being disabled.</summary>
        public void Disable()
        {
            Log.DebugFormat("[Archnemesis] Disable");
        }
        
        #endregion
        
        
        #region Implementation of ILogic
        
#pragma warning disable 1998
        public async Task<LogicResult> Logic(Logic logic) {
#pragma warning restore 1998
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInHideout)
                return LogicResult.Unprovided;

            
            return LogicResult.Unprovided;
        }
        #endregion
        
        #region Implementation of IAuthored

        /// <summary> The name of the plugin. </summary>
        public string Name => "Archnemesis";

        /// <summary>The author of the plugin.</summary>
        public string Author => "keksdev";

        /// <summary> The description of the plugin. </summary>
        public string Description => "A plugin to handle archnemesis recipes and tangle altars.";

        /// <summary>The version of the plugin.</summary>
        public string Version => "1.0.0";

        #endregion

        #region Implementation of IBase

        /// <summary>Initializes this plugin.</summary>
        public void Initialize()
        {
            Log.DebugFormat("[Archnemesis] Initialize");
            var modJson = File.ReadAllText("GGPK/archnemesis_recipies.json");
            _modDB = JsonConvert.DeserializeObject<List<ArchnemesisMod>>(modJson);

            if (!ArchnemesisSettings.Instance.TargetRecipe.Any())
            {
                ArchnemesisSettings.Instance.TargetRecipe = SampleTargetRecipe;
            }
        }

        /// <summary>Deinitializes this object. This is called when the object is being unloaded from the bot.</summary>
        public void Deinitialize()
        {
            Log.DebugFormat("[Archnemesis] Deinitialize");
        }

        #endregion

        #region Implementation of IConfigurable

        /// <summary>The settings object. This will be registered in the current configuration.</summary>
        public JsonSettings Settings => ArchnemesisSettings.Instance;

        /// <summary> The plugin's settings control. This will be added to the Exilebuddy Settings tab.</summary>
        public UserControl Control => (_instance ?? (_instance = new Gui()));

        #endregion

        #region Override of Object

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name + ": " + Description;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        #endregion
        
        private enum ModTypes
        {
            Boss, Eldrich, Player, Unknown
        }
        
        private class ArchnemesisData {
            public Dictionary<int, CachedObject> UseableAltars = new Dictionary<int, CachedObject>();
            public Dictionary<int, CachedObject> IgnoredAltars = new Dictionary<int, CachedObject>();
            public Dictionary<int, CachedObject> Tangles = new Dictionary<int, CachedObject>();

            // contains the componenets if we have all to complete a recipe
            public List<string> Completable = null;
            public List<string> Completable2 = null;

            // Keep track of what components are used this instannce so that we can consider more recipes or extra components
            // when the first recipe is done.
            public List<string> UsedComponents = new List<string>();

            public void RegisterUsedComponent(string name, List<string> listToRemoveFrom)
            {
                listToRemoveFrom.Remove(name);
                UsedComponents.Add(name);

                var log = $"[Archnemesis] Component {name} of {string.Join(", ", listToRemoveFrom)} is registered as used and removed. " +
                    $"[Completable]: {Completable.Count} [Completable2]: {Completable2?.Count} " +
                    $"[UsedComponents]: {UsedComponents.Count} [CanFitMore]: {CanFitMore}";

                if (Verbose)
                {
                    Log.Info(log);
                }
            }

            // Returns true if we have completed a recipe with less than 4 components.
            public bool CanFitMore =>
                UsedComponents.Count < 4 &&
                UsedComponents.Count > 0 &&
                Completable?.Count == 0;
        }

        private static ArchnemesisData CachedData
        {
            get
            {
                if (CombatAreaCache.Current.Storage["ArchnemesisData"] is ArchnemesisData data) 
                    return data;
                data = new ArchnemesisData();
                CombatAreaCache.Current.Storage["ArchnemesisData"] = data;
                return data;
            }
        }
    }
}
