namespace School_Library_Management.Models;

public static class MemberType
{
    public const string Student = "Student";
    public const string Teacher = "Teacher";
    public const string Staff = "Staff";

    public static readonly string[] All = [Student, Teacher, Staff];
}
