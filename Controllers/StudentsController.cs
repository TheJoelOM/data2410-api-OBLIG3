using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using data2410_api_v1.Models;

namespace data2410_api_v1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController(IConfiguration config) : ControllerBase
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    private static string GetGrade(int marks) => marks switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 60 => "C",
        _ => "D"
    };

    [HttpGet]
    public async Task<ActionResult<List<Student>>> GetAll()
    {
        var students = new List<Student>();
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            students.Add(new Student
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Course = reader.GetString(2),
                Marks = reader.GetInt32(3),
                Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return students;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return NotFound();

        return new Student
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Course = reader.GetString(2),
            Marks = reader.GetInt32(3),
            Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "INSERT INTO Students (Name, Course, Marks) OUTPUT INSERTED.Id VALUES (@Name, @Course, @Marks)", conn);
        cmd.Parameters.AddWithValue("@Name", student.Name);
        cmd.Parameters.AddWithValue("@Course", student.Course);
        cmd.Parameters.AddWithValue("@Marks", student.Marks);

        student.Id = (int)await cmd.ExecuteScalarAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Student updated)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(
            "UPDATE Students SET Name = @Name, Course = @Course, Marks = @Marks WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Name", updated.Name);
        cmd.Parameters.AddWithValue("@Course", updated.Course);
        cmd.Parameters.AddWithValue("@Marks", updated.Marks);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 0 ? NotFound() : NoContent();
    }

    [HttpPost("calculate-grades")]
    public async Task<ActionResult<List<Student>>> CalculateGrades()
    {
        // Write code to calculate and update grades
        var studentsWithGrade = new List<Student>();
        using var conn = new SqlConnection(_connectionString); 
        await conn.OpenAsync();//open asynchrounous connection to the sql server
        using var cmd = new SqlCommand("SELECT Id, Name, Course, Marks, Grade FROM Students", conn);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                studentsWithGrade.Add(new Student //We add json format of the students to studentsWithGrade
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Course = reader.GetString(2),
                    Marks = reader.GetInt32(3),
                    Grade = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

        }        
        foreach (var stu in studentsWithGrade)
        {
            stu.Grade = GetGrade(stu.Marks); //We get the Students grade foreach iteration and update the grade.
            using var cmd2 = new SqlCommand("UPDATE Students SET Grade = @Grade WHERE Id = @Id", conn); //We do the SqlCommand now for update 
            cmd2.Parameters.AddWithValue("@Id", stu.Id); //Specifies which Id that we iterate in studentsWithGrade
            cmd2.Parameters.AddWithValue("@Grade", stu.Grade); //Updates the student with x id's grade, we input stu.Grade because we just got its grade from the mark litte above here
             await cmd2.ExecuteNonQueryAsync(); //Makes sure we update before we update the next one  
        }
        return studentsWithGrade;
    }

    [HttpGet("report")]
    public async Task<IActionResult> Report()
    {
        // Write code for the report generation logic.
        var reports = new List<CourseReport>(); //This list will store final report objects
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        //below is a sql querry that will group students by course and calulate the total students average marks and the number of abcd grades
        var query = @"
            SELECT 
                Course,
                COUNT(*) AS TotalStudents,
                AVG(Marks) AS AverageMarks,
                SUM(CASE WHEN Grade = 'A' THEN 1 ELSE 0 END) AS ACount,
                SUM(CASE WHEN Grade = 'B' THEN 1 ELSE 0 END) AS BCount,
                SUM(CASE WHEN Grade = 'C' THEN 1 ELSE 0 END) AS CCount,
                SUM(CASE WHEN Grade = 'D' THEN 1 ELSE 0 END) AS DCount
            FROM Students
            GROUP BY Course;
        ";

        using var cmd = new SqlCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync(); //Execute the query and get a reader to iterate through the reults
        while (await reader.ReadAsync()) //Loop through each row returned by sql
        {
            reports.Add(new CourseReport // Converting each row into a coursereport object and adding it to the list
            {
                Course = reader.GetString(0),
                TotalStudents = reader.GetInt32(1),
                AverageMarks = Convert.ToDouble(reader.GetValue(2)),
                ACount = reader.GetInt32(3),
                BCount = reader.GetInt32(4),
                CCount = reader.GetInt32(5),
                DCount = reader.GetInt32(6)
            });
        }
        return Ok(reports); //Return the full report as JSON to the client
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand("DELETE FROM Students WHERE Id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows == 0 ? NotFound() : NoContent();
    }
}
