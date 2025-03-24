using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using MauiApp1.Views;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MauiApp1.ViewModels
{
    public partial class HomePageViewModel : BindableObject
    {
        private readonly DataService _dataService;
        private Student _student = new Student();
        private string _fullDataJson;

        public Student CurrentStudent
        {
            get => _student;
            set
            {
                _student = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Faculty));
                OnPropertyChanged(nameof(Major));
                OnPropertyChanged(nameof(YearDisplay));
                OnPropertyChanged(nameof(GpaDisplay));
                OnPropertyChanged(nameof(ProfileImage));
            }
        }

        public string FullName => $"{CurrentStudent?.FirstName} {CurrentStudent?.LastName}";
        public string Email => CurrentStudent?.Email ?? "N/A";
        public string Faculty => $"คณะ{CurrentStudent?.Faculty}" ?? "N/A";
        public string Major => $"สาขา{CurrentStudent?.Major}" ?? "N/A";
        public string YearDisplay => CurrentStudent?.Year > 0 ? $"{CurrentStudent.Year}" : "N/A";
        public string GpaDisplay => CurrentStudent?.Gpa >= 0 ? $"{CurrentStudent.Gpa:F2}" : "N/A";
        public string ProfileImage => CurrentStudent?.ProfileImage ?? "default_profile.png";

        public string FullDataJson
        {
            get => _fullDataJson;
            set
            {
                _fullDataJson = value;
                OnPropertyChanged();
            }
        }

        public List<Student> Students { get; set; }
        public List<Course> Courses { get; set; }
        public Dictionary<string, RegistrationData> Registrations { get; set; }


        public HomePageViewModel(DataService dataService)
        {
            _dataService = dataService;

            LoadStudentDataAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task LoadStudentDataAsync()
        {
            try
            {
                var student = await _dataService.LoadCurrentStudentAsync();
                if (student != null)
                {
                    CurrentStudent = student;
                    // ตรวจสอบข้อมูลที่โหลดมา
                    // System.Diagnostics.Debug.WriteLine($"Student loaded: {FullName}, Faculty: {Faculty}, Major: {Major}, Year: {YearDisplay}, GPA: {GpaDisplay}, Profile: {ProfileImage}");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("No student data found. Redirecting to LoginPage.");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student data: {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("User logged out successfully.");
                await Shell.Current.GoToAsync("LoginPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }
        [RelayCommand]
        private async Task NavigateToProfileAsync()
        {
                await Shell.Current.GoToAsync("ProfilePage");

        }
        [RelayCommand]
        private async Task NavigateToRegistrationAsync()
        {
                await Shell.Current.GoToAsync("RegistrationPage");

        }
        [RelayCommand]
        private async Task NavigateToWithdrawAsync()
        {
                await Shell.Current.GoToAsync("WithdrawPage");

        }

        [RelayCommand]
        private async Task NavigateToHistory()
        {
                await Shell.Current.GoToAsync("HistoryPage");
        }

    }
}