select * from Teacher;
Select * from TeacherSection;

CREATE TABLE Course (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Section (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE Teacher (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    CourseId INT NOT NULL FOREIGN KEY REFERENCES Course(Id)
);

CREATE TABLE TeacherSection (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TeacherId INT FOREIGN KEY REFERENCES Teacher(Id),
    SectionId INT FOREIGN KEY REFERENCES Section(Id)
);

INSERT INTO Course (Name) VALUES ('Math'), ('Science'), ('English');
INSERT INTO Section (Name) VALUES ('Section A'), ('Section B'), ('Section C');


-- Student Table
CREATE TABLE Students (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    SectionId INT NOT NULL,
    FOREIGN KEY (SectionId) REFERENCES Sections(Id)
);

-- StudentCourse Table (Many-to-Many relationship between Students and Courses)
CREATE TABLE StudentCourse (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

-- Attendance Table
CREATE TABLE Attendance (
    Id INT PRIMARY KEY IDENTITY(1,1),
    StudentId INT NOT NULL,
    CourseId INT NOT NULL,
    SectionId INT NOT NULL,
    Day1 BIT DEFAULT NULL,
    Day2 BIT DEFAULT NULL,
    Day3 BIT DEFAULT NULL,
    -- Add more days as needed
    Day30 BIT DEFAULT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    FOREIGN KEY (SectionId) REFERENCES Sections(Id)
);
