using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using MauiApp1.Views;
using System.Collections.Generic;

namespace MauiApp1.ViewModels
{
    public class HomePageViewModel : BindableObject
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

        public ICommand LoadStudentDataCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand NavigateToProfileCommand { get; }
        public ICommand NavigateToRegistrationCommand { get; }
        public ICommand LoadFullDataCommand { get; }

        public HomePageViewModel(DataService dataService)
        {
            _dataService = dataService;

            LoadStudentDataCommand = new Command(async () => await LoadStudentDataAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());
            NavigateToProfileCommand = new Command(async () => await NavigateToProfileAsync());
            NavigateToRegistrationCommand = new Command(async () => await NavigateToRegistrationCommandAsync());
            LoadFullDataCommand = new Command(async () => await LoadFullDataAsync());

            LoadStudentDataAsync().ConfigureAwait(false);
        }

        // ตัวที่แยกเรียกใช้แต่ละข้อมูลที่เราจะเอาแสดง
        private async Task LoadStudentDataAsync()
        {
            try
            {
                var student = await _dataService.LoadCurrentStudentAsync();
                if (student != null)
                {
                    CurrentStudent = student;

                    // ตรวจสอบข้อมูลที่โหลดมา
                    System.Diagnostics.Debug.WriteLine($"Student loaded: {FullName}, Faculty: {Faculty}, Major: {Major}, Year: {YearDisplay}, GPA: {GpaDisplay}, Profile: {ProfileImage}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No student data found. Redirecting to LoginPage.");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student data: {ex.Message}");
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                _dataService.Logout();
                System.Diagnostics.Debug.WriteLine("User logged out successfully.");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during logout: {ex.Message}");
            }
        }
        private async Task NavigateToProfileAsync()
        {
                await Shell.Current.GoToAsync("//ProfilePage");

        }
        private async Task NavigateToRegistrationCommandAsync()
        {
                await Shell.Current.GoToAsync("//RegistrationPage");

        }

        private async Task LoadFullDataAsync()
        {
            try
            {
                var fullData = await _dataService.GetStudentFullDataAsync(CurrentStudent.Id);
                if (fullData != null)
                {
                    FullDataJson = System.Text.Json.JsonSerializer.Serialize(fullData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.Diagnostics.Debug.WriteLine($"Full Data:\n{FullDataJson}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No full data found for the current student.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading full data: {ex.Message}");
            }
        }
    }
}