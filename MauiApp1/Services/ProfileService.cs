using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiApp1.Models;

namespace MauiApp1.Services;

public class ProfileService
{
    private const string DataFileName = "data.json";
    private readonly string WritableDataFilePath = Path.Combine(FileSystem.AppDataDirectory, DataFileName);
    
    public async Task<DataModel> GetDataAsync()
    {
        try
        {
            // ตรวจสอบว่ามีไฟล์ในโฟลเดอร์ที่เขียนได้หรือไม่
            if (!File.Exists(WritableDataFilePath))
            {
                System.Diagnostics.Debug.WriteLine("Copying data.json to writable directory.");
                using var stream = await FileSystem.OpenAppPackageFileAsync(DataFileName);
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();
                await File.WriteAllTextAsync(WritableDataFilePath, content);
            }

            // อ่านไฟล์จากโฟลเดอร์ที่เขียนได้
            var json = await File.ReadAllTextAsync(WritableDataFilePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<DataModel>(json, options);

            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("Deserialized data is null");
                return new DataModel();
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded {data.Students.Count} students and {data.Courses.Count} courses.");
            return data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading JSON: {ex.Message}");
            return new DataModel();
        }
    }

    public async Task<Student?> LoadCurrentStudentAsync()
    {
        try
        {
            // ดึง StudentId จาก Preferences
            string studentId = Preferences.Get("StudentId", string.Empty);
            if (string.IsNullOrEmpty(studentId))
            {
                System.Diagnostics.Debug.WriteLine("No StudentId found in preferences.");
                return null;
            }

            // โหลดข้อมูลจาก data.json
            var data = await GetDataAsync();

            // ค้นหานักศึกษาตาม StudentId
            var student = data.Students.FirstOrDefault(s => s.Id == studentId);
            if (student == null)
            {
                System.Diagnostics.Debug.WriteLine($"Student with ID {studentId} not found.");
            }

            return student;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading current student: {ex.Message}");
            return null;
        }
    }

    public async Task<(List<Course> Courses, string Term)> GetStudentCoursesAsync(string studentId)
    {
        try
        {
            var data = await GetDataAsync();
            string currentTerm = "";
            
            if (data.Registrations.TryGetValue(studentId, out var registrationData))
            {
                // ดึงเฉพาะรายวิชาที่ลงทะเบียนและยังไม่ได้ถอน
                var courses = new List<Course>();
                
                // เรียงลำดับตามวันที่ลงทะเบียนล่าสุดก่อน
                var sortedRegistrations = registrationData.Current
                    .Where(r => r.Status == "registered")
                    .OrderByDescending(r => r.RegistrationDate)
                    .ToList();
                
                foreach (var registration in sortedRegistrations)
                {
                    var course = data.Courses.FirstOrDefault(c => c.CourseId == registration.CourseId);
                    if (course != null)
                    {
                        courses.Add(course);
                        // ดึงข้อมูลเทอมจากรายวิชาแรก
                        if (string.IsNullOrEmpty(currentTerm) && !string.IsNullOrEmpty(course.Term))
                        {
                            currentTerm = course.Term;
                        }
                    }
                }
                
                return (courses, currentTerm);
            }
            
            return (new List<Course>(), "");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting student courses: {ex.Message}");
            return (new List<Course>(), "");
        }
    }
}