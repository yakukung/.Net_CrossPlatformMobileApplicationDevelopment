using MauiApp1.Services;
using MauiApp1.Models;
using Microsoft.Maui.Storage;

namespace MauiApp1.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly DataService _dataService = new();

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string idOrEmail = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(idOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                MessageLabel.Text = "Please enter ID/Email and password.";
                return;
            }

            try
            {
                // Authenticate based on input type (email or ID)
                Student? student = idOrEmail.Contains("@")
                    ? await _dataService.AuthenticateAsync(idOrEmail, password)
                    : await _dataService.AuthenticateByIdAsync(idOrEmail, password);

                if (student != null)
                {
                    // Save login state and user info
                    Preferences.Set("IsLoggedIn", true);
                    Preferences.Set("UserEmail", student.Email);
                    Preferences.Set("StudentId", student.Id);

                    // Navigate to the home page
                    await Shell.Current.GoToAsync("//HomePage");
                }
                else
                {
                    MessageLabel.Text = "Invalid ID/Email or password.";
                }
            }
            catch (Exception ex)
            {
                MessageLabel.Text = $"An error occurred: {ex.Message}";
            }
        }
    }
}