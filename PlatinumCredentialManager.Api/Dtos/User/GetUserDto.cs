using System.ComponentModel.DataAnnotations;
using PlatinumCredentialManager.Api.Dtos.Category;

namespace PlatinumCredentialManager.Api.Dtos.User;

public record class GetUserDto(int Id, string KeycloakId, ICollection<CategorySummaryDto> Categories);