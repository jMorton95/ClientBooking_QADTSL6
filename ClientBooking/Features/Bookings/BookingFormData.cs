namespace ClientBooking.Features.Bookings;

public class BookingFormData
{
    public List<ClientResponse> Clients { get; set; } = new();
    public TimeSpan WorkingHoursStart { get; set; }
    public TimeSpan WorkingHoursEnd { get; set; }
    public TimeSpan BreakTimeStart { get; set; }
    public TimeSpan BreakTimeEnd { get; set; }
    public bool DoesWorkWeekends { get; set; }
}

public class ClientResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}