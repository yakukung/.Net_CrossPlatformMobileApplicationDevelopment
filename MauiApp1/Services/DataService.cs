using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MauiApp1.Models;
using Microsoft.Maui.Storage;

namespace MauiApp1.Services
{
    public class DataService
    {
        private const string DataFilePath = "data.json";
        private readonly string WritableDataFilePath = Path.Combine(FileSystem.AppDataDirectory, DataFilePath);
        private DataModel? _data;
        public Student? CurrentStudent { get; private set; }
        public List<Course> CurrentCourses { get; private set; } = new List<Course>();

        private class DataModel
        {
            public List<Student> Students { get; set; } = new();
            public List<Course> Courses { get; set; } = new();
            public Dictionary<string, RegistrationData> Registrations { get; set; } = new();
        }

        private class RegistrationData
        {
            public List<Registration> Current { get; set; } = new();
            public List<Registration> Previous { get; set; } = new();
        }

        public async Task<Student?> AuthenticateAsync(string email, string password)
        {
            var students = await LoadStudentsAsync();
            return students.FirstOrDefault(s => s.Email == email && s.Password == password);
        }

        public async Task<Student?> AuthenticateByIdAsync(string id, string password)
        {
            var students = await LoadStudentsAsync();
            return students.FirstOrDefault(s => s.Id == id && s.Password == password);
        }

        public async Task<Student?> LoadCurrentStudentAsync()
        {
            if (!Preferences.Get("IsLoggedIn", false))
            {
                System.Diagnostics.Debug.WriteLine("User is not logged in.");
                return null;
            }

            string studentId = Preferences.Get("StudentId", "");
            if (string.IsNullOrEmpty(studentId))
            {
                System.Diagnostics.Debug.WriteLine("No student ID found in preferences.");
                return null;
            }

            var data = await GetDataAsync();
            var student = data.Students.FirstOrDefault(s => s.Id == studentId);

            if (student == null)
            {
                System.Diagnostics.Debug.WriteLine($"Student with ID {studentId} not found.");
                return null;
            }

            // ตรวจสอบว่าข้อมูลนักศึกษาครบถ้วนหรือไม่
            // System.Diagnostics.Debug.WriteLine($"Loaded student: {student.FirstName} {student.LastName}, Faculty: {student.Faculty}, Major: {student.Major}");
            return student;
        }

        public void Logout()
        {
            Preferences.Clear();
            CurrentStudent = null;
            _data = null;
        }

        public async Task<List<Course>> GetStudentCoursesAsync(string studentId)
        {
            var data = await GetDataAsync();
            return data.Registrations.TryGetValue(studentId, out var registration)
                ? registration.Current
                    .Where(r => r.Status == "registered")
                    .Select(r => data.Courses.FirstOrDefault(c => c.CourseId == r.CourseId))
                    .Where(c => c != null)
                    .ToList()
                : new List<Course>();
        }

        public async Task<(List<Course> Current, List<Course> Previous)> GetStudentRegistrationsAsync(string studentId)
        {
            var data = await GetDataAsync();

            if (!data.Registrations.TryGetValue(studentId, out var registration))
                return (new List<Course>(), new List<Course>());

            var currentCourses = registration.Current
                .Select(r => MapCourse(data.Courses, r))
                .Where(c => c != null)
                .ToList();

            var previousCourses = registration.Previous
                .Select(r => MapCourse(data.Courses, r))
                .Where(c => c != null)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"ข้อมูลลงทะเบียนปัจจุบัน Student ID: {studentId}, Courses: {string.Join(", ", currentCourses.Select(c => c.Name))}");

            return (currentCourses, previousCourses);
        }

        private Course? MapCourse(List<Course> courses, Registration reg)
        {
            var course = courses.FirstOrDefault(c => c.CourseId == reg.CourseId);
            if (course != null)
            {
                course.Status = reg.Status;
                course.Grade = reg.Grade;
                course.RegistrationDate = reg.RegistrationDate;
                course.WithdrawDate = reg.WithdrawDate;
            }
            return course;
        }

        private void SetCurrentStudent(Student student) => CurrentStudent = student;

        private void SaveLoginState(Student student)
        {
            Preferences.Set("IsLoggedIn", true);
            Preferences.Set("StudentId", student.Id);
        }

        private async Task<DataModel> GetDataAsync()
        {
            if (_data != null)
                return _data;

            try
            {
                System.Diagnostics.Debug.WriteLine($"Checking writable file at: {WritableDataFilePath}");

                if (File.Exists(WritableDataFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Found existing file in writable directory");
                    var json = await File.ReadAllTextAsync(WritableDataFilePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _data = JsonSerializer.Deserialize<DataModel>(json, options);

                    if (_data != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded {_data.Students.Count} students and {_data.Courses.Count} courses from writable file.");
                        return _data;
                    }
                }

                System.Diagnostics.Debug.WriteLine("Writable file not found or failed to load. Falling back to app package.");
                using var packageStream = await FileSystem.OpenAppPackageFileAsync(DataFilePath);
                using var reader = new StreamReader(packageStream);
                var packageJson = await reader.ReadToEndAsync();
                var packageOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _data = JsonSerializer.Deserialize<DataModel>(packageJson, packageOptions);

                if (_data == null)
                {
                    System.Diagnostics.Debug.WriteLine("Deserialized data from app package is null");
                    return new DataModel();
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {_data.Students.Count} students and {_data.Courses.Count} courses from app package.");
                return _data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading JSON: {ex.Message}");
                return new DataModel();
            }
        }

        private async Task<List<Student>> LoadStudentsAsync()
        {
            var data = await GetDataAsync();
            return data.Students;
        }

        public async Task<object?> GetStudentFullDataAsync(string studentId)
        {
            try
            {
                var data = await GetDataAsync();

                // ค้นหานักศึกษาจาก studentId
                var student = data.Students.FirstOrDefault(s => s.Id == studentId);
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Student with ID {studentId} not found.");
                    return null;
                }

                // ค้นหาการลงทะเบียนของนักศึกษา
                data.Registrations.TryGetValue(studentId, out var registration);

                // เตรียมข้อมูลทั้งหมด
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

                System.Diagnostics.Debug.WriteLine($"Full data for student {studentId}: {JsonSerializer.Serialize(fullData)}");
                return fullData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving full student data: {ex.Message}");
                return null;
            }
        }
        public void RefreshData()
        {
            _data = null; // รีเซ็ตข้อมูลในหน่วยความจำ
            System.Diagnostics.Debug.WriteLine("DataService: Data has been reset. It will be reloaded on the next request.");
        }

        public async Task RefreshDataAsync()
        {
            if (_data == null)
            {
                System.Diagnostics.Debug.WriteLine("DataService: Data is null. Reloading data...");
                await GetDataAsync();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DataService: Data is already loaded. Skipping reload.");
            }
        }

        public async Task LoadStudentDataAsync()
        {
            try
            {
                var dataService = new DataService();
                var student = await dataService.LoadCurrentStudentAsync();
                if (student == null)
                {
                    System.Diagnostics.Debug.WriteLine("No student data found.");
                    return;
                }

                CurrentStudent = student;

                var (currentCourses, previousCourses) = await dataService.GetStudentRegistrationsAsync(student.Id);
                CurrentCourses.Clear();
                foreach (var course in currentCourses)
                {
                    CurrentCourses.Add(course);
                }

                System.Diagnostics.Debug.WriteLine($"Profile Loaded courses for student {student.Id}: {string.Join(", ", CurrentCourses.Select(c => c.Name))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading student data: {ex.Message}");
            }
        }
    }
}