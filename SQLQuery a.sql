select*from Student;
select * from StudentCourse;


SELECT 
    s.Id AS StudentId,
    s.Name AS StudentName,
    c.Name AS CourseName,
    sec.Name AS SectionName,
    a.Day1,
    a.Day2,
    a.Day3,
    a.Day4,
    a.Day5,
    a.Day6,
    a.Day7,
    a.Day8,
    a.Day9,
    a.Day10,
    a.Day11,
    a.Day12,
    a.Day13,
    a.Day14,
    a.Day15,
    a.Day16,
    a.Day17,
    a.Day18,
    a.Day19,
    a.Day20,
    a.Day21,
    a.Day22,
    a.Day23,
    a.Day24,
    a.Day25,
    a.Day26,
    a.Day27,
    a.Day28,
    a.Day29,
    a.Day30
FROM 
    Attendance a
INNER JOIN 
    Student s ON a.SectionId = s.SectionId
INNER JOIN 
    Course c ON a.CourseId = c.Id
INNER JOIN 
    Section sec ON a.SectionId = sec.Id
ORDER BY 
   c.Name ;


