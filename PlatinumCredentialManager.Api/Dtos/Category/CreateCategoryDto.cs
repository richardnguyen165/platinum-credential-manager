using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class CreateCategoryDto
(
    [Required][StringLength(100)] string CategoryName
);