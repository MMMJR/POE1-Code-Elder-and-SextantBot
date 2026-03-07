using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Class;
using Newtonsoft.Json;
using System.Globalization;
using System.Windows.Data;
using MmmjrBot.QuestBot;
using System.Runtime.Serialization;

namespace MmmjrBot
{
    public class EnumToBoolConverter : IValueConverter
    {
        public static readonly EnumToBoolConverter Instance = new EnumToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
    public class Upgrade
    {
        public bool TierEnabled { get; set; }
        public int Tier { get; set; } = 1;
    }
    public class MmmjrBotSettings : JsonSettings
    {
        private static MmmjrBotSettings _instance;
        public static MmmjrBotSettings Instance => _instance ?? (_instance = new MmmjrBotSettings());

        private MmmjrBotSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "MmmjrBot.json"))
        {
            GlobalLog.Info("Loading");
            enableSextantBot = false;
            enableFarmingBot = false;

            GlobalLog.Info("Mod ");
            if(SextantModSelector == null)
                SextantModSelector = SetupDefaultSextantModsCollection();

            if (MapperDefensiveSkills == null)
                MapperDefensiveSkills = SetupDefaultDefensiveSkills();
            if (MapperFlasks == null)
                MapperFlasks = SetupDefaultFlasks();

            //MaperBot
            if (MapperRoutineSelector == null)
                MapperRoutineSelector = SetupMapperRoutineConfiguration();

        }

        #region Current quest

        private string _currentQuestName;
        private string _currentQuestState;

        [JsonIgnore]
        public string CurrentQuestName
        {
            get => _currentQuestName;
            set
            {
                if (value == _currentQuestName) return;
                _currentQuestName = value;
                NotifyPropertyChanged(() => CurrentQuestName);
            }
        }

        [JsonIgnore]
        public string CurrentQuestState
        {
            get => _currentQuestState;
            set
            {
                if (value == _currentQuestState) return;
                _currentQuestState = value;
                NotifyPropertyChanged(() => CurrentQuestState);
            }
        }

        #endregion

        #region Grinding

        private bool _enableabyss;

        /// <summary>Should Breaches logic run? If false, Breaches will be skipped when possible.</summary>
        [DefaultValue(true)]
        public bool EnabledAbyss
        {
            get { return _enableabyss; }
            set
            {
                _enableabyss = value;
                NotifyPropertyChanged(() => EnabledAbyss);
            }
        }

        private bool _enablebreachs;

        /// <summary>Should Breaches logic run? If false, Breaches will be skipped when possible.</summary>
        [DefaultValue(true)]
        public bool EnabledBreachs
        {
            get { return _enablebreachs; }
            set
            {
                _enablebreachs = value;
                NotifyPropertyChanged(() => EnabledBreachs);
            }
        }

        public int ExplorationPercent { get; set; } = 85;
        public int MaxDeaths { get; set; } = 7;
        public bool TrackMob { get; set; }
        public bool UseHideout { get; set; }

        public ObservableCollection<GrindingRule> GrindingRules { get; set; } = new ObservableCollection<GrindingRule>();

        [JsonIgnore]
        public static List<Quest> QuestList
        {
            get
            {
                var list = new List<Quest>();
                foreach (var quest in Quests.All)
                {
                    if (quest == Quests.RibbonSpool || //completes along with Fiery Dust
                        quest == Quests.SwigOfHope || //early decanter messes this up
                        quest == Quests.EndToHunger) //epilogue one should be used
                        continue;

                    list.Add(quest);
                }
                return list;
            }
        }

        [JsonIgnore]
        public static List<Area> AreaList
        {
            get
            {
                var list = new List<Area>();
                foreach (var act in typeof(World).GetNestedTypes())
                {
                    foreach (var field in act.GetFields())
                    {
                        var area = field.GetValue(field) as AreaInfo;

                        if (area == null)
                            continue;

                        var id = area.Id;
                        if (id == World.Act1.TwilightStrand.Id ||
                            id == World.Act7.MaligaroSanctum.Id ||
                            id == World.Act11.TemplarLaboratory.Id ||
                            area.Id.Contains("town"))
                            continue;

                        list.Add(new Area(area));
                    }
                }
                return list;
            }
        }

        public class GrindingRule
        {
            public Quest Quest { get; set; } = Quests.EnemyAtTheGate;
            public int LevelCap { get; set; } = 100;
            public ObservableCollection<GrindingArea> Areas { get; set; } = new ObservableCollection<GrindingArea>();
        }

        public class GrindingArea
        {
            public Area Area { get; set; } = new Area(World.Act1.Coast);
            public int Pool { get; set; } = 1;
        }

        public class Area : IEquatable<Area>
        {
            public string Id { get; set; }
            public string Name { get; set; }

            public Area()
            {
            }

            public Area(AreaInfo area)
            {
                Id = area.Id;
                Name = area.Name;
            }

            public bool Equals(Area other)
            {
                return this == other;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Area);
            }

            public static bool operator ==(Area left, Area right)
            {
                if (ReferenceEquals(left, right))
                    return true;

                if (((object)left == null) || ((object)right == null))
                    return false;

                return left.Id == right.Id;
            }

