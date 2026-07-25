using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.User;
public record class UpdateUserDto
(
    [Required][StringLength(100)] string Email,
    [Required][StringLength(100)] string Password
);