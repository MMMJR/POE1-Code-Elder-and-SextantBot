using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using log4net;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using DreamPoeBot.Loki.Coroutine;
using MmmjrBot.MapperBot;
using System.Windows;
using MmmjrBot.Class;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;
using static MmmjrBot.MmmjrBotSettings;

namespace MmmjrBot
{
	/// <summary> </summary>
	public static class MmmjrRoutine
	{
		public static bool mainweapon;
		
		#region Temp Compatibility 

		public static int NumberOfMobsBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 5,
			bool dontLeaveFrame = false)
		{
			var mobs = LokiPoe.ObjectManager.GetObjectsByType<Monster>().Where(d => d.IsAliveHostile).ToList();
			if (!mobs.Any())
				return 0;

			var path = ExilePather.GetPointsOnSegment(start.Position, end.Position, dontLeaveFrame);

			var count = 0;
			for (var i = 0; i < path.Count; i += 10)
			{
				foreach (var mob in mobs)
				{
					if (mob.Position.Distance(path[i]) <= distanceFromPoint)
					{
						++count;
					}
				}
			}

			return count;
		}

		// <param name="start"></param>
		// <param name="end"></param>
		// <param name="distanceFromPoint">How far to check around each point for a door object.</param>
		// <param name="stride">The distance between points to check in the path.</param>
		// <param name="dontLeaveFrame">Should the current frame not be left?</param>
		
		public static bool ClosedDoorBetween(NetworkObject start, NetworkObject end, int distanceFromPoint = 10,
			int stride = 10, bool dontLeaveFrame = false)
		{
			return ClosedDoorBetween(start.Position, end.Position, distanceFromPoint, stride, dontLeaveFrame);
		}
		
		public static bool ClosedDoorBetween(NetworkObject start, Vector2i end, int distanceFromPoint = 10,
			int stride = 10, bool dontLeaveFrame = false)
		{
			return ClosedDoorBetween(start.Position, end, distanceFromPoint, stride, dontLeaveFrame);
		}
		
		public static bool ClosedDoorBetween(Vector2i start, NetworkObject end, int distanceFromPoint = 10,
			int stride = 10, bool dontLeaveFrame = false)
		{
			return ClosedDoorBetween(start, end.Position, distanceFromPoint, stride, dontLeaveFrame);
		}
		
		public static bool ClosedDoorBetween(Vector2i start, Vector2i end, int distanceFromPoint = 10, int stride = 10,
			bool dontLeaveFrame = false)
		{
			var doors = LokiPoe.ObjectManager.AnyDoors.Where(d => !d.IsOpened).ToList();
			if (!doors.Any())
				return false;

			var path = ExilePather.GetPointsOnSegment(start, end, dontLeaveFrame);

			for (var i = 0; i < path.Count; i += stride)
			{
				foreach (var door in doors)
				{
					if (door.Position.Distance(path[i]) <= distanceFromPoint)
					{
						return true;
					}
				}
			}

			return false;
		}
		
		public static int NumberOfMobsNear(NetworkObject target, float distance, bool dead = false)
		{
			var mpos = target.Position;

			var curCount = 0;

			foreach (var mob in LokiPoe.ObjectManager.Objects.OfType<Monster>())
			{
				if (mob.Id == target.Id)
				{
					continue;
				}

				if (dead)
				{
					if (!mob.IsDead)
					{
						continue;
					}
				}
				else if (!mob.IsAliveHostile)
				{
					continue;
				}

				if (mob.Position.Distance(mpos) < distance)
				{
					curCount++;
				}
			}

			return curCount;
		}

		#endregion

		private static readonly ILog Log = Logger.GetLoggerInstanceForType();

		private static int _totalCursesAllowed;

