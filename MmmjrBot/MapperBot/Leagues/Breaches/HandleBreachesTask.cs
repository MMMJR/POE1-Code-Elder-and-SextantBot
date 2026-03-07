using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using log4net;

namespace MmmjrBot.Leagues.Breaches
{
	public static class HandleBreachesTask
	{
		private static readonly ILog Log = Logger.GetLoggerInstanceForType();

		private static bool _skip;

		// This needs to be static so we have persistent data to use. Alternatively, we could host it elsewhere and pass it to the task, but
		// for this plugin, we'll do the former.
		internal static readonly AreaDataManager<BreachData> BreachDataManager =
			new AreaDataManager<BreachData>(hash => new BreachData(hash)) {DebugLogging = true};

		private static BreachCache _current;
		private static int _moveErrors;

		static bool ShouldActivate(BreachCache breach)
		{
			// Cache and reuse this for performance reasons.
			if (breach.Activate != null)
			{
				return breach.Activate.Value;
			}

			Log.DebugFormat("[HandleBreachesTask] The Breach [{0}] will be activated.", breach.Id);

			breach.Activate = true;

			return false;
		}

		/// <summary>
		/// Coroutine logic to execute.
		/// </summary>
		/// <returns>true if logic was executed to handle this type and false otherwise.</returns>
		public static async Task<bool> Run()
		{
			// NOTE: This task's Run function is triggered from "hook_post_combat" Logic, as it's added via a secondary TaskManager!

			// If this task needs to be disabled due to errors, support doing so.
			if (_skip)
			{
				return false;
			}

			// Don't do anything in these cases.
			if (LokiPoe.Me.IsDead || LokiPoe.Me.IsInHideout || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInMapRoom)
				return false;

			// If we're currently disabled, skip logic.
			if (!MmmjrBotSettings.Instance.EnabledBreachs)
				return false;

			var myPos = LokiPoe.MyPosition;

			var active = BreachDataManager.Active;
			if (active == null)
				return false;

			// Make sure the breach is still valid and not blacklisted if it's set.
			// We don't re-eval current against settings, because of the performance overhead.
			if (_current != null)
			{
				if (!_current.IsValid || Blacklist.Contains(_current.Id))
				{
					_current = null;
				}
			}

			// Find the next best breach.
			if (_current == null)
			{
				_current =
					active.Breaches.Where(m => m.IsValid && !Blacklist.Contains(m.Id) && ShouldActivate(m))
						.OrderBy(m => m.Position.Distance(myPos))
						.FirstOrDefault();
				_moveErrors = 0;
			}

			// Nothing to do if there's no breach.
			if (_current == null)
			{
				return false;
			}

			// If we can't move to the breach, blacklist it.
			if (_moveErrors > 5)
			{
				Blacklist.Add(_current.Id, TimeSpan.FromHours(1),
					string.Format("[HandleBreachesTask::Logic] Unable to move to the Breach."));
				_current = null;
				return true;
			}

			// If we are too far away to interact, move towards the object.
			if (myPos.Distance(_current.WalkablePosition) > 50)
			{
				// Make sure nothing is in the way.
				await Coroutines.CloseBlockingWindows();

				// Try to move towards the location.
				if (!PlayerMoverManager.MoveTowards(_current.WalkablePosition))
				{
					Log.ErrorFormat("[HandleBreachesTask::Logic] PlayerMoverManager.MoveTowards({0}) failed for Breach [{1}].",
						_current.WalkablePosition, _current.Id);
					_moveErrors++;
					return true;
				}

				_moveErrors = 0;

				return true;
			}

			// Make sure we're not doing anything before we interact.
			await Coroutines.FinishCurrentAction();

			// Now process the object, but make sure it exists.
			var breach = _current.NetworkObject;
			if (breach == null)
			{
				_current.Activate = false;
				Log.ErrorFormat("[HandleBreachesTask::Logic] The NetworkObject does not exist for the Breach [{0}] yet.", _current.Id);
				_current = null;
				return true;
			}

			// Try to move towards the location.
			if (!PlayerMoverManager.MoveTowards(_current.WalkablePosition))
			{
				Log.ErrorFormat("[HandleBreachesTask::Logic] PlayerMoverManager.MoveTowards({0}) failed for Breach [{1}].",
					_current.WalkablePosition, _current.Id);
				_moveErrors++;
				return true;
			}

			return true;
		}
		
		/// <summary>The bot Start event.</summary>
		public static void Start()
		{
			_skip = false;
			_current = null; // Force clear, in case settings changed.

			BreachDataManager.Start(); // Will check IsInGame as needed
		}

		/// <summary>The bot Tick event.</summary>
		public static void Tick()
		{
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
                return;

            BreachDataManager.Tick(); // Will check IsInGame as needed
		}

		/// <summary>The bot Stop event.</summary>
		public static void Stop()
		{
			BreachDataManager.Stop(); // Will check IsInGame as needed
		}
	}
}