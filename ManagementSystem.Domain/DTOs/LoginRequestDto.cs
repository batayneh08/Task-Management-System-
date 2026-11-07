using System.ComponentModel.DataAnnotations;

namespace ManagementSystem.Domain.DTOs;

public sealed class LoginRequestDto
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
}
