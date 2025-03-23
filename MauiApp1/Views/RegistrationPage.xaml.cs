using MauiApp1.ViewModels;

namespace MauiApp1.Views
{
    public partial class RegistrationPage : ContentPage
    {
        public RegistrationPage(RegistrationPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}