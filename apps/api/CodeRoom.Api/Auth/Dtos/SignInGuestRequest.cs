using System.ComponentModel.DataAnnotations;

namespace CodeRoom.Api.Auth.Dtos;

public class SignInGuestRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Name { get; set; }
}