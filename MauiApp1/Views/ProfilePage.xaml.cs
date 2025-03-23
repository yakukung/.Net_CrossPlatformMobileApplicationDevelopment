using MauiApp1.ViewModels;

namespace MauiApp1.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfilePageViewModel _viewModel;

        public ProfilePage(ProfilePageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // โหลดข้อมูลใหม่ทุกครั้งที่หน้าแสดงผล
            if (_viewModel != null)
            {
                await _viewModel.LoadStudentDataAsync();
            }
        }
    }
}