using MauiApp1.ViewModels;
using MauiApp1.Services;

namespace MauiApp1.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            
            // ใช้ DataService ที่มีอยู่แล้วจาก App แทนการสร้างใหม่
            var dataService = ((App)Application.Current).GetDataService();
            BindingContext = new HomePageViewModel(dataService);
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // รีเฟรชข้อมูลเมื่อกลับมาที่หน้านี้
            if (BindingContext is HomePageViewModel viewModel)
            {
                viewModel.LoadStudentDataCommand.Execute(null);
            }
        }
    }
}