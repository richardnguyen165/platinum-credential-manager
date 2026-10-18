using System.ComponentModel.DataAnnotations;
using PlatinumCredentialManager.Api.Dtos.Category;

namespace PlatinumCredentialManager.Api.Dtos.User;

public record class GetUserDto(
    [Required] int Id, 
    [Required] string KeycloakId, 
    [Required] ICollection<CategorySummaryDto> Categories
);