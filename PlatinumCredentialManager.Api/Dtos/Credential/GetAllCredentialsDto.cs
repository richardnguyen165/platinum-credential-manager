using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class GetAllCredentialsDto
(
    int Id,
    string ServiceName,
    string Username,
    DateOnly DateCreated,
    DateOnly DateLastUpdated
);