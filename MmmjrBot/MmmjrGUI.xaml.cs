using System;
using System.Windows;
using System.Windows.Controls;
using MmmjrBot.Class;
using log4net;
using DreamPoeBot.Loki.Common;
using MmmjrBot.Lib;
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

        private void MapperGlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                MapperGlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }

        private void ChangeStance_OnClick2(object sender, RoutedEventArgs e)
        {
            if (MmmjrBotSettings.Instance.MapperBloorOrSand == MmmjrBotSettings.BloodAndSand.Blood)
                MmmjrBotSettings.Instance.MapperBloorOrSand = MmmjrBotSettings.BloodAndSand.Sand;
            else
                MmmjrBotSettings.Instance.MapperBloorOrSand = MmmjrBotSettings.BloodAndSand.Blood;
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
