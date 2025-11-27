using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Shared.Enums;
using ClientBooking.Shared.Extensions;
using ClientBooking.Shared.Mapping;
using ClientBooking.Shared.Models;

namespace ClientBooking.Features.Bookings;

public interface IBookingService
{
    Task<Result<List<BookingRequest>>> EnforceBookingSchedulingRules(BookingRequest request, Client client, User user, Settings settings, int? optionalExcludeBookingId = null, Guid? optionalExcludeSeriesId = null);
    
    Task EnsureBookingRequestHasNoOverlaps(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client, User user, Settings systemSettings, int? optionalExcludeBookingId = null, Guid? optionalExcludeSeriesId = null);

    List<BookingRequest> PlotRecurringBookingRequests(BookingRequest request);
    
    void CheckRequestIsWithinUserSchedule(Dictionary<string, string[]> validationErrors, BookingRequest request, User user, Settings systemSettings);
    
    Task CheckOverlappingUserBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, User user, int? optionalExcludeBookingId = null, Guid? optionalExcludeSeriesId = null);
    
    Task CheckOverlappingClientBookings(Dictionary<string, string[]> validationErrors, BookingRequest request, Client client, int? optionalExcludeBookingId = null, Guid? optionalExcludeSeriesId = null);
    Task CreateBooking(List<BookingRequest> request, Client client, int userId);
    
    Task UpdateBooking(Booking existingBooking, List<BookingRequest> validatedUpdatedBooking, Client client, int userId);
    
    Task<List<Booking>> GetBookingsInSeriesAsync(Guid recurrenceSeriesId, int? bookingIdToExclude = null);
    Task CancelEntireSeriesAsync(Guid recurrenceSeriesId, int userId, int? bookingIdToExclude = null);
}

public class BookingService(DataContext dataContext) : IBookingService
{
    public async Task<Result<List<BookingRequest>>> EnforceBookingSchedulingRules(
        BookingRequest request, 
        Client client, 
        User user, 
        Settings settings, 
        int? optionalExcludeBookingId = null,
        Guid? optionalExcludeSeriesId = null)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (request is { IsRecurring: true, RecurrencePattern: > 0, NumberOfRecurrences: >= 2 })
        {
            var allRecurringRequests = PlotRecurringBookingRequests(request);
            
            foreach (var recurringRequest in allRecurringRequests)
            {
                await EnsureBookingRequestHasNoOverlaps(
                    validationErrors, 
                    recurringRequest, 
                    client, 
                    user, 
                    settings, 
                    optionalExcludeBookingId,
                    optionalExcludeSeriesId);
            }
            
            return validationErrors.Count != 0
                ? Result<List<BookingRequest>>.ValidationFailure(validationErrors)
                : Result<List<BookingRequest>>.Success(allRecurringRequests);
        }

        await EnsureBookingRequestHasNoOverlaps(
            validationErrors, 
            request, 
            client, 
            user, 
            settings, 
            optionalExcludeBookingId,
            optionalExcludeSeriesId);
            
