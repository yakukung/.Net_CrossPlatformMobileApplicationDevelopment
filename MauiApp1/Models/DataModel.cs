using System;
using System.Collections.Generic;

namespace MauiApp1.Models
{
    // DataModel Class
    public class DataModel
    {
        public List<Student> Students { get; set; } = new List<Student>();
        public List<Course> Courses { get; set; } = new List<Course>();
        public Dictionary<string, RegistrationData> Registrations { get; set; } = new Dictionary<string, RegistrationData>();
        public List<Term> Terms { get; set; } = new List<Term>();
    }

    // Term Model
    public class Term
    {
        public string Id { get; set; } = string.Empty; // Term ID (e.g., "1/2567")
        public string Name { get; set; } = string.Empty; // Term Name (e.g., "ภาคต้น ปีการศึกษา 2567")
        public bool IsCurrent { get; set; } // Is Current Term
        public DateTime StartDate { get; set; } // Term Start Date
        public DateTime EndDate { get; set; } // Term End Date
        public RegistrationPeriod RegistrationPeriod { get; set; } = new RegistrationPeriod(); // Registration Period
        public RegistrationPeriod AddDropPeriod { get; set; } = new RegistrationPeriod(); // Add/Drop Period
        public DateTime WithdrawDeadline { get; set; } // Withdraw Deadline
    }

    // Registration Period Model
    public class RegistrationPeriod
    {
        public DateTime Start { get; set; } // Period Start Date
        public DateTime End { get; set; } // Period End Date
    }

    // RegistrationData Class
    public class RegistrationData
    {
        public List<Registration> Current { get; set; } = new List<Registration>();
        public List<Registration> Previous { get; set; } = new List<Registration>();
    }

    // Student Model
    public class Student
    {
        public string Id { get; set; } = string.Empty; // Student ID
        public string FirstName { get; set; } = string.Empty; // First Name
        public string LastName { get; set; } = string.Empty; // Last Name
        public string Email { get; set; } = string.Empty; // Email Address
        public string Password { get; set; } = string.Empty; // Password
        public string Faculty { get; set; } = string.Empty; // Faculty Name
        public string Major { get; set; } = string.Empty; // Major Name
        public int Year { get; set; } // Year of Study
        public double Gpa { get; set; } // Grade Point Average
        public string ProfileImage { get; set; } = "default_profile.png"; // Profile Image Path
    }

    // Course Model
    public class Course
    {
        public string CourseId { get; set; } = string.Empty; // Course ID
        public string Name { get; set; } = string.Empty; // Course Name
        public int Credit { get; set; } // Credit Hours
        public int Section { get; set; } // Section Number
        public string Instructor { get; set; } = string.Empty; // Instructor Name
        public string Schedule { get; set; } = string.Empty; // Schedule (e.g., "Monday 9:00-12:00")
        public string Room { get; set; } = string.Empty; // Room Location
        public int MaxStudents { get; set; } // Maximum Number of Students
        public int CurrentStudents { get; set; } // Current Number of Students
        public string Faculty { get; set; } = string.Empty; // Faculty Offering the Course
        public string Term { get; set; } = string.Empty; // Term (e.g., "1/2566")
        public List<string> Prerequisites { get; set; } = new(); // Prerequisite Course IDs
        public string Description { get; set; } = string.Empty; // Course Description
        public string Status { get; set; } = "open"; // Course Status (e.g., "open", "closed")
        public string? Grade { get; set; } // Grade (if completed)
        public DateTime? RegistrationDate { get; set; } // Registration Date
        public DateTime? WithdrawDate { get; set; } // Withdraw Date
    }

    // Registration Model
    public class Registration
    {
        public string CourseId { get; set; } = string.Empty; // Course ID
        public string Term { get; set; } = string.Empty; // Term (e.g., "1/2566")
        public string Status { get; set; } = string.Empty; // Registration Status (e.g., "registered", "withdrawn")
        public DateTime RegistrationDate { get; set; } // Registration Date
        public DateTime? WithdrawDate { get; set; } // Withdraw Date (if applicable)
        public string? Grade { get; set; } // Grade (if completed)
    }
}