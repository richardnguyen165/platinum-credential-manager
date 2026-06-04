using System.ComponentModel.DataAnnotations;

public record class GetAllCredentialsDto
(
    int Id,
    string ServiceName,
    string Username,
    DateOnly DateCreated
);