        return validationErrors.Count != 0
            ? Result<List<BookingRequest>>.ValidationFailure(validationErrors)
            : Result<List<BookingRequest>>.Success([request]);
    }

    public async Task EnsureBookingRequestHasNoOverlaps(
        Dictionary<string, string[]> validationErrors, 
        BookingRequest request, 
        Client client, 
        User user,
        Settings systemSettings, 
        int? optionalExcludeBookingId = null,
        Guid? optionalExcludeSeriesId = null)
    {
        CheckRequestIsWithinUserSchedule(validationErrors, request, user, systemSettings);

        await CheckOverlappingUserBookings(validationErrors, request, user, optionalExcludeBookingId, optionalExcludeSeriesId);

        await CheckOverlappingClientBookings(validationErrors, request, client, optionalExcludeBookingId, optionalExcludeSeriesId);
    }

    public void CheckRequestIsWithinUserSchedule(Dictionary<string, string[]> validationErrors, BookingRequest request, User user, Settings systemSettings)
    {
        var (userWorkingHoursStart, userWorkingHoursEnd, userBreakTimeStart, userBreakTimeEnd)
            = user.GetEffectiveWorkingHours(systemSettings);
        
        if (request.StartDateTime.TimeOfDay < userWorkingHoursStart
            || request.EndDateTime.TimeOfDay > userWorkingHoursEnd
            || request.StartDateTime.Date != request.EndDateTime.Date)
        {
            validationErrors.TryAdd("WorkingHours",
                [$"Booking must start and end within your time range. Reminder, your working hours are: {userWorkingHoursStart} - {userWorkingHoursEnd}."]);
        }

        if (request.StartDateTime.TimeOfDay < userBreakTimeEnd && request.EndDateTime.TimeOfDay > userBreakTimeStart)
        {
            validationErrors.TryAdd("BreakTime", [$"Booking must not overlap your break hours. Reminder, your break time is: {userWorkingHoursStart} - {userWorkingHoursEnd}."]);
        }

        if (!user.DoesWorkWeekends && request.StartDateTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            validationErrors.TryAdd("WeekendWork", ["Cannot schedule a booking for the weekend when you do not work weekends."]);
        }
    }

    public async Task CheckOverlappingUserBookings(
        Dictionary<string, string[]> validationErrors, 
        BookingRequest request, 
        User user, 
        int? optionalExcludeBookingId = null,
        Guid? optionalExcludeSeriesId = null)
    {
        var overlappingUserBookingsQuery = dataContext.UserBookings
            .Include(ub => ub.Booking)
            .ThenInclude(b => b.Client)
            .Where(x => x.UserId == user.Id)
            .Where(y => y.Booking.StartDateTime < request.EndDateTime && y.Booking.EndDateTime > request.StartDateTime)
            .Where(y => y.Booking.Status != BookingStatus.Cancelled);

        if (optionalExcludeBookingId.HasValue)
        {
            overlappingUserBookingsQuery = overlappingUserBookingsQuery.Where(y => y.Booking.Id != optionalExcludeBookingId.Value);
        }

        if (optionalExcludeSeriesId.HasValue)
        {
            overlappingUserBookingsQuery = overlappingUserBookingsQuery.Where(y => y.Booking.RecurrenceSeriesId != optionalExcludeSeriesId.Value);
        }

        var overlappingUserBookings = await overlappingUserBookingsQuery
            .AsSplitQuery()
            .ToListAsync();

        if (overlappingUserBookings.Count != 0)
        {
            var overlappingUserBookingErrorMessages = overlappingUserBookings
                .Select(x => $"Conflict with your booking on {x.Booking.StartDateTime:MMM dd, yyyy 'at' h:mm tt} with {x.Booking.Client.Name}.");
            
            if (!validationErrors.TryGetValue("OverlappingUserBookings", out var value))
                validationErrors["OverlappingUserBookings"] = overlappingUserBookingErrorMessages.ToArray();
            else
                validationErrors["OverlappingUserBookings"] = value.Concat(overlappingUserBookingErrorMessages).ToArray();
        }
    }

    public async Task CheckOverlappingClientBookings(
        Dictionary<string, string[]> validationErrors, 
        BookingRequest request, 
        Client client, 
        int? optionalExcludeBookingId = null,
        Guid? optionalExcludeSeriesId = null)
    {
        var overlappingClientBookingsQuery = dataContext.UserBookings
            .Include(ub => ub.Booking)
            .Include(ub => ub.User)
            .Where(x => x.Booking.ClientId == client.Id)
            .Where(x => x.Booking.StartDateTime < request.EndDateTime && x.Booking.EndDateTime > request.StartDateTime)
            .Where(x => x.Booking.Status != BookingStatus.Cancelled);

        if (optionalExcludeBookingId.HasValue)
        {
            overlappingClientBookingsQuery = overlappingClientBookingsQuery.Where(x => x.Booking.Id != optionalExcludeBookingId.Value);
        }

        if (optionalExcludeSeriesId.HasValue)
        {
            overlappingClientBookingsQuery = overlappingClientBookingsQuery.Where(x => x.Booking.RecurrenceSeriesId != optionalExcludeSeriesId.Value);
        }

        var overlappingClientBookings = await overlappingClientBookingsQuery
            .AsSplitQuery()
            .ToListAsync();

        if (overlappingClientBookings.Count != 0)
        {
            var overlappingClientBookingErrorMessages = overlappingClientBookings
                .Select(x => $"Conflicts with {client.Name}'s booking with {x.User.FirstName} {x.User.LastName} at {x.Booking.StartDateTime:MMM dd, yyyy 'at' h:mm tt}");
            
            if (!validationErrors.TryGetValue("OverlappingClientBookings", out var value))
                validationErrors["OverlappingClientBookings"] = overlappingClientBookingErrorMessages.ToArray();
            else
                validationErrors["OverlappingClientBookings"] = value.Concat(overlappingClientBookingErrorMessages).ToArray();
        }
    }
    
    public List<BookingRequest> PlotRecurringBookingRequests(BookingRequest request)
    {
        var requestedRecurrencePattern = request.RecurrencePattern;
        var requestedRecurrences = request.NumberOfRecurrences;
        
        List<BookingRequest> allRecurrences = [request];
        
        for (var i = 1; i < requestedRecurrences; i++)
        {
            var nextBookingRequest = request.ToNewBookingRequest();

            nextBookingRequest.StartDateTime = requestedRecurrencePattern switch
            {
                BookingRecurrencePattern.Weekly  => request.StartDateTime.AddDays(7 * i),
                BookingRecurrencePattern.Monthly => request.StartDateTime.AddMonths(i),
                _ => throw new ArgumentOutOfRangeException(nameof(request.RecurrencePattern),
                    "Booking recurrences cannot be assigned if 'None' is selected.")
            };

            nextBookingRequest.EndDateTime = requestedRecurrencePattern switch
            {
                BookingRecurrencePattern.Weekly  => request.EndDateTime.AddDays(7 * i),
                BookingRecurrencePattern.Monthly => request.EndDateTime.AddMonths(i),
                _ => throw new ArgumentOutOfRangeException(nameof(request.RecurrencePattern),
                    "Booking recurrences cannot be assigned if 'None' is selected.")
            };

            allRecurrences.Add(nextBookingRequest);
        }

        return allRecurrences;
    }
    
    public async Task CreateBooking(List<BookingRequest> request, Client client, int userId)
    {
        Guid? seriesId = request.Count > 1 ? Guid.NewGuid() : null;
    
        var userBookings = request.ToNewBookings(client, userId, seriesId);
        await dataContext.UserBookings.AddRangeAsync(userBookings);
        await dataContext.SaveChangesAsync();
    }

    public async Task UpdateBooking(Booking existingBooking, List<BookingRequest> validatedUpdatedBooking, Client client, int userId)
    {
        var existingBookingRequest = validatedUpdatedBooking.First();
        var wasPreviouslyRecurring = existingBooking.IsRecurring; 
        
        existingBooking.StartDateTime = existingBookingRequest.StartDateTime;
        existingBooking.EndDateTime = existingBookingRequest.EndDateTime;
        existingBooking.RecurrencePattern = existingBookingRequest.RecurrencePattern;
        existingBooking.IsRecurring = existingBookingRequest.IsRecurring;
        existingBooking.NumberOfRecurrences = existingBookingRequest.NumberOfRecurrences;
        existingBooking.Notes = existingBookingRequest.Notes;

        if (existingBookingRequest.IsRecurring && validatedUpdatedBooking.Count > 1)
        {
            if (existingBooking.RecurrenceSeriesId != null)
            {
                await CancelEntireSeriesAsync(existingBooking.RecurrenceSeriesId!.Value, userId);
            }
            
            var newSeriesId = Guid.NewGuid();
            existingBooking.RecurrenceSeriesId = newSeriesId;
            
            var newRecurringBookings = validatedUpdatedBooking.Skip(1).ToList().ToNewBookings(client, userId, newSeriesId);
            await dataContext.UserBookings.AddRangeAsync(newRecurringBookings);
        }
        else if (wasPreviouslyRecurring && !existingBookingRequest.IsRecurring && existingBooking.RecurrenceSeriesId.HasValue)
        {
            var seriesId = existingBooking.RecurrenceSeriesId.Value;
            existingBooking.RecurrenceSeriesId = null;
            existingBooking.IsRecurring = false;
            existingBooking.NumberOfRecurrences = 1;
            existingBooking.RecurrencePattern = BookingRecurrencePattern.None;
            
            await CancelEntireSeriesAsync(seriesId, userId, existingBooking.Id);
        }
        else if (!existingBookingRequest.IsRecurring)
        {
            existingBooking.RecurrenceSeriesId = null;
            existingBooking.NumberOfRecurrences = 1;
            existingBooking.RecurrencePattern = BookingRecurrencePattern.None;
        }
        
        await dataContext.SaveChangesAsync(); 
    }

    public Task<List<Booking>> GetBookingsInSeriesAsync(Guid recurrenceSeriesId, int? bookingIdToExclude = null)
    {
        var query = dataContext.Bookings
            .Where(b => b.RecurrenceSeriesId == recurrenceSeriesId && b.Status != BookingStatus.Cancelled);

        if (bookingIdToExclude.HasValue)
            query = query.Where(b => b.Id != bookingIdToExclude.Value);

        return query.OrderBy(b => b.StartDateTime).ToListAsync();
    }

    public async Task CancelEntireSeriesAsync(Guid recurrenceSeriesId, int userId, int? bookingIdToExclude = null)
    {
        var seriesBookings = await GetBookingsInSeriesAsync(recurrenceSeriesId, bookingIdToExclude);

        var bookingsToRemove = seriesBookings.Where(x => x.StartDateTime > DateTime.UtcNow);

        dataContext.RemoveRange(bookingsToRemove);
        await dataContext.SaveChangesAsync();
    }
}