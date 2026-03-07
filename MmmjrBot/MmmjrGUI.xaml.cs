using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MmmjrBot.Class;
using log4net;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game.GameData;
using MmmjrBot.Lib;
using MmmjrBot.QuestBot;
using System.Globalization;
using System.Collections.ObjectModel;
using DreamPoeBot.Loki.Game;

namespace MmmjrBot
{
    /// <summary>
    /// Interaction logic for MmmjrGUI.xaml
    /// </summary>
    public partial class MmmjrGUI : UserControl
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        public MmmjrGUI()
        {
            InitializeComponent();
        }

        private void RemoveDefensiveSkillRule(object sender, RoutedEventArgs e)
        {
            DefensiveSkillsClass rule = (sender as Button).DataContext as DefensiveSkillsClass;
            MmmjrBotSettings.Instance.DefensiveSkills.Remove(rule);
        }
        private void QuestRemoveDefensiveSkillRule(object sender, RoutedEventArgs e)
        {
            DefensiveSkillsClass rule = (sender as Button).DataContext as DefensiveSkillsClass;
            MmmjrBotSettings.Instance.QuestDefensiveSkills.Remove(rule);
        }
        private void MapperRemoveDefensiveSkillRule(object sender, RoutedEventArgs e)
        {
            DefensiveSkillsClass rule = (sender as Button).DataContext as DefensiveSkillsClass;
            MmmjrBotSettings.Instance.MapperDefensiveSkills.Remove(rule);
        }
        private void MapperAddGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = MapperGlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!MmmjrBotSettings.Instance.MapperGlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.MapperGlobalNameIgnoreList.Add(text);
                MmmjrBotSettings.Instance.MapperUpdateGlobalNameIgnoreList();
                MapperGlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat(
                     "[AddGlobalNameIgnoreButtonOnClick] The skillgem {0} is already in the GlobalNameIgnoreList.", text);
            }
        }

        private void QuestAddGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = QuestGlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!MmmjrBotSettings.Instance.QuestGlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.QuestGlobalNameIgnoreList.Add(text);
                MmmjrBotSettings.Instance.QuestUpdateGlobalNameIgnoreList();
                QuestGlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat(
                     "[AddGlobalNameIgnoreButtonOnClick] The skillgem {0} is already in the GlobalNameIgnoreList.", text);
            }
        }


        private void AddGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!MmmjrBotSettings.Instance.GlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.GlobalNameIgnoreList.Add(text);
                MmmjrBotSettings.Instance.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
               Log.ErrorFormat(
                    "[AddGlobalNameIgnoreButtonOnClick] The skillgem {0} is already in the GlobalNameIgnoreList.", text);
            }
        }
        private void QuestRemoveGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = QuestGlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (MmmjrBotSettings.Instance.QuestGlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.QuestGlobalNameIgnoreList.Remove(text);
                MmmjrBotSettings.Instance.QuestUpdateGlobalNameIgnoreList();
                QuestGlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat("[RemoveGlobalNameIgnoreButtonOnClick] The skillgem {0} is not in the GlobalNameIgnoreList.", text);
            }
        }

        private void MapperRemoveGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = MapperGlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (MmmjrBotSettings.Instance.MapperGlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.MapperGlobalNameIgnoreList.Remove(text);
                MmmjrBotSettings.Instance.MapperUpdateGlobalNameIgnoreList();
                MapperGlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat("[RemoveGlobalNameIgnoreButtonOnClick] The skillgem {0} is not in the GlobalNameIgnoreList.", text);
            }
        }

        private void RemoveGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (MmmjrBotSettings.Instance.GlobalNameIgnoreList.Contains(text))
            {
                MmmjrBotSettings.Instance.GlobalNameIgnoreList.Remove(text);
                MmmjrBotSettings.Instance.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat("[RemoveGlobalNameIgnoreButtonOnClick] The skillgem {0} is not in the GlobalNameIgnoreList.", text);
            }
        }

        private void GlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                GlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }

        private void MapperGlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                MapperGlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }
        private void QuestGlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                QuestGlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }

        private void ChangeStance_OnClick(object sender, RoutedEventArgs e)
        {
            if (MmmjrBotSettings.Instance.BloorOrSand == MmmjrBotSettings.BloodAndSand.Blood)
                MmmjrBotSettings.Instance.BloorOrSand = MmmjrBotSettings.BloodAndSand.Sand;
            else
                MmmjrBotSettings.Instance.BloorOrSand = MmmjrBotSettings.BloodAndSand.Blood;
        }

        private void ChangeStance_OnClick2(object sender, RoutedEventArgs e)
        {
            if (MmmjrBotSettings.Instance.MapperBloorOrSand == MmmjrBotSettings.BloodAndSand.Blood)
                MmmjrBotSettings.Instance.MapperBloorOrSand = MmmjrBotSettings.BloodAndSand.Sand;
            else
                MmmjrBotSettings.Instance.MapperBloorOrSand = MmmjrBotSettings.BloodAndSand.Blood;
        }
        private void ChangeStance_OnClick3(object sender, RoutedEventArgs e)
        {
            if (MmmjrBotSettings.Instance.QuestBloorOrSand == MmmjrBotSettings.BloodAndSand.Blood)
                MmmjrBotSettings.Instance.QuestBloorOrSand = MmmjrBotSettings.BloodAndSand.Sand;
            else
                MmmjrBotSettings.Instance.QuestBloorOrSand = MmmjrBotSettings.BloodAndSand.Blood;
        }


        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MmmjrBotSettings.Instance.OnSingleMapSettingsChanged(5);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public class RewardConverter : IMultiValueConverter
        {
            public static readonly RewardConverter Instance = new RewardConverter();

            private static readonly RewardEntry Any = new RewardEntry(MmmjrBotSettings.DefaultRewardName, Rarity.Normal);

            private static readonly List<RewardEntry> NoClass = new List<RewardEntry> { new RewardEntry(MmmjrBotSettings.UnsetClassRewardName, Rarity.Normal) };

            private static readonly List<RewardEntry> Bandits = new List<RewardEntry>
            {
                new RewardEntry(BanditHelper.EramirName, Rarity.Normal),
                new RewardEntry(BanditHelper.AliraName, Rarity.Normal),
                new RewardEntry(BanditHelper.KraitynName, Rarity.Normal),
                new RewardEntry(BanditHelper.OakName, Rarity.Normal)
            };

            private static readonly List<RewardEntry> ThresholdJewels = new List<RewardEntry>
            {
                Any,
                new RewardEntry("Collateral Damage", Rarity.Unique),
                new RewardEntry("Fight for Survival", Rarity.Unique),
                new RewardEntry("First Snow", Rarity.Unique),
                new RewardEntry("Frozen Trail", Rarity.Unique),
                new RewardEntry("Hazardous Research", Rarity.Unique),
                new RewardEntry("Inevitability", Rarity.Unique),
                new RewardEntry("Omen on the Winds", Rarity.Unique),
                new RewardEntry("Overwhelming Odds", Rarity.Unique),
                new RewardEntry("Rapid Expansion", Rarity.Unique),
                new RewardEntry("Ring of Blades", Rarity.Unique),
                new RewardEntry("Spreading Rot", Rarity.Unique),
                new RewardEntry("Violent Dead", Rarity.Unique),
                new RewardEntry("Wildfire", Rarity.Unique),
            };

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                var questId = (string)values[0];
                var charClass = (CharacterClass)values[1];

                if (charClass == CharacterClass.None)
                    return NoClass;

                if (questId == Quests.DealWithBandits.Id)
                    return Bandits;

                if (questId == Quests.DeathToPurity.Id)
                    return ThresholdJewels;

                var datRewards = Dat.QuestRewards
                    .Where(r => r.Quest.Id == questId && (r.Class == charClass || r.Class == CharacterClass.None))
                    .ToList();

                if (datRewards.Count == 0)
                    return new List<RewardEntry> { new RewardEntry("Error! No values.", Rarity.Normal) };

                var rewardEntries = new List<RewardEntry> { Any };
                rewardEntries.AddRange(datRewards.Select(r => new RewardEntry(r.Item.Name, r.Rarity)));
                return rewardEntries;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class RarityToColorConverter : IValueConverter
        {
            public static readonly RarityToColorConverter Instance = new RarityToColorConverter();

            private static readonly HashSet<string> WhiteColorOverride = new HashSet<string>
            {
                MmmjrBotSettings.DefaultRewardName,
                BanditHelper.EramirName,
                BanditHelper.AliraName,
                BanditHelper.KraitynName,
                BanditHelper.OakName
            };

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var reward = (RewardEntry)value;
                var name = reward.Name;
                var rarity = reward.Rariry;

                if (WhiteColorOverride.Contains(name))
                    return Brushes.White;

                //for some reason unique jewel rewards for "Through Sacred Ground" have Quest rarity
                if (rarity == Rarity.Quest && name.ContainsIgnorecase("jewel"))
                    return RarityColors.Unique;

                return RarityColors.FromRarity(rarity);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class CharClassToBoolConverter : IValueConverter
        {
            public static readonly CharClassToBoolConverter Instance = new CharClassToBoolConverter();

            // used for element's IsEnabled
            // if parameter is true and class is not selected, element (character selection box) will be enabled
            // if parameter is false and class is not selected, element (quest reward selection box) will be disabled
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var charClass = (CharacterClass)value;
                return (bool)parameter ? charClass == CharacterClass.None : charClass != CharacterClass.None;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class RewardEntry
        {
            public string Name { get; set; }
            public Rarity Rariry { get; set; }

            public RewardEntry(string name, Rarity rariry)
            {
                Name = name;
                Rariry = rariry;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private void AddRuleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var rule = new MmmjrBotSettings.GrindingRule();
            rule.Areas.Add(new MmmjrBotSettings.GrindingArea());
            MmmjrBotSettings.Instance.GrindingRules.Add(rule);
        }

        private void DeleteRuleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var rule = (MmmjrBotSettings.GrindingRule)button.DataContext;
            MmmjrBotSettings.Instance.GrindingRules.Remove(rule);
        }

        private void AddAreaButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var areas = (ObservableCollection<MmmjrBotSettings.GrindingArea>)button.Tag;
            areas.Add(new MmmjrBotSettings.GrindingArea());
        }

        private void AreaSelectionComboBox_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var cb = (ComboBox)sender;
            var area = (MmmjrBotSettings.GrindingArea)cb.DataContext;
            var rule = (MmmjrBotSettings.GrindingRule)cb.Tag;
            rule.Areas.Remove(area);
        }

        private void SetCharNameButton_Click(object sender, RoutedEventArgs e)
        {
                if (LokiPoe.IsInGame)
                {
                    var charName = LokiPoe.Me.Name;
                    MmmjrBotSettings.Instance.Character = string.IsNullOrEmpty(charName) ? "error" : charName;
                }
                else
                {
                    MessageBoxes.Error("You must be in the game.");
                }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Combobox_OnLoaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
