namespace Cwiczenie8.DTOs;

public class TripDetailsGetDto
{
    public int TripId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    
    public List<string> Countries { get; set; }
    
    
    
    
}