using DreamPoeBot.Loki.Game;
using System.Windows.Forms;

namespace Default.AutoLogin
{
    public partial class Gui : UserControl
    {
        public Gui()
        {
            InitializeComponent();
        }

        private void SetCharNameButton_Click(object sender, RoutedEventArgs e)
        {
            using (LokiPoe.AcquireFrame())
            {
                if (LokiPoe.IsInGame)
                {
                    var charName = LokiPoe.Me.Name;
                    Settings.Instance.Character = string.IsNullOrEmpty(charName) ? "error" : charName;
                }
                else
                {
                    MessageBoxes.Error("You must be in the game.");
                }
            }
        }
    }
}