using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.ViewModels
{
    public class ProfilePageViewModel : BindableObject
    {
        private readonly DataService _dataService;
        private Student? _student;

        public ProfilePageViewModel(DataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            LoadStudentDataCommand = new Command(async () => await LoadStudentDataAsync());
            goBackCommand = new Command(async () => await GoBackCommandAsync());
            CurrentCourses = new ObservableCollection<Course>();

            System.Diagnostics.Debug.WriteLine("ProfilePageViewModel initialized.");

            Task.Run(async () => await LoadStudentDataAsync());
        }

        public Student? CurrentStudent
        {
            get => _student;
            set
            {
                if (_student != value)
                {
                    _student = value;
                    OnPropertyChanged(nameof(CurrentStudent));
                    OnPropertyChanged(nameof(StudentId));
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(Faculty));
                    OnPropertyChanged(nameof(Major));
                    OnPropertyChanged(nameof(YearDisplay));
                    OnPropertyChanged(nameof(GpaDisplay));
                    OnPropertyChanged(nameof(ProfileImage));
                }
            }
        }

        public string StudentId => CurrentStudent?.Id ?? "N/A";
        public string FullName => $"{CurrentStudent?.FirstName} {CurrentStudent?.LastName}";
        public string Faculty => CurrentStudent?.Faculty ?? "N/A";
        public string Major => CurrentStudent?.Major ?? "N/A";
        public string YearDisplay => CurrentStudent?.Year > 0 ? $"ปี {CurrentStudent.Year}" : "N/A";
        public string GpaDisplay => CurrentStudent?.Gpa >= 0 ? $"GPA: {CurrentStudent.Gpa:F2}" : "N/A";
        public string ProfileImage => CurrentStudent?.ProfileImage ?? "default_profile.png";

        public ObservableCollection<Course> CurrentCourses { get; }

        public ICommand LoadStudentDataCommand { get; }
        public ICommand goBackCommand { get; }

        private async Task LoadStudentDataAsync()
        {
            try
            {
                var student = await _dataService.LoadCurrentStudentAsync();
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine("No student data found.");
                    return;
                }

                CurrentStudent = student;

                var currentCourses = await _dataService.GetStudentCoursesAsync(student.Id);
                CurrentCourses.Clear();
                foreach (var course in currentCourses)
                {
                    CurrentCourses.Add(course);
                }

                // System.Diagnostics.Debug.WriteLine($"Profile loaded: {FullName}, Faculty: {Faculty}, Major: {Major}, Year: {YearDisplay}, GPA: {GpaDisplay}, Profile: {ProfileImage}");
                // System.Diagnostics.Debug.WriteLine("Profile loaded successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student data: {ex.Message}");
            }
        }

        private async Task GoBackCommandAsync()
        {
            System.Diagnostics.Debug.WriteLine("Navigating back to HomePage.");
            await Shell.Current.GoToAsync("//HomePage");
        }
    }
}