using Microsoft.AspNetCore.Identity;
using RoomBook.Api.Common;

namespace RoomBook.Api.Entities;

public class User
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    private User() { } // для EF Core

    public User(string email, string password, string fullName)
    {
        Email = email;
        ChangeFullName(fullName);
        PasswordHash = PasswordHasher.HashPassword(this, password);
    }

    public long Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.User;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public bool IsAdmin() => Role == UserRole.Admin;

    public bool VerifyPassword(string password) =>
        PasswordHasher.VerifyHashedPassword(this, PasswordHash, password) != PasswordVerificationResult.Failed;

    public void ChangeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Укажите имя пользователя.");
        }
        FullName = fullName.Trim();
    }
}
