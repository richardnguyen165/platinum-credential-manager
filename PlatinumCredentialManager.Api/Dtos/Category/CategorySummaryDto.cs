using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Category;

public record class CategorySummaryDto(int Id, string CategoryName);