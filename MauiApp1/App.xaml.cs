using MauiApp1.Services;

namespace MauiApp1
{
    public partial class App : Application
    {
        private readonly DataService _dataService;
        
        public App()
        {
            InitializeComponent();
            
            _dataService = new DataService();
            MainPage = new AppShell();
        }
        
        public DataService GetDataService()
        {
            return _dataService;
        }
    }
}