		private static int _sgSlot = -1;
		private static int _summonChaosGolemSlot = -1;
		private static int _summonIceGolemSlot = -1;
		private static int _summonFlameGolemSlot = -1;
		private static int _summonStoneGolemSlot = -1;
		private static int _summonLightningGolemSlot = -1;
		private static int _raiseZombieSlot = -1;
		private static int _summonBestialUrsaSlot = -1;
		private static int _summonBestialRhoaSlot = -1;
		private static int _summonBestialSnakeSlot = -1;
		private static int _raiseSpectreSlot = -1;
		private static int _desecrateSlot = -1;
		private static int _animateWeaponSlot = -1;
		private static int _animateGuardianSlot = -1;
		private static int _flameblastSlot = -1;
		private static int _bladeflurrySlot = -1;
		private static int _orbOfStormsSlot = -1;
		private static int _enduringCrySlot = -1;
		private static int _moltenShellSlot = -1;
		private static int _bloodRageSlot = -1;
		private static int _tempestShieldSlot = -1;
		private static int _rfSlot = -1;
		private static int _vaalDiscSlot = -1;
		private static int _vaalSummonSkeletonsSlot = -1;
		private static int _vaalGraceSlot = -1;
		private static int _vaalHasteSlot = -1;
		private static int _vaalRainSlot = -1;
		private static int _auraSlot = -1;
		private static int _trapSlot = -1;
		private static int _mineSlot = -1;
		private static int _summonSkeletonsSlot = -1;
		private static int _srsSlot = -1;
		private static int _coldSnapSlot = -1;
		private static int _fleshOfferingSlot = -1;
		private static int _convocationSlot = -1;
		private static int _immortalCallSlot  = -1;
		private static int _lightningTendrilsSlot  = -1;
		private static int _scorchingRaySlot  = -1;
		private static int _contagionSlot = -1;
		private static int _witherSlot = -1;

		private static int _currentLeashRange = -1;

		private static bool _needsUpdate;
		
		private static readonly List<int> _curseSlots = new List<int>();
		
		private static readonly List<int> _ignoreAnimatedItems = new List<int>();

		private static readonly Targeting _combatTargeting = new Targeting();

		private static readonly Stopwatch _trapStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _totemoneStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _totemtwoStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _mineStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _animateWeaponStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _animateGuardianStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _moltenShellStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _orbOfStormsStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _fleshOfferingStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _convocationStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _fuelCartStopwatch = Stopwatch.StartNew();
		private static readonly Stopwatch _targetStopwatch = Stopwatch.StartNew();

		private static Dictionary<string, Func<Tuple<object, string>[], object>> _exposedSettings;

		private static void RegisterExposedSettings()
		{
			if (_exposedSettings != null)
				return;

			_exposedSettings = new Dictionary<string, Func<Tuple<object, string>[], object>>();

			_exposedSettings.Add("SetLeash", param =>
			{
				_currentLeashRange = (int) param[0].Item1;
				return null;
			});

			_exposedSettings.Add("GetLeash", param =>
			{
				return _currentLeashRange;
			});

			_currentLeashRange = MmmjrBotSettings.Instance.CombatRange;


            PropertyInfo[] properties = typeof(MmmjrBotSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo p in properties)
			{
				
				if (p.PropertyType != typeof(int) && p.PropertyType != typeof(bool))
				{
					continue;
				}
				
				if (!p.CanWrite || !p.CanRead)
				{
					continue;
				}

				MethodInfo mget = p.GetGetMethod(false);
				MethodInfo mset = p.GetSetMethod(false);
				
				if (mget == null)
				{
					continue;
				}
				if (mset == null)
				{
					continue;
				}

				Log.InfoFormat("Name: {0} ({1})", p.Name, p.PropertyType);

				_exposedSettings.Add("Set" + p.Name, param =>
				{
					p.SetValue(MmmjrBotSettings.Instance, param[0]);
					return null;
				});

				_exposedSettings.Add("Get" + p.Name, param =>
				{
					return p.GetValue(MmmjrBotSettings.Instance);
				});
			}
		}

