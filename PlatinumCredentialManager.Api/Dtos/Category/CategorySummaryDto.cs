using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class CategorySummaryDto(
    [Required] int Id, 
    [Required] string CategoryName
);