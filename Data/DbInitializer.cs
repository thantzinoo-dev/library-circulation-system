using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IConfiguration configuration)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var existingStudentIds = await context.Members.Select(member => member.StudentId).ToListAsync();
        var membersToSeed = GetInitialMembers()
            .Concat(GetDemoMembers())
            .Where(member => !existingStudentIds.Contains(member.StudentId))
            .ToList();

        if (membersToSeed.Count > 0)
        {
            await context.Members.AddRangeAsync(membersToSeed);
            await context.SaveChangesAsync();
        }

        var existingBooks = await context.Books.ToListAsync();
        var booksToSeed = GetInitialBooks().Concat(GetMyanmarBooks()).ToList();
        foreach (var seedBook in booksToSeed)
        {
            var existing = existingBooks.FirstOrDefault(book =>
                (seedBook.ISBN is not null && book.ISBN == seedBook.ISBN) || book.Title == seedBook.Title);
            if (existing is null)
            {
                context.Books.Add(seedBook);
                existingBooks.Add(seedBook);
            }
            else if (string.IsNullOrWhiteSpace(existing.CoverImagePath) && !string.IsNullOrWhiteSpace(seedBook.CoverImagePath))
            {
                existing.CoverImagePath = seedBook.CoverImagePath;
            }
        }

        await context.SaveChangesAsync();
        await SeedPreferencesAsync(context);
        await SeedAdminAccountAsync(context, configuration);
        await SeedDemoBorrowingActivityAsync(context);
        await transaction.CommitAsync();
    }

    private static async Task SeedAdminAccountAsync(ApplicationDbContext context, IConfiguration configuration)
    {
        if (await context.AdminAccounts.AnyAsync())
        {
            return;
        }

        var username = configuration["SeedAdmin:Username"]?.Trim() ?? "admin";
        var email = configuration["SeedAdmin:Email"]?.Trim() ?? "admin@schoollibrary.edu";
        var password = configuration["SeedAdmin:InitialPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "No admin account exists. Configure SeedAdmin:InitialPassword through development settings or an environment secret.");
        }

        var account = new AdminAccount
        {
            Username = username,
            NormalizedUsername = AdminAuthentication.Normalize(username),
            Email = email,
            NormalizedEmail = AdminAuthentication.Normalize(email),
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        account.PasswordHash = new PasswordHasher<AdminAccount>().HashPassword(account, password);

        context.AdminAccounts.Add(account);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPreferencesAsync(ApplicationDbContext context)
    {
        if (!await context.LibrarySettings.AnyAsync())
        {
            context.LibrarySettings.Add(new LibrarySettings
            {
                DefaultReportPeriod = ReportPeriod.ThisMonth,
                Theme = ThemePreference.System,
                ShowPdfExport = true,
                ShowExcelExport = true,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (!await context.AdminProfiles.AnyAsync())
        {
            context.AdminProfiles.Add(new AdminProfile
            {
                FullName = "Admin User",
                Role = "Administrator",
                Email = "admin@schoollibrary.edu",
                Phone = "09 400 123 456",
                Department = "Library Administration",
                Bio = "School library administrator responsible for circulation, members, and catalogue reporting.",
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDemoBorrowingActivityAsync(ApplicationDbContext context)
    {
        const string marker = "DashboardSeed:";
        if (await context.BorrowRecords.AnyAsync(record => record.Notes != null && record.Notes.StartsWith(marker)))
        {
            return;
        }

        var members = await context.Members.Where(member => member.IsActive).OrderBy(member => member.Id).ToListAsync();
        var featuredTitles = GetMyanmarBooks().Select(book => book.Title).ToList();
        var books = await context.Books.Where(book => featuredTitles.Contains(book.Title)).OrderBy(book => book.Id).ToListAsync();
        if (members.Count == 0 || books.Count == 0)
        {
            return;
        }

        var today = DateTime.Today;
        var currentMonth = new DateTime(today.Year, today.Month, 1);
        var monthlyRecordCounts = new[] { 8, 11, 14, 12, 17, 19 };
        var weightedBooks = new[] { 0, 0, 0, 1, 1, 2, 2, 3, 4, 5, 6, 7 };
        var sequence = 0;

        for (var monthIndex = 0; monthIndex < monthlyRecordCounts.Length; monthIndex++)
        {
            var monthStart = currentMonth.AddMonths(monthIndex - 5);
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            var latestBorrowDay = monthIndex == 5 ? Math.Max(1, today.Day) : Math.Min(daysInMonth, 25);

            for (var itemIndex = 0; itemIndex < monthlyRecordCounts[monthIndex]; itemIndex++)
            {
                var book = books[weightedBooks[(sequence + monthIndex) % weightedBooks.Length] % books.Count];
                var member = members[(sequence * 3 + monthIndex) % members.Count];
                var borrowDay = 1 + ((itemIndex * 3 + monthIndex) % latestBorrowDay);
                var borrowDate = monthStart.AddDays(borrowDay - 1);
                var quantity = itemIndex % 7 == 0 ? 2 : 1;
                var shouldRemainOpen = (monthIndex == 3 && itemIndex == 0) ||
                                       (monthIndex == 4 && itemIndex < 2) ||
                                       (monthIndex == 5 && itemIndex < 6);
                var dueDate = borrowDate.AddDays(monthIndex == 5 && itemIndex < 3 ? 3 : 14);
                DateTime? returnDate = null;
                var status = BorrowStatus.Borrowed;
                var fine = 0m;
                string? returnCondition = null;

                if (!shouldRemainOpen)
                {
                    var intendedLateReturn = itemIndex % 4 == 0;
                    if (monthIndex == 5)
                    {
                        dueDate = borrowDate.AddDays(3);
                    }

                    var candidateReturnDate = intendedLateReturn ? dueDate.AddDays(2 + itemIndex % 3) : borrowDate.AddDays(monthIndex == 5 ? 2 : 7 + itemIndex % 5);
                    returnDate = candidateReturnDate > today ? today : candidateReturnDate;
                    var returnedLate = returnDate > dueDate;
                    status = BorrowStatus.Returned;
                    fine = returnedLate ? 1000m + ((itemIndex % 4) * 500m) : 0m;
                    returnCondition = itemIndex % 9 == 0 ? "Damaged" : returnedLate ? "Late" : "Good";
                }
                else
                {
                    if (book.AvailableCopies < quantity)
                    {
                        book = books.First(candidate => candidate.AvailableCopies >= quantity);
                    }

                    book.AvailableCopies -= quantity;
                }

                context.BorrowRecords.Add(new BorrowRecord
                {
                    BookId = book.Id,
                    MemberId = member.Id,
                    BorrowDate = borrowDate,
                    DueDate = dueDate,
                    ReturnDate = returnDate,
                    Status = status,
                    FineAmount = fine,
                    Quantity = quantity,
                    Notes = $"{marker}{sequence + 1:000}",
                    ReturnCondition = returnCondition
                });

                sequence++;
            }
        }

        await context.SaveChangesAsync();
    }

    private static List<Member> GetDemoMembers()
    {
        var joinedThisMonth = DateTime.UtcNow.Date.AddDays(-3);
        return
        [
            new() { StudentId = "STU1001", Name = "Thazin Hlaing", MembershipType = MemberType.Student, Department = "Myanmar Literature", Phone = "09 780 110 201", Email = "thazin.hlaing@school.edu", IsActive = true, CreatedAt = joinedThisMonth },
            new() { StudentId = "STU1002", Name = "Min Khant Kyaw", MembershipType = MemberType.Student, Department = "Computer Science", Phone = "09 780 110 202", Email = "min.khant@school.edu", IsActive = true, CreatedAt = joinedThisMonth.AddDays(1) },
            new() { StudentId = "STU1003", Name = "Su Myat Noe", MembershipType = MemberType.Student, Department = "Business Administration", Phone = "09 780 110 203", Email = "su.myat@school.edu", IsActive = true, CreatedAt = joinedThisMonth.AddDays(1) },
            new() { StudentId = "TEA1001", Name = "Daw Nandar Win", MembershipType = MemberType.Teacher, Department = "Myanmar Literature", Phone = "09 780 110 204", Email = "nandar.win@school.edu", IsActive = true, CreatedAt = joinedThisMonth.AddDays(2) },
            new() { StudentId = "STA1001", Name = "Ko Hein Htet", MembershipType = MemberType.Staff, Department = "Library Services", Phone = "09 780 110 205", Email = "hein.htet@school.edu", IsActive = true, CreatedAt = joinedThisMonth.AddDays(2) }
        ];
    }

    private static List<Book> GetMyanmarBooks()
    {
        var addedThisMonth = DateTime.UtcNow.Date.AddDays(-5);
        return
        [
            new() { Title = "Smile as They Bow", Author = "Nu Nu Yi (Inwa)", Category = "Myanmar Literature", Language = "Burmese", PublishedYear = 1994, TotalCopies = 12, AvailableCopies = 12, CoverImagePath = "/images/books/smile-as-they-bow.svg", CreatedAt = addedThisMonth },
            new() { Title = "Not Out of Hate", Author = "Journal Kyaw Ma Ma Lay", Category = "Myanmar Literature", Language = "Burmese", PublishedYear = 1955, TotalCopies = 10, AvailableCopies = 10, CoverImagePath = "/images/books/not-out-of-hate.svg", CreatedAt = addedThisMonth.AddDays(1) },
            new() { Title = "A Man Like Him", Author = "Journal Kyaw Ma Ma Lay", Category = "Biography", Language = "Burmese", PublishedYear = 1947, TotalCopies = 9, AvailableCopies = 9, CoverImagePath = "/images/books/a-man-like-him.svg", CreatedAt = addedThisMonth.AddDays(1) },
            new() { Title = "Across the Mountain of Swords and the Sea of Fire", Author = "Mya Than Tint", Category = "Myanmar Literature", Language = "Burmese", PublishedYear = 1973, TotalCopies = 10, AvailableCopies = 10, CoverImagePath = "/images/books/mountain-of-swords.svg", CreatedAt = addedThisMonth.AddDays(2) },
            new() { Title = "The River of Lost Footsteps", Author = "Thant Myint-U", Category = "Myanmar History", Language = "English", PublishedYear = 2006, TotalCopies = 8, AvailableCopies = 8, CoverImagePath = "/images/books/river-lost-footsteps.svg", CreatedAt = addedThisMonth.AddDays(2) },
            new() { Title = "The Hidden History of Burma", Author = "Thant Myint-U", Category = "Myanmar History", Language = "English", PublishedYear = 2019, TotalCopies = 8, AvailableCopies = 8, CoverImagePath = "/images/books/hidden-history-burma.svg", CreatedAt = addedThisMonth.AddDays(3) },
            new() { Title = "From the Land of Green Ghosts", Author = "Pascal Khoo Thwe", Category = "Memoir", Language = "English", PublishedYear = 2002, TotalCopies = 7, AvailableCopies = 7, CoverImagePath = "/images/books/green-ghosts.svg", CreatedAt = addedThisMonth.AddDays(3) },
            new() { Title = "The Glass Palace", Author = "Amitav Ghosh", Category = "Historical Fiction", Language = "English", PublishedYear = 2000, TotalCopies = 8, AvailableCopies = 8, CoverImagePath = "/images/books/glass-palace.svg", CreatedAt = addedThisMonth.AddDays(4) }
        ];
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