        private static bool IsBlacklistedSkill(int id)
		{
			var tokens = MmmjrBotSettings.Instance.BlacklistedSkillIds.Split(new[]
			{
				' ', ',', ';', '-'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var token in tokens)
			{
				int result;
				if (int.TryParse(token, out result))
				{
					if (result == id)
						return true;
				}
			}
			return false;
		}

		public static Targeting CombatTargeting => _combatTargeting;

		#region Targeting

		private static void CombatTargetingOnWeightCalculation(NetworkObject entity, ref float weight)
		{
			var m = entity as Monster;
			if (m == null)
				return;
				
			if (m.HasAura("monster_aura_cannot_die"))
			{	
				weight += 100;
			}

			if (m.Type.Contains("/BeastHeart"))
			{
				weight += 80;
			}

			if (m.Metadata == "Metadata/Monsters/Tukohama/TukohamaShieldTotem")
			{
				weight += 70;
			}
			
			if (m.Rarity == Rarity.Unique)
			{
				weight += 50;
			}
			
			if (m.IsStrongboxMinion || m.IsBreachMonster || m.IsCorruptedMissionBeast || m.IsMissionMob)
			{
				weight += 30;
			}
			
			if (m.ExplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")) || m.ImplicitAffixes.Any(a => a.InternalName.Contains("RaisesUndead")))
			{
				weight += 15;
			}
			
			if (m.Distance <= 50)
			{
				weight += 5;
			}
			
			if (m.Distance <= 20)
			{
				weight += 5;
			}
		}

		private static readonly string[] _aurasToIgnore = new[]
		{
			"shrine_godmode", // Ignore any mob near Divine Shrine
			"bloodlines_invulnerable", // Ignore Phylacteral Link
			"god_mode", // Ignore Animated Guardian
			"bloodlines_necrovigil",
		};

		private static bool CombatTargetingOnInclusionCalcuation(NetworkObject entity)
		{
			try
			{
				var m = entity as Monster;
				if (m == null)
					return false;

				if (Blacklist.Contains(m))
					return false;

				if (!m.IsActive)
					return false;

				if (m.CannotDie)
					return false;

				if (m.Distance > (_currentLeashRange != -1 ? _currentLeashRange : MmmjrBotSettings.Instance.CombatRange))
					return false;

				if (m.HasAura(_aurasToIgnore))
					return false;

				if (m.ExplicitAffixes.Any(a => a.DisplayName == "Voidspawn of Abaxoth"))
					return false;

				if (m.Name == "Miscreation")
				{
					var dom = LokiPoe.ObjectManager.GetObjectByName<Monster>("Dominus, High Templar");
					if (dom != null && !dom.IsDead &&
						(dom.Components.TransitionableComponent.Flag1 == 6 || dom.Components.TransitionableComponent.Flag1 == 5))
					{
						Blacklist.Add(m.Id, TimeSpan.FromHours(1), "Miscreation");
						return false;
					}
				}
				
				if (m.Name == "Chilling Portal" || m.Name == "Burning Portal")
				{
					Blacklist.Add(m.Id, TimeSpan.FromHours(1), "Piety portal");
					return false;
				}

				if (m.Name == "Lightless Grub")
				{
					Blacklist.Add(m.Id, TimeSpan.FromHours(1), "Lightless Grub");
					return false;
				}
				
				if (m.Type.Contains("TaniwhaTail"))
				{
					Blacklist.Add(m.Id, TimeSpan.FromHours(1), "TaniwhaTail");
					return false;
				}
			}
			catch (Exception ex)
			{
				Log.Error("[CombatOnInclusionCalcuation]", ex);
				return false;
			}
			return true;
		}

		#endregion

		#region Implementation of IBase

		public static void Initialize()
		{
            _combatTargeting.ResetInclusionCalcuation();
            _combatTargeting.ResetWeightCalculation();
            _combatTargeting.InclusionCalcuation += CombatTargetingOnInclusionCalcuation;
			_combatTargeting.WeightCalculation += CombatTargetingOnWeightCalculation;
            RegisterExposedSettings();
		}
		
		public static void Deinitialize()
		{
		}

		#endregion

        #region Implementation of ITickEvents / IStartStopEvents

        public static void Start()
		{
		}

		private static bool IsCastableHelper(Skill skill)
		{
			return skill != null && skill.IsCastable && !skill.IsTrap && !skill.IsMine;
		}

		private static bool IsAuraName(string name)
		{
			if (!MmmjrBotSettings.Instance.EnableAurasFromItems)
			{
				return false;
			}

			var auraNames = new string[]
			{
				"Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Purity of Elements",
				"Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath", "Envy"
			};

			return auraNames.Contains(name);
		}

		/// <summary> The routine tick callback. Do any update logic here. </summary>
		public static void Tick()
		{
			if (!LokiPoe.IsInGame)
				return;
		}

		#endregion

		#region Implementation of IConfigurable

		public static JsonSettings Settings => MmmjrBotSettings.Instance;

		#endregion

		#region Implementation of ILogicProvider

		private static TimeSpan EnduranceChargesTimeLeft
		{
			get
			{
				Aura aura = LokiPoe.Me.EnduranceChargeAura;
				if (aura != null)
				{
					return aura.TimeLeft;
				}

				return TimeSpan.Zero;
			}
		}

		public static async Task<LogicResult> RunCombat()
		{
				
				if (_targetStopwatch.ElapsedMilliseconds > 10)
				{
					CombatTargeting.Update();
					_targetStopwatch.Restart();
				}
				await EnableAlwaysHighlight();
				
				var currentTarget = CombatTargeting.Targets<Monster>().FirstOrDefault();
				
				var guardian = LokiPoe.ObjectManager.GetObjectByName<Monster>("Animated Guardian");

				if (currentTarget == null)
				{

				if ((LokiPoe.MyPosition.X < 165 || LokiPoe.MyPosition.X > 340) || (LokiPoe.MyPosition.Y < 165 || LokiPoe.MyPosition.Y > 340))
				{
                    return LogicResult.Unprovided;
                }

                if (MmmjrBotSettings.Instance.SingleUseElderFragments)
                {
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                    var skill = MmmjrBotSettings.Instance.MapperRoutineSelector.FirstOrDefault<MapperRoutineSkill>(s => (s.SkType == 0 || s.SkType == 1) && s.Enabled);
                    if (skill == null) return await CombatLogicEnd();
                    int slotM = int.Parse(skill.SlotIndex);
                    slotM = EnsurceCast(slotM);
                    if (slotM == -1) return await CombatLogicEnd();
					Vector2i randomPos1 = new Vector2i(1, 1);
                    var r2 = LokiPoe.Random.Next(1, 4);
					if(r2 == 1)
                        randomPos1 = LokiPoe.MyPosition + new Vector2i(38, 38);
                    else if (r2 == 2)
                        randomPos1 = LokiPoe.MyPosition + new Vector2i(-38, 38);
                    else if (r2 == 3)
                        randomPos1 = LokiPoe.MyPosition + new Vector2i(38, -38);
                    else
                        randomPos1 = LokiPoe.MyPosition + new Vector2i(-38, -38);

                    if (MmmjrBotSettings.Instance.CastLong)
                    {
                        SkillBarHud.BeginUseAt(slotM, true, randomPos1);
                    }
                    else
                    {
                        SkillBarHud.BeginUseAt(slotM, true, LokiPoe.Me.Position);
                    }
                    return LogicResult.Unprovided;
                }
            }


				var cachedPosition = currentTarget.Position;
				var cachedId = currentTarget.Id;
				var cachedRarity = currentTarget.Rarity;
				var cachedDistance = currentTarget.Distance;
				var cachedHasCurseFrom = new Dictionary<string, bool>();
				var cachedProxShield = currentTarget.HasProximityShield;
				var cachedHpPercent = (int) currentTarget.HealthPercentTotal;

				if (Blacklist.Contains(cachedId))
				{
					currentTarget = null;
					return LogicResult.Provided;
				}

				if(MmmjrBotSettings.Instance.SingleUseElderFragments)
				{
					if(!currentTarget.IsMapBoss)
					{
						Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as path distance cannot be determined.");
                    return LogicResult.Unprovided;
                }
				}

				if (currentTarget.Metadata.Contains("TentaclePortal") || currentTarget.Name.Contains("Portal") || currentTarget.Name.Contains("Formless") || currentTarget.Name.Contains("Witness"))
				{
					Blacklist.Add(cachedId, TimeSpan.FromHours(5), "Ignoring current target as path distance cannot be determined.");
                return LogicResult.Unprovided;	
            }

				if (currentTarget.Metadata == "Metadata/Monsters/AtlasBosses/ZanaElder" || currentTarget.Metadata == "Metadata/Monsters/AtlasBosses/TheShaperBossElderEncounter" || currentTarget.Metadata == "Metadata/Monsters/ArcticBreath/ArcticBreathSkull" || currentTarget.Metadata == "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStandard")            
                {
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(240), "Ignoring current target as path distance cannot be determined.");
					return LogicResult.Unprovided;
				}
				
				var pathDistance = ExilePather.PathDistance(LokiPoe.MyPosition, cachedPosition, false, false);
				var mapData = MapData.Current;
				if (mapData == null)
				{
                return LogicResult.Unprovided;
            }

				var type = mapData.Type;

				if (pathDistance.CompareTo(float.MaxValue) == 0 && type != MapType.Bossroom)
				{
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as path distance cannot be determined.");
                return LogicResult.Unprovided;
            }

				if (pathDistance > (MmmjrBotSettings.Instance.CombatRange + 50) && type != MapType.Bossroom)
				{
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as its path distance is greater then max combat range.");
                return LogicResult.Unprovided;
            }

				var myspot = LokiPoe.MyPosition;
				var canSee = ExilePather.CanObjectSee(LokiPoe.Me, currentTarget, MmmjrBotSettings.Instance.LeaveFrame);
				var blockedByDoor = ClosedDoorBetween(LokiPoe.Me, currentTarget, 10, 10, MmmjrBotSettings.Instance.LeaveFrame);
				var rangelocation = myspot.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2);
				
				var ranged = false;
				if (MmmjrBotSettings.Instance.Ranged) //Safe Movement?
				{	
					ranged = true;
				}
				
				if (!ExilePather.PathExistsBetween(myspot, cachedPosition, false) && ranged && type != MapType.Bossroom)
				{ 
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as we cannot determine a safe movement position.");
                return LogicResult.Unprovided;
            }

				
				
				if (!canSee || blockedByDoor)
				{
					//if (ranged && rangelocation != null)
					//{
					//	if (!PlayerMoverManager.MoveTowards(rangelocation))
					//	{
					//	}
					//}
					//else
					//{
					if(!MmmjrBotSettings.Instance.SingleUseElderFragments)
				{
                    if (!PlayerMoverManager.MoveTowards(cachedPosition))
                    {
                    }

                }
						
					//}	
					
					return LogicResult.Provided;
				}
				
				int movementskill = -1;
				var moveskillrange = 50.0f;
				
				var movement = false;

				if(!MmmjrBotSettings.Instance.SingleUseElderFragments)
				{
					if (cachedDistance > MmmjrBotSettings.Instance.CombatRange && cachedProxShield)
					{
						PlayerMoverManager.MoveTowards(cachedPosition);

						return LogicResult.Provided;
					}
				}

				if (MmmjrBotSettings.Instance.AutoCastVaalSkills)
				{
					foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
					{
						if (skill.SkillTags.Contains("vaal"))
						{
							if (skill.CanUse())
							{
								await Coroutines.FinishCurrentAction();

								var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(skill.Slot, false, cachedPosition);
								if (err1 == LokiPoe.InGameState.UseResult.None)
								{
									await Coroutines.FinishCurrentAction(false);

									return LogicResult.Provided;
								}
							}
						}
					}
				}
				
				int slot = -1;
				
				var aip = false;
				var aoe = false;
				var aoeuniques = true;
				var curseonhit = false;
				if (MmmjrBotSettings.Instance.AlwaysAttackInPlace)
				{
					aip = true;
				}
				GlobalLog.Info("Aqui COmbat");
				foreach (MapperRoutineSkill sk in MmmjrBotSettings.Instance.MapperRoutineSelector)
				{
                GlobalLog.Info("Aqui COmbat");
				GlobalLog.Info("IsEnabled: " + sk.Enabled);
                GlobalLog.Info("CachedDistance: " + cachedDistance);
                GlobalLog.Info("CombatRange: " + MmmjrBotSettings.Instance.CombatRange);
                GlobalLog.Info("Isready: " + sk.IsReadyToCast);
				GlobalLog.Info("SkType:" + sk.SkType);
                if (!sk.Enabled) continue;
					int slotIndex = int.Parse(sk.SlotIndex);
					if (slotIndex < 1 || slotIndex > 13) continue;
				if (sk.SkType == 0) continue;
					
					if (sk.IsReadyToCast)
					{
						if(cachedDistance < MmmjrBotSettings.Instance.CombatRange) //Ranged
						{
							var slotToUse = EnsurceCast(slotIndex);
							if (slotToUse != -1)
							{
							    if(sk.SkType == 4)
								{
									var err = LokiPoe.InGameState.SkillBarHud.Use(slotToUse, false);
									if (err != LokiPoe.InGameState.UseResult.None)
									{
										continue;
									}
								}
								else
								{
									var err = LokiPoe.InGameState.SkillBarHud.UseAt(slotToUse, aip, LokiPoe.MyPosition);
									if (err != LokiPoe.InGameState.UseResult.None)
									{
										continue;
									}
								FarmingTask.IsCombatActive = true;
								}	
								
								

								sk.Casted();
								FarmingTask.IsCombatActive = false;
							}
						}
					}
				}

			FarmingTask.IsCombatActive = true;
            if (!currentTarget.IsMapBoss)
            {
                if (!MmmjrBotSettings.Instance.SingleUseElderFragments && !MmmjrBotSettings.Instance.SingleUseShaperFragments)
                {
                    Blacklist.Add(currentTarget.Id, TimeSpan.FromHours(1), "");
                }

            }

            MapperRoutineSkill skillMain;
            
            skillMain = MmmjrBotSettings.Instance.MapperRoutineSelector.FirstOrDefault<MapperRoutineSkill>(s => (s.SkType == 0 || s.SkType == 1) && s.Enabled);
			if (skillMain == null) return await CombatLogicEnd();

            int slotMain = int.Parse(skillMain.SlotIndex);
            slotMain = EnsurceCast(slotMain);
            if (slotMain == -1) return await CombatLogicEnd();
            slot = slotMain;

            LokiPoe.ProcessHookManager.ClearAllKeyStates();
			UseResult errMain;
            Vector2i randomPos = new Vector2i(1, 1);
            var r = LokiPoe.Random.Next(1, 4);
            if (r == 1)
                randomPos = LokiPoe.MyPosition + new Vector2i(38, 38);
            else if (r == 2)
                randomPos = LokiPoe.MyPosition + new Vector2i(-38, 38);
            else if (r == 3)
                randomPos = LokiPoe.MyPosition + new Vector2i(38, -38);
            else
                randomPos = LokiPoe.MyPosition + new Vector2i(-38, -38);
            if (MmmjrBotSettings.Instance.CastLong)
			{
				errMain = SkillBarHud.BeginUseAt(slot, aip, randomPos);
            }
			else
			{
                errMain = SkillBarHud.BeginUseAt(slot, aip, LokiPoe.Me.Position);
            }
                

            if (errMain != UseResult.None)
            {
                Log.WarnFormat("[Logic] BeginUseAt returned {0}.", errMain);
            }
            return LogicResult.Provided;
        }

