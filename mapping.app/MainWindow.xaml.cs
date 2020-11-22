namespace mapping.app
{
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var mapWindow = new MapWindow
            {
                DataContext = DataContext
            };

            mapWindow.Show();

            Closed += (s, e) => mapWindow.Close();
        }
    }
}
