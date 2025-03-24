using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Graphics;

namespace MauiApp1.ViewModels
{
    public partial class HistoryViewModel : ObservableObject
    {
        private readonly ProfileService _profileService;
        private Student? _student;
        private bool _showRegistrations = true;
        private bool _showCurrent = true;
        private string _selectedTermId = string.Empty;
        private List<Term> _availableTerms = new();

        [ObservableProperty]
        private ObservableCollection<RegistrationHistoryItem> _historyItems = new();

        [ObservableProperty]
        private string _studentInfo = string.Empty;

        [ObservableProperty]
        private string _currentTermDisplay = string.Empty;

        [ObservableProperty]
        private Color _registrationTabColor;

        [ObservableProperty]
        private Color _registrationTabTextColor;

        [ObservableProperty]
        private Color _withdrawalTabColor;

        [ObservableProperty]
        private Color _withdrawalTabTextColor;

        [ObservableProperty]
        private Color _term1TabColor;

        [ObservableProperty]
        private Color _term1TabTextColor;

        [ObservableProperty]
        private Color _term2TabColor;

        [ObservableProperty]
        private Color _term2TabTextColor;

        [ObservableProperty]
        private Color _term3TabColor;

        [ObservableProperty]
        private Color _term3TabTextColor;

        [ObservableProperty]
        private string _term1Text = "เทอม 1";

        [ObservableProperty]
        private string _term2Text = "เทอม 2";

        [ObservableProperty]
        private string _term3Text = "เทอม 3";