		#endregion

		private static WorldItem BestAnimateGuardianTarget(Monster monster, int maxLevel)
		{
			var worldItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
				.Where(wi => !_ignoreAnimatedItems.Contains(wi.Id) && wi.Distance < 30)
				.OrderBy(wi => wi.Distance);
			foreach (var wi in worldItems)
			{
				var item = wi.Item;
				if (item.RequiredLevel <= maxLevel && item.IsIdentified && !item.IsChromatic && item.SocketCount < 5 && item.MaxLinkCount < 5 && item.Rarity <= Rarity.Magic)
				{
					if (monster == null || monster.LeftHandWeaponVisual == "")
					{
						if (item.AnyMetadataFlags(MetadataFlags.Claws, MetadataFlags.OneHandAxes, MetadataFlags.OneHandMaces,
							MetadataFlags.OneHandSwords, MetadataFlags.OneHandThrustingSwords, MetadataFlags.TwoHandAxes,
							MetadataFlags.TwoHandMaces, MetadataFlags.TwoHandSwords))
						{
							_ignoreAnimatedItems.Add(wi.Id);
							return wi;
						}
					}

					if (monster == null || monster.ChestVisual == "")
					{
						if (item.HasMetadataFlags(MetadataFlags.BodyArmours))
						{
							_ignoreAnimatedItems.Add(wi.Id);
							return wi;
						}
					}

					if (monster == null || monster.HelmVisual == "")
					{
						if (item.HasMetadataFlags(MetadataFlags.Helmets))
						{
							_ignoreAnimatedItems.Add(wi.Id);
							return wi;
						}
					}

					if (monster == null || monster.BootsVisual == "")
					{
						if (item.HasMetadataFlags(MetadataFlags.Boots))
						{
							_ignoreAnimatedItems.Add(wi.Id);
							return wi;
						}
					}

					if (monster == null || monster.GlovesVisual == "")
					{
						if (item.HasMetadataFlags(MetadataFlags.Gloves))
						{
							_ignoreAnimatedItems.Add(wi.Id);
							return wi;
						}
					}
				}
			}
			return null;
		}

