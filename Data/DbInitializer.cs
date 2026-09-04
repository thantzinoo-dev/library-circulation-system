using Microsoft.EntityFrameworkCore;
using School_Library_Management.Models;

namespace School_Library_Management.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Seed Members if count is less than 15
        var currentMemberCount = await context.Members.CountAsync();
        if (currentMemberCount < 15)
        {
            var existingStudentIds = await context.Members.Select(m => m.StudentId).ToListAsync();
            var membersToSeed = GetInitialMembers()
                .Where(m => !existingStudentIds.Contains(m.StudentId))
                .ToList();

            if (membersToSeed.Count > 0)
            {
                await context.Members.AddRangeAsync(membersToSeed);
                await context.SaveChangesAsync();
            }
        }

        // 2. Seed Books if count is less than 15
        var currentBookCount = await context.Books.CountAsync();
        if (currentBookCount < 15)
        {
            var existingIsbns = await context.Books
                .Where(b => b.ISBN != null)
                .Select(b => b.ISBN!)
                .ToListAsync();
            var existingTitles = await context.Books.Select(b => b.Title).ToListAsync();

            var booksToSeed = GetInitialBooks()
                .Where(b => (b.ISBN == null || !existingIsbns.Contains(b.ISBN)) && !existingTitles.Contains(b.Title))
                .ToList();

            if (booksToSeed.Count > 0)
            {
                await context.Books.AddRangeAsync(booksToSeed);
                await context.SaveChangesAsync();
            }
        }
    }

    private static List<Member> GetInitialMembers()
    {
        var baseDate = DateTime.UtcNow.AddMonths(-6);
        return new List<Member>
        {
            new()
            {
                StudentId = "STU0003",
                Name = "Emily Watson",
                MembershipType = MemberType.Student,
                Department = "Computer Science",
                Phone = "09 777 123 456",
                Email = "emily.watson@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(5)
            },
            new()
            {
                StudentId = "STU0004",
                Name = "Htet Naing Oo",
                MembershipType = MemberType.Student,
                Department = "Information Technology",
                Phone = "09 450 987 654",
                Email = "htet.naing@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(12)
            },
            new()
            {
                StudentId = "TEA0001",
                Name = "Dr. Robert Vance",
                MembershipType = MemberType.Teacher,
                Department = "Computer Science",
                Phone = "09 250 112 233",
                Email = "robert.vance@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(18)
            },
            new()
            {
                StudentId = "STU0005",
                Name = "Sophia Chen",
                MembershipType = MemberType.Student,
                Department = "Business Administration",
                Phone = "09 978 445 566",
                Email = "sophia.chen@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(25)
            },
            new()
            {
                StudentId = "STU0006",
                Name = "Kaung Kin",
                MembershipType = MemberType.Student,
                Department = "Civil Engineering",
                Phone = "09 951 223 344",
                Email = "kaung.kin@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(30)
            },
            new()
            {
                StudentId = "STA0001",
                Name = "Daw Aye Aye Win",
                MembershipType = MemberType.Staff,
                Department = "Library Administration",
                Phone = "09 420 334 455",
                Email = "aye.win@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(35)
            },
            new()
            {
                StudentId = "STU0007",
                Name = "Lucas Miller",
                MembershipType = MemberType.Student,
                Department = "Arts & Humanities",
                Phone = "09 789 667 788",
                Email = "lucas.miller@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(42)
            },
            new()
            {
                StudentId = "TEA0002",
                Name = "Prof. Daw Myint Zu",
                MembershipType = MemberType.Teacher,
                Department = "English Literature",
                Phone = "09 250 889 900",
                Email = "myint.zu@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(50)
            },
            new()
            {
                StudentId = "STU0008",
                Name = "Zwe Htet Aung",
                MembershipType = MemberType.Student,
                Department = "Mathematics",
                Phone = "09 960 111 222",
                Email = "zwe.htet@school.edu",
                IsActive = false,
                CreatedAt = baseDate.AddDays(60)
            },
            new()
            {
                StudentId = "STU0009",
                Name = "Olivia Davis",
                MembershipType = MemberType.Student,
                Department = "Applied Physics",
                Phone = "09 778 333 444",
                Email = "olivia.davis@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(68)
            },
            new()
            {
                StudentId = "STU0010",
                Name = "Aung Kaung Myat",
                MembershipType = MemberType.Student,
                Department = "Chemistry",
                Phone = "09 450 555 666",
                Email = "aung.kaung@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(75)
            },
            new()
            {
                StudentId = "STA0002",
                Name = "U Kyaw Moe",
                MembershipType = MemberType.Staff,
                Department = "Campus IT Services",
                Phone = "09 421 777 888",
                Email = "kyaw.moe@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(82)
            },
            new()
            {
                StudentId = "TEA0003",
                Name = "Dr. Sarah Jenkins",
                MembershipType = MemberType.Teacher,
                Department = "Physics",
                Phone = "09 251 999 000",
                Email = "sarah.jenkins@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(90)
            },
            new()
            {
                StudentId = "STU0011",
                Name = "Thiri San",
                MembershipType = MemberType.Student,
                Department = "Biotechnology",
                Phone = "09 979 222 333",
                Email = "thiri.san@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(98)
            },
            new()
            {
                StudentId = "STU0012",
                Name = "Daniel Brown",
                MembershipType = MemberType.Student,
                Department = "Computer Science",
                Phone = "09 780 444 555",
                Email = "daniel.brown@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(105)
            },
            new()
            {
                StudentId = "STU0013",
                Name = "Pyae Sone",
                MembershipType = MemberType.Student,
                Department = "Economics",
                Phone = "09 952 666 777",
                Email = "pyae.sone@school.edu",
                IsActive = false,
                CreatedAt = baseDate.AddDays(112)
            },
            new()
            {
                StudentId = "TEA0004",
                Name = "U Than Lwin",
                MembershipType = MemberType.Teacher,
                Department = "World History",
                Phone = "09 252 888 999",
                Email = "than.lwin@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(120)
            },
            new()
            {
                StudentId = "STU0014",
                Name = "Emma Wilson",
                MembershipType = MemberType.Student,
                Department = "Literature",
                Phone = "09 781 123 789",
                Email = "emma.wilson@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(128)
            },
            new()
            {
                StudentId = "STU0015",
                Name = "Min Thura",
                MembershipType = MemberType.Student,
                Department = "Electrical Engineering",
                Phone = "09 961 456 123",
                Email = "min.thura@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(135)
            },
            new()
            {
                StudentId = "STA0003",
                Name = "Daw Khin Mar",
                MembershipType = MemberType.Staff,
                Department = "Student Affairs",
                Phone = "09 422 789 456",
                Email = "khin.mar@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(142)
            },
            new()
            {
                StudentId = "STU0016",
                Name = "Noah Taylor",
                MembershipType = MemberType.Student,
                Department = "Law",
                Phone = "09 782 321 654",
                Email = "noah.taylor@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(150)
            },
            new()
            {
                StudentId = "STU0017",
                Name = "May Myat Noe",
                MembershipType = MemberType.Student,
                Department = "Accounting & Finance",
                Phone = "09 970 654 987",
                Email = "may.myat@school.edu",
                IsActive = true,
                CreatedAt = baseDate.AddDays(158)
            }
        };
    }

    private static List<Book> GetInitialBooks()
    {
        var baseDate = DateTime.UtcNow.AddMonths(-8);
        return new List<Book>
        {
            new()
            {
                ISBN = "9780132350884",
                Title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                Author = "Robert C. Martin",
                Category = "Technology",
                Language = "English",
                PublishedYear = 2008,
                TotalCopies = 5,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(4)
            },
            new()
            {
                ISBN = "9780201633610",
                Title = "Design Patterns: Elements of Reusable Object-Oriented Software",
                Author = "Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides",
                Category = "Technology",
                Language = "English",
                PublishedYear = 1994,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(10)
            },
            new()
            {
                ISBN = "9780134685991",
                Title = "Effective Java",
                Author = "Joshua Bloch",
                Category = "Technology",
                Language = "English",
                PublishedYear = 2017,
                TotalCopies = 3,
                AvailableCopies = 2,
                CreatedAt = baseDate.AddDays(18)
            },
            new()
            {
                ISBN = "9780262033848",
                Title = "Introduction to Algorithms",
                Author = "Thomas H. Cormen, Charles E. Leiserson, Ronald L. Rivest, Clifford Stein",
                Category = "Computer Science",
                Language = "English",
                PublishedYear = 2009,
                TotalCopies = 4,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(25)
            },
            new()
            {
                ISBN = "9780743273565",
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Category = "Classic Literature",
                Language = "English",
                PublishedYear = 1925,
                TotalCopies = 6,
                AvailableCopies = 5,
                CreatedAt = baseDate.AddDays(32)
            },
            new()
            {
                ISBN = "9780451524935",
                Title = "1984",
                Author = "George Orwell",
                Category = "Dystopian Fiction",
                Language = "English",
                PublishedYear = 1949,
                TotalCopies = 8,
                AvailableCopies = 6,
                CreatedAt = baseDate.AddDays(40)
            },
            new()
            {
                ISBN = "9780061120084",
                Title = "To Kill a Mockingbird",
                Author = "Harper Lee",
                Category = "Classic Literature",
                Language = "English",
                PublishedYear = 1960,
                TotalCopies = 5,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(48)
            },
            new()
            {
                ISBN = "9780141439518",
                Title = "Pride and Prejudice",
                Author = "Jane Austen",
                Category = "Romance / Classic",
                Language = "English",
                PublishedYear = 1813,
                TotalCopies = 4,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(55)
            },
            new()
            {
                ISBN = "9780547928227",
                Title = "The Hobbit",
                Author = "J.R.R. Tolkien",
                Category = "Fantasy Fiction",
                Language = "English",
                PublishedYear = 1937,
                TotalCopies = 6,
                AvailableCopies = 5,
                CreatedAt = baseDate.AddDays(65)
            },
            new()
            {
                ISBN = "9780307474278",
                Title = "One Hundred Years of Solitude",
                Author = "Gabriel García Márquez",
                Category = "Magical Realism",
                Language = "English",
                PublishedYear = 1967,
                TotalCopies = 3,
                AvailableCopies = 2,
                CreatedAt = baseDate.AddDays(72)
            },
            new()
            {
                ISBN = "9780385490818",
                Title = "The Handmaid's Tale",
                Author = "Margaret Atwood",
                Category = "Dystopian Fiction",
                Language = "English",
                PublishedYear = 1985,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(80)
            },
            new()
            {
                ISBN = "9780062316097",
                Title = "Sapiens: A Brief History of Humankind",
                Author = "Yuval Noah Harari",
                Category = "History",
                Language = "English",
                PublishedYear = 2014,
                TotalCopies = 5,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(90)
            },
            new()
            {
                ISBN = "9780735211292",
                Title = "Atomic Habits",
                Author = "James Clear",
                Category = "Self-Help",
                Language = "English",
                PublishedYear = 2018,
                TotalCopies = 7,
                AvailableCopies = 5,
                CreatedAt = baseDate.AddDays(100)
            },
            new()
            {
                ISBN = "9780062457714",
                Title = "The Subtle Art of Not Giving a F*ck",
                Author = "Mark Manson",
                Category = "Self-Help",
                Language = "English",
                PublishedYear = 2016,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(110)
            },
            new()
            {
                ISBN = "9780062315007",
                Title = "The Alchemist",
                Author = "Paulo Coelho",
                Category = "Philosophical Fiction",
                Language = "English",
                PublishedYear = 1988,
                TotalCopies = 6,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(120)
            },
            new()
            {
                ISBN = "9780143127741",
                Title = "Thinking, Fast and Slow",
                Author = "Daniel Kahneman",
                Category = "Psychology",
                Language = "English",
                PublishedYear = 2011,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(130)
            },
            new()
            {
                ISBN = "9780345391803",
                Title = "The Hitchhiker's Guide to the Galaxy",
                Author = "Douglas Adams",
                Category = "Science Fiction",
                Language = "English",
                PublishedYear = 1979,
                TotalCopies = 5,
                AvailableCopies = 5,
                CreatedAt = baseDate.AddDays(140)
            },
            new()
            {
                ISBN = "9780441013593",
                Title = "Dune",
                Author = "Frank Herbert",
                Category = "Science Fiction",
                Language = "English",
                PublishedYear = 1965,
                TotalCopies = 5,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(150)
            },
            new()
            {
                ISBN = "9780140283334",
                Title = "Fahrenheit 451",
                Author = "Ray Bradbury",
                Category = "Dystopian Fiction",
                Language = "English",
                PublishedYear = 1953,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(160)
            },
            new()
            {
                ISBN = "9780143039433",
                Title = "The Grapes of Wrath",
                Author = "John Steinbeck",
                Category = "Historical Fiction",
                Language = "English",
                PublishedYear = 1939,
                TotalCopies = 3,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(170)
            },
            new()
            {
                ISBN = "9780393040029",
                Title = "Guns, Germs, and Steel",
                Author = "Jared Diamond",
                Category = "History",
                Language = "English",
                PublishedYear = 1997,
                TotalCopies = 3,
                AvailableCopies = 2,
                CreatedAt = baseDate.AddDays(180)
            },
            new()
            {
                ISBN = "9780679783268",
                Title = "Crime and Punishment",
                Author = "Fyodor Dostoevsky",
                Category = "Classic Literature",
                Language = "English",
                PublishedYear = 1866,
                TotalCopies = 4,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(190)
            },
            new()
            {
                ISBN = "9781451673319",
                Title = "Steve Jobs",
                Author = "Walter Isaacson",
                Category = "Biography",
                Language = "English",
                PublishedYear = 2011,
                TotalCopies = 4,
                AvailableCopies = 3,
                CreatedAt = baseDate.AddDays(200)
            },
            new()
            {
                ISBN = "9780385504201",
                Title = "The Da Vinci Code",
                Author = "Dan Brown",
                Category = "Mystery / Thriller",
                Language = "English",
                PublishedYear = 2003,
                TotalCopies = 5,
                AvailableCopies = 4,
                CreatedAt = baseDate.AddDays(210)
            },
            new()
            {
                ISBN = "9780131103627",
                Title = "The C Programming Language",
                Author = "Brian W. Kernighan, Dennis M. Ritchie",
                Category = "Computer Science",
                Language = "English",
                PublishedYear = 1988,
                TotalCopies = 3,
                AvailableCopies = 2,
                CreatedAt = baseDate.AddDays(220)
            }
        };
    }
}
