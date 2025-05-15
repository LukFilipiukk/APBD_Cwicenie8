using System.ComponentModel.DataAnnotations;

namespace Cwiczenie8.DTOs;

public class ClientCreateDto
{
    [MaxLength(120)]
    public string FirstName { get; set; }
    [MaxLength(120)]
    public string LastName { get; set; } 
    [MaxLength(120)]
    public string Email { get; set; }
    [MaxLength(120)]
    public string Telephone { get; set; }
    [MaxLength(120)]
    public string Pesel { get; set; }
}