		private static WorldItem BestAnimateWeaponTarget(int maxLevel)
		{
			var worldItems = LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
				.Where(wi => !_ignoreAnimatedItems.Contains(wi.Id) && wi.Distance < 30)
				.OrderBy(wi => wi.Distance);
			foreach (var wi in worldItems)
			{
				var item = wi.Item;
				if (item.IsIdentified &&
					item.RequiredLevel <= maxLevel &&
					!item.IsChromatic &&
					item.SocketCount < 5 &&
					item.MaxLinkCount < 5 &&
					item.Rarity <= Rarity.Magic &&
					item.AnyMetadataFlags(MetadataFlags.Claws, MetadataFlags.OneHandAxes, MetadataFlags.OneHandMaces,
						MetadataFlags.OneHandSwords, MetadataFlags.OneHandThrustingSwords, MetadataFlags.TwoHandAxes,
						MetadataFlags.TwoHandMaces, MetadataFlags.TwoHandSwords, MetadataFlags.Daggers, MetadataFlags.Staves))
				{
					_ignoreAnimatedItems.Add(wi.Id);
					return wi;
				}
			}
			return null;
		}

		private static Monster BestDeadTarget
		{
			get
			{
				return LokiPoe.ObjectManager.GetObjectsByType<Monster>()
					.Where(
						m =>
							m.Distance < 30 && m.IsActiveDead && m.Rarity != Rarity.Unique && m.CorpseUsable &&
							ExilePather.PathDistance(LokiPoe.MyPosition, m.Position, false, MmmjrBotSettings.Instance.LeaveFrame) < 30)
					.OrderBy(m => m.Distance)
					.FirstOrDefault();
			}
		}

