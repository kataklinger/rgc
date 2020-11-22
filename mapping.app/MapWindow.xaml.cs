using System.Windows;

namespace mapping.app
{
    public partial class MapWindow : System.Windows.Window
    {
        public MapWindow()
        {
            InitializeComponent();

            Closing += (s, e) => { e.Cancel = Application.Current.MainWindow != null; };
        }
    }
}
