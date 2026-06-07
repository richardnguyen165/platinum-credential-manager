public record class GetDetailedCredentialDto
(
    int Id,
    string ServiceName,
    string Username,
    string Password,
    DateOnly DateCreated,
    DateOnly DateLastUpdated
);