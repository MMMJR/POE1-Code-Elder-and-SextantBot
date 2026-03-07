using System.Windows;
using System.Windows.Controls;
using MmmjrBot.Class;
using log4net;
using DreamPoeBot.Loki.Common;

namespace MmmjrBot
{
    /// <summary>
    /// Logica di interazione per LabRunnerBotGui.xaml
    /// </summary>
    public partial class MmmjrGui : UserControl
    {
		private static readonly ILog Log = Logger.GetLoggerInstanceForType();
		public MmmjrGui()
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
    }
}
