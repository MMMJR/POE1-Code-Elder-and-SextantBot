using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ArchnemesisRecipies.Models;
using Newtonsoft.Json;

namespace Archnemesis.Models
{
    [DebuggerDisplay("{Name}")]
    public class ArchnemesisMod : Entity
    {
        public string Image { get; set; }
        public string Mod { get; set; }
        public string Type { get; set; }
        [JsonProperty("Components")]
        public List<string> ComponentNames { get; set; } = new List<string>();
        public string Effect { get; set; }
        public string Regex { get; set; }
        public List<Map> Maps { get; set; } = new List<Map>();
    }

    public static class ArchnemesisModExtensions
    {
        public static List<ArchnemesisMod> GetAllComponents(this ArchnemesisMod parent, bool highestTier = false)
        {
            var components = new List<ArchnemesisMod>();
            if (parent.ComponentNames.Any())
            {
                foreach (var componentName in parent.ComponentNames)
                {
                    var component = Archnemesis.GetRecipe(componentName);
                    var componentChildren = component.GetAllComponents();
                    components.Add(component);
                    components.AddRange(componentChildren);
                }
            }

            return components;
        }

        public static List<ArchnemesisMod> Children(this ArchnemesisMod parent)
        {
            return parent.ComponentNames.Select(Archnemesis.GetRecipe).ToList();
        }


        public static int Tier(this ArchnemesisMod mod)
        {
            // Todo: Currently only works for T1
            return mod.ComponentNames.Any() ? 2 : 1;
        }
    }
}
