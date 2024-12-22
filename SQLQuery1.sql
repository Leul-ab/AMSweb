
select * from Course;
select * from Section;
select * from Student;
select * from Teacher;

create Table Course(Id int primary key identity(1,1), Name nvarchar(100) not null);

create table Section (Id int primary key identity (1,1), Name nvarchar(100) not null);

create table Teacher (Id int primary key identity (1,1), Name nvarchar(100) not null, Password nvarchar(100) not null, CourseId int not null
,foreign key (CourseId) References Course(Id));

create table Student (Id int primary key identity(1,1), Name nvarchar(100) not null, Password nvarchar(100) not null, SectionId int not null
,foreign key (SectionId) References Section(Id));

create table TeacherSection (TeacherId int not null, SectionId int not null, 
primary key (TeacherId, SectionId), 
Foreign key (TeacherId) References Teacher(Id),
Foreign key (SectionId) references Section(Id));

create table StudentCourse (StudentId int not null, CourseId int not null,
primary key (StudentId, CourseId),
foreign key (StudentId) references Student(Id),
foreign key (CourseId) references Course(Id));

Create table Attendance(Id int primary key not null, CourseID int not null, SectionId int not null, StudentId int not null
,StudentName nvarchar (100) not null, CourseName nvarchar (100) not null,
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
foreign key (CourseId) References Course(Id),
foreign key (SectionId) References Section(Id),
foreign key (StudentId) references Student(Id)
);

-- Insert into Course table
INSERT INTO Course (Name) VALUES ('Mathematics'), ('Science'), ('History'), ('Art');

-- Insert into Section table
INSERT INTO Section (Name) VALUES ('A'), ('B'), ('C'), ('D');

