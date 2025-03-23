using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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

    public async Task<List<Course>> GetStudentCoursesAsync(string studentId)
    {
        try
        {
            var data = await GetDataAsync();
            
            if (data.Registrations.TryGetValue(studentId, out var registrationData))
            {
                // ดึงรายวิชาที่ลงทะเบียนปัจจุบัน
                var courses = new List<Course>();
                foreach (var registration in registrationData.Current)
                {
                    var course = data.Courses.FirstOrDefault(c => c.CourseId == registration.CourseId);
                    if (course != null)
                    {
                        courses.Add(course);
                    }
                }
                
                return courses;
            }
            
            return new List<Course>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting student courses: {ex.Message}");
            return new List<Course>();
        }
    }
}