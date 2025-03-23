using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.IO;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MauiApp1.ViewModels
{
    public class RegistrationPageViewModel : BindableObject
    {
        private ObservableCollection<Course> _availableCourses;
        private DataModel _data;
        private readonly DataService _dataService;

        public ObservableCollection<Course> AvailableCourses
        {
            get => _availableCourses;
            set
            {
                _availableCourses = value;
                OnPropertyChanged();
            }
        }

        public ICommand GoBackCommand { get; private set; }
        public ICommand LoadCoursesCommand { get; private set; }
        public ICommand RegisterCourseCommand { get; private set; }

        public RegistrationPageViewModel(DataService dataService = null)
        {
            _dataService = dataService ?? new DataService();
            AvailableCourses = new ObservableCollection<Course>();

            GoBackCommand = new Command(async () => await GoBack());
            LoadCoursesCommand = new Command(async () => await LoadAvailableCourses());
            RegisterCourseCommand = new Command<Course>(async (course) => await RegisterCourse(course));

            LoadAvailableCourses();
        }

        private async Task<DataModel> GetDataAsync()
        {
            if (_data != null) return _data;

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("data.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                _data = JsonSerializer.Deserialize<DataModel>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return _data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading JSON: {ex.Message}");
                throw;
            }
        }

        private async Task LoadAvailableCourses()
        {
            try
            {
                var data = await GetDataAsync();
                AvailableCourses.Clear();
                foreach (var course in data.Courses)
                {
                    AvailableCourses.Add(course);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load courses", "OK");
            }
        }

        private async Task RegisterCourse(Course course)
        {
            try
            {
                var data = await GetDataAsync();
                var studentId = Preferences.Get("StudentId", "");
                
                if (string.IsNullOrEmpty(studentId))
                {
                    await Shell.Current.DisplayAlert("Error", "Please login first", "OK");
                    return;
                }

                if (data.Registrations.TryGetValue(studentId, out var studentReg))
                {
                    var isAlreadyRegistered = studentReg.Current?.Any(r => 
                        r.CourseId == course.CourseId && r.Status == "registered") ?? false;

                    if (isAlreadyRegistered)
                    {
                        await Shell.Current.DisplayAlert("Error", "You are already registered for this course", "OK");
                        return;
                    }
                }

                await Shell.Current.DisplayAlert("Success", "Course registered successfully", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to register course", "OK");
            }
        }

        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}