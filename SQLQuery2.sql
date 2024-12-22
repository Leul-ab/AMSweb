ALTER TABLE Teacher
ADD CONSTRAINT FK_Teacher_Course
FOREIGN KEY (CourseId)
REFERENCES Course(Id);


ALTER TABLE TeacherSection
ADD CONSTRAINT FK_TeacherSection_Teacher
FOREIGN KEY (TeacherId)
REFERENCES Teacher(Id);

ALTER TABLE TeacherSection
ADD CONSTRAINT FK_TeacherSection_Section
FOREIGN KEY (SectionId)
REFERENCES Section(Id);


ALTER TABLE Student
ADD CONSTRAINT FK_Student_Section
FOREIGN KEY (SectionId)
REFERENCES Section(Id);


ALTER TABLE StudentCourse
ADD CONSTRAINT FK_StudentCourse_Student
FOREIGN KEY (StudentId)
REFERENCES Student(Id);

ALTER TABLE StudentCourse
ADD CONSTRAINT FK_StudentCourse_Course
FOREIGN KEY (CourseId)
REFERENCES Course(Id);



ALTER TABLE Attendance
ADD CONSTRAINT FK_Attendance_Student
FOREIGN KEY (StudentId)
REFERENCES Student(Id);

ALTER TABLE Attendance
ADD CONSTRAINT FK_Attendance_Course
FOREIGN KEY (CourseId)
REFERENCES Course(Id);

ALTER TABLE Attendance
ADD CONSTRAINT FK_Attendance_Section
FOREIGN KEY (SectionId)
REFERENCES Section(Id);


SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables AS tr ON fk.referenced_object_id = tr.object_id;
