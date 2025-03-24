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
    public partial class WithdrawPageViewModel : ObservableObject
    {
        private readonly RegisterAndWithdrawCourseService _registerService;

        [ObservableProperty]
        private ObservableCollection<Course> registeredCourses;

        public WithdrawPageViewModel(RegisterAndWithdrawCourseService registerService)
        {
            _registerService = registerService;
            RegisteredCourses = new ObservableCollection<Course>();

            LoadRegisteredCoursesAsync();
        }

        private async void LoadRegisteredCoursesAsync()
        {
            try
            {
                var studentId = await _registerService.GetCurrentStudentIdAsync();
                if (string.IsNullOrEmpty(studentId))
                {
                    await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", "ไม่พบข้อมูลนักศึกษา", "ตกลง");
                    return;
                }

                // ดึงเฉพาะรายวิชาที่ลงทะเบียนและยังไม่ได้ถอน
                var courses = await _registerService.GetRegisteredCoursesAsync(studentId);
                
                RegisteredCourses.Clear();
                foreach (var course in courses)
                {
                    RegisteredCourses.Add(course);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"เกิดข้อผิดพลาด: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", $"เกิดข้อผิดพลาด: {ex.Message}", "ตกลง");
            }
        }

        [RelayCommand]
        private async Task WithdrawAsync(string courseId)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert("ยืนยัน", $"คุณต้องการถอนรายวิชา {courseId} ใช่หรือไม่?", "ใช่", "ไม่");
            if (!confirm) return;
        
            try
            {
                var studentId = await _registerService.GetCurrentStudentIdAsync();
                if (string.IsNullOrEmpty(studentId))
                {
                    await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", "ไม่พบข้อมูลนักศึกษา", "ตกลง");
                    return;
                }
        
                await _registerService.WithdrawCourseAsync(studentId, courseId);
                await Application.Current.MainPage.DisplayAlert("สำเร็จ", $"ถอนรายวิชา {courseId} เรียบร้อยแล้ว", "ตกลง");
                
                // ลบรายวิชาที่ถอนออกจาก UI ทันที
                var courseToRemove = RegisteredCourses.FirstOrDefault(c => c.CourseId == courseId);
                if (courseToRemove != null)
                {
                    RegisteredCourses.Remove(courseToRemove);
                }
                
                // รีเฟรชรายการเพื่อความมั่นใจ
                await Task.Delay(100); // รอให้การบันทึกข้อมูลเสร็จสิ้น
                LoadRegisteredCoursesAsync();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("ข้อผิดพลาด", $"ไม่สามารถถอนรายวิชาได้: {ex.Message}", "ตกลง");
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            LoadRegisteredCoursesAsync();
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("//HomePage");
        }
    }
}