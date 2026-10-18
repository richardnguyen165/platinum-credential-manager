using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class ImportCategoriesDto
(
    [Required] IFormFile File
);