using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.User;

public record class GetUserDto(int Id, string KeycloakId, ICollection<string> Categories);