		private static readonly Dictionary<int, int> _shrineTries = new Dictionary<int, int>();
		
		private static Func<Rarity, int, bool> _flaskHook;

        private static async Task<LogicResult> CombatLogicEnd()
        {
            var res = LogicResult.Unprovided;
            return res;
        }
        private static async Task<bool> HandleShrines()
		{
			if (MmmjrBotSettings.Instance.SkipShrines)
			{
				return false;
			}
			
			var shrines = LokiPoe.ObjectManager.Objects.OfType<Shrine>()
					.Where(s => !Blacklist.Contains(s.Id) && !s.IsDeactivated && s.Distance < 50)
					.OrderBy(s => s.Distance)
					.ToList();

			if (!shrines.Any())
			{
				return false;
			}

			Log.InfoFormat("[HandleShrines]");

			var shrine = shrines[0];
			int tries;

			if (!_shrineTries.TryGetValue(shrine.Id, out tries))
			{
				tries = 0;
				_shrineTries.Add(shrine.Id, tries);
			}

			if (tries > 10)
			{
				Blacklist.Add(shrine.Id, TimeSpan.FromHours(1), "Could not interact with the shrine.");

				return true;
			}

			var skellyOverride = shrine.ShrineId == "Skeletons";

			if (NumberOfMobsNear(LokiPoe.Me, 20) < 3 || skellyOverride)
			{
				var pos = ExilePather.FastWalkablePositionFor(shrine);

				var pathDistance = ExilePather.PathDistance(LokiPoe.MyPosition, pos, false, MmmjrBotSettings.Instance.LeaveFrame);

				Log.DebugFormat("[HandleShrines] Now moving towards the Shrine {0} [pathPos: {1} pathDis: {2}].", shrine.Id, pos,
					pathDistance);

				if (pathDistance > 50)
				{
					Log.DebugFormat("[HandleShrines] Not attempting to move towards Shrine [{0}] because the path distance is: {1}.",
						shrine.Id, pathDistance);
					return false;
				}

				var inDistance = LokiPoe.MyPosition.Distance(pos) < 20 && pathDistance < 25;
				if (inDistance)
				{
					Log.DebugFormat("[HandleShrines] Now attempting to interact with the Shrine {0}.", shrine.Id);

					await Coroutines.FinishCurrentAction();

					await Coroutines.InteractWith(shrine);

					_shrineTries[shrine.Id]++;
				}
				else
				{
					if (!PlayerMoverManager.MoveTowards(pos))
					{
						Log.ErrorFormat("[HandleShrines] MoveTowards failed for {0}.", pos);

						Blacklist.Add(shrine.Id, TimeSpan.FromHours(1), "Could not move towards the shrine.");

						await Coroutines.FinishCurrentAction();
					}
				}
				return true;
			}
			return false;
		}

		private static async Task DisableAlwaysHiglight()
		{
			if (LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
			{
				LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
			}
		}

		private static async Task EnableAlwaysHighlight()
		{
			if (!LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
			{
				LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
			}
		}

		private static int EnsurceCast(int slot)
		{
			if (slot == -1)
				return slot;
			if (slot > 13)
				return -1;

			var slotSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
			if (slotSkill == null || !slotSkill.CanUse())
			{
				return -1;
			}
			return slot;
		}

	}
}