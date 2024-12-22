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
