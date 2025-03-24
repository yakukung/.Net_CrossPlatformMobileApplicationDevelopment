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
        private Color _currentTabColor;

        [ObservableProperty]
        private Color _currentTabTextColor;

        [ObservableProperty]
        private Color _previousTabColor;

        [ObservableProperty]
        private Color _previousTabTextColor;

        public HistoryViewModel(ProfileService profileService)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            
            // ตั้งค่าสีเริ่มต้นของแท็บ
            UpdateTabColors();
            
            // โหลดข้อมูลเมื่อเริ่มต้น
            Task.Run(async () => await LoadDataAsync());
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                // โหลดข้อมูลนักศึกษา
                _student = await _profileService.LoadCurrentStudentAsync();
                if (_student == null)
                {
                    return;
                }

                // อัพเดทข้อมูลนักศึกษา
                StudentInfo = $"{_student.Id} - {_student.FirstName} {_student.LastName}";

                // โหลดข้อมูลเทอมปัจจุบัน
                var data = await _profileService.GetDataAsync();
                var currentTerm = data.Terms.FirstOrDefault(t => t.IsCurrent);
                if (currentTerm != null)
                {
                    CurrentTermDisplay = $"ภาคการศึกษา: {currentTerm.Name}";
                }
                else
                {
                    CurrentTermDisplay = "ภาคการศึกษาปัจจุบัน";
                }

                // โหลดข้อมูลประวัติการลงทะเบียน
                await LoadHistoryItemsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
        }

        private async Task LoadHistoryItemsAsync()
        {
            if (_student == null) return;

            HistoryItems.Clear();
            
            var data = await _profileService.GetDataAsync();
            if (data.Registrations.TryGetValue(_student.Id, out var registrationData))
            {
                var items = new List<RegistrationHistoryItem>();
                
                if (_showCurrent)
                {
                    // แสดงรายการในเทอมปัจจุบัน
                    var registrations = _showRegistrations 
                        ? registrationData.Current.Where(r => r.Status == "registered")
                        : registrationData.Current.Where(r => r.Status == "withdrawn");
                    
                    foreach (var reg in registrations)
                    {
                        var course = data.Courses.FirstOrDefault(c => c.CourseId == reg.CourseId);
                        if (course != null)
                        {
                            items.Add(CreateHistoryItem(course, reg));
                        }
                    }
                }
                else
                {
                    // แสดงรายการในเทอมก่อนหน้า
                    var registrations = _showRegistrations 
                        ? registrationData.Previous.Where(r => r.Status == "completed")
                        : registrationData.Previous.Where(r => r.Status == "withdrawn");
                    
                    foreach (var reg in registrations)
                    {
                        var course = data.Courses.FirstOrDefault(c => c.CourseId == reg.CourseId);
                        if (course != null)
                        {
                            items.Add(CreateHistoryItem(course, reg));
                        }
                    }
                }
                
                // เรียงลำดับตามวันที่ล่าสุดก่อน
                if (_showRegistrations)
                {
                    items = items.OrderByDescending(i => i.RegistrationDate).ToList();
                }
                else
                {
                    items = items.OrderByDescending(i => i.WithdrawalDate).ToList();
                }
                
                foreach (var item in items)
                {
                    HistoryItems.Add(item);
                }
            }
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
                    item.StatusColor = Colors.Blue;
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
        private async Task ShowCurrentAsync()
        {
            if (_showCurrent) return;
            
            _showCurrent = true;
            UpdateTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task ShowPreviousAsync()
        {
            if (!_showCurrent) return;
            
            _showCurrent = false;
            UpdateTabColors();
            await LoadHistoryItemsAsync();
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        private void UpdateTabColors()
        {
            // สีแท็บลงทะเบียน/ถอนรายวิชา
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
            
            // สีแท็บเทอมปัจจุบัน/เทอมก่อนหน้า
            if (_showCurrent)
            {
                CurrentTabColor = Colors.Green;
                CurrentTabTextColor = Colors.White;
                PreviousTabColor = Colors.LightGray;
                PreviousTabTextColor = Colors.Black;
            }
            else
            {
                CurrentTabColor = Colors.LightGray;
                CurrentTabTextColor = Colors.Black;
                PreviousTabColor = Colors.DarkOrange;
                PreviousTabTextColor = Colors.White;
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