using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Services;

public static class BookCoverStorage
{
    private const long MaximumFileSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public static async Task<string> SaveAsync(
        IFormFile file,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new ValidationException("Choose a non-empty cover image.");
        }

        if (file.Length > MaximumFileSize)
        {
            throw new ValidationException("Cover images must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ValidationException("Use a JPG, PNG, or WebP cover image.");
        }

        var uploadRoot = Path.Combine(environment.WebRootPath, "uploads", "books");
        Directory.CreateDirectory(uploadRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var destination = Path.Combine(uploadRoot, storedFileName);

        await using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, cancellationToken);

        return $"/uploads/books/{storedFileName}";
    }

    public static void DeleteUploadedCover(string? coverImagePath, IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(coverImagePath) ||
            !coverImagePath.StartsWith("/uploads/books/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var uploadRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "uploads", "books"));
        var candidate = Path.GetFullPath(Path.Combine(environment.WebRootPath, coverImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(uploadRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(candidate))
        {
            File.Delete(candidate);
        }
    }
}
