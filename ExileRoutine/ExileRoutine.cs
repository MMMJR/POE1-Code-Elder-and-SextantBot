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

namespace ExileRoutine
{
	/// <summary> </summary>
	public static class ExileRoutine
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

		private static int _sgSlot;
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

		public static bool _needsUpdate;
		
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

			PropertyInfo[] properties = typeof(ExileRoutineSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
					p.SetValue(ExileRoutineSettings.Instance, param[0]);
					return null;
				});

				_exposedSettings.Add("Get" + p.Name, param =>
				{
					return p.GetValue(ExileRoutineSettings.Instance);
				});
			}
		}

        private static bool IsBlacklistedSkill(int id)
		{
			var tokens = ExileRoutineSettings.Instance.BlacklistedSkillIds.Split(new[]
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

				if (m.Distance > (_currentLeashRange != -1 ? _currentLeashRange : ExileRoutineSettings.Instance.CombatRange))
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
			_needsUpdate = true;
		}

		private static bool IsCastableHelper(Skill skill)
		{
			return skill != null && skill.IsCastable && !skill.IsTrap && !skill.IsMine;
		}

		private static bool IsAuraName(string name)
		{
			if (!ExileRoutineSettings.Instance.EnableAurasFromItems)
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

			if (_needsUpdate)
			{
				_sgSlot = -1;
				_summonChaosGolemSlot = -1;
				_summonFlameGolemSlot = -1;
				_summonIceGolemSlot = -1;
				_summonStoneGolemSlot = -1;
				_summonLightningGolemSlot = -1;
				_raiseZombieSlot = -1;
				_summonBestialUrsaSlot = -1;
				_summonBestialRhoaSlot = -1;
				_summonBestialSnakeSlot = -1;
				_raiseSpectreSlot = -1;
				_desecrateSlot = -1;
				_animateWeaponSlot = -1;
				_animateGuardianSlot = -1;
				_flameblastSlot = -1;
				_bladeflurrySlot = -1;
				_enduringCrySlot = -1;
				_moltenShellSlot = -1;
				_auraSlot = -1;
				_trapSlot = -1;
				_coldSnapSlot = -1;
				_contagionSlot = -1;
				_witherSlot = -1;
				_immortalCallSlot  = -1;
				_lightningTendrilsSlot  = -1;
				_scorchingRaySlot  = -1;
				_bloodRageSlot = -1;
				_tempestShieldSlot = -1;
				_orbOfStormsSlot = -1;
				_rfSlot = -1;
				_vaalDiscSlot = -1;
				_vaalSummonSkeletonsSlot = -1;
				_vaalGraceSlot = -1;
				_vaalHasteSlot = -1;
				_vaalRainSlot = -1;
				_summonSkeletonsSlot = -1;
				_srsSlot = -1;
				_mineSlot = -1;
				_fleshOfferingSlot = -1;
				_convocationSlot = -1;
				_curseSlots.Clear();
				_totalCursesAllowed = LokiPoe.Me.TotalCursesAllowed;

				foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
				{
					var tags = skill.SkillTags;
					var name = skill.Name;

					if (tags.Contains("curse"))
					{
						var slot = skill.Slot;
						if (slot != -1 && skill.IsCastable && !skill.IsAurifiedCurse)
						{
							_curseSlots.Add(slot);
						}
					}

					if (_auraSlot == -1 &&
						((tags.Contains("aura") && !tags.Contains("vaal")) || IsAuraName(name) || skill.IsAurifiedCurse ||
						skill.IsConsideredAura))
					{
						_auraSlot = skill.Slot;
					}

					if (skill.IsTrap && _trapSlot == -1)
					{
						_trapSlot = skill.Slot;
					}

					if (skill.IsMine && _mineSlot == -1)
					{
						_mineSlot = skill.Slot;
					}
				}
				
				var oos = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.InternalId == "orb_of_storms");
				if (IsCastableHelper(oos))
				{
					_orbOfStormsSlot = oos.Slot;
				}
				
				var conv = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.InternalId == "convocation");
				if (IsCastableHelper(conv))
				{
					_convocationSlot = conv.Slot;
				}

				var fo = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Flesh Offering");
				if (IsCastableHelper(fo))
				{
					_fleshOfferingSlot = fo.Slot;
				}

				var cs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Cold Snap");
				if (IsCastableHelper(cs))
				{
					_coldSnapSlot = cs.Slot;
				}
				
				var imm = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Immortal Call");
				if (IsCastableHelper(imm))
				{
					_immortalCallSlot = imm.Slot;
				}
				
				var lt = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Lightning Tendrils");
				if (IsCastableHelper(lt))
				{
					_lightningTendrilsSlot = lt.Slot;
				}
				
				var sr = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Scorching Ray");
				if (IsCastableHelper(sr))
				{
					_scorchingRaySlot = sr.Slot;
				}

				var con = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Contagion");
				if (IsCastableHelper(con))
				{
					_contagionSlot = con.Slot;
				}

				var wither = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Wither");
				if (IsCastableHelper(wither))
				{
					_witherSlot = wither.Slot;
				}

				var ss = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.InternalId == "summon_skeletons"); // Name changed 3.0, InternalId should be used for all skills.
				if (IsCastableHelper(ss))
				{
					_summonSkeletonsSlot = ss.Slot;
				}
				
				var srs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Raging Spirit");
				if (IsCastableHelper(srs))
				{
					_srsSlot = srs.Slot;
				}

				var rf = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Righteous Fire");
				if (IsCastableHelper(rf))
				{
					_rfSlot = rf.Slot;
				}

				var br = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Blood Rage");
				if (IsCastableHelper(br))
				{
					_bloodRageSlot = br.Slot;
				}
				
				var ts = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Tempest Shield");
				if (IsCastableHelper(ts))
				{
					_tempestShieldSlot = ts.Slot;
				}
				
				var vd = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Vaal Discipline");
				if (IsCastableHelper(vd))
				{
					_vaalDiscSlot = vd.Slot;
				}
				
				var vg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Vaal Grace");
				if (IsCastableHelper(vg))
				{
					_vaalGraceSlot = vg.Slot;
				}
				
				var vh = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Vaal Haste");
				if (IsCastableHelper(vh))
				{
					_vaalHasteSlot = vh.Slot;
				}
				
				var vr = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Vaal Rain of Arrows");
				if (IsCastableHelper(vr))
				{
					_vaalRainSlot = vr.Slot;
				}
				var vss = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Vaal Summon Skeletons");
				if (IsCastableHelper(vss))
				{
					_vaalSummonSkeletonsSlot = vss.Slot;
				}

				var mc = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Molten Shell");
				if (IsCastableHelper(mc))
				{
					_moltenShellSlot = mc.Slot;
				}

				var ec = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Enduring Cry");
				if (IsCastableHelper(ec))
				{
					_enduringCrySlot = ec.Slot;
				}

				var scg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Chaos Golem");
				if (IsCastableHelper(scg))
				{
					_summonChaosGolemSlot = scg.Slot;
					_sgSlot = _summonChaosGolemSlot;
				}

				var sig = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Ice Golem");
				if (IsCastableHelper(sig))
				{
					_summonIceGolemSlot = sig.Slot;
					_sgSlot = _summonIceGolemSlot;
				}

				var sfg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Flame Golem");
				if (IsCastableHelper(sfg))
				{
					_summonFlameGolemSlot = sfg.Slot;
					_sgSlot = _summonFlameGolemSlot;
				}

				var ssg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Stone Golem");
				if (IsCastableHelper(ssg))
				{
					_summonStoneGolemSlot = ssg.Slot;
					_sgSlot = _summonStoneGolemSlot;
				}

				var slg = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Lightning Golem");
				if (IsCastableHelper(slg))
				{
					_summonLightningGolemSlot = slg.Slot;
					_sgSlot = _summonLightningGolemSlot;
				}

				var rz = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Raise Zombie");
				if (IsCastableHelper(rz))
				{
					_raiseZombieSlot = rz.Slot;
				}
				
				var sbu = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Bestial Ursa");
				if (IsCastableHelper(sbu))
				{
					_summonBestialUrsaSlot = sbu.Slot;
				}
				
				var sbr = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Bestial Rhoa");
				if (IsCastableHelper(sbr))
				{
					_summonBestialRhoaSlot = sbr.Slot;
				}
				
				var sbs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Summon Bestial Snake");
				if (IsCastableHelper(sbs))
				{
					_summonBestialSnakeSlot = sbs.Slot;
				}

				var rs = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Raise Spectre");
				if (IsCastableHelper(rs))
				{
					_raiseSpectreSlot = rs.Slot;
				}
				
				var ds = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Desecrate");
				if (IsCastableHelper(ds))
				{
					_desecrateSlot = ds.Slot;
				}

				var fb = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Flameblast");
				if (IsCastableHelper(fb))
				{
					_flameblastSlot = fb.Slot;
				}
				
				var bf = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Blade Flurry");
				if (IsCastableHelper(bf))
				{
					_bladeflurrySlot = bf.Slot;
				}

				var ag = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Animate Guardian");
				if (IsCastableHelper(ag))
				{
					_animateGuardianSlot = ag.Slot;
				}

				var aw = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Animate Weapon");
				if (IsCastableHelper(aw))
				{
					_animateWeaponSlot = aw.Slot;
				}

				_needsUpdate = false;
			}
		}

		#endregion

		#region Implementation of IConfigurable

		public static UserControl Control
		{
			get
			{

				using (
					var fs = new FileStream(
						Path.Combine(ThirdPartyLoader.GetInstance("ExileRoutine").ContentPath, "SettingsGui.xaml"),
						FileMode.Open))
				{
					var root = (UserControl) XamlReader.Load(fs);


					if (
						!Wpf.SetupCheckBoxBinding(root, "CullingStrikeCheckBox", "CullingStrike",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'CullingStrikeCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "CullingPercentTextBox", "CullingPercent",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'CullingPercentTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "SkipShrinesCheckBox", "SkipShrines",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'SkipShrinesCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "LeaveFrameCheckBox", "LeaveFrame",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'LeaveFrameCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "EnableAurasFromItemsCheckBox", "EnableAurasFromItems",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'EnableAurasFromItemsCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "AlwaysAttackInPlaceCheckBox", "AlwaysAttackInPlace",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'AlwaysAttackInPlaceCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupCheckBoxBinding(root, "RangedCheckBox", "Ranged",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'RangedCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupCheckBoxBinding(root, "DontAoEUniquesCheckBox", "DontAoEUniques",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'DontAoEUniquesCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupCheckBoxBinding(root, "ChannelCheckBox", "Channel",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'ChannelCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupCheckBoxBinding(root, "FollowCheckBox", "Follow",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'FollowCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "DebugAurasCheckBox", "DebugAuras",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'DebugAurasCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "AutoCastVaalSkillsCheckBox", "AutoCastVaalSkills",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'AutoCastVaalSkillsCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupCheckBoxBinding(root, "ChannelSkillCheckBox", "ChannelSkill",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'ChannelSkillCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupCheckBoxBinding(root, "SummonSpectreTargetCheckBox", "SummonSpectreTarget",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'SummonSpectreTargetCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupCheckBoxBinding(root, "DesecrateForSpectresCheckBox", "DesecrateForSpectres",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'DesecrateForSpectresCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxItemsBinding(root, "SingleTargetDPSSlotComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'SingleTargetDPSSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "SingleTargetDPSSlotComboBox",
							"SingleTargetDPSSlot", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'SingleTargetDPSSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "AoESlotComboBox",
							"AoESlot", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'AoESlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupComboBoxItemsBinding(root, "AoESlotComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'AoESlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxItemsBinding(root, "MovementSlotComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'MovementSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "MovementSlotComboBox",
							"MovementSlot", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'MovementSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxItemsBinding(root, "CurseOnHitSlotComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'CurseOnHitSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "CurseOnHitSlotComboBox",
							"CurseOnHitSlot", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'CurseOnHitSlotComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupComboBoxItemsBinding(root, "TotemSlotOneComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'TotemSlotOneComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "TotemSlotOneComboBox",
							"TotemSlotOne", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'TotemSlotOneComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (
						!Wpf.SetupComboBoxItemsBinding(root, "TotemSlotTwoComboBox", "AllSkillSlots",
							BindingMode.OneWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxItemsBinding failed for 'TotemSlotTwoComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupComboBoxSelectedItemBinding(root, "TotemSlotTwoComboBox",
							"TotemSlotTwo", BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupComboBoxSelectedItemBinding failed for 'TotemSlotTwoComboBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "CombatRangeTextBox", "CombatRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'CombatRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "ChickenPercentTextBox", "ChickenPercent",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'ChickenPercentTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "LeaderTextBox", "Leader",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'LeaderTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "MaxFollowRangeTextBox", "MaxFollowRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxFollowRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "MaxRangeTextBox", "MaxRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MaxRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "AoeRangeTextBox", "AoeRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'AoeRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "MoveSkillRangeTextBox", "MoveSkillRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MoveSkillRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "MobsNearForLootRangeTextBox", "MobsNearForLootRange",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MobsNearForLootRangeTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "LootDistanceCheckTextBox", "LootDistanceCheck",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'LootDistanceCheckTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "ClaspedDistanceCheckTextBox", "ClaspedDistanceCheck",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'ClaspedDistanceCheckTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "LootHealthTextBox", "LootHealth",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'LootHealthTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupCheckBoxBinding(root, "CheckForHealthCheckBox", "CheckForHealth",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'CheckForHealthCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "LootESTextBox", "LootES",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'LootESTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupCheckBoxBinding(root, "CheckForESCheckBox", "CheckForES",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupCheckBoxBinding failed for 'CheckForESCheckBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "MaxFlameBlastChargesTextBox", "MaxFlameBlastCharges",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'MaxFlameBlastChargesTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "MoltenShellDelayMsTextBox", "MoltenShellDelayMs",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'MoltenShellDelayMsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "TotemOneDelayTextBox", "TotemOneDelay",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'TotemOneDelayTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "TotemTwoDelayTextBox", "TotemTwoDelay",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'TotemTwoDelayTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "TotemOneMaxTextBox", "TotemOneMax",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'TotemOneMaxTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "TotemTwoMaxTextBox", "TotemTwoMax",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat("[SettingsControl] SetupTextBoxBinding failed for 'TotemTwoMaxTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "TrapDelayMsTextBox", "TrapDelayMs",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'TrapDelayMsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "FleshOfferingDelayMsTextBox", "FleshOfferingDelayMs",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'FleshOfferingDelayMsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (
						!Wpf.SetupTextBoxBinding(root, "SpectreTargetTextBox", "SpectreTarget",
							BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'SpectreTargetTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "SummonSkeletonDelayMsTextBox", "SummonSkeletonDelayMs",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'SummonSkeletonDelayMsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "MineDelayMsTextBox", "MineDelayMs",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'MineDelayMsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					if (!Wpf.SetupTextBoxBinding(root, "BlacklistedSkillIdsTextBox", "BlacklistedSkillIds",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'BlacklistedSkillIdsTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}
					
					if (!Wpf.SetupTextBoxBinding(root, "NumberOfEnemiesToEngageCombatTextBox", "NumberOfEnemiesToEngageCombat",
						BindingMode.TwoWay, ExileRoutineSettings.Instance))
					{
						Log.DebugFormat(
							"[SettingsControl] SetupTextBoxBinding failed for 'NumberOfEnemiesToEngageCombatTextBox'.");
						throw new Exception("The SettingsControl could not be created.");
					}

					return root;
				}
			}
		}

		public static JsonSettings Settings => ExileRoutineSettings.Instance;

		#endregion

		#region Implementation of ILogicProvider

		public static async Task<bool> TryUseAura(Skill skill)
		{
			var doCast = true;

			while (skill.Slot == -1)
			{
				Log.InfoFormat("[TryUseAura] Now assigning {0} to the skillbar.", skill.Name);

				var sserr = LokiPoe.InGameState.SkillBarHud.SetSlot(_auraSlot, skill);

				if (sserr != LokiPoe.InGameState.SetSlotResult.None)
				{
					Log.ErrorFormat("[TryUseAura] SetSlot returned {0}.", sserr);

					doCast = false;

					break;
				}

				await Coroutines.LatencyWait();

				await Coroutine.Sleep(1000);
			}

			if (!doCast)
			{
				return false;
			}

			await Coroutines.FinishCurrentAction();

			await Coroutines.LatencyWait();

			var err1 = LokiPoe.InGameState.SkillBarHud.Use(skill.Slot, false);
			if (err1 == LokiPoe.InGameState.UseResult.None)
			{
				await Coroutines.LatencyWait();

				await Coroutines.FinishCurrentAction(false);

				await Coroutines.LatencyWait();

				return true;
			}

			Log.ErrorFormat("[TryUseAura] Use returned {0} for {1}.", err1, skill.Name);

			return false;
		}

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

		public static async Task<LogicResult> RunCombat(Logic logic)
		{
            _combatTargeting.ResetInclusionCalcuation();
            _combatTargeting.ResetWeightCalculation();
            _combatTargeting.InclusionCalcuation += CombatTargetingOnInclusionCalcuation;
            _combatTargeting.WeightCalculation += CombatTargetingOnWeightCalculation;

				var checkforhealth = ExileRoutineSettings.Instance.CheckForHealth;
				var checkfores = ExileRoutineSettings.Instance.CheckForES;
				var chickenpercent = ExileRoutineSettings.Instance.ChickenPercent;
				
				if (_targetStopwatch.ElapsedMilliseconds > 10)
				{
					CombatTargeting.Update();
					_targetStopwatch.Restart();
				}
				
				await EnableAlwaysHighlight();
				
				var currentTarget = CombatTargeting.Targets<Monster>().FirstOrDefault();
				var cullingstrike = ExileRoutineSettings.Instance.CullingStrike;
				var cullingpercent = ExileRoutineSettings.Instance.CullingPercent;
				
				if (_auraSlot != -1)
				{
					foreach (var skill in LokiPoe.InGameState.SkillBarHud.Skills)
					{
						if (IsBlacklistedSkill(skill.Id))
							continue;

						if (skill.IsAurifiedCurse)
						{
							if (!skill.AmICursingWithThis && skill.CanUse(ExileRoutineSettings.Instance.DebugAuras, true))
							{
								if (NumberOfMobsNear(LokiPoe.Me, 60) != 0)
									break;

								if (await TryUseAura(skill))
								{
									return LogicResult.Provided;
								}
							}
						}
						else if (skill.IsConsideredAura)
						{
							if (!skill.AmIUsingConsideredAuraWithThis && skill.CanUse(ExileRoutineSettings.Instance.DebugAuras, true))
							{
								if (NumberOfMobsNear(LokiPoe.Me, 60) != 0)
									break;

								if (await TryUseAura(skill))
								{
									return LogicResult.Provided;
								}
							}
						}
						else if ((skill.SkillTags.Contains("aura") && !skill.SkillTags.Contains("vaal")) || IsAuraName(skill.Name))
						{
							if (!LokiPoe.Me.HasAura(skill.Name) && skill.CanUse(ExileRoutineSettings.Instance.DebugAuras, true))
							{
								if (NumberOfMobsNear(LokiPoe.Me, 60) != 0)
									break;

								if (await TryUseAura(skill))
								{
									return LogicResult.Provided;
								}
							}
						}
					}
				}
				
				if (_immortalCallSlot != -1)
                {
                    var skill = LokiPoe.InGameState.SkillBarHud.Slot(_immortalCallSlot);
                    if (skill.CanUse())
                    {
                    //  if (LokiPoe.Me.EnduranceCharges >= 3 && NumberOfMobsNear(LokiPoe.Me, 100) > 15 || LokiPoe.Me.EnduranceCharges >= 3 && (cachedRarity >= Rarity.Unique))
                        if (LokiPoe.Me.EnduranceCharges >= 3 && !LokiPoe.Me.HasAura("arcane_surge"))
                        {
							await Coroutines.FinishCurrentAction();
 
                            var err1 = LokiPoe.InGameState.SkillBarHud.Use(_immortalCallSlot, true);
                            if (err1 == LokiPoe.InGameState.UseResult.None)
                            {
								await Coroutines.FinishCurrentAction(false);
 
                                return LogicResult.Provided;
                            }
                        }
                    }
                }
				
				var guardian = LokiPoe.ObjectManager.GetObjectByName<Monster>("Animated Guardian");
				if (_animateGuardianSlot != -1 && guardian == null)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_animateGuardianSlot);
					if (skill.CanUse())
					{
						await Coroutines.FinishCurrentAction();

						var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_animateGuardianSlot, true, LokiPoe.Me.Position);
						if (uaerr == LokiPoe.InGameState.UseResult.None)
						{
						}
						
						return LogicResult.Provided;
					}
				}

				if (_animateWeaponSlot != -1 && _animateWeaponStopwatch.ElapsedMilliseconds > 1000)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_animateWeaponSlot);
					if (skill.CanUse())
					{
						var target = BestAnimateWeaponTarget(skill.GetStat(StatTypeGGG.AnimateItemMaximumLevelRequirement));
						if (target != null)
						{
							await Coroutines.FinishCurrentAction();

							var uaerr = LokiPoe.InGameState.SkillBarHud.UseOn(_animateWeaponSlot, true, target);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								LokiPoe.ProcessHookManager.ClearAllKeyStates();

								_animateWeaponStopwatch.Restart();

								return LogicResult.Provided;
							}
						}
					}
				}

				if (_raiseSpectreSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_raiseSpectreSlot);
					if (skill.CanUse())
					{
						var max = skill.GetStat(StatTypeGGG.NumberOfSpectresAllowed);
						if (skill.NumberDeployed < max)
						{
							var specific = false;
							
							if (ExileRoutineSettings.Instance.SummonSpectreTarget)
							{
								specific = true;
							}
							
							if (!specific)
							{
								var target = BestDeadTarget;
								if (target != null)
								{
									await DisableAlwaysHiglight();

									await Coroutines.FinishCurrentAction();

									var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseSpectreSlot, false, target.Position);
									if (uaerr == LokiPoe.InGameState.UseResult.None)
									{
										await Coroutines.FinishCurrentAction(false);

										return LogicResult.Provided;
									}
								}
							}
							if (specific)
							{
								var targeted = LokiPoe.ObjectManager.GetObjectByName<Monster>(ExileRoutineSettings.Instance.SpectreTarget);
								if (targeted != null && targeted.IsDead)
								{
									await DisableAlwaysHiglight();

									await Coroutines.FinishCurrentAction();

									var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseSpectreSlot, false,
									targeted.Position);
									if (uaerr == LokiPoe.InGameState.UseResult.None)
									{
										await Coroutines.FinishCurrentAction(false);

										return LogicResult.Provided;
									}
								}
							}
						}
					}
				}

				if (_sgSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_sgSlot);
					if (skill.CanUse())
					{
						var max = skill.GetStat(StatTypeGGG.NumberOfGolemsAllowed);
						if (skill.NumberDeployed < max)
						{
							await Coroutines.FinishCurrentAction();

							var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_sgSlot, true, LokiPoe.MyPosition);
							if (err1 == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}

				if (_raiseZombieSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_raiseZombieSlot);
					if (skill.CanUse())
					{
						var max = skill.GetStat(StatTypeGGG.NumberOfZombiesAllowed);
						if (skill.NumberDeployed < max)
						{
							var target = BestDeadTarget;
							if (target != null)
							{
								var pos = target.Position;

								await DisableAlwaysHiglight();

								await Coroutines.FinishCurrentAction();

								var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_raiseZombieSlot, false, pos);
								if (uaerr == LokiPoe.InGameState.UseResult.None)
								{
									await Coroutines.FinishCurrentAction(false);

									return LogicResult.Provided;
								}
							}
						}
					}
				}
				
				if (_summonBestialRhoaSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonBestialRhoaSlot);
					if (skill.CanUse())
					{
						if (skill.NumberDeployed < 1)
						{
							var pos = LokiPoe.Me.Position;

							await Coroutines.FinishCurrentAction();

							var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_summonBestialRhoaSlot, false, pos);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}
				
				if (_summonBestialUrsaSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonBestialUrsaSlot);
					if (skill.CanUse())
					{
						if (skill.NumberDeployed < 1)
						{
							var pos = LokiPoe.Me.Position;

							await Coroutines.FinishCurrentAction();

							var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_summonBestialUrsaSlot, false, pos);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}
				
				if (_summonBestialSnakeSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonBestialSnakeSlot);
					if (skill.CanUse())
					{
						if (skill.NumberDeployed < 1)
						{
							var pos = LokiPoe.Me.Position;

							await Coroutines.FinishCurrentAction();

							var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_summonBestialSnakeSlot, false, pos);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}

				if (_convocationSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_convocationSlot);
					if (skill.CanUse())
					{
						if (_convocationStopwatch.ElapsedMilliseconds > 8000)
						{
							await Coroutines.FinishCurrentAction();
							
							var uaerr = LokiPoe.InGameState.SkillBarHud.Use(_convocationSlot, true);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								_convocationStopwatch.Restart();

								return LogicResult.Provided;
							}
						}
					}
				}

				if (_fleshOfferingSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_fleshOfferingSlot);
					if (skill.CanUse())
					{
						var offeringdelay = ExileRoutineSettings.Instance.FleshOfferingDelayMs;
						if (_fleshOfferingStopwatch.ElapsedMilliseconds > offeringdelay)
						{
							var target = BestDeadTarget;
							if (target != null)
							{
								await DisableAlwaysHiglight();
								
								var uaerr = LokiPoe.InGameState.SkillBarHud.UseAt(_fleshOfferingSlot, false, target.Position);
								if (uaerr == LokiPoe.InGameState.UseResult.None)
								{
								}
								
								_fleshOfferingStopwatch.Restart();

								return LogicResult.Provided;
							}
						}
					}
				}

				if (_enduringCrySlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_enduringCrySlot);
					if (skill.CanUse())
					{
						if (LokiPoe.Me.HealthPercent < 70)
						{
							await Coroutines.FinishCurrentAction();

							var err1 = LokiPoe.InGameState.SkillBarHud.Use(_enduringCrySlot, true);
							if (err1 == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}
				
				if (_tempestShieldSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_tempestShieldSlot);
					if (skill.CanUse() && !LokiPoe.Me.HasTempestShieldBuff)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_tempestShieldSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}

				if (_rfSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_rfSlot);
					if (skill.CanUse() && !LokiPoe.Me.HasRighteousFireBuff)
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_rfSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							return LogicResult.Provided;
						}
					}
				}

				if (_moltenShellSlot != -1 && _moltenShellStopwatch.ElapsedMilliseconds >= ExileRoutineSettings.Instance.MoltenShellDelayMs)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_moltenShellSlot);
					if (!LokiPoe.Me.HasMoltenShellBuff && skill.CanUse())
					{
						if (NumberOfMobsNear(LokiPoe.Me, ExileRoutineSettings.Instance.CombatRange) > 0)
						{
							await Coroutines.FinishCurrentAction();

							var err1 = LokiPoe.InGameState.SkillBarHud.Use(_moltenShellSlot, true);

							_moltenShellStopwatch.Restart();

							if (err1 == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}

				if (currentTarget == null || currentTarget.HasAura("shrine_godmode"))
				{
					if (await HandleShrines())
					{
						return LogicResult.Provided;
					}
				}
				
				if (currentTarget == null)
				{		
					if (_desecrateSlot != -1)
					{
						var checkformobs = NumberOfMobsNear(LokiPoe.Me, 50);
						var desecrateforspectres = ExileRoutineSettings.Instance.DesecrateForSpectres;
						if (desecrateforspectres && checkformobs == 0)
						{
							var skill = LokiPoe.InGameState.SkillBarHud.Slot(_desecrateSlot);
							var spectre = LokiPoe.InGameState.SkillBarHud.Slot(_raiseSpectreSlot);
							var max = spectre.GetStat(StatTypeGGG.NumberOfSpectresAllowed);
								
							if (skill.CanUse() && spectre.NumberDeployed < max)
							{
								var err1 = LokiPoe.InGameState.SkillBarHud.Use(_desecrateSlot, true);
								if (err1 == LokiPoe.InGameState.UseResult.None)
								{
									return LogicResult.Provided;
								}
							}
						}
					}
					
					return LogicResult.Unprovided;
				}

				// The Beacon Handling Beginning
				if (LokiPoe.LocalData.WorldArea.Id == "2_6_14")
				{
					var fuelCarts = LokiPoe.ObjectManager.GetObjectsByMetadata("Metadata/QuestObjects/Act6/BeaconPayload").ToList();
					foreach (var fuelCart in fuelCarts)
					{
						if (fuelCart.Components.TransitionableComponent.Flag2 == 1 && fuelCart.Distance < 30)
						{
							if (_fuelCartStopwatch.ElapsedMilliseconds > 3000) // Allow 3s of combat
							{
								_fuelCartStopwatch.Restart();
							}
							else
							{
								return LogicResult.Unprovided;
							}
						}
					}
				}
				// The Beacon Handling End
				
				var cachedPosition = currentTarget.Position;
				var cachedId = currentTarget.Id;
				var cachedRarity = currentTarget.Rarity;
				var cachedDistance = currentTarget.Distance;
				var cachedHasCurseFrom = new Dictionary<string, bool>();
				var cachedProxShield = currentTarget.HasProximityShield;
				var cachedHpPercent = (int) currentTarget.HealthPercentTotal;

				foreach (var curseSlot in _curseSlots)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(curseSlot);
					cachedHasCurseFrom.Add(skill.Name, currentTarget.HasCurseFrom(skill.Name));
				}
				
				var pathDistance = ExilePather.PathDistance(LokiPoe.MyPosition, cachedPosition, false, false);

				var mapData = MapData.Current;
				if (mapData == null)
				{
					return LogicResult.Provided;
				}
				GlobalLog.Info("Info Null1");
				var type = mapData.Type;

				if (pathDistance.CompareTo(float.MaxValue) == 0 && type != MapType.Bossroom)
				{
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as path distance cannot be determined.");
					return LogicResult.Provided;
				}

				if (pathDistance > ExileRoutineSettings.Instance.CombatRange && type != MapType.Bossroom)
				{
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as its path distance is greater then max combat range.");
					return LogicResult.Provided;
				}
				
				// Movement Section --- Start
				var myspot = LokiPoe.MyPosition;
				var canSee = ExilePather.CanObjectSee(LokiPoe.Me, currentTarget, ExileRoutineSettings.Instance.LeaveFrame);
				var blockedByDoor = ClosedDoorBetween(LokiPoe.Me, currentTarget, 10, 10, ExileRoutineSettings.Instance.LeaveFrame);
				var rangelocation = myspot.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2);
				
				var ranged = false;
				if (ExileRoutineSettings.Instance.Ranged)
				{	
					ranged = true;
				}
				
				if (!ExilePather.PathExistsBetween(myspot, cachedPosition, false) && ranged && type != MapType.Bossroom)
				{ 
					Blacklist.Add(cachedId, TimeSpan.FromSeconds(5), "Ignoring current target as we cannot determine a safe movement position.");
					return LogicResult.Provided;
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
						if (!PlayerMoverManager.MoveTowards(cachedPosition))
						{
						}
					//}	
					
					return LogicResult.Provided;
				}
				
				int movementskill = -1;
				var moveskillrange = ExileRoutineSettings.Instance.MoveSkillRange;
				
				var movement = false;
				if (ExileRoutineSettings.Instance.MovementSlot != -1)
				{
					movement = true;
				}
				
				if (cachedDistance > ExileRoutineSettings.Instance.MaxRange)
				{
					if (movement && !LokiPoe.Me.HasKaruiSpirit && cachedDistance > moveskillrange)
					{	
						movementskill = EnsurceCast(ExileRoutineSettings.Instance.MovementSlot);
						
						if (ranged)
						{
							var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(movementskill, true, rangelocation);

							if (err1 != LokiPoe.InGameState.UseResult.None)
							{
							}
							
							return LogicResult.Provided;	
							
						}
						else
						{
							var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(movementskill, true, cachedPosition);

							if (err1 != LokiPoe.InGameState.UseResult.None)
							{
							}
							
							return LogicResult.Provided;
						}
					}
					else
					{
						if (ranged)
						{
							if (!PlayerMoverManager.MoveTowards(rangelocation))
							{
							}
							
							return LogicResult.Provided;	
						}
						else
						{
							if (!PlayerMoverManager.MoveTowards(cachedPosition))
							{
							}
							
							return LogicResult.Provided;	
						}
					}
				}
				
				if (cachedDistance > 10 && cachedProxShield)
				{
					if (movement && !LokiPoe.Me.HasKaruiSpirit && cachedDistance > moveskillrange)
					{	
						movementskill = EnsurceCast(ExileRoutineSettings.Instance.MovementSlot);

						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(movementskill, true, cachedPosition);

						if (err1 != LokiPoe.InGameState.UseResult.None)
						{
						}
							
						return LogicResult.Provided;	
						
					}
					else
					{
						if (!PlayerMoverManager.MoveTowards(cachedPosition))
						{
						}
						
						return LogicResult.Provided;	
					}
				}
				// Movement Section --- End
				
				if (_orbOfStormsSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_orbOfStormsSlot);
					if (skill.CanUse() && cachedRarity >= Rarity.Rare)
					{
						if (_orbOfStormsStopwatch.ElapsedMilliseconds > 2000)
						{
							var uaerr = LokiPoe.InGameState.SkillBarHud.Use(_orbOfStormsSlot, true);
							if (uaerr == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								_orbOfStormsStopwatch.Restart();

								return LogicResult.Provided;
							}
						}
					}
 				}

				if ((_trapSlot != -1) && (_trapStopwatch.ElapsedMilliseconds > ExileRoutineSettings.Instance.TrapDelayMs))
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_trapSlot);
					if (skill.CanUse())
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_trapSlot, true, LokiPoe.MyPosition.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2));

						_trapStopwatch.Restart();

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}

				if (_bloodRageSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_bloodRageSlot);
					if (skill.CanUse() && !LokiPoe.Me.HasBloodRageBuff && cachedDistance < ExileRoutineSettings.Instance.CombatRange)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_bloodRageSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_tempestShieldSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_tempestShieldSlot);
					if (skill.CanUse() && !LokiPoe.Me.HasTempestShieldBuff)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_tempestShieldSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}

				if (_rfSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_rfSlot);
					if (skill.CanUse() && !LokiPoe.Me.HasRighteousFireBuff)
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_rfSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							return LogicResult.Provided;
						}
					}
				}

				if (_moltenShellSlot != -1 && _moltenShellStopwatch.ElapsedMilliseconds >= ExileRoutineSettings.Instance.MoltenShellDelayMs)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_moltenShellSlot);
					if (!LokiPoe.Me.HasMoltenShellBuff && skill.CanUse())
					{
						if (NumberOfMobsNear(LokiPoe.Me, ExileRoutineSettings.Instance.CombatRange) > 0)
						{
							await Coroutines.FinishCurrentAction();

							var err1 = LokiPoe.InGameState.SkillBarHud.Use(_moltenShellSlot, true);

							_moltenShellStopwatch.Restart();

							if (err1 == LokiPoe.InGameState.UseResult.None)
							{
								await Coroutines.FinishCurrentAction(false);

								return LogicResult.Provided;
							}
						}
					}
				}
				
				if (_vaalDiscSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_vaalDiscSlot);
					if (skill.CanUse() && LokiPoe.Me.EnergyShieldPercent < 60)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_vaalDiscSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_vaalSummonSkeletonsSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_vaalSummonSkeletonsSlot);
					if (skill.CanUse() && LokiPoe.Me.HealthPercent < 75)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_vaalSummonSkeletonsSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_vaalGraceSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_vaalGraceSlot);
					if (skill.CanUse() && LokiPoe.Me.HealthPercent < 60)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_vaalGraceSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_vaalRainSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_vaalRainSlot);
					if (skill.CanUse() && cachedRarity == Rarity.Unique)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_vaalRainSlot, true, cachedPosition);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_vaalHasteSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_vaalHasteSlot);
					if (skill.CanUse() && cachedRarity == Rarity.Unique)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_vaalHasteSlot, true);
						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							return LogicResult.Provided;
						}
					}
				}
				
				if (_srsSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_srsSlot);
					var maxsrs = 20;
					if (skill.NumberDeployed < maxsrs && skill.CanUse())
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_srsSlot, true);

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
						}
					}
				}

				if (_summonSkeletonsSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_summonSkeletonsSlot);
					var max = skill.GetStat(StatTypeGGG.NumberOfSkeletonsAllowed);
					if (skill.NumberDeployed < max && skill.CanUse())
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_summonSkeletonsSlot, true, LokiPoe.MyPosition.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2));

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
						}
					}
				}

				if ((_mineSlot != -1) && (_mineStopwatch.ElapsedMilliseconds > ExileRoutineSettings.Instance.MineDelayMs) && (cachedDistance < ExileRoutineSettings.Instance.MaxRange))
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_mineSlot);
					var max = skill.GetStat(StatTypeGGG.SkillDisplayNumberOfRemoteMinesAllowed);
					var insta = skill.GetStat(StatTypeGGG.MineDetonationIsInstant) == 1;
					if (skill.NumberDeployed < max && skill.CanUse())
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.Use(_mineSlot, true);

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							if (!insta)
							{
								await Coroutine.Sleep(500);

								LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.detonate_mines, true, false, false);
							}

							_mineStopwatch.Restart();

							return LogicResult.Provided;
						}
					}
				}

				if (_witherSlot != -1)
				{
					var cachedWither = currentTarget.HasWither;
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_witherSlot);
					if (skill.CanUse() && !cachedWither)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_witherSlot, true, cachedPosition);

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							return LogicResult.Provided;
						}
					}
				}

				if (_contagionSlot != -1)
				{
					var cachedContagion = currentTarget.HasContagion;
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_contagionSlot);
					if (skill.CanUse(false, false, false) && !cachedContagion)
					{
						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_contagionSlot, true, cachedPosition);

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							return LogicResult.Provided;
						}
					}
				}

				if (_coldSnapSlot != -1)
				{
					var skill = LokiPoe.InGameState.SkillBarHud.Slot(_coldSnapSlot);
					if (skill.CanUse(false, false, false))
					{
						await Coroutines.FinishCurrentAction();

						var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(_coldSnapSlot, true, cachedPosition);

						if (err1 == LokiPoe.InGameState.UseResult.None)
						{
							await Coroutines.FinishCurrentAction(false);

							return LogicResult.Provided;
						}
					}
				}

				if (ExileRoutineSettings.Instance.AutoCastVaalSkills)
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
				var aoerange = ExileRoutineSettings.Instance.AoeRange;
				var maxenemydistance = ExileRoutineSettings.Instance.MaxRange;
				//var targetposition = currentTarget.Position;
				
				if (ExileRoutineSettings.Instance.AlwaysAttackInPlace)
				{	
					aip = true;
				}
				
				if (ExileRoutineSettings.Instance.AoESlot != -1)
				{	
					aoe = true;
				}
				
				if (ExileRoutineSettings.Instance.DontAoEUniques)
				{	
					aoeuniques = false;
				}
				
				if (ExileRoutineSettings.Instance.CurseOnHitSlot != -1)
				{
					curseonhit = true;
				}
				
				if (ExileRoutineSettings.Instance.TotemSlotOne != -1)
				{
					int totemslotone = -1;
					
					if (_totemoneStopwatch.ElapsedMilliseconds >= ExileRoutineSettings.Instance.TotemOneDelay)
					{
						totemslotone = EnsurceCast(ExileRoutineSettings.Instance.TotemSlotOne);
						var totemskillone = LokiPoe.InGameState.SkillBarHud.Slot(totemslotone);
						if (totemskillone.CanUse() && (totemskillone.DeployedObjects.Select(o => o as Monster).Count(t => !t.IsDead && t.Distance < 30) < ExileRoutineSettings.Instance.TotemOneMax))
						{
							var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(totemslotone, true, LokiPoe.MyPosition.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2));
							
							LokiPoe.ProcessHookManager.ClearAllKeyStates();

							if (err1 != LokiPoe.InGameState.UseResult.None)
							{
							}
							_totemoneStopwatch.Restart();
							
							return LogicResult.Provided;
						}
					}
				}
				
				if (ExileRoutineSettings.Instance.TotemSlotTwo != -1)
				{
					int totemslottwo = -1;
					
					if (_totemtwoStopwatch.ElapsedMilliseconds >= ExileRoutineSettings.Instance.TotemTwoDelay)
					{
						totemslottwo = EnsurceCast(ExileRoutineSettings.Instance.TotemSlotTwo);
						var totemskilltwo = LokiPoe.InGameState.SkillBarHud.Slot(totemslottwo);
						if (totemskilltwo.CanUse() && (totemskilltwo.DeployedObjects.Select(o => o as Monster).Count(t => !t.IsDead && t.Distance < 30) < ExileRoutineSettings.Instance.TotemTwoMax))
						{
							var err1 = LokiPoe.InGameState.SkillBarHud.UseAt(totemslottwo, true, LokiPoe.MyPosition.GetPointAtDistanceAfterThis(cachedPosition, cachedDistance / 2));
							
							LokiPoe.ProcessHookManager.ClearAllKeyStates();

							if (err1 != LokiPoe.InGameState.UseResult.None)
							{
							}
							_totemtwoStopwatch.Restart();
							
							return LogicResult.Provided;	
						}
					}
				}
				
				if (cachedDistance <= maxenemydistance)
				{	
					if (curseonhit)
					{
						if (currentTarget.IsCursable && currentTarget.CurseCount < 1)
						{			
							slot = EnsurceCast(ExileRoutineSettings.Instance.CurseOnHitSlot);
						}
						else
						{
							if (aoe)
							{
								if (aoeuniques)
								{	
									if (NumberOfMobsNear(currentTarget, aoerange) >= 3)
									{
										slot = EnsurceCast(ExileRoutineSettings.Instance.AoESlot);
									}
									else
									{
										slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
									}
								}
								else
								{
									if ((NumberOfMobsNear(currentTarget, aoerange) >= 3) && (cachedRarity <= Rarity.Rare))
									{
										slot = EnsurceCast(ExileRoutineSettings.Instance.AoESlot);
									}
									else
									{
										slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
									}
								}
							}
							else
							{
								slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
							}
						}	
					}
					else 
					{	
						if (aoe)
						{
							if (aoeuniques)
							{	
								if (NumberOfMobsNear(currentTarget, aoerange) >= 3)
								{
									slot = EnsurceCast(ExileRoutineSettings.Instance.AoESlot);
								}
								else
								{
									slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
								}
							}
							else
							{
								if ((NumberOfMobsNear(currentTarget, aoerange) >= 3) && (cachedRarity <= Rarity.Rare))
								{
									slot = EnsurceCast(ExileRoutineSettings.Instance.AoESlot);
								}	
								else
								{
									slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
								}
							}
						}
						else
						{
							slot = EnsurceCast(ExileRoutineSettings.Instance.SingleTargetDPSSlot);
						}
					}
				}
				else
				{
                    return LogicResult.Provided;
				}
				
				if (_flaskHook != null && _flaskHook(cachedRarity, cachedHpPercent))
				{
                    return LogicResult.Provided;
				}
				
				if (slot != -1)
				{
					var err = LokiPoe.InGameState.SkillBarHud.BeginUseAt(slot, aip, cachedPosition);
					if (err != LokiPoe.InGameState.UseResult.None)
					{
					}
					return LogicResult.Provided;
				}
				else
				{
					return LogicResult.Provided;
				}

			return LogicResult.Unprovided;
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
							ExilePather.PathDistance(LokiPoe.MyPosition, m.Position, false, ExileRoutineSettings.Instance.LeaveFrame) < 30)
					.OrderBy(m => m.Distance)
					.FirstOrDefault();
			}
		}

		private static readonly Dictionary<int, int> _shrineTries = new Dictionary<int, int>();
		
		private static Func<Rarity, int, bool> _flaskHook;


		private static async Task<bool> HandleShrines()
		{
			if (ExileRoutineSettings.Instance.SkipShrines)
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

				var pathDistance = ExilePather.PathDistance(LokiPoe.MyPosition, pos, false, ExileRoutineSettings.Instance.LeaveFrame);

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

			var slotSkill = LokiPoe.InGameState.SkillBarHud.Slot(slot);
			if (slotSkill == null || !slotSkill.CanUse())
			{
				return -1;
			}
			return slot;
		}

	}
}