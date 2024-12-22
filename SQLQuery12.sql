-- Student Table
CREATE TABLE Student (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    SectionId INT NOT NULL,
    FOREIGN KEY (SectionId) REFERENCES Section(Id)
);

-- StudentCourse Table (Many-to-Many relationship between Students and Courses)
CREATE TABLE StudentCourse (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Student(Id),
    FOREIGN KEY (CourseId) REFERENCES Course(Id)
);

-- Attendance Table
CREATE TABLE Attendance (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    SectionId INT NOT NULL,
    Day1 Bit null,
Day2 bit null,
Day3 bit null,
Day4 bit null,
Day5 bit null,
Day6 Bit null,
Day7 bit null,
Day8 bit null,
Day9 bit null,
Day10 bit null,
Day11 Bit null,
Day12 bit null,
Day13 bit null,
Day14 bit null,
Day15 bit null,
Day16 Bit null,
Day17 bit null,
Day18 bit null,
Day19 bit null,
Day20 bit null,
Day21 Bit null,
Day22 bit null,
Day23 bit null,
Day24 bit null,
Day25 bit null,
Day26 Bit null,
Day27 bit null,
Day28 bit null,
Day29 bit null,
Day30 bit null,
    FOREIGN KEY (StudentId) REFERENCES Student(Id),
    FOREIGN KEY (CourseId) REFERENCES Course(Id),
    FOREIGN KEY (SectionId) REFERENCES Section(Id)
);

select * from Teacher;
select *from TeacherSection;

Drop table Attendance;

CREATE TABLE Attendance (
    Id INT PRIMARY KEY IDENTITY,
    TeacherId INT NOT NULL,
    CourseId INT NOT NULL,
    SectionId INT NOT NULL,
    TemporaryId NVARCHAR(12) NOT NULL UNIQUE,
    Day1 bit null,
    Day2 bit null,
Day3 bit null,
Day4 bit null,
Day5 bit null,
Day6 Bit null,
Day7 bit null,
Day8 bit null,
Day9 bit null,
Day10 bit null,
Day11 Bit null,
Day12 bit null,
Day13 bit null,
Day14 bit null,
Day15 bit null,
Day16 Bit null,
Day17 bit null,
Day18 bit null,
Day19 bit null,
Day20 bit null,
Day21 Bit null,
Day22 bit null,
Day23 bit null,
Day24 bit null,
Day25 bit null,
Day26 Bit null,
Day27 bit null,
Day28 bit null,
Day29 bit null,
Day30 bit null,
    FOREIGN KEY (TeacherId) REFERENCES Teacher(Id),
    FOREIGN KEY (CourseId) REFERENCES Course(Id),
    FOREIGN KEY (SectionId) REFERENCES Section(Id)
);

select * from Attendance;
