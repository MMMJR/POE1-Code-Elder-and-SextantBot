using System.Collections.Generic;
using System.ComponentModel;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;

namespace ExileRoutine
{
    /// <summary>Settings for the ExileRoutine. </summary>
    public class ExileRoutineSettings : JsonSettings
    {
        private static ExileRoutineSettings _instance;

        /// <summary>The current instance for this class. </summary>
        public static ExileRoutineSettings Instance => _instance ?? (_instance = new ExileRoutineSettings());

	    /// <summary>The default ctor. Will use the settings path "ExileRoutine".</summary>
        public ExileRoutineSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, string.Format("{0}.json", "ExileRoutine")))
        {
        }

        private int _singleTargetDPSSlot;
        private int _aoESlot;
        private int _movementSlot;
        private int _curseOnHitSlot;
        private int _totemSlotOne;
        private int _totemSlotTwo;
        private int _combatRange;
        private int _chickenPercent;
        private int _maxRange;
        private int _aoeRange;
        private int _moveSkillRange;
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
        private int _totemOneDelay;
        private int _totemTwoDelay;
        private int _totemOneMax;
        private int _totemTwoMax;
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
        [DefaultValue(1)]
        public int TotemOneMax
        {
            get { return _totemOneMax; }
            set
            {
                if (value.Equals(_totemOneMax))
                {
                    return;
                }
                _totemOneMax = value;
                NotifyPropertyChanged(() => TotemOneMax);
            }
        }

        /// <summary>
        /// How long should the CR wait before using mines again.
        /// </summary>
        [DefaultValue(1)]
        public int TotemTwoMax
        {
            get { return _totemTwoMax; }
            set
            {
                if (value.Equals(_totemTwoMax))
                {
                    return;
                }
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
        [DefaultValue(-1)]
        public int SingleTargetDPSSlot
        {
            get { return _singleTargetDPSSlot; }
            set
            {
                if (value.Equals(_singleTargetDPSSlot))
                {
                    return;
                }
                _singleTargetDPSSlot = value;
                NotifyPropertyChanged(() => SingleTargetDPSSlot);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int AoESlot
        {
            get { return _aoESlot; }
            set
            {
                if (value.Equals(_aoESlot))
                {
                    return;
                }
                _aoESlot = value;
                NotifyPropertyChanged(() => AoESlot);
            }
        }

        /// <summary>
        /// The skill slot to use in melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int MovementSlot
        {
            get { return _movementSlot; }
            set
            {
                if (value.Equals(_movementSlot))
                {
                    return;
                }
                _movementSlot = value;
                NotifyPropertyChanged(() => MovementSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int CurseOnHitSlot
        {
            get { return _curseOnHitSlot; }
            set
            {
                if (value.Equals(_curseOnHitSlot))
                {
                    return;
                }
                _curseOnHitSlot = value;
                NotifyPropertyChanged(() => CurseOnHitSlot);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int TotemSlotOne
        {
            get { return _totemSlotOne; }
            set
            {
                if (value.Equals(_totemSlotOne))
                {
                    return;
                }
                _totemSlotOne = value;
                NotifyPropertyChanged(() => TotemSlotOne);
            }
        }

        /// <summary>
        /// The skill slot to use outside of melee range.
        /// </summary>
        [DefaultValue(-1)]
        public int TotemSlotTwo
        {
            get { return _totemSlotTwo; }
            set
            {
                if (value.Equals(_totemSlotTwo))
                {
                    return;
                }
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
        [DefaultValue(10)]
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
        [DefaultValue(15)]
        public int MoveSkillRange
        {
            get { return _moveSkillRange; }
            set
            {
                if (value.Equals(_moveSkillRange))
                {
                    return;
                }
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
        [DefaultValue(1500)]
        public int TotemTwoDelay
        {
            get { return _totemTwoDelay; }
            set
            {
                if (value.Equals(_totemTwoDelay))
                {
                    return;
                }
                _totemTwoDelay = value;
                NotifyPropertyChanged(() => TotemTwoDelay);
            }
        }

        [DefaultValue(1500)]
        public int TotemOneDelay
        {
            get { return _totemOneDelay; }
            set
            {
                if (value.Equals(_totemOneDelay))
                {
                    return;
                }
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

        [JsonIgnore] private List<int> _allSkillSlots;

        /// <summary> </summary>
        [JsonIgnore]
        public List<int> AllSkillSlots => _allSkillSlots ?? (_allSkillSlots = new List<int>
        {
            -1,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8
        });
    }
}