            public static bool operator !=(Area left, Area right)
            {
                return !(left == right);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        #endregion

        private bool enableFarmingBot;
        [DefaultValue(false)]
        public bool EnableFarmingBot
        {
            get { return enableFarmingBot; }
            set
            { enableFarmingBot = value; NotifyPropertyChanged(() => enableFarmingBot); MmmjrBot.OnChangeEnabledBots(2, enableFarmingBot); enableSextantBot = false; NotifyPropertyChanged(() => enableSextantBot); }
        }
        public int MaxMapTier { get; set; } = 16;
        public int MobRemaining { get; set; } = 20;
        public bool FastTransition { get; set; } = true;
        public bool AtlasExplorationEnabled { get => atlasExplorationEnabled; set { atlasExplorationEnabled = value; NotifyPropertyChanged(() => atlasExplorationEnabled); enableSingleMap = false; NotifyPropertyChanged(() => enableSingleMap); } }
        public Upgrade MagicUpgrade { get; set; } = new Upgrade { TierEnabled = true };
        public Upgrade RareUpgrade { get; set; } = new Upgrade();
        public Upgrade ChiselUpgrade { get; set; } = new Upgrade();
        public Upgrade VaalUpgrade { get; set; } = new Upgrade();
        public Upgrade FragmentUpgrade { get; set; } = new Upgrade();
        public Upgrade MagicRareUpgrade { get; set; } = new Upgrade();
        public bool OpenPortals { get; set; } = true;
        private bool _stopRequested;

        private bool enableMapperCustomRoutine;
        private bool enableMapperRoutine;
        private bool enableMapperRoutineTriggerCWDT;
        private ObservableCollection<MapperRoutineSkill> _mapperRoutineSelector;
        public ObservableCollection<MapperRoutineSkill> MapperRoutineSelector
        {
            get => _mapperRoutineSelector;//?? (_defensiveSkills = new ObservableCollection<DefensiveSkillsClass>());
            set
            {
                _mapperRoutineSelector = value;
                NotifyPropertyChanged(() => _mapperRoutineSelector);
            }
        }

        private bool enableSingleMap;
        private string singleMapMetadata;
        private bool singleUseElderFragments;
        private bool singleUseShaperFragments;
        private bool singleUseConquerorMap;
        private bool singleUseElderGuardianMap;
        private bool singleUseShaperGuardianMap;
        private bool atlasExplorationEnabled;

        [DefaultValue(false)]
        public bool EnableMapperCustomRoutine
        {
            get { return enableMapperCustomRoutine; }
            set
            { enableMapperCustomRoutine = value; NotifyPropertyChanged(() => enableMapperCustomRoutine); enableMapperRoutine = false; NotifyPropertyChanged(() => enableMapperRoutine); enableExileRoutine = false; NotifyPropertyChanged(() => enableExileRoutine); }
        }

        [DefaultValue(true)]
        public bool EnableMapperRoutine
        {
            get { return enableMapperRoutine; }
            set
            { enableMapperRoutine = value; NotifyPropertyChanged(() => enableMapperRoutine); enableMapperCustomRoutine = false; NotifyPropertyChanged(() => enableMapperCustomRoutine); enableExileRoutine = false; NotifyPropertyChanged(() => enableExileRoutine); }
        }

        [DefaultValue(false)]
        public bool EnableMapperRoutineTriggerCWDT
        {
            get { return enableMapperRoutineTriggerCWDT; }
            set
            { enableMapperRoutineTriggerCWDT = value; NotifyPropertyChanged(() => enableMapperRoutineTriggerCWDT); }
        }

        [DefaultValue(false)]
        public bool EnableSingleMap
        {
            get { return enableSingleMap; }
            set
            { enableSingleMap = value; NotifyPropertyChanged(() => enableSingleMap); atlasExplorationEnabled = false; NotifyPropertyChanged(() => atlasExplorationEnabled); }
        }

        [DefaultValue("")]
        public string SingleMapMetadata
        {
            get { return singleMapMetadata; }
            set
            { singleMapMetadata = value; NotifyPropertyChanged(() => singleMapMetadata);}
        }
        [DefaultValue(false)]
        public bool SingleUseElderFragments
        {
            get { return singleUseElderFragments; }
            set
            { singleUseElderFragments = value; NotifyPropertyChanged(() => singleUseElderFragments); OnSingleMapSettingsChanged(4); }
        }

        [DefaultValue(false)]
        public bool SingleUseShaperFragments
        {
            get { return singleUseShaperFragments; }
            set
            { singleUseShaperFragments = value; NotifyPropertyChanged(() => singleUseShaperFragments); OnSingleMapSettingsChanged(3); }
        }
        [DefaultValue(false)]
        public bool SingleUseConquerorMap
        {
            get { return singleUseConquerorMap; }
            set
            { singleUseConquerorMap = value; NotifyPropertyChanged(() => singleUseConquerorMap); OnSingleMapSettingsChanged(0); }
        }
        [DefaultValue(false)]
        public bool SingleUseElderGuardianMap
        {
            get { return singleUseElderGuardianMap; }
            set
            { singleUseElderGuardianMap = value; NotifyPropertyChanged(() => singleUseElderGuardianMap); OnSingleMapSettingsChanged(1); }
        }
        [DefaultValue(false)]
        public bool SingleUseShaperGuardianMap
        {
            get { return singleUseShaperGuardianMap; }
            set
            { singleUseShaperGuardianMap = value; NotifyPropertyChanged(() => singleUseShaperGuardianMap); OnSingleMapSettingsChanged(2); }
        }

        public void OnSingleMapSettingsChanged(int index)
        {
            if (index == 0)
            {
                singleMapMetadata = "";
                singleUseElderGuardianMap = false;
                singleUseShaperGuardianMap = false;
                singleUseShaperFragments = false;
                singleUseElderFragments = false;
            }
            else if (index == 1)
            {
                singleMapMetadata = "";
                singleUseConquerorMap = false;
                singleUseShaperGuardianMap = false;
                singleUseShaperFragments = false;
                singleUseElderFragments = false;
            }
            else if (index == 2)
            {
                singleMapMetadata = "";
                singleUseElderGuardianMap = false;
                singleUseConquerorMap = false;
                singleUseShaperFragments = false;
                singleUseElderFragments = false;
            }
            else if (index == 3)
            {
                singleMapMetadata = "";
                singleUseElderGuardianMap = false;
                singleUseShaperGuardianMap = false;
                singleUseConquerorMap = false;
                singleUseElderFragments = false;
            }
            else if (index == 4)
            {
                singleMapMetadata = "";
                singleUseElderGuardianMap = false;
                singleUseShaperGuardianMap = false;
                singleUseShaperFragments = false;
                singleUseConquerorMap = false;
            }
            else if (index == 5)
            {
                singleUseElderGuardianMap = false;
                singleUseShaperGuardianMap = false;
                singleUseShaperFragments = false;
                singleUseConquerorMap = false;
                singleUseElderFragments = false;
            }
        }

        #region ExileRoutine
        private bool enableExileRoutine;
        public bool EnableExileRoutine
        {
            get { return enableExileRoutine; }
            set
            {
                enableExileRoutine = value;
                NotifyPropertyChanged(() => enableExileRoutine);
                enableMapperRoutine = false; 
                NotifyPropertyChanged(() => enableMapperRoutine); 
                enableMapperCustomRoutine = false; 
                NotifyPropertyChanged(() => enableMapperCustomRoutine);
            }
        }
        private string _singleTargetDPSSlot;
        private string _aoESlot;
        private string _movementSlot;
        private string _curseOnHitSlot;
        private string _totemSlotOne;
        private string _totemSlotTwo;
        private int _combatRange;
        private int _cwdttimes;
        private int _chickenPercent;
        private int _maxRange;
        private int _aoeRange;
        private string _moveSkillRange;
        private bool _alwaysAttackInPlace;
        private bool _ranged;
        private bool _dontaoeuniques;
        private bool _channel;
        private bool _channelSkill;
        private bool _follow;
        private bool _desecrateForSpectres;
        private bool _cullingStrike;
        private int _cullingPercent;

        private int _moltenShellDelayMs;
        private string _totemOneDelay;
        private string _totemTwoDelay;
        private string _totemOneMax;
        private string _totemTwoMax;
        private int _maxFollowRange;
        private string _leader;

        private int _mobsNearForLootRange;
        private int _lootDistanceCheck;
        private int _claspedDistanceCheck;
        private int _lootHealth;
        private int _lootES;
        private bool _checkForHealth;
        private bool _checkForES;

        private int _trapDelayMs;
        private int _maxFlameBlastCharges;

        private string _spectreTarget;
        private int _summonSkeletonDelayMs;

        private bool _summonSpectreTarget;
        private int _fleshOfferingDelayMs;

        private int _mineDelayMs;

        private bool _autoCastVaalSkills;

        private bool _enableAurasFromItems;

        private bool _debugAuras;

        private bool _leaveFrame;

        private string _blacklistedSkillIds;

        private bool _skipShrines;

        private int _numberOfEnemiesToEngageCombat;

        [DefaultValue(false)]
        public bool CullingStrike
        {
            get { return _cullingStrike; }
            set
            {
                if (value.Equals(_cullingStrike))
                {
                    return;
                }
                _cullingStrike = value;
                NotifyPropertyChanged(() => CullingStrike);
            }
        }

        [DefaultValue(10)]
        public int CullingPercent
        {
            get { return _cullingPercent; }
            set
            {
                if (value.Equals(_cullingPercent))
                {
                    return;
                }
                _cullingPercent = value;
                NotifyPropertyChanged(() => CullingPercent);
            }
        }

        /// <summary>
        /// A list of skill ids that should not be used. Right now, applies to auras.
        /// Comma or space separated.
        /// </summary>
        [DefaultValue("")]
        public string BlacklistedSkillIds
        {
            get
            {
                return _blacklistedSkillIds;
            }
            set
            {
                if (Equals(value, _blacklistedSkillIds))
                {
                    return;
                }
                _blacklistedSkillIds = value;
                NotifyPropertyChanged(() => BlacklistedSkillIds);
            }
        }

        /// <summary>
        /// Should the CR leave the current frame to do pathfinds and other frame intensive tasks.
        /// NOTE: This might cause random memory exceptions due to memory no longer being valid in the CR.
        /// </summary>
        [DefaultValue(false)]
        public bool LeaveFrame
        {
            get { return _leaveFrame; }
            set
            {
                if (value.Equals(_leaveFrame))
                {
                    return;
                }
                _leaveFrame = value;
                NotifyPropertyChanged(() => LeaveFrame);
            }
        }

        /// <summary>
        /// Should the CR skip shrines?
        /// </summary>
        [DefaultValue(false)]
        public bool SkipShrines
        {
            get { return _skipShrines; }
            set
            {
                if (value.Equals(_skipShrines))
                {
                    return;
                }
                _skipShrines = value;
                NotifyPropertyChanged(() => SkipShrines);
            }
        }

        /// <summary>
        /// Should the CR use auras granted by items rather than skill gems?
        /// </summary>
        [DefaultValue(true)]
        public bool EnableAurasFromItems
        {
            get { return _enableAurasFromItems; }
            set
            {
                if (value.Equals(_enableAurasFromItems))
                {
                    return;
                }
                _enableAurasFromItems = value;
                NotifyPropertyChanged(() => EnableAurasFromItems);
            }
        }

        /// <summary>
        /// Should the CR output casting errors for auras?
        /// </summary>
        [DefaultValue(false)]
        public bool DebugAuras
        {
            get { return _debugAuras; }
            set
            {
                if (value.Equals(_debugAuras))
                {
                    return;
                }
                _debugAuras = value;
                NotifyPropertyChanged(() => DebugAuras);
            }
        }

        /// <summary>
        /// Should vaal skills be auto-cast during combat.
        /// </summary>
        [DefaultValue(false)]
        public bool AutoCastVaalSkills
        {
            get { return _autoCastVaalSkills; }
            set
            {
                if (value.Equals(_autoCastVaalSkills))
                {
                    return;
                }
                _autoCastVaalSkills = value;
                NotifyPropertyChanged(() => AutoCastVaalSkills);
            }
        }

        /// <summary>
        ///  Should the bot use a specific spectre target?
        /// </summary>
        [DefaultValue(false)]
        public bool SummonSpectreTarget
        {
            get { return _summonSpectreTarget; }
            set
            {
                if (value.Equals(_summonSpectreTarget))
                {
                    return;
                }
                _summonSpectreTarget = value;
                NotifyPropertyChanged(() => SummonSpectreTarget);
            }
        }

        /// <summary>
        ///  Should the bot use a specific spectre target?
        /// </summary>
        [DefaultValue(false)]
        public bool DesecrateForSpectres
        {
            get { return _desecrateForSpectres; }
            set
            {
                if (value.Equals(_desecrateForSpectres))
                {
                    return;
                }
                _desecrateForSpectres = value;
                NotifyPropertyChanged(() => DesecrateForSpectres);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(20)]
        public int MobsNearForLootRange
        {
            get { return _mobsNearForLootRange; }
            set
            {
                if (value.Equals(_mobsNearForLootRange))
                {
                    return;
                }
                _mobsNearForLootRange = value;
                NotifyPropertyChanged(() => MobsNearForLootRange);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(50)]
        public int LootDistanceCheck
        {
            get { return _lootDistanceCheck; }
            set
            {
                if (value.Equals(_lootDistanceCheck))
                {
                    return;
                }
                _lootDistanceCheck = value;
                NotifyPropertyChanged(() => LootDistanceCheck);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(50)]
        public int ClaspedDistanceCheck
        {
            get { return _claspedDistanceCheck; }
            set
            {
                if (value.Equals(_claspedDistanceCheck))
                {
                    return;
                }
                _claspedDistanceCheck = value;
                NotifyPropertyChanged(() => ClaspedDistanceCheck);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(85)]
        public int LootHealth
        {
            get { return _lootHealth; }
            set
            {
                if (value.Equals(_lootHealth))
                {
                    return;
                }
                _lootHealth = value;
                NotifyPropertyChanged(() => LootHealth);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(85)]
        public int LootES
        {
            get { return _lootES; }
            set
            {
                if (value.Equals(_lootES))
                {
                    return;
                }
                _lootES = value;
                NotifyPropertyChanged(() => LootES);
            }
        }

        /// <summary>
        ///  Should the bot use a specific spectre target?
        /// </summary>
        [DefaultValue(false)]
        public bool CheckForHealth
        {
            get { return _checkForHealth; }
            set
            {
                if (value.Equals(_checkForHealth))
                {
                    return;
                }
                _checkForHealth = value;
                NotifyPropertyChanged(() => CheckForHealth);
            }
        }

        /// <summary>
        ///  Should the bot use a specific spectre target?
        /// </summary>
        [DefaultValue(false)]
        public bool CheckForES
        {
            get { return _checkForES; }
            set
            {
                if (value.Equals(_checkForES))
                {
                    return;
                }
                _checkForES = value;
                NotifyPropertyChanged(() => CheckForES);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(3000)]
        public int FleshOfferingDelayMs
        {
            get { return _fleshOfferingDelayMs; }
            set
            {
                if (value.Equals(_fleshOfferingDelayMs))
                {
                    return;
                }
                _fleshOfferingDelayMs = value;
                NotifyPropertyChanged(() => FleshOfferingDelayMs);
            }
        }

        /// <summary>
        /// How many casts to perform before the delay happens.
        /// </summary>
        [DefaultValue("Target")]
        public string SpectreTarget
        {
            get { return _spectreTarget; }
            set
            {
                if (value.Equals(_spectreTarget))
                {
                    return;
                }
                _spectreTarget = value;
                NotifyPropertyChanged(() => SpectreTarget);
            }
        }

        /// <summary>
        /// How long should the CR wait after performing all the casts.
        /// </summary>
        [DefaultValue(5000)]
        public int SummonSkeletonDelayMs
        {
            get { return _summonSkeletonDelayMs; }
            set
            {
                if (value.Equals(_summonSkeletonDelayMs))
                {
                    return;
                }
                _summonSkeletonDelayMs = value;
                NotifyPropertyChanged(() => SummonSkeletonDelayMs);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue(5000)]
        public int MineDelayMs
        {
            get { return _mineDelayMs; }
            set
            {
                if (value.Equals(_mineDelayMs))
                {
                    return;
                }
                _mineDelayMs = value;
                NotifyPropertyChanged(() => MineDelayMs);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue("Exact Character Name")]
        public string Leader
        {
            get { return _leader; }
            set
            {
                if (value.Equals(_leader))
                {
                    return;
                }
                _leader = value;
                NotifyPropertyChanged(() => Leader);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue(40)]
        public int MaxFollowRange
        {
            get { return _maxFollowRange; }
            set
            {
                if (value.Equals(_maxFollowRange))
                {
                    return;
                }
                _maxFollowRange = value;
                NotifyPropertyChanged(() => MaxFollowRange);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue("1")]
        public string TotemOneMax
        {
            get { return _totemOneMax; }
            set
            {
                _totemOneMax = value;
                NotifyPropertyChanged(() => TotemOneMax);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue("")]
        public string TotemTwoMax
        {
            get { return _totemTwoMax; }
            set
            {
                _totemTwoMax = value;
                NotifyPropertyChanged(() => TotemTwoMax);
            }
        }

        /// <summary>
        /// Should the CR always attack in place.
        /// </summary>
        [DefaultValue(true)]
        public bool AlwaysAttackInPlace
        {
            get { return _alwaysAttackInPlace; }
            set
            {
                if (value.Equals(_alwaysAttackInPlace))
                {
                    return;
                }
                _alwaysAttackInPlace = value;
                NotifyPropertyChanged(() => AlwaysAttackInPlace);
            }
        }

        /// <summary>
        /// Should the CR always attack in place.
        /// </summary>
        [DefaultValue(false)]
        public bool DontAoEUniques
        {
            get { return _dontaoeuniques; }
            set
            {
                if (value.Equals(_dontaoeuniques))
                {
                    return;
                }
                _dontaoeuniques = value;
                NotifyPropertyChanged(() => DontAoEUniques);
            }
        }

        /// <summary>
        /// Should the CR treat spell as channeled?
        /// </summary>
        [DefaultValue(false)]
        public bool Channel
        {
            get { return _channel; }
            set
            {
                if (value.Equals(_channel))
                {
                    return;
                }
                _channel = value;
                NotifyPropertyChanged(() => Channel);
            }
        }

        /// <summary>
        /// Should the CR treat spell as channeled?
        /// </summary>
        [DefaultValue(false)]
        public bool ChannelSkill
        {
            get { return _channelSkill; }
            set
            {
                if (value.Equals(_channelSkill))
                {
                    return;
                }
                _channelSkill = value;
                NotifyPropertyChanged(() => ChannelSkill);
            }
        }

        /// <summary>
        /// Should the CR treat spell as channeled?
        /// </summary>
        [DefaultValue(false)]
        public bool Follow
        {
            get { return _follow; }
            set
            {
                if (value.Equals(_follow))
                {
                    return;
                }
                _follow = value;
                NotifyPropertyChanged(() => Follow);
            }
        }

        /// <summary>
        /// Should the CR treat spell as channeled?
        /// </summary>
        [DefaultValue(false)]
        public bool Ranged
        {
            get { return _ranged; }
            set
            {
                if (value.Equals(_ranged))
                {
                    return;
                }
                _ranged = value;
                NotifyPropertyChanged(() => Ranged);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue("")]
        public string SingleTargetDPSSlot
        {
            get { return _singleTargetDPSSlot; }
            set
            {
                _singleTargetDPSSlot = value;
                NotifyPropertyChanged(() => SingleTargetDPSSlot);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue("")]
        public string AoESlot
        {
            get { return _aoESlot; }
            set
            {
                _aoESlot = value;
                NotifyPropertyChanged(() => AoESlot);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue("")]
        public string MovementSlot
        {
            get { return _movementSlot; }
            set
            {
                _movementSlot = value;
                NotifyPropertyChanged(() => MovementSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue("")]
        public string CurseOnHitSlot
        {
            get { return _curseOnHitSlot; }
            set
            {
                _curseOnHitSlot = value;
                NotifyPropertyChanged(() => CurseOnHitSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue("")]
        public string TotemSlotOne
        {
            get { return _totemSlotOne; }
            set
            {
                _totemSlotOne = value;
                NotifyPropertyChanged(() => TotemSlotOne);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue("")]
        public string TotemSlotTwo
        {
            get { return _totemSlotTwo; }
            set
            {
                _totemSlotTwo = value;
                NotifyPropertyChanged(() => TotemSlotTwo);
            }
        }

        /// <summary>
        /// Only attack mobs within this range.
        /// </summary>
        [DefaultValue(50)]
        public int CombatRange
        {
            get { return _combatRange; }
            set
            {
                if (value.Equals(_combatRange))
                {
                    return;
                }
                _combatRange = value;
                NotifyPropertyChanged(() => CombatRange);
            }
        }

        [DefaultValue(1)]
        public int CWDTTimes
        {
            get { return _cwdttimes; }
            set
            {
                if (value.Equals(_cwdttimes))
                {
                    return;
                }
                _cwdttimes = value;
                NotifyPropertyChanged(() => CWDTTimes);
            }
        }

        /// <summary>
        /// Only attack mobs within this range.
        /// </summary>
        [DefaultValue(30)]
        public int ChickenPercent
        {
            get { return _chickenPercent; }
            set
            {
                if (value.Equals(_chickenPercent))
                {
                    return;
                }
                _chickenPercent = value;
                NotifyPropertyChanged(() => ChickenPercent);
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Melee skill.
        /// Do not set too high, as the cursor will overlap the GUI.
        /// </summary>
        [DefaultValue(50)]
        public int MaxRange
        {
            get { return _maxRange; }
            set
            {
                if (value.Equals(_maxRange))
                {
                    return;
                }
                _maxRange = value;
                NotifyPropertyChanged(() => MaxRange);
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Ranged skill.
        /// Do not set too high, as the cursor will overlap the GUI.
        /// </summary>
        [DefaultValue(40)]
        public int AoeRange
        {
            get { return _aoeRange; }
            set
            {
                if (value.Equals(_aoeRange))
                {
                    return;
                }
                _aoeRange = value;
                NotifyPropertyChanged(() => AoeRange);
            }
        }

        /// <summary>
        /// How close does a mob need to be to trigger the Ranged skill.
        /// Do not set too high, as the cursor will overlap the GUI.
        /// </summary>
        [DefaultValue("15")]
        public string MoveSkillRange
        {
            get { return _moveSkillRange; }
            set
            {
                _moveSkillRange = value;
                NotifyPropertyChanged(() => MoveSkillRange);
            }
        }

        /// <summary>
        /// How many flameblast charges to build up before releasing.
        /// </summary>
        [DefaultValue(5)]
        public int MaxFlameBlastCharges
        {
            get { return _maxFlameBlastCharges; }
            set
            {
                if (value.Equals(_maxFlameBlastCharges))
                {
                    return;
                }
                _maxFlameBlastCharges = value;
                NotifyPropertyChanged(() => MaxFlameBlastCharges);
            }
        }

        /// <summary>
        /// The delay between casting molten shell in combat.
        /// </summary>
        [DefaultValue(5000)]
        public int MoltenShellDelayMs
        {
            get { return _moltenShellDelayMs; }
            set
            {
                if (value.Equals(_moltenShellDelayMs))
                {
                    return;
                }
                _moltenShellDelayMs = value;
                NotifyPropertyChanged(() => MoltenShellDelayMs);
            }
        }

        /// <summary>
        /// The delay between casting totems in combat.
        /// </summary>
        [DefaultValue("1500")]
        public string TotemTwoDelay
        {
            get { return _totemTwoDelay; }
            set
            {
                _totemTwoDelay = value;
                NotifyPropertyChanged(() => TotemTwoDelay);
            }
        }

        [DefaultValue("1500")]
        public string TotemOneDelay
        {
            get { return _totemOneDelay; }
            set
            {
                _totemOneDelay = value;
                NotifyPropertyChanged(() => TotemOneDelay);
            }
        }

        /// <summary>
        /// The delay between casting totems in combat.
        /// </summary>
        [DefaultValue(0)]
        public int NumberOfEnemiesToEngageCombat
        {
            get { return _numberOfEnemiesToEngageCombat; }
            set
            {
                if (value.Equals(_numberOfEnemiesToEngageCombat))
                {
                    return;
                }
                _numberOfEnemiesToEngageCombat = value;
                NotifyPropertyChanged(() => NumberOfEnemiesToEngageCombat);
            }
        }

        /// <summary>
        /// The delay between casting traps in combat.
        /// </summary>
        [DefaultValue(2500)]
        public int TrapDelayMs
        {
            get { return _trapDelayMs; }
            set
            {
                if (value.Equals(_trapDelayMs))
                {
                    return;
                }
                _trapDelayMs = value;
                NotifyPropertyChanged(() => TrapDelayMs);
            }
        }

        private int _allSkillSlots;
        public int AllSkillSlots
        {
            get { return _allSkillSlots; }
            set
            {
                _allSkillSlots = value;
                NotifyPropertyChanged(() => _allSkillSlots);
            }
        }
        #endregion

        [JsonIgnore]
        public bool StopRequested
        {
            get => _stopRequested;
            set
            {
                if (value == _stopRequested) return;
                _stopRequested = value;
                NotifyPropertyChanged(() => StopRequested);
            }
        }

        //---------------------
        private bool _mapperignoreHiddenAuras;

        [DefaultValue(false)]
        public bool MapperIgnoreHiddenAuras
        {
            get { return _mapperignoreHiddenAuras; }
            set
            { _mapperignoreHiddenAuras = value; NotifyPropertyChanged(() => MapperIgnoreHiddenAuras); }
        }

        private bool _ignoreHiddenAuras;

        [DefaultValue(false)]
        public bool IgnoreHiddenAuras
        {
            get { return _ignoreHiddenAuras; }
            set
            { _ignoreHiddenAuras = value; NotifyPropertyChanged(() => IgnoreHiddenAuras); }
        }
        private int _mappermaxLootDistance;
        private bool _mappershouldLoot;

        private bool _enableMapperAspectsOfTheAvian;
        private bool _enableMapperAspectsOfTheCat;
        private bool _enableMapperAspectsOfTheCrab;
        private bool _enableMapperAspectsOfTheSpider;
        private BloodAndSand _MapperbloodOrSand;

        [DefaultValue(false)]
        public bool EnableMapperAspectsOfTheAvian
        {
            get
            {
                return _enableMapperAspectsOfTheAvian;
            }
            set
            {
                _enableMapperAspectsOfTheAvian = value;
                NotifyPropertyChanged(() => EnableMapperAspectsOfTheAvian);
            }
        }
        [DefaultValue(false)]
        public bool EnableMapperAspectsOfTheCat
        {
            get
            {
                return _enableMapperAspectsOfTheCat;
            }
            set
            {
                _enableMapperAspectsOfTheCat = value;
                NotifyPropertyChanged(() => EnableMapperAspectsOfTheCat);
            }
        }
        [DefaultValue(false)]
        public bool EnableMapperAspectsOfTheCrab
        {
            get
            {
                return _enableMapperAspectsOfTheCrab;
            }
            set
            {
                _enableMapperAspectsOfTheCrab = value;
                NotifyPropertyChanged(() => EnableMapperAspectsOfTheCrab);
            }
        }
        [DefaultValue(false)]
        public bool EnableMapperAspectsOfTheSpider
        {
            get
            {
                return _enableMapperAspectsOfTheSpider;
            }
            set
            {
                _enableMapperAspectsOfTheSpider = value;
                NotifyPropertyChanged(() => EnableMapperAspectsOfTheSpider);
            }
        }

        [DefaultValue(BloodAndSand.Sand)]
        public BloodAndSand MapperBloorOrSand
        {
            get
            {
                return _MapperbloodOrSand;
            }
            set
            {
                _bloodOrSand = value;
                NotifyPropertyChanged(() => MapperBloorOrSand);
            }
        }

        private bool _enableAspectsOfTheAvian;
        private bool _enableAspectsOfTheCat;
        private bool _enableAspectsOfTheCrab;
        private bool _enableAspectsOfTheSpider;
        private BloodAndSand _bloodOrSand;

        [DefaultValue(false)]
        public bool EnableAspectsOfTheAvian
        {
            get
            {
                return _enableAspectsOfTheAvian;
            }
            set
            {
                _enableAspectsOfTheAvian = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheAvian);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheCat
        {
            get
            {
                return _enableAspectsOfTheCat;
            }
            set
            {
                _enableAspectsOfTheCat = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheCat);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheCrab
        {
            get
            {
                return _enableAspectsOfTheCrab;
            }
            set
            {
                _enableAspectsOfTheCrab = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheCrab);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheSpider
        {
            get
            {
                return _enableAspectsOfTheSpider;
            }
            set
            {
                _enableAspectsOfTheSpider = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheSpider);
            }
        }

        [DefaultValue(BloodAndSand.Sand)]
        public BloodAndSand BloorOrSand
        {
            get
            {
                return _bloodOrSand;
            }
            set
            {
                _bloodOrSand = value;
                NotifyPropertyChanged(() => BloorOrSand);
            }
        }

        private ObservableCollection<DefensiveSkillsClass> _mapperdefensiveSkills;
        private ObservableCollection<FlasksClass> _mapperflasks;

        private bool _mappergemDebugStatements;
        private bool _mapperlevelAllGems;
        private bool _mapperlevelOffhandOnly;
        private ObservableCollection<string> _mapperglobalNameIgnoreList;
        [DefaultValue(40)]
        public int MapperMaxLootDistance
        {
            get { return _mappermaxLootDistance; }
            set
            { _mappermaxLootDistance = value; NotifyPropertyChanged(() => MapperMaxLootDistance); }
        }
        [DefaultValue(false)]
        public bool MapperShouldLoot
        {
            get { return _mappershouldLoot; }
            set
            { _mappershouldLoot = value; NotifyPropertyChanged(() => MapperShouldLoot); }
        }
        public ObservableCollection<DefensiveSkillsClass> MapperDefensiveSkills
        {
            get => _mapperdefensiveSkills;//?? (_defensiveSkills = new ObservableCollection<DefensiveSkillsClass>());
            set
            {
                _mapperdefensiveSkills = value;
                NotifyPropertyChanged(() => _mapperdefensiveSkills);
            }
        }

        public ObservableCollection<FlasksClass> MapperFlasks
        {
            get => _mapperflasks;//?? (_flasks = new ObservableCollection<FlasksClass>());
            set
            {
                _mapperflasks = value;
                NotifyPropertyChanged(() => _mapperflasks);
            }
        }

        private bool enableAutoLogin;
        private bool enableSextantBot;
        private bool enableCerimonialVoidstone;
        private bool enableDecayedVoidstone;
        private bool enableGraspingVoidstone;
        private bool enableOmniscientVoidstone;
        private int selectedSextant;
        private string minSextantsInInventory;
        private string minCompassInInventory;
        private ObservableCollection<SextantModSelectorClass> _sextantModSelector;

        [DefaultValue(true)]
        public bool EnableAutoLogin
        {
            get { return enableAutoLogin; }
            set
            { enableAutoLogin = value; NotifyPropertyChanged(() => enableAutoLogin); }
        }

        [DefaultValue(false)]
        public bool EnableSextantBot
        {
            get { return enableSextantBot; }
            set
            { enableSextantBot = value; NotifyPropertyChanged(() => enableSextantBot); MmmjrBot.OnChangeEnabledBots(1, enableSextantBot); enableFarmingBot = false; NotifyPropertyChanged(() => enableFarmingBot); }
        }
        [DefaultValue(true)]
        public bool EnableCerimonialVoidstone
        {
            get { return enableCerimonialVoidstone; }
            set
            { enableCerimonialVoidstone = value; NotifyPropertyChanged(() => enableCerimonialVoidstone); OnVoidStoneSettingsChanged(0); }
        }
        [DefaultValue(true)]
        public bool EnableDecayedVoidstone
        {
            get { return enableDecayedVoidstone; }
            set
            { enableDecayedVoidstone = value; NotifyPropertyChanged(() => enableDecayedVoidstone); OnVoidStoneSettingsChanged(1); }
        }
        [DefaultValue(true)]
        public bool EnableGraspingVoidstone
        {
            get { return enableGraspingVoidstone; }
            set
            { enableGraspingVoidstone = value; NotifyPropertyChanged(() => enableGraspingVoidstone); OnVoidStoneSettingsChanged(2); }
        }
        [DefaultValue(true)]
        public bool EnableOmniscientVoidstone
        {
            get { return enableOmniscientVoidstone; }
            set
            { enableOmniscientVoidstone = value; NotifyPropertyChanged(() => enableOmniscientVoidstone); OnVoidStoneSettingsChanged(3); }
        }
        [DefaultValue(2)]
        public int SelectedSextant
        {
            get { return selectedSextant; }
            set
            { selectedSextant = value; NotifyPropertyChanged(() => selectedSextant); MmmjrBot.Log.Info("Selected Sextant: " + selectedSextant); }
        }
        [DefaultValue("")]
        public string MinSextantsInInventory
        {
            get { return minSextantsInInventory; }
            set
            { minSextantsInInventory = value; NotifyPropertyChanged(() => minSextantsInInventory); }
        }
        [DefaultValue("")]
        public string MinCompassInInventory
        {
            get { return minCompassInInventory; }
            set
            { minCompassInInventory = value; NotifyPropertyChanged(() => minCompassInInventory); }
        }

        public ObservableCollection<SextantModSelectorClass> SextantModSelector
        {
            get => _sextantModSelector;
            set
            {
                _sextantModSelector = value;
                NotifyPropertyChanged(() => SextantModSelector);
            }
        }
        public List<SextantModSelectorClass> GetAllEnabledMods()
        {
            List<SextantModSelectorClass> result = new List<SextantModSelectorClass>();

            foreach (SextantModSelectorClass i in SextantModSelector)
            {
                if(i.Enabled)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        public void OnVoidStoneSettingsChanged(int index)
        {
            if(index ==  0)
            {
                enableDecayedVoidstone = false;
                enableGraspingVoidstone= false;
                enableOmniscientVoidstone = false;
            }
            if (index == 1)
            {
                enableCerimonialVoidstone = false;
                enableGraspingVoidstone = false;
                enableOmniscientVoidstone = false;
            }
            if (index == 2)
            {
                enableDecayedVoidstone = false;
                enableCerimonialVoidstone = false;
                enableOmniscientVoidstone = false;
            }
            if (index == 3)
            {
                enableDecayedVoidstone = false;
                enableGraspingVoidstone = false;
                enableCerimonialVoidstone = false;
            }

        }

        private ObservableCollection<DefensiveSkillsClass> SetupDefaultDefensiveSkills()
        {
            ObservableCollection<DefensiveSkillsClass> skills = new ObservableCollection<DefensiveSkillsClass>
            {
                new DefensiveSkillsClass(false, "Vaal Molten Shell", false, 0, 0, false),
                new DefensiveSkillsClass(false, "Vaal Discipline", false, 0, 0, false),
                new DefensiveSkillsClass(false, "Molten Shell", false, 0, 0, false),
                new DefensiveSkillsClass(false, "Steelskin", false, 0, 0, false)
            };
            return skills;
        }

        private ObservableCollection<FlasksClass> SetupDefaultFlasks()
        {
            ObservableCollection<FlasksClass> flasks = new ObservableCollection<FlasksClass>
            {
                new FlasksClass(false, 1, false, false, 0, 0, false),
                new FlasksClass(false, 2, false, false, 0, 0, false),
                new FlasksClass(false, 3, false, false, 0, 0, false),
                new FlasksClass(false, 4, false, false, 0, 0, false),
                new FlasksClass(false, 5, false, false, 0, 0, false)
            };
            return flasks;
        }

        [DefaultValue(false)]
        public bool MapperGemDebugStatements
        {
            get
            {
                return _mappergemDebugStatements;
            }
            set
            {
                if (value.Equals(_mappergemDebugStatements))
                {
                    return;
                }
                _mappergemDebugStatements = value;
                NotifyPropertyChanged(() => MapperGemDebugStatements);
            }
        }
        [DefaultValue(false)]
        public bool MapperLevelOffhandOnly
        {
            get
            {
                return _mapperlevelOffhandOnly;
            }
            set
            {
                if (value.Equals(_mapperlevelOffhandOnly))
                {
                    return;
                }
                _mapperlevelOffhandOnly = value;
                NotifyPropertyChanged(() => MapperLevelOffhandOnly);
            }
        }
        [DefaultValue(false)]
        public bool MapperLevelAllGems
        {
            get
            {
                return _mapperlevelAllGems;
            }
            set
            {
                if (value.Equals(_mapperlevelAllGems))
                {
                    return;
                }
                _mapperlevelAllGems = value;
                NotifyPropertyChanged(() => MapperLevelAllGems);
            }
        }
        public ObservableCollection<string> MapperGlobalNameIgnoreList
        {
            get
            {
                return _mapperglobalNameIgnoreList ?? (_mapperglobalNameIgnoreList = new ObservableCollection<string>());
            }
            set
            {
                if (value.Equals(_mapperglobalNameIgnoreList))
                {
                    return;
                }
                _mapperglobalNameIgnoreList = value;
                NotifyPropertyChanged(() => MapperGlobalNameIgnoreList);
            }
        }
        [JsonIgnore]
        public ObservableCollection<SkillGemEntry> MapperUserSkillGemsInOffHands
        {
            get
            {
                //using (LokiPoe.AcquireFrame())
                //{
                ObservableCollection<SkillGemEntry> mapperskillGemEntries = new ObservableCollection<SkillGemEntry>();

                if (!LokiPoe.IsInGame)
                {
                    return mapperskillGemEntries;
                }

                foreach (Inventory inv in UsableOffInventories)
                {
                    foreach (Item item in inv.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Components.SocketsComponent == null)
                        {
                            continue;
                        }

                        for (int idx = 0; idx < item.SocketedGems.Length; idx++)
                        {
                            Item gem = item.SocketedGems[idx];
                            if (gem == null)
                            {
                                continue;
                            }

                            mapperskillGemEntries.Add(new SkillGemEntry(gem.Name, inv.PageSlot, idx));
                        }
                    }
                }
                return mapperskillGemEntries;
                //}
            }
        }

        [JsonIgnore]
        public ObservableCollection<SkillGemEntry> MapperUserSkillGems
        {
            get
            {
                //using (LokiPoe.AcquireFrame())
                //{
                ObservableCollection<SkillGemEntry> mapperskillGemEntries = new ObservableCollection<SkillGemEntry>();

                if (!LokiPoe.IsInGame)
                {
                    return mapperskillGemEntries;
                }

                foreach (Inventory inv in UsableInventories)
                {
                    foreach (Item item in inv.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Components.SocketsComponent == null)
                        {
                            continue;
                        }

                        for (int idx = 0; idx < item.SocketedGems.Length; idx++)
                        {
                            Item gem = item.SocketedGems[idx];
                            if (gem == null)
                            {
                                continue;
                            }

                            mapperskillGemEntries.Add(new SkillGemEntry(gem.Name, inv.PageSlot, idx));
                        }
                    }
                }
                return mapperskillGemEntries;
                //}
            }
        }
        public void MapperUpdateGlobalNameIgnoreList()
        {
            NotifyPropertyChanged(() => MapperGlobalNameIgnoreList);
        }
        public class SkillGemEntry
        {
            public string Name;
            public InventorySlot InventorySlot;
            public int SocketIndex;

            public string SerializationString { get; private set; }

            public SkillGemEntry(string name, InventorySlot slot, int socketIndex)
            {
                Name = name;
                InventorySlot = slot;
                SocketIndex = socketIndex;
                SerializationString = string.Format("{0} [{1}: {2}]", Name, InventorySlot, SocketIndex);
            }

            public Item InventoryItem
            {
                get
                {
                    return UsableInventories.Where(ui => ui.PageSlot == InventorySlot)
                        .Select(ui => ui.Items.FirstOrDefault())
                        .FirstOrDefault();
                }
            }

            public Item SkillGem
            {
                get
                {
                    Item item = InventoryItem;
                    if (item == null || item.Components.SocketsComponent == null)
                    {
                        return null;
                    }

                    Item sg = item.SocketedGems[SocketIndex];
                    if (sg == null)
                    {
                        return null;
                    }

                    if (sg.Name != Name)
                    {
                        return null;
                    }

                    return sg;
                }
            }
        }
        private static IEnumerable<Inventory> UsableInventories => new[]
        {
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffLeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffRightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Head),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Chest),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Gloves),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Boots),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Neck)
        };
        private static IEnumerable<Inventory> UsableOffInventories => new[]
        {
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffLeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffRightHand),
        };

        private bool castLong;
        private bool _enableOverlay;
        private bool _drawInBackground;
        private bool _drawMobs;
        private bool _drawCorpses;
        private int _fps;
        private int _overlayXCoord;
        private int _overlayYCoord;
        private int _overlayTransparency;

        [DefaultValue(false)]
        public bool CastLong
        {
            get => castLong;
            set
            {
                castLong = value;
                NotifyPropertyChanged(() => castLong);
            }
        }

        [DefaultValue(false)]
        public bool EnableOverlay
        {
            get => _enableOverlay;
            set
            {
                if (value == _enableOverlay) return;
                _enableOverlay = value;
                NotifyPropertyChanged(() => EnableOverlay);
            }
        }
        [DefaultValue(false)]
        public bool DrawInBackground
        {
            get => _drawInBackground;
            set
            {
                if (value == _drawInBackground) return;
                _drawInBackground = value;
                NotifyPropertyChanged(() => DrawInBackground);
            }
        }
        [DefaultValue(false)]
        public bool DrawMobs
        {
            get => _drawMobs;
            set
            {
                if (value == _drawMobs) return;
                _drawMobs = value;
                NotifyPropertyChanged(() => DrawMobs);
            }
        }
        [DefaultValue(false)]
        public bool DrawCorpses
        {
            get => _drawCorpses;
            set
            {
                if (value == _drawCorpses) return;
                _drawCorpses = value;
                NotifyPropertyChanged(() => DrawCorpses);
            }
        }

        [DefaultValue(30)]
        public int FPS
        {
            get => _fps;
            set
            {
                if (value == _fps) return;
                _fps = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetFps(_fps);
                NotifyPropertyChanged(() => FPS);
            }
        }

        [DefaultValue(15)]
        public int OverlayXCoord
        {
            get => _overlayXCoord;
            set
            {
                if (value == _overlayXCoord) return;
                _overlayXCoord = value;
                NotifyPropertyChanged(() => OverlayXCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayYCoord
        {
            get => _overlayYCoord;
            set
            {
                if (value == _overlayYCoord) return;
                _overlayYCoord = value;
                NotifyPropertyChanged(() => OverlayYCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayTransparency
        {
            get => _overlayTransparency;
            set
            {
                if (value == _overlayTransparency) return;
                _overlayTransparency = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetTransparency(_overlayTransparency);
                NotifyPropertyChanged(() => OverlayTransparency);
            }
        }

        public enum BloodAndSand
        {
            Blood,
            Sand
        }
        private ObservableCollection<MapperRoutineSkill> SetupMapperRoutineConfiguration()
        {
            ObservableCollection<MapperRoutineSkill> _modss = new ObservableCollection<MapperRoutineSkill>
            {
                new MapperRoutineSkill("Skillbar Slot:", "1", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "2", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "3", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "4", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "5", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "6", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "7", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "8", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "9", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "10", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "11", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "12", false, 0, 1),
                new MapperRoutineSkill("Skillbar Slot:", "13", false, 0, 1)
            };
            return _modss;
        }

        private string _character;

        public string Character
        {
            get => _character;
            set
            {
                if (value == _character) return;
                _character = value;
                NotifyPropertyChanged(() => Character);
            }
        }

        public float LoginDelayInitial { get; set; } = 0.5f;
        public float LoginDelayStep { get; set; } = 3;
        public float LoginDelayFinal { get; set; } = 300;
        public int LoginDelayRandPct { get; set; } = 15;
        public float CharSelectDelay { get; set; } = 0.5f;

        public bool LoginUsingUserCredentials { get; set; }
        public bool LoginUsingGateway { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Gateway { get; set; }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (string.IsNullOrEmpty(Password))
                return;

            // Encrypt the key when serializing to file.
            Password = GlobalSettings.Crypto.EncryptStringAes(Password, "autologinsharedsecret");
        }

        [OnSerialized]
        internal void OnSerialized(StreamingContext context)
        {
            if (string.IsNullOrEmpty(Password))
                return;

            // Decrypt the key when we're done serializing, so we can have the plain-text version back.
            Password = GlobalSettings.Crypto.DecryptStringAes(Password, "autologinsharedsecret");
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            // Make sure we decrypt the license key, so we can use it.
            OnSerialized(context);
        }

        public static readonly string[] GatewayList =
        {
            "Auto-select Gateway",
            "Texas (US)",
            "Washington, D.C. (US)",
            "California (US)",
            "Amsterdam (EU)",
            "London (EU)",
            "Frankfurt (EU)",
            "Milan (EU)",
            "Singapore",
            "Australia",
            "Sao Paulo (BR)",
            "Paris (EU)",
            "Moscow (RU)",
            "Japan"
        };
        private ObservableCollection<SextantModSelectorClass> SetupDefaultSextantModsCollection()
        {
            ObservableCollection<SextantModSelectorClass> _modss = new ObservableCollection<SextantModSelectorClass>
            {
                new SextantModSelectorClass("Awakened Sextant", false, "25% increased Magic Pack Size", "3 uses remaining", "", "", StatTypeGGG.MapMagicPackSizePosPct, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "30% increased Magic Pack Size", "15 uses remaining", "", "", StatTypeGGG.MapMagicPackSizePosPct, 30),
                new SextantModSelectorClass("Awakened Sextant", false, "100% increased Intelligence gained from Immortal Syndicate targets encountered in your Maps", "3 uses remaining", "", "", StatTypeGGG.MapBetrayalIntelligencePosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "100% increased Intelligence gained from Immortal Syndicate targets encountered in your Maps", "15 uses remaining", "", "", StatTypeGGG.MapBetrayalIntelligencePosPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Area contains a Smuggler's Cache", "3 uses remaining", "", "", StatTypeGGG.MapSpawnHeistSmugglersCache),
                new SextantModSelectorClass("Elevated Sextant", false, "Area contains a Smuggler's Cache", "15 uses remaining", "", "", StatTypeGGG.MapSpawnHeistSmugglersCache),
                new SextantModSelectorClass("Awakened Sextant", false, "Area contains Metamorph Monsters", "3 uses remaining", "", "", StatTypeGGG.MapAreaContainsMetamorphs),
                new SextantModSelectorClass("Elevated Sextant", false, "Area contains Metamorph Monsters", "15 uses remaining", "", "", StatTypeGGG.MapAreaContainsMetamorphs),
                new SextantModSelectorClass("Awakened Sextant", false, "Breaches in your Maps belong to Chayula", "Breaches in your Maps contain 3 additional Clasped Hands", "3 uses remaining", "", StatTypeGGG.MapBreachTypeOverride, 5, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Elevated Sextant", false, "Breaches in your Maps belong to Chayula", "Breaches in your Maps contain 3 additional Clasped Hands", "15 uses remaining", "", StatTypeGGG.MapBreachTypeOverride, 5, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Awakened Sextant", false, "Breaches in your Maps belong to Esh", "Breaches in your Maps contain 3 additional Clasped Hands", "", "3 uses remaining", StatTypeGGG.MapBreachTypeOverride, 4, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Elevated Sextant", false, "Breaches in your Maps belong to Esh", "Breaches in your Maps contain 3 additional Clasped Hands", "", "15 uses remaining", StatTypeGGG.MapBreachTypeOverride, 4, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Awakened Sextant", false, "Breaches in your Maps belong to Tul", "Breaches in your Maps contain 3 additional Clasped Hands", "", "3 uses remaining", StatTypeGGG.MapBreachTypeOverride, 3, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Elevated Sextant", false, "Breaches in your Maps belong to Tul", "Breaches in your Maps contain 3 additional Clasped Hands", "", "15 uses remaining", StatTypeGGG.MapBreachTypeOverride, 3, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Awakened Sextant", false, "Breaches in your Maps belong to Xoph", "Breaches in your Maps contain 3 additional Clasped Hands", "", "3 uses remaining", StatTypeGGG.MapBreachTypeOverride, 2, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Elevated Sextant", false, "Breaches in your Maps belong to Xoph", "Breaches in your Maps contain 3 additional Clasped Hands", "", "15 uses remaining", StatTypeGGG.MapBreachTypeOverride, 2, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Awakened Sextant", false, "Breaches in your Maps belong to Uul-Netol", "Breaches in your Maps contain 3 additional Clasped Hands", "", "3 uses remaining", StatTypeGGG.MapBreachTypeOverride, 1, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Elevated Sextant", false, "Breaches in your Maps belong to Uul-Netol", "Breaches in your Maps contain 3 additional Clasped Hands", "", "15 uses remaining", StatTypeGGG.MapBreachTypeOverride, 1, StatTypeGGG.MapBreachesNumAdditionalChestsToSpawn),
                new SextantModSelectorClass("Awakened Sextant", false, "Catalysts dropped by Metamorphs in your Maps are Duplicated", "Metamorphs in your Maps have 100% more Life", "", "3 uses remaining", StatTypeGGG.MapMetamorphCatalystDropsDuplicated, 0, StatTypeGGG.MapMetamorphosisBossLifePosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Catalysts dropped by Metamorphs in your Maps are Duplicated", "Metamorphs in your Maps have 100% more Life", "", "15 uses remaining", StatTypeGGG.MapMetamorphCatalystDropsDuplicated, 0, StatTypeGGG.MapMetamorphosisBossLifePosPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Create a copy of Beasts Captured in your Maps", "", "", "3 uses remaining", StatTypeGGG.MapDuplicateCapturedBeastsChancePct),
                new SextantModSelectorClass("Elevated Sextant", false, "Create a copy of Beasts Captured in your Maps", "", "", "15 uses remaining", StatTypeGGG.MapDuplicateCapturedBeastsChancePct),
                new SextantModSelectorClass("Awakened Sextant", false, "Delirium Reward Bars fill 100% faster in your Maps", "", "", "3 uses remaining", StatTypeGGG.MapAfflictionRewardKillsPosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Delirium Reward Bars fill 100% faster in your Maps", "", "", "15 uses remaining", StatTypeGGG.MapAfflictionRewardKillsPosPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Items found in your Identified Maps are Identified", "20% increased Pack Size in your Unidentified Maps", "", "3 uses remaining", StatTypeGGG.MapPackSizePosPctInUnidentifiedAreas, 20, StatTypeGGG.MapEquipmentDropsIdentifiedInIdentifiedAreas),
                new SextantModSelectorClass("Elevated Sextant", false, "Items found in your Identified Maps are Identified", "25% increased Pack Size in your Unidentified Maps", "", "15 uses remaining", StatTypeGGG.MapPackSizePosPctInUnidentifiedAreas, 20, StatTypeGGG.MapEquipmentDropsIdentifiedInIdentifiedAreas),
                new SextantModSelectorClass("Awakened Sextant", false, "Legion Monsters in your Maps have 100% more Life", "Splinters and Emblems dropped by Legion Monsters in your Maps are duplicated", "", "3 uses remaining", StatTypeGGG.MapLegionMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapLegionMonsterSplinterEmblemDropsDuplicated),
                new SextantModSelectorClass("Elevated Sextant", false, "Legion Monsters in your Maps have 100% more Life", "Splinters and Emblems dropped by Legion Monsters in your Maps are duplicated", "", "15 uses remaining", StatTypeGGG.MapLegionMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapLegionMonsterSplinterEmblemDropsDuplicated),
                new SextantModSelectorClass("Awakened Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Blue Plants", "3 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 3),
                new SextantModSelectorClass("Elevated Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Blue Plants", "15 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 3),
                new SextantModSelectorClass("Awakened Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Purple Plants", "3 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 1),
                new SextantModSelectorClass("Elevated Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Purple Plants", "15 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 1),
                new SextantModSelectorClass("Awakened Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Yellow Plants", "3 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 2),
                new SextantModSelectorClass("Elevated Sextant", false, "Lifeforce dropped by Harvest Monsters in your Maps is Duplicated", "Harvest Monsters in your Maps have 100% more Life", "Harvests in your Maps contain at least one Crop of Yellow Plants", "15 uses remaining", StatTypeGGG.MapHarvestDoubleLifeforceDropped, 0, StatTypeGGG.MapHarvestMonsterLifePosPctFinalFromSextant, 0, StatTypeGGG.MapHarvestSeeds1OfEvery2PlotTypeOverride, 2),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses are accompanied by a mysterious Harbinger", "Map Bosses drop additional Currency Shards", "Harbingers in your Maps drop additional Currency Shards", "3 uses remaining", StatTypeGGG.MapBossAccompaniedByHarbinger, 0, StatTypeGGG.MapBossDropsAdditionalCurrencyShards, 0, StatTypeGGG.MapHarbingersDropsAdditionalCurrencyShards),
                new SextantModSelectorClass("Elevated Sextant", false, "Map Bosses are accompanied by a mysterious Harbinger", "Map Bosses drop additional Currency Shards", "Harbingers in your Maps drop additional Currency Shards", "15 uses remaining", StatTypeGGG.MapBossAccompaniedByHarbinger, 0, StatTypeGGG.MapBossDropsAdditionalCurrencyShards, 0, StatTypeGGG.MapHarbingersDropsAdditionalCurrencyShards),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses are accompanied by Bodyguards", "An additional Map drops on Completing your Maps", "", "3 uses remaining", StatTypeGGG.MapOnCompleteDropXAdditionalMaps, 1, StatTypeGGG.MapBossAccompaniedByBodyguards),
                new SextantModSelectorClass("Elevated Sextant", false, "Map Bosses are accompanied by Bodyguards", "2 additional Maps drop on Completing your Maps", "", "15 uses remaining", StatTypeGGG.MapOnCompleteDropXAdditionalMaps, 2, StatTypeGGG.MapBossAccompaniedByBodyguards),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses deal 20% increased Damage", "Your Maps have 20% Quality", "", "3 uses remaining", StatTypeGGG.MapBossDamagePosPct, 20, StatTypeGGG.MapHasXPctQuality, 20),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses drop 1 additional Unique Item", "", "", "3 uses remaining", StatTypeGGG.MapBossAdditionalUniquesToDrop),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses have 20% increased Life", "Quality bonus of your Maps also applies to Rarity of Items found", "", "3 uses remaining", StatTypeGGG.MapBossMaximumLifePosPct, 20, StatTypeGGG.MapItemDropQualityAlsoAppliesToMapItemDropRarity),
                new SextantModSelectorClass("Awakened Sextant", false, "Map Bosses of your Corrupted Maps drop 2 additional Vaal Items", "Items found in your Maps have 5% chance to be Corrupted", "", "3 uses remaining", StatTypeGGG.MapCorruptedBossesDropXAdditionalVaalItems, 2, StatTypeGGG.MapItemsDropCorrupted, 5),
                new SextantModSelectorClass("Elevated Sextant", false, "Map Bosses of your Corrupted Maps drop 3 additional Vaal Items", "Items found in your Maps have 5% chance to be Corrupted", "", "15 uses remaining", StatTypeGGG.MapCorruptedBossesDropXAdditionalVaalItems, 3, StatTypeGGG.MapItemsDropCorrupted, 5),
                new SextantModSelectorClass("Awakened Sextant", false, "Maps found in your Maps are Corrupted with 8 Modifiers", "", "", "3 uses remaining", StatTypeGGG.MapDroppedMapsAreCorruptedWith8Mods),
                new SextantModSelectorClass("Elevated Sextant", false, "Maps found in your Maps are Corrupted with 8 Modifiers", "", "", "15 uses remaining", StatTypeGGG.MapDroppedMapsAreCorruptedWith8Mods),
                new SextantModSelectorClass("Elevated Sextant", false, "Monsters Imprisoned by Essences have a 50% chance to contain a Remnant of Corruption", "Your Maps contain 2 additional Essences", "", "15 uses remaining", StatTypeGGG.MapExtraMonoliths, 2, StatTypeGGG.MapEssenceMonolithContainsEssenceOfCorruptionPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Monsters Imprisoned by Essences have a 50% chance to contain a Remnant of Corruption", "Your Maps contain an additional Essence", "", "3 uses remaining", StatTypeGGG.MapExtraMonoliths, 2, StatTypeGGG.MapEssenceMonolithContainsEssenceOfCorruptionPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Non-Unique Heist Contracts found in your Maps have an additional Implicit Modifier", "", "", "3 uses remaining", StatTypeGGG.MapContractsDropWithAdditionalSpecialImplicitPctChance),
                new SextantModSelectorClass("Elevated Sextant", false, "Non-Unique Heist Contracts found in your Maps have an additional Implicit Modifier", "", "", "15 uses remaining", StatTypeGGG.MapContractsDropWithAdditionalSpecialImplicitPctChance),
                new SextantModSelectorClass("Awakened Sextant", false, "Oils found in your Maps are 1 tier higher", "Cost of Building and Upgrading Blight Towers in your Maps is doubled", "", "3 uses remaining", StatTypeGGG.MapBlightOilsChanceToDropATierHigherPct, 0, StatTypeGGG.MapBlightTowerCostDoubled),
                new SextantModSelectorClass("Elevated Sextant", false, "Oils found in your Maps are 1 tier higher", "Cost of Building and Upgrading Blight Towers in your Maps is doubled", "", "15 uses remaining", StatTypeGGG.MapBlightOilsChanceToDropATierHigherPct, 0, StatTypeGGG.MapBlightTowerCostDoubled),
                new SextantModSelectorClass("Awakened Sextant", false, "Player's Life and Mana Recovery from Flasks are instant", "Your Maps contain 6 additional packs of Monsters that Heal", "", "3 uses remaining", StatTypeGGG.MapPlayerFlaskRecoveryIsInstant, 0, StatTypeGGG.MapContainsXAdditionalHealingPacks, 6),
                new SextantModSelectorClass("Elevated Sextant", false, "Player's Life and Mana Recovery from Flasks are instant", "Your Maps contain 8 additional packs of Monsters that Heal", "", "15 uses remaining", StatTypeGGG.MapPlayerFlaskRecoveryIsInstant, 0, StatTypeGGG.MapContainsXAdditionalHealingPacks, 8),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and Monsters take 12% increased Chaos Damage", "Your Maps contain 6 additional packs of Monsters that deal Chaos Damage", "", "3 uses remaining", StatTypeGGG.MapPlayersAndMonstersChaosDamageTakenPosPct, 12, StatTypeGGG.MapContainsAdditionalPacksOfChaosMonsters, 6),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and Monsters take 12% increased Cold Damage", "Your Maps contain 6 additional packs of Monsters that deal Cold Damage", "", "3 uses remaining", StatTypeGGG.MapPlayersAndMonstersColdDamageTakenPosPct, 12, StatTypeGGG.MapContainsAdditionalPacksOfColdMonsters, 6),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and Monsters take 12% increased Fire Damage", "Your Maps contain 6 additional packs of Monsters that deal Fire Damage", "", "3 uses remaining", StatTypeGGG.MapPlayersAndMonstersFireDamageTakenPosPct, 12, StatTypeGGG.MapContainsAdditionalPacksOfFireMonsters, 6),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and Monsters take 12% increased Lightning Damage", "Your Maps contain 6 additional packs of Monsters that deal Lightning Damage", "", "3 uses remaining", StatTypeGGG.MapPlayersAndMonstersLightningDamageTakenPosPct, 12, StatTypeGGG.MapContainsAdditionalPacksOfLightningMonsters, 6),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and Monsters take 12% increased Physical Damage", "Your Maps contain 6 additional packs of Monsters that deal Physical Damage", "", "3 uses remaining", StatTypeGGG.MapPlayersAndMonstersPhysicalDamageTakenPosPct, 12, StatTypeGGG.MapContainsAdditionalPacksOfPhysicalMonsters, 6),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and Monsters take 14% increased Chaos Damage", "Your Maps contain 8 additional packs of Monsters that deal Chaos Damage", "", "15 uses remaining", StatTypeGGG.MapPlayersAndMonstersChaosDamageTakenPosPct, 14, StatTypeGGG.MapContainsAdditionalPacksOfChaosMonsters, 8),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and Monsters take 14% increased Cold Damage", "Your Maps contain 8 additional packs of Monsters that deal Cold Damage", "", "15 uses remaining", StatTypeGGG.MapPlayersAndMonstersColdDamageTakenPosPct, 14, StatTypeGGG.MapContainsAdditionalPacksOfColdMonsters, 8),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and Monsters take 14% increased Fire Damage", "Your Maps contain 8 additional packs of Monsters that deal Fire Damage", "", "15 uses remaining", StatTypeGGG.MapPlayersAndMonstersFireDamageTakenPosPct, 14, StatTypeGGG.MapContainsAdditionalPacksOfFireMonsters, 8),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and Monsters take 14% increased Lightning Damage", "Your Maps contain 8 additional packs of Monsters that deal Lightning Damage", "", "15 uses remaining", StatTypeGGG.MapPlayersAndMonstersLightningDamageTakenPosPct, 14, StatTypeGGG.MapContainsAdditionalPacksOfLightningMonsters, 8),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and Monsters take 14% increased Physical Damage", "Your Maps contain 8 additional packs of Monsters that deal Physical Damage", "", "15 uses remaining", StatTypeGGG.MapPlayersAndMonstersPhysicalDamageTakenPosPct, 14, StatTypeGGG.MapContainsAdditionalPacksOfPhysicalMonsters, 8),
                new SextantModSelectorClass("Awakened Sextant", false, "Players and their Minions cannot take Reflected Damage", "Your Maps contain 4 additional Packs with Mirrored Rare Monsters", "", "3 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksWithMirroredRareMonsters, 4, StatTypeGGG.MapPlayersCannotTakeReflectedDamage),
                new SextantModSelectorClass("Elevated Sextant", false, "Players and their Minions cannot take Reflected Damage", "Your Maps contain 5 additional Packs with Mirrored Rare Monsters", "", "15 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksWithMirroredRareMonsters, 5, StatTypeGGG.MapPlayersCannotTakeReflectedDamage),
                new SextantModSelectorClass("Awakened Sextant", false, "Players gain an additional Vaal Soul on Kill", "Your Maps contain 6 additional packs of Corrupted Vaal Monsters", "", "3 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 6, StatTypeGGG.MapPlayerChanceToGainVaalSoulOnKillPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Players gain an additional Vaal Soul on Kill", "Your Maps contain 8 additional packs of Corrupted Vaal Monsters", "", "15 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 8, StatTypeGGG.MapPlayerChanceToGainVaalSoulOnKillPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Players' Vaal Skills do not apply Soul Gain Prevention", "Your Maps contain 6 additional packs of Corrupted Vaal Monsters", "", "3 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 6, StatTypeGGG.MapPlayerDisableSoulGainPrevention),
                new SextantModSelectorClass("Elevated Sextant", false, "Players' Vaal Skills do not apply Soul Gain Prevention", "Your Maps contain 8 additional packs of Corrupted Vaal Monsters", "", "15 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 8, StatTypeGGG.MapPlayerDisableSoulGainPrevention),
                new SextantModSelectorClass("Awakened Sextant", false, "Rerolling Favours at Ritual Altars in your Maps has no Cost the first 1 time", "", "", "3 uses remaining", StatTypeGGG.MapRitualNumberOfFreeRerolls),
                new SextantModSelectorClass("Elevated Sextant", false, "Rerolling Favours at Ritual Altars in your Maps has no Cost the first 1 time", "", "", "15 uses remaining", StatTypeGGG.MapRitualNumberOfFreeRerolls),
                new SextantModSelectorClass("Awakened Sextant", false, "Slaying Enemies close together can attract monsters from Beyond this realm", "25% increased Beyond Demon Pack Size in your Maps", "", "3 uses remaining", StatTypeGGG.MapBeyondDemonPackSizePosPct, 25, StatTypeGGG.MapBeyondRules),
                new SextantModSelectorClass("Elevated Sextant", false, "Slaying Enemies close together can attract monsters from Beyond this realm", "35% increased Beyond Demon Pack Size in your Maps", "", "15 uses remaining", StatTypeGGG.MapBeyondDemonPackSizePosPct, 35, StatTypeGGG.MapBeyondRules),
                new SextantModSelectorClass("Awakened Sextant", false, "Rogue Exiles deal 20% increased Damage", "Rogue Exiles drop 2 additional Jewels", "Rogue Exiles in your Maps have 20% increased Life", "3 uses remaining", StatTypeGGG.MapRogueExilesDamagePosPct, 0, StatTypeGGG.MapSpawnExtraExiles, 0, StatTypeGGG.MapRogueExilesMaximumLifePosPct, 0, StatTypeGGG.MapRogueExilesDropXAdditionalJewels),
                new SextantModSelectorClass("Awakened Sextant", false, "Strongbox Monsters are Enraged", "Strongbox Monsters have 500% increased Item Quantity", "Your Maps contain an additional Strongbox", "3 uses remaining", StatTypeGGG.MapStrongboxMonstersItemQuantityPosPct, 500, StatTypeGGG.MapNumExtraStrongboxes),
                new SextantModSelectorClass("Elevated Sextant", false, "Strongbox Monsters are Enraged", "Strongbox Monsters have 600% increased Item Quantity", "Your Maps contain an additional Strongbox", "15 uses remaining", StatTypeGGG.MapStrongboxMonstersItemQuantityPosPct, 600, StatTypeGGG.MapNumExtraStrongboxes),
                new SextantModSelectorClass("Awakened Sextant", false, "The First 3 Possessed Monsters drop an additional Rusted Scarab", "Your Maps contain an additional Tormented Betrayer", "", "3 uses remaining", StatTypeGGG.MapPossessedMonstersDropRustedScarabChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedBetrayers),
                new SextantModSelectorClass("Awakened Sextant", false, "The First 3 Possessed Monsters drop an additional Gilded Scarab", "Your Maps contain an additional Tormented Betrayer", "", "3 uses remaining", StatTypeGGG.MapPossessedMonstersDropGildedScarabChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedBetrayers),
                new SextantModSelectorClass("Awakened Sextant", false, "The First 3 Possessed Monsters drop an additional Polished Scarab", "Your Maps contain an additional Tormented Betrayer", "", "3 uses remaining", StatTypeGGG.MapPossessedMonstersDropPolishedScarabChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedBetrayers),
                new SextantModSelectorClass("Elevated Sextant", false, "The First 3 Possessed Monsters drop an additional Winged Scarab", "Your Maps contain an additional Tormented Betrayer", "", "15 uses remaining", StatTypeGGG.MapPossessedMonstersDropWingedScarabChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedBetrayers),
                new SextantModSelectorClass("Awakened Sextant", false, "The First 3 Possessed Monsters drop an additional Unique Item", "Your Maps contain an additional Tormented Graverobber", "", "3 uses remaining", StatTypeGGG.MapPossessedMonstersDropUniqueChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedGraverobbers),
                new SextantModSelectorClass("Awakened Sextant", false, "The First 3 Possessed Monsters drop an additional Map", "Your Maps contain an additional Tormented Heretic", "", "3 uses remaining", StatTypeGGG.MapPossessedMonstersDropMapChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedHeretics),
                new SextantModSelectorClass("Elevated Sextant", false, "The First 3 Possessed Monsters drop an additional Map", "Your Maps contain an additional Tormented Heretic", "", "15 uses remaining", StatTypeGGG.MapPossessedMonstersDropMapChancePct, 0, StatTypeGGG.MapContainsAdditionalTormentedHeretics),
                new SextantModSelectorClass("Awakened Sextant", false, "Unique Monsters drop Corrupted Items", "", "", "3 uses remaining", StatTypeGGG.MapUniqueMonstersDropCorruptedItems),
                new SextantModSelectorClass("Elevated Sextant", false, "Unique Monsters drop Corrupted Items", "", "", "15 uses remaining", StatTypeGGG.MapUniqueMonstersDropCorruptedItems),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Magic Maps contain 4 additional packs of Magic Monsters", "Your Normal Maps contain 4 additional packs of Normal Monsters", "Your Rare Maps contain 4 additional Rare Monster packs", "3 uses remaining", StatTypeGGG.MapContainsXAdditionalNormalPacksIfNormal, 4, StatTypeGGG.MapContainsXAdditionalMagicPacksIfMagic, 4, StatTypeGGG.MapContainsXAdditionalRarePacksIfRare, 4),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Magic Maps contain 5 additional packs of Magic Monsters", "Your Normal Maps contain 5 additional packs of Normal Monsters", "Your Rare Maps contain 5 additional Rare Monster packs", "15 uses remaining", StatTypeGGG.MapContainsXAdditionalNormalPacksIfNormal, 5, StatTypeGGG.MapContainsXAdditionalMagicPacksIfMagic, 5, StatTypeGGG.MapContainsXAdditionalRarePacksIfRare, 5),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps are Alluring", "", "", "3 uses remaining", StatTypeGGG.MapFishyEffect0, 0, StatTypeGGG.MapFishyEffect1, 10, StatTypeGGG.MapFishyEffect2),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 2 additional Abysses", "Your Maps can contain Abysses", "", "15 uses remaining", StatTypeGGG.MapNumExtraAbysses, 0 ,StatTypeGGG.MapSpawnAbysses),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps can contain Breaches", "Your Maps contain an additional Breach", "", "3 uses remaining", StatTypeGGG.MapContainsAdditionalBreaches, 1, StatTypeGGG.MapBreachRules),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps can contain Breaches", "Your Maps contain 2 additional Breach", "", "15 uses remaining", StatTypeGGG.MapContainsAdditionalBreaches, 2, StatTypeGGG.MapBreachRules),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 2 additional Strongboxes", "Strongboxes in your Maps are Corrupted", "Strongboxes in your Maps are at least Rare", "3 uses remaining", StatTypeGGG.MapNumExtraStrongboxes, 2, StatTypeGGG.MapStrongboxesAreCorrupted, 0, StatTypeGGG.MapStrongboxesMinimumRarity),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 3 additional Strongboxes", "Strongboxes in your Maps are Corrupted", "Strongboxes in your Maps are at least Rare", "15 uses remaining", StatTypeGGG.MapNumExtraStrongboxes, 3, StatTypeGGG.MapStrongboxesAreCorrupted, 0, StatTypeGGG.MapStrongboxesMinimumRarity),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 6 additional packs of Corrupted Vaal Monsters", "Items dropped by Corrupted Vaal Monsters in your Maps have 25% chance to be Corrupted", "", "3 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 6, StatTypeGGG.MapVaalMonsterItemsDropCorruptedPct, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 8 additional packs of Corrupted Vaal Monsters", "Items dropped by Corrupted Vaal Monsters in your Maps have 30% chance to be Corrupted", "", "15 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 8, StatTypeGGG.MapVaalMonsterItemsDropCorruptedPct, 30),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 6 additional packs of Corrupted Vaal Monsters", "Your Maps have a 50% chance to contain Gifts of the Red Queen per Mortal Fragment used", "Your Maps have a 50% chance to contain Gifts of the Sacrificed per Sacrifice Fragment used", "3 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 6, StatTypeGGG.MapVaalMortalStrongboxChancePerFragmentPct, 0, StatTypeGGG.MapVaalSacrificeStrongboxChancePerFragmentPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 8 additional packs of Corrupted Vaal Monsters", "Your Maps have a 50% chance to contain Gifts of the Red Queen per Mortal Fragment used", "Your Maps have a 50% chance to contain Gifts of the Sacrificed per Sacrifice Fragment used", "15 uses remaining", StatTypeGGG.MapContainsAdditionalPacksOfVaalMonsters, 8, StatTypeGGG.MapVaalMortalStrongboxChancePerFragmentPct, 0, StatTypeGGG.MapVaalSacrificeStrongboxChancePerFragmentPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 6 additional packs of Monsters that Convert when Killed", "", "", "3 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksThatConvertOnDeath, 6),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 8 additional packs of Monsters that Convert when Killed", "", "", "15 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksThatConvertOnDeath, 8),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 25 additional Clusters of Wealthy Mysterious Barrels", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfWealthyBarrels, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 35 additional Clusters of Wealthy Mysterious Barrels", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfWealthyBarrels, 35),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 25 additional Clusters of Volatile Mysterious Barrels", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfVolatileBarrels, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 35 additional Clusters of Volatile Mysterious Barrels", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfVolatileBarrels, 35),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 25 additional Clusters of BloodWorm Mysterious Barrels", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfBloodwormBarrels, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 35 additional Clusters of BloodWorm Mysterious Barrels", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfBloodwormBarrels, 35),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 25 additional Clusters of Parasite Mysterious Barrels", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfParasiteBarrels, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 35 additional Clusters of Parasite Mysterious Barrels", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfParasiteBarrels, 35),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 25 additional Clusters of Beacon Mysterious Barrels", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfBeaconBarrels, 25),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 35 additional Clusters of Beacon Mysterious Barrels", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsXAdditionalClustersOfBeaconBarrels, 35),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain 100% increased number of Runic Monster Markers", "", "", "3 uses remaining", StatTypeGGG.MapExpeditionEliteMarkerCountPosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain 100% increased number of Runic Monster Markers", "", "", "15 uses remaining", StatTypeGGG.MapExpeditionEliteMarkerCountPosPct),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain a Blight Encounter", "", "", "3 uses remaining", StatTypeGGG.MapNumExtraBlights),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain a Blight Encounter", "", "", "15 uses remaining", StatTypeGGG.MapNumExtraBlights),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain a Mirror of Delirium", "", "", "3 uses remaining", StatTypeGGG.MapSpawnAfflictionMirror),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain a Mirror of Delirium", "", "", "15 uses remaining", StatTypeGGG.MapSpawnAfflictionMirror),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain Alva", "", "", "3 uses remaining", StatTypeGGG.MapMissionId, 3),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain Alva", "", "", "15 uses remaining", StatTypeGGG.MapMissionId, 3),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain Niko", "", "", "3 uses remaining", StatTypeGGG.MapMissionId, 5),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain Niko", "", "", "15 uses remaining", StatTypeGGG.MapMissionId, 5),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain Jun", "", "", "3 uses remaining", StatTypeGGG.MapMissionId, 6),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain Jun", "", "", "15 uses remaining", StatTypeGGG.MapMissionId, 6),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain Eihar", "", "", "3 uses remaining", StatTypeGGG.MapMissionId, 2),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain Eihar", "", "", "15 uses remaining", StatTypeGGG.MapMissionId, 2),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain an additional Abyss", "Your Maps can contain Abysses", "", "3 uses remaining", StatTypeGGG.MapSpawnAbysses, 0, StatTypeGGG.MapNumExtraAbysses),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain an additional Gloom Shrine", "50% increased Duration of Shrine Effects on Players in your Maps", "", "3 uses remaining", StatTypeGGG.MapNumExtraGloomShrines, 0, StatTypeGGG.MapPlayerShrineEffectDurationPosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain an additional Gloom Shrine", "50% increased Duration of Shrine Effects on Players in your Maps", "Your Maps contain an additional Shrine", "15 uses remaining", StatTypeGGG.MapNumExtraGloomShrines, 0, StatTypeGGG.MapPlayerShrineEffectDurationPosPct, 0, StatTypeGGG.MapNumExtraShrines, 1),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain an additional Legion Encounter", "", "", "3 uses remaining", StatTypeGGG.MapLegionLeagueExtraSpawns),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain an additional Legion Encounter", "", "", "15 uses remaining", StatTypeGGG.MapLegionLeagueExtraSpawns),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain an additional Resonating Shrine", "50% increased Duration of Shrine Effects on Players in your Maps", "Your Maps contain an additional Shrine", "3 uses remaining", StatTypeGGG.MapNumExtraResonatingShrines, 0, StatTypeGGG.MapPlayerShrineEffectDurationPosPct),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain an additional Resonating Shrine", "50% increased Duration of Shrine Effects on Players in your Maps", "Your Maps contain an additional Shrine", "15 uses remaining", StatTypeGGG.MapNumExtraResonatingShrines, 0, StatTypeGGG.MapPlayerShrineEffectDurationPosPct, 0, StatTypeGGG.MapNumExtraShrines, 1),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain hunted traitors", "", "", "3 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksOnTheirOwnTeam),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain hunted traitors", "", "", "15 uses remaining", StatTypeGGG.MapContainsXAdditionalPacksOnTheirOwnTeam),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain Ritual Altars", "", "", "3 uses remaining", StatTypeGGG.MapAreaContainsRituals),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain Ritual Altars", "", "", "15 uses remaining", StatTypeGGG.MapAreaContainsRituals),
                new SextantModSelectorClass("Awakened Sextant", false, "Your Maps contain The Sacred Grove", "", "", "3 uses remaining", StatTypeGGG.MapChanceForAreaPctToContainHarvest),
                new SextantModSelectorClass("Elevated Sextant", false, "Your Maps contain The Sacred Grove", "", "", "15 uses remaining", StatTypeGGG.MapChanceForAreaPctToContainHarvest)
            };
            return _modss;
        }
    }
}
