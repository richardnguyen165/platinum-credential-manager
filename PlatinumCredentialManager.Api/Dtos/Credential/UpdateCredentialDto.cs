using System.ComponentModel.DataAnnotations;

namespace PlatinumCredentialManager.Api.Dtos.Credential;

public record class UpdateCredentialDto
(
    // Not necessary to update a field
    string ServiceName,
    string Username, // user may not have a username for application
    string Password,
    int? CategoryId
);