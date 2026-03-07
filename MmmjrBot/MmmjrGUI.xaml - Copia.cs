using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MmmjrBot.Class;
using log4net;
using DreamPoeBot.Loki.Common;

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

        private void ChangeStance_OnClick(object sender, RoutedEventArgs e)
        {
            if (MmmjrBotSettings.Instance.BloorOrSand == MmmjrBotSettings.BloodAndSand.Blood)
                MmmjrBotSettings.Instance.BloorOrSand = MmmjrBotSettings.BloodAndSand.Sand;
            else
                MmmjrBotSettings.Instance.BloorOrSand = MmmjrBotSettings.BloodAndSand.Blood;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MmmjrBotSettings.Instance.OnSingleMapSettingsChanged(5);
        }
    }
}
