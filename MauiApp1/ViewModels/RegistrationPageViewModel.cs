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

        [ObservableProperty]
        private ObservableCollection<Course> availableCourses;


        public RegistrationPageViewModel(RegisterAndWithdrawCourseService registerService)
        {
            _registerService = registerService;
            AvailableCourses = new ObservableCollection<Course>();

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

                var allCourses = await _registerService.GetAvailableCoursesAsync();
                var registeredCourses = await _registerService.GetStudentCoursesAsync(studentId);
                var courses = allCourses
                    .Where(c => c.Status == "open" && !registeredCourses.Any(rc => rc.CourseId == c.CourseId))
                    .ToList();

                AvailableCourses.Clear();
                foreach (var course in courses)
                {
                    AvailableCourses.Add(course);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", $"เกิดข้อผิดพลาด: {ex.Message}", "ตกลง");
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
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("//HomePage");
        }

      
    }
}