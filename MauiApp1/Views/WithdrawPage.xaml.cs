using MauiApp1.ViewModels;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class WithdrawPage : ContentPage
{
    public WithdrawPage(RegisterAndWithdrawCourseService registerService)
    {
        InitializeComponent();
        BindingContext = new WithdrawPageViewModel(registerService);
    }
}