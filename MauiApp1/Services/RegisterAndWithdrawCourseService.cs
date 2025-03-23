using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Services
{
    public class RegisterAndWithdrawCourseService
    {
       private const string DataFileName = "data.json";
       private readonly string WritableDataFilePath = Path.Combine(FileSystem.AppDataDirectory, DataFileName);
       private DataModel _data; 

        // อ่านข้อมูลจากไฟล์ JSON
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

        // ดึงข้อมูลนักศึกษาแบบเต็ม
        public async Task<object?> GetStudentFullDataAsync(string studentId)
        {
            if (string.IsNullOrEmpty(studentId))
                throw new ArgumentException("Student ID cannot be null or empty", nameof(studentId));

            try
            {
                var data = await GetDataAsync();
                var student = data.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ไม่พบข้อมูลสำหรับนักศึกษา ID: {studentId}");
                    return null;
                }

                data.Registrations.TryGetValue(studentId, out var registration);

                var fullData = new
                {
                    Student = student,
                    CurrentRegistrations = registration?.Current
                        .Select(r => MapCourse(data.Courses, r))
                        .Where(c => c != null)
                        .ToList(),
                    PreviousRegistrations = registration?.Previous
                        .Select(r => MapCourse(data.Courses, r))
                        .Where(c => c != null)
                        .ToList()
                };

                return fullData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"เกิดข้อผิดพลาดในการดึงข้อมูลนักศึกษา: {ex.Message}");
                return null;
            }
        }

        // ดึงรายวิชาที่เปิดให้ลงทะเบียน
        public async Task<List<Course>> GetAvailableCoursesAsync()
        {
            try
            {
                var data = await GetDataAsync();
                return data.Courses
                    .Where(c => c.Status == "open")
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"เกิดข้อผิดพลาดในการดึงรายวิชาที่เปิดสอน: {ex.Message}");
                return new List<Course>();
            }
        }

        // ลงทะเบียนรายวิชา
        public async Task RegisterCourseAsync(string studentId, string courseId)
        {
            try
            {
                var data = await GetDataAsync();
                var course = data.Courses.FirstOrDefault(c => c.CourseId == courseId);
                if (course == null)
                {
                    throw new Exception("Course not found.");
                }

                if (!data.Registrations.TryGetValue(studentId, out var registration))
                {
                    registration = new RegistrationData();
                    data.Registrations[studentId] = registration;
                }

                registration.Current.Add(new Registration
                {
                    CourseId = courseId,
                    Term = course.Term,
                    Status = course.Status,
                    RegistrationDate = DateTime.UtcNow
                });

                await SaveDataAsync(data, courseId);

                // รีเฟรชข้อมูลใน DataService
                var dataService = new DataService();

                // โหลดข้อมูลใหม่ใน UI
                await dataService.LoadStudentDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering course: {ex.Message}");
            }
        }

        // ถอนรายวิชา
        public async Task WithdrawCourseAsync(string studentId, string courseId)
        {
            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(courseId))
                throw new ArgumentException("Student ID และ Course ID ต้องไม่เป็นค่าว่าง");

            try
            {
                var data = await GetDataAsync();
                if (data.Registrations.TryGetValue(studentId, out var registration))
                {
                    var reg = registration.Current.FirstOrDefault(r => r.CourseId == courseId && r.Status == "registered");
                    if (reg == null)
                        throw new Exception("ไม่พบรายวิชานี้ในรายการลงทะเบียนปัจจุบัน");

                    reg.Status = "withdrawn";
                    reg.WithdrawDate = DateTime.UtcNow;

                    var course = data.Courses.FirstOrDefault(c => c.CourseId == courseId);
                    if (course != null) course.CurrentStudents--;

                    await SaveDataAsync(data, courseId);
                }
                else
                {
                    throw new Exception("ไม่พบข้อมูลการลงทะเบียนของนักศึกษา");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"เกิดข้อผิดพลาดในการถอนรายวิชา: {ex.Message}");
                throw;
            }
        }

        // บันทึกข้อมูลกลับไปยังไฟล์ JSON
        private async Task SaveDataAsync(DataModel data, string courseId)
        {
            try
            {
                // ค้นหาชื่อวิชาจาก CourseId
                var course = data.Courses.FirstOrDefault(c => c.CourseId == courseId);
                var courseName = course != null ? course.Name : "Unknown Course";

                // แสดงชื่อวิชาที่ลงทะเบียน
                System.Diagnostics.Debug.WriteLine($"Registering course: {courseName}");

                // เขียนข้อมูลลงไฟล์ JSON
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(data, options);
                await File.WriteAllTextAsync(WritableDataFilePath, json);
                System.Diagnostics.Debug.WriteLine($"Data saved to {WritableDataFilePath}");

                // แจ้งเตือนว่าลงทะเบียนสำเร็จ
                System.Diagnostics.Debug.WriteLine($"Successfully registered course: {courseName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving JSON: {ex.Message}");
                throw;
            }
        }

        // ดึง ID นักศึกษาปัจจุบัน
        public async Task<string> GetCurrentStudentIdAsync()
        {
            var data = await GetDataAsync();
            var currentStudent = data.Students.FirstOrDefault(); // สมมติว่าดึงนักศึกษาคนแรก
            return currentStudent?.Id ?? string.Empty;
        }

        // ดึงรายวิชาที่นักศึกษาลงทะเบียนแล้ว
        public async Task<List<Course>> GetStudentCoursesAsync(string studentId)
        {
            var data = await GetDataAsync();
            if (data.Registrations.TryGetValue(studentId, out var registration))
            {
                return registration.Current
                    .Select(r => MapCourse(data.Courses, r))
                    .Where(c => c != null)
                    .ToList()!;
            }

            return new List<Course>();
        }

        // Helper method สำหรับ MapCourse
        private Course? MapCourse(List<Course> courses, Registration reg)
        {
            return courses.FirstOrDefault(c => c.CourseId == reg.CourseId);
        }
    }
}