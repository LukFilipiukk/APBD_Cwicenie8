namespace Cwiczenie8.DTOs;

public class ClientTripDetailsGetDto
{
    public TripDetailsGetDto Trip { get; set; } 
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
}