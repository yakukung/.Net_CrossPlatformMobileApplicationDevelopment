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

                // ตรวจสอบการซ้อนทับของเวลาเรียนกับวิชาที่ลงทะเบียนแล้ว
                if (data.Registrations.TryGetValue(studentId, out var existingRegistration))
                {
                    var registeredCourses = existingRegistration.Current
                        .Where(r => r.Status == "registered") // เฉพาะวิชาที่ลงทะเบียนและยังไม่ถอน
                        .Select(r => data.Courses.FirstOrDefault(c => c.CourseId == r.CourseId))
                        .Where(c => c != null)
                        .ToList();
                
                    foreach (var registeredCourse in registeredCourses)
                    {
                        if (HasTimeConflict(course.Schedule, registeredCourse.Schedule))
                        {
                            throw new Exception($"เวลาเรียนซ้อนทับกับรายวิชา {registeredCourse.CourseId}: {registeredCourse.Name} ({registeredCourse.Schedule})");
                        }
                    }
                }
        
                // ถ้าไม่มีการซ้อนทับของเวลาเรียน ดำเนินการลงทะเบียน
                if (!data.Registrations.TryGetValue(studentId, out var registration))
                {
                    registration = new RegistrationData();
                    data.Registrations[studentId] = registration;
                }
        
                registration.Current.Add(new Registration
                {
                    CourseId = courseId,
                    Term = course.Term,
                    Status = "registered",
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
                throw; // ส่งต่อข้อผิดพลาดเพื่อให้ UI จัดการได้
            }
        }
        
        // เมธอดสำหรับตรวจสอบการซ้อนทับของเวลาเรียน
        private bool HasTimeConflict(string schedule1, string schedule2)
        {
            try
            {
                // ตรวจสอบกรณีที่ข้อมูลว่างเปล่า
                if (string.IsNullOrWhiteSpace(schedule1) || string.IsNullOrWhiteSpace(schedule2))
                {
                    System.Diagnostics.Debug.WriteLine("Schedule data is empty or null");
                    return false;
                }
        
                // รองรับกรณีที่มีหลายวันหรือหลายช่วงเวลา (คั่นด้วย comma หรือ semicolon)
                var schedules1 = schedule1.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                var schedules2 = schedule2.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        
                // ตรวจสอบทุกคู่ของตารางเวลา
                foreach (var sched1 in schedules1)
                {
                    foreach (var sched2 in schedules2)
                    {
                        if (CheckSingleScheduleConflict(sched1.Trim(), sched2.Trim()))
                        {
                            return true; // พบการซ้อนทับ
                        }
                    }
                }
        
                return false; // ไม่พบการซ้อนทับ
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking time conflict: {ex.Message}");
                return false; // หากมีข้อผิดพลาดในการแยกข้อมูล ให้ถือว่าไม่มีการซ้อนทับ
            }
        }
        
        // ตรวจสอบการซ้อนทับของตารางเวลาเดี่ยว
        private bool CheckSingleScheduleConflict(string sched1, string sched2)
        {
            // แยกข้อมูลตารางเรียนออกเป็นวันและเวลา
            // รูปแบบที่คาดหวัง: "Monday 9:00-12:00"
            var parts1 = sched1.Split(new[] { ' ' }, 2);
            var parts2 = sched2.Split(new[] { ' ' }, 2);
        
            if (parts1.Length < 2 || parts2.Length < 2)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid schedule format: '{sched1}' or '{sched2}'");
                return false;
            }
        
            // ตรวจสอบว่าเป็นวันเดียวกันหรือไม่
            string day1 = parts1[0].ToLower().Trim();
            string day2 = parts2[0].ToLower().Trim();
        
            // ถ้าเป็นคนละวัน จะไม่มีการซ้อนทับ
            if (day1 != day2)
                return false;
        
            // แยกเวลาเริ่มต้นและสิ้นสุด
            var timeRange1 = parts1[1].Trim();
            var timeRange2 = parts2[1].Trim();
        
            var time1 = timeRange1.Split('-');
            var time2 = timeRange2.Split('-');
        
            if (time1.Length != 2 || time2.Length != 2)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid time format: '{timeRange1}' or '{timeRange2}'");
                return false;
            }
        
            // แปลงเวลาเป็นนาที (นับจากเที่ยงคืน)
            int start1 = ConvertTimeToMinutes(time1[0].Trim());
            int end1 = ConvertTimeToMinutes(time1[1].Trim());
            int start2 = ConvertTimeToMinutes(time2[0].Trim());
            int end2 = ConvertTimeToMinutes(time2[1].Trim());
        
            // ตรวจสอบความถูกต้องของเวลา
            if (start1 >= end1 || start2 >= end2 || start1 < 0 || start2 < 0 || end1 <= 0 || end2 <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid time values: {start1}-{end1} or {start2}-{end2}");
                return false;
            }
        
            // ตรวจสอบการซ้อนทับ
            bool hasConflict = (start1 < end2) && (start2 < end1);
            
            if (hasConflict)
            {
                System.Diagnostics.Debug.WriteLine($"Time conflict detected: {day1} {start1/60}:{start1%60:D2}-{end1/60}:{end1%60:D2} conflicts with {day2} {start2/60}:{start2%60:D2}-{end2/60}:{end2%60:D2}");
            }
            
            return hasConflict;
        }
        
        // แปลงเวลาในรูปแบบ "HH:MM" เป็นนาที (นับจากเที่ยงคืน)
        private int ConvertTimeToMinutes(string time)
        {
            try
            {
                // รองรับรูปแบบเวลาหลายแบบ
                time = time.Trim().ToLower().Replace("am", "").Replace("pm", "").Trim();
                
                var parts = time.Split(':');
                if (parts.Length < 1)
                    return 0;
        
                int hours = 0;
                int minutes = 0;
        
                // กรณีมีแค่ชั่วโมง
                if (parts.Length == 1)
                {
                    if (int.TryParse(parts[0], out hours))
                        return hours * 60;
                    return 0;
                }
        
                // กรณีมีทั้งชั่วโมงและนาที
                if (int.TryParse(parts[0], out hours) && int.TryParse(parts[1], out minutes))
                {
                    // ตรวจสอบความถูกต้องของเวลา
                    if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59)
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid time values: {hours}:{minutes}");
                        return 0;
                    }
                    
                    return hours * 60 + minutes;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error converting time to minutes: {ex.Message}");
                return 0;
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
        
                var dataService = new DataService();
        
                // โหลดข้อมูลใหม่ใน UI
                await dataService.LoadStudentDataAsync();
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
            try
            {
                // ดึง StudentId จาก Preferences
                string studentId = Preferences.Get("StudentId", string.Empty);
                if (string.IsNullOrEmpty(studentId))
                {
                    System.Diagnostics.Debug.WriteLine("No StudentId found in preferences.");
                    return string.Empty;
                }

                // ตรวจสอบว่า StudentId มีอยู่ใน data.json หรือไม่
                var data = await GetDataAsync();
                var currentStudent = data.Students.FirstOrDefault(s => s.Id == studentId);
                if (currentStudent == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Student with ID {studentId} not found in data.");
                    return string.Empty;
                }

                return currentStudent.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving current student ID: {ex.Message}");
                return string.Empty;
            }
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

        // ดึงเฉพาะรายวิชาที่ลงทะเบียนและยังไม่ได้ถอน
public async Task<List<Course>> GetRegisteredCoursesAsync(string studentId)
{
    var data = await GetDataAsync();
    if (data.Registrations.TryGetValue(studentId, out var registration))
    {
        return registration.Current
            .Where(r => r.Status == "registered") // เฉพาะวิชาที่ยังไม่ได้ถอน
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
