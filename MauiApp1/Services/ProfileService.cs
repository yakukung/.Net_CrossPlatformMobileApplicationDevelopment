using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MauiApp1.Models;

namespace MauiApp1.Services
{
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
                // System.Diagnostics.Debug.WriteLine($"JSON content: {json}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<DataModel>(json, options);

                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("Deserialized data is null");
                    return new DataModel();
                }

                // ถ้า Terms ว่างเปล่า ดึงข้อมูล term จาก registrations
                if (data.Terms == null || !data.Terms.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Terms is empty in JSON. Generating terms from registrations.");

                    // ดึง term ทั้งหมดจาก registrations
                    var allTerms = new HashSet<string>();
                    foreach (var registrationData in data.Registrations.Values)
                    {
                        foreach (var reg in registrationData.Current)
                        {
                            if (!string.IsNullOrEmpty(reg.Term))
                            {
                                allTerms.Add(reg.Term);
                            }
                        }
                        foreach (var reg in registrationData.Previous)
                        {
                            if (!string.IsNullOrEmpty(reg.Term))
                            {
                                allTerms.Add(reg.Term);
                            }
                        }
                    }

                    // หาเทอมล่าสุด (เช่น "3/2567" จะเป็นเทอมล่าสุด)
                    var latestTerm = allTerms.OrderByDescending(t => t).FirstOrDefault();

                    // สร้าง List<Term> จาก term ที่พบ
                    data.Terms = allTerms.Select(termId => new Term
                    {
                        Id = termId,
                        Name = $"เทอม {termId}", // สร้างชื่อจาก termId (เช่น "1/2567" -> "เทอม 1/2567")
                        IsCurrent = termId == latestTerm, // เทอมล่าสุดเป็นเทอมปัจจุบัน
                        StartDate = "", // ไม่มีข้อมูลใน JSON
                        EndDate = "", // ไม่มีข้อมูลใน JSON
                        RegistrationPeriod = new RegistrationPeriod
                        {
                            Start = "", // ไม่มีข้อมูลใน JSON
                            End = "" // ไม่มีข้อมูลใน JSON
                        },
                        AddDropPeriod = new RegistrationPeriod
                        {
                            Start = "", // ไม่มีข้อมูลใน JSON
                            End = "" // ไม่มีข้อมูลใน JSON
                        },
                        WithdrawDeadline = "" // ไม่มีข้อมูลใน JSON
                    }).OrderBy(t => t.Id).ToList();

                    System.Diagnostics.Debug.WriteLine($"Generated {data.Terms.Count} terms from registrations.");
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {data.Students.Count} students, {data.Courses.Count} courses, {data.Terms.Count} terms.");
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
}