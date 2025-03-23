using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.ViewModels
{
    public partial class ProfilePageViewModel : BindableObject
    {
        private readonly ProfileService _profileService;
        private Student? _student;

        public ProfilePageViewModel(ProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            LoadStudentDataCommand = new Command(async () => await LoadStudentDataAsync());
            CurrentCourses = new ObservableCollection<Course>();

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

        public async Task LoadStudentDataAsync()
        {
            try
            {
                // โหลดข้อมูลนักศึกษา
                var student = await _profileService.LoadCurrentStudentAsync();
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine("No student data found.");
                    return;
                }

                CurrentStudent = student;

                // โหลดรายวิชาที่ลงทะเบียน
                var currentCourses = await _profileService.GetStudentCoursesAsync(student.Id);
                CurrentCourses.Clear();
                foreach (var course in currentCourses)
                {
                    CurrentCourses.Add(course);
                }

                // แสดงข้อมูลใน Debug Logs
                // System.Diagnostics.Debug.WriteLine($"Student ID: {CurrentStudent.Id}");
                // System.Diagnostics.Debug.WriteLine($"Student Name: {CurrentStudent.FirstName} {CurrentStudent.LastName}");
                // System.Diagnostics.Debug.WriteLine("Registered Courses:");
                foreach (var course in CurrentCourses)
                {
                    System.Diagnostics.Debug.WriteLine($"- {course.CourseId}: {course.Name}");
                }

                System.Diagnostics.Debug.WriteLine("Profile data loaded successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student data: {ex.Message}");
            }
        }
        
        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("//HomePage");
        }
    }
}