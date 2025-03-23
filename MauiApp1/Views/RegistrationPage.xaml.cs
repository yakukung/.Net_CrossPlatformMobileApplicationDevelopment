using MauiApp1.Services;
using MauiApp1.ViewModels;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views;

public partial class RegistrationPage : ContentPage
{
    public RegistrationPage()
    {
        InitializeComponent();

        // สร้าง RegisterAndWithdrawCourseService และส่งเข้าไปใน ViewModel
        var registerService = new RegisterAndWithdrawCourseService();
        BindingContext = new RegistrationPageViewModel(registerService);
    }
}