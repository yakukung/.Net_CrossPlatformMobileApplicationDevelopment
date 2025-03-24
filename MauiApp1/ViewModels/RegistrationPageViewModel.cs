using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.ViewModels
{
    public partial class RegistrationPageViewModel : ObservableObject
    {
        private readonly RegisterAndWithdrawCourseService _registerService;
        private List<Course> allCourses; // เก็บรายการวิชาทั้งหมดเพื่อใช้ในการกรอง

        [ObservableProperty]
        private ObservableCollection<Course> availableCourses;

        [ObservableProperty]
        private string searchText; // เก็บคำค้นหา

        public RegistrationPageViewModel(RegisterAndWithdrawCourseService registerService)
        {
            _registerService = registerService;
            AvailableCourses = new ObservableCollection<Course>();
            allCourses = new List<Course>();

            LoadAvailableCoursesAsync();
        }
       private async void LoadAvailableCoursesAsync()
        {
            try
            {
                var studentId = await _registerService.GetCurrentStudentIdAsync();
                if (string.IsNullOrEmpty(studentId))
                {
                    await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", "ไม่พบข้อมูลนักศึกษา", "ตกลง");
                    return;
                }

                var allCoursesList = await _registerService.GetAvailableCoursesAsync();
                var registeredCourses = await _registerService.GetRegisteredCoursesAsync(studentId);
                var withdrawnCoursesList = await _registerService.GetWithdrawnCoursesAsync(studentId);
                // กรองวิชาที่เปิดและยังไม่ได้ลงทะเบียน (ไม่รวมวิชาที่ลงทะเบียนแล้ว แต่รวมวิชาที่ถอนแล้ว)
                allCourses = allCoursesList
                    .Where(c => c.Status == "open" && !registeredCourses.Any(rc => rc.CourseId == c.CourseId))
                    .ToList();
            
                // อัพเดท AvailableCourses ด้วยรายการทั้งหมด
                UpdateAvailableCourses();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", $"เกิดข้อผิดพลาด: {ex.Message}", "ตกลง");
            }
        }
        

        // เมธอดสำหรับอัพเดท AvailableCourses ตามคำค้นหา
        private void UpdateAvailableCourses()
        {
            AvailableCourses.Clear();
            var filteredCourses = string.IsNullOrWhiteSpace(SearchText)
                ? allCourses // ถ้าไม่มีคำค้นหา แสดงทั้งหมด
                : allCourses.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList(); // กรองตามชื่อ

            foreach (var course in filteredCourses)
            {
                AvailableCourses.Add(course);
            }
        }

        [RelayCommand]
        private async Task RegisterAsync(string courseId)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert("ยืนยัน", $"คุณต้องการลงทะเบียนรายวิชา {courseId} ใช่หรือไม่?", "ใช่", "ไม่");
            if (!confirm) return;

            try
            {
                var studentId = await _registerService.GetCurrentStudentIdAsync();
                if (string.IsNullOrEmpty(studentId))
                {
                    await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", "ไม่พบข้อมูลนักศึกษา", "ตกลง");
                    return;
                }

                await _registerService.RegisterCourseAsync(studentId, courseId);
                await Application.Current.MainPage.DisplayAlert("สำเร็จ", $"ลงทะเบียนรายวิชา {courseId} เรียบร้อยแล้ว", "ตกลง");
                LoadAvailableCoursesAsync(); // รีเฟรชรายการ
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", $"ไม่สามารถลงทะเบียนได้: {ex.Message}", "ตกลง");
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadAvailableCoursesAsync();
        }

        [RelayCommand]
        private void Search()
        {
            UpdateAvailableCourses(); // อัพเดทรายการตามคำค้นหา
        }

        // อัพเดท SearchText และกรองทันทีเมื่อผู้ใช้พิมพ์
        partial void OnSearchTextChanged(string value)
        {
            UpdateAvailableCourses(); // เรียกเมื่อ SearchText เปลี่ยน
        }
    }
}