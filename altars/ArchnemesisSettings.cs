using System.Collections.Generic;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace Archnemesis
{
    class ArchnemesisSettings : JsonSettings
    {
        private static ArchnemesisSettings _instance;

        /// <summary>The current instance for this class. </summary>
        public static ArchnemesisSettings Instance => _instance ?? (_instance = new ArchnemesisSettings());

        /// <summary>The default ctor. Will use the settings path "Archnemesis".</summary>
        public ArchnemesisSettings()
			: base(GetSettingsFilePath(Configuration.Instance.Name, $"{"Archnemesis"}.json"))
		{
        }

        public bool VerboseLogging { get; set; }
        public bool DoCombine { get; set; }
        public bool ActivateRemnants { get; set; }

        public bool PickupOpulent { get; set; }

        public int MaxT1Reserve { get; set; } = 3;
        public int MaxT2Reserve { get; set; } = 3;
        public int MaxT1ReserveDisposable { get; set; } = 3;
        public int MaxT2ReserveDisposable { get; set; } = 3;
        public int MinFreeSpaceForT1Reserve { get; set; } = 4;
        public int CastPortalBeforeAltarRange { get; set; }

        public List<string> TargetRecipe { get; set; }

        /// <summary>
        /// These components will be kept/built to use as a supplement for other recipes if there is
        /// space in the altar.
        /// </summary>
        public List<string> TargetDisposableComponents { get; set; } = new List<string>();

        // Used to force pickup in for disposable components.
        public List<string> AdditionalPickup { get; set; } 

        // statistics
        public Dictionary<string, Dictionary<string, int>> ItemFinds { get; set; } = new Dictionary<string, Dictionary<string, int>>();
        
    }
}
