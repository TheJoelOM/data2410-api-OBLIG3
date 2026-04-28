using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

namespace data2410_api_v1.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Course { get; set; }
    public int Marks { get; set; }
    public string? Grade { get; set; }
}

public class CourseReport
{
    public string Course {get; set;} = string.Empty;
    public int TotalStudents {get; set;}
    public Double AverageMarks {get; set;}
    public int A { get; set;}
    public int B {get; set;}
    public int C {get; set;}

    public int D {get; set;}
}
