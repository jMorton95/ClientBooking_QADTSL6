using ClientBooking.Shared.Enums;

namespace ClientBooking.Features.Home;

public class WeeklyStats
{
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public double TotalHours { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public BookingStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class WeeklyHours
{
    public List<DailyHours> DailyHours { get; set; } = new();
    public double TotalHours { get; set; }
}

public class DailyHours
{
    public DateTime Date { get; set; }
    public double HoursWorked { get; set; }
    public int BookingCount { get; set; }
}