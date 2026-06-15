using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class UpdateCategoryDto
(
    [Required][StringLength(100)] string CategoryName
);