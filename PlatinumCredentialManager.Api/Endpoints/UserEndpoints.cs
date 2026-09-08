using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PlatinumCredentialManager.Api.Data;
using PlatinumCredentialManager.Api.Dtos.Category;
using PlatinumCredentialManager.Api.Models;
using Superpower.Model;

namespace PlatinumCredentialManager.Api.Endpoints;

public static class UserEndpoints
{
    private const string GetUserEndpointName = "GetUser";

    public static void MapUserEndpoints(this WebApplication app)
    {
        var categoryURLGroup = app.MapGroup("/user").RequireAuthorization();
    }
}