        public HistoryViewModel(ProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            
            // Initialize tab colors
            Term1TabColor = Colors.LightGray;
            Term1TabTextColor = Colors.Black;
            Term2TabColor = Colors.LightGray;
            Term2TabTextColor = Colors.Black;
            Term3TabColor = Colors.LightGray;
            Term3TabTextColor = Colors.Black;
            
            UpdateTabColors();
            
            Task.Run(async () => await LoadDataAsync());
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                _student = await _profileService.LoadCurrentStudentAsync();
                if (_student == null)
                {
                    System.Diagnostics.Debug.WriteLine("No student data loaded");
                    return;
                }

                StudentInfo = $"{_student.Id} - {_student.FirstName} {_student.LastName}";

                var data = await _profileService.GetDataAsync();
                _availableTerms = data.Terms.OrderByDescending(t => t.Id).ToList();
                System.Diagnostics.Debug.WriteLine($"Available terms loaded: {_availableTerms.Count}");

                // Set term tab texts to match the term IDs (e.g., "1/2567")
                if (_availableTerms.Count >= 1)
                {
                    Term1Text = $"เทอม {_availableTerms[0].Id}"; // e.g., "เทอม 1/2567"
                    System.Diagnostics.Debug.WriteLine($"Term1Text set to: {Term1Text}");
                }
                if (_availableTerms.Count >= 2)
                {
                    Term2Text = $"เทอม {_availableTerms[1].Id}"; // e.g., "เทอม 2/2567"
                    System.Diagnostics.Debug.WriteLine($"Term2Text set to: {Term2Text}");
                }
                if (_availableTerms.Count >= 3)
                {
                    Term3Text = $"เทอม {_availableTerms[2].Id}"; // e.g., "เทอม 3/2567"
                    System.Diagnostics.Debug.WriteLine($"Term3Text set to: {Term3Text}");
                }

                // Select default term
                var currentTerm = _availableTerms.FirstOrDefault(t => t.IsCurrent);
                if (currentTerm != null)
                {
                    _selectedTermId = currentTerm.Id;
                    CurrentTermDisplay = $"ภาคการศึกษา: {currentTerm.Name}";
                }
                else if (_availableTerms.Count > 0)
                {
                    _selectedTermId = _availableTerms[0].Id;
                    CurrentTermDisplay = $"ภาคการศึกษา: {_availableTerms[0].Name}";
                }
                else
                {
                    CurrentTermDisplay = "ภาคการศึกษาปัจจุบัน";
                }
                System.Diagnostics.Debug.WriteLine($"Initial selected term: {_selectedTermId}");

                UpdateTermTabColors();
                await LoadHistoryItemsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async Task LoadHistoryItemsAsync()
        {
            if (_student == null)
            {
                System.Diagnostics.Debug.WriteLine("Student is null, cannot load history items.");
                return;
            }

            HistoryItems.Clear();
            // System.Diagnostics.Debug.WriteLine($"Loading history items for term: {_selectedTermId}, ShowRegistrations: {_showRegistrations}");

            var data = await _profileService.GetDataAsync();
            if (!data.Registrations.TryGetValue(_student.Id, out var registrationData))
            {
                System.Diagnostics.Debug.WriteLine($"No registration data found for student {_student.Id}");
                return;
            }

            var items = new List<RegistrationHistoryItem>();

            // Combine all registrations
            var allRegistrations = new List<Registration>();
            allRegistrations.AddRange(registrationData.Current);
            allRegistrations.AddRange(registrationData.Previous);
            System.Diagnostics.Debug.WriteLine($"Total registrations found: {allRegistrations.Count}");

            // Filter by selected term
            var termRegistrations = allRegistrations;
            if (!string.IsNullOrEmpty(_selectedTermId))
            {
                termRegistrations = allRegistrations.Where(r => r.Term == _selectedTermId).ToList();
                System.Diagnostics.Debug.WriteLine($"Registrations after term filter ({_selectedTermId}): {termRegistrations.Count}");
            }

            // Filter by status
            var filteredRegistrations = _showRegistrations
                ? termRegistrations.Where(r => r.Status == "registered" || r.Status == "completed")
                : termRegistrations.Where(r => r.Status == "withdrawn");
            System.Diagnostics.Debug.WriteLine($"Registrations after status filter: {filteredRegistrations.Count()}");

            foreach (var reg in filteredRegistrations)
            {
                var course = data.Courses.FirstOrDefault(c => c.CourseId == reg.CourseId);
                if (course != null)
                {
                    items.Add(CreateHistoryItem(course, reg));
                }
            }

            // Sort items
            items = _showRegistrations
                ? items.OrderByDescending(i => i.RegistrationDate).ToList()
                : items.OrderByDescending(i => i.WithdrawalDate).ToList();

            foreach (var item in items)
            {
                HistoryItems.Add(item);
            }

            System.Diagnostics.Debug.WriteLine($"Final history items loaded: {HistoryItems.Count}");
        }

        private RegistrationHistoryItem CreateHistoryItem(Course course, Registration reg)
        {
            var item = new RegistrationHistoryItem
            {
                CourseId = course.CourseId,
                CourseName = course.Name,
                Credits = course.Credit.ToString(),
                Term = reg.Term,
                RegistrationDate = reg.RegistrationDate,
                RegistrationDateDisplay = reg.RegistrationDate.ToString("dd/MM/yyyy HH:mm"),
                Section = course.Section.ToString(),
                Instructor = course.Instructor,
                Schedule = course.Schedule,
                Room = course.Room
            };
            
            if (_showRegistrations)
            {
                if (reg.Status == "registered")
                {
                    item.Status = "registered";
                    item.StatusDisplay = "ลงทะเบียนแล้ว";
                    item.StatusColor = Colors.Green;
                }
                else if (reg.Status == "completed")
                {
                    item.Status = "completed";
                    item.StatusDisplay = $"เรียนจบแล้ว (เกรด {reg.Grade})";
                    item.StatusColor = Colors.Yellow;
                    item.Grade = reg.Grade;
                }
            }
            else
            {
                item.Status = "withdrawn";
                item.StatusDisplay = "ถอนรายวิชา";
                item.StatusColor = Colors.Red;
                item.WithdrawalDate = reg.WithdrawDate ?? DateTime.MinValue;
                item.WithdrawalDateDisplay = reg.WithdrawDate?.ToString("dd/MM/yyyy HH:mm") ?? "-";
            }
            
            return item;
        }

        [RelayCommand]
        private async Task ShowRegistrationsAsync()
        {
            if (_showRegistrations) return;
            
            _showRegistrations = true;
            UpdateTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task ShowWithdrawalsAsync()
        {
            if (!_showRegistrations) return;
            
            _showRegistrations = false;
            UpdateTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task SelectTerm1Async()
        {
            System.Diagnostics.Debug.WriteLine("SelectTerm1Async called");
            
            if (_availableTerms.Count < 1)
            {
                System.Diagnostics.Debug.WriteLine("No terms available");
                return;
            }
            
            if (_selectedTermId == _availableTerms[0].Id)
            {
                System.Diagnostics.Debug.WriteLine("Term already selected");
                return;
            }
            
            _selectedTermId = _availableTerms[0].Id;
            CurrentTermDisplay = $"ภาคการศึกษา: {_availableTerms[0].Name}";
            System.Diagnostics.Debug.WriteLine($"Selected term: {_selectedTermId}");
            
            UpdateTermTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task SelectTerm2Async()
        {
            System.Diagnostics.Debug.WriteLine("SelectTerm2Async called");
            
            if (_availableTerms.Count < 2)
            {
                System.Diagnostics.Debug.WriteLine("Less than 2 terms available");
                return;
            }
            
            if (_selectedTermId == _availableTerms[1].Id)
            {
                System.Diagnostics.Debug.WriteLine("Term already selected");
                return;
            }
            
            _selectedTermId = _availableTerms[1].Id;
            CurrentTermDisplay = $"ภาคการศึกษา: {_availableTerms[1].Name}";
            System.Diagnostics.Debug.WriteLine($"Selected term: {_selectedTermId}");
            
            UpdateTermTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task SelectTerm3Async()
        {
            System.Diagnostics.Debug.WriteLine("SelectTerm3Async called");
            
            if (_availableTerms.Count < 3)
            {
                System.Diagnostics.Debug.WriteLine("Less than 3 terms available");
                return;
            }
            
            if (_selectedTermId == _availableTerms[2].Id)
            {
                System.Diagnostics.Debug.WriteLine("Term already selected");
                return;
            }
            
            _selectedTermId = _availableTerms[2].Id;
            CurrentTermDisplay = $"ภาคการศึกษา: {_availableTerms[2].Name}";
            System.Diagnostics.Debug.WriteLine($"Selected term: {_selectedTermId}");
            
            UpdateTermTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void UpdateTabColors()
        {
            if (_showRegistrations)
            {
                RegistrationTabColor = Colors.Blue;
                RegistrationTabTextColor = Colors.White;
                WithdrawalTabColor = Colors.LightGray;
                WithdrawalTabTextColor = Colors.Black;
            }
            else
            {
                RegistrationTabColor = Colors.LightGray;
                RegistrationTabTextColor = Colors.Black;
                WithdrawalTabColor = Colors.Red;
                WithdrawalTabTextColor = Colors.White;
            }
            
            UpdateTermTabColors();
        }
        
        private void UpdateTermTabColors()
        {
            Term1TabColor = Colors.LightGray;
            Term1TabTextColor = Colors.Black;
            Term2TabColor = Colors.LightGray;
            Term2TabTextColor = Colors.Black;
            Term3TabColor = Colors.LightGray;
            Term3TabTextColor = Colors.Black;
            
            if (_availableTerms.Count >= 1 && _selectedTermId == _availableTerms[0].Id)
            {
                Term1TabColor = Colors.Green;
                Term1TabTextColor = Colors.White;
            }
            else if (_availableTerms.Count >= 2 && _selectedTermId == _availableTerms[1].Id)
            {
                Term2TabColor = Colors.Green;
                Term2TabTextColor = Colors.White;
            }
            else if (_availableTerms.Count >= 3 && _selectedTermId == _availableTerms[2].Id)
            {
                Term3TabColor = Colors.Green;
                Term3TabTextColor = Colors.White;
            }
        }
    }

    public class RegistrationHistoryItem
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Credits { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public Color StatusColor { get; set; } = Colors.Black;
        public DateTime RegistrationDate { get; set; }
        public string RegistrationDateDisplay { get; set; } = string.Empty;
        public DateTime? WithdrawalDate { get; set; }
        public string WithdrawalDateDisplay { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Instructor { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
    
        public bool IsRegistration => Status == "registered" || Status == "completed";
        public bool IsWithdrawal => Status == "withdrawn";
    }
}