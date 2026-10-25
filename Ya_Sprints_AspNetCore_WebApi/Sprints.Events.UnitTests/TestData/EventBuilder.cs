using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Domain.Entities;
using System.Reflection;


namespace Sprints.Events.UnitTests.TestData;


internal static class EventBuilder
{
    public static Event Create(
        Guid? id = null,
        string title = "Test Event",
        string? description = "Test description",
        int totalSeats = 100,
        int? availableSeats = null)
    {
        var evt = Event.Create(
            id ?? Guid.NewGuid(),
            title,
            description,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2),
            totalSeats);

        var target = availableSeats ?? totalSeats;
        if (target != totalSeats)
            SetAvailableSeats(evt, target);

        return evt;
    }

    /// <summary>
    /// EventInfoDto с обязательными StartAt/EndAt — чтобы не писать их в каждом тесте.
    /// </summary>
    public static EventInfoDto CreateDto(
        Guid? id = null,
        string title = "Test Event",
        string? description = "Test description",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 100,
        int? availableSeats = null)
    {
        var now = DateTime.UtcNow;
        return new EventInfoDto
        {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt ?? now.AddDays(1),
            EndAt = endAt ?? now.AddDays(2),
            TotalSeats = totalSeats,
            AvailableSeats = availableSeats ?? totalSeats
        };
    }

    private static void SetAvailableSeats(Event evt, int value)
    {
        var prop = typeof(Event).GetProperty(
            nameof(Event.AvailableSeats),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (prop is null)
            throw new InvalidOperationException("Event.AvailableSeats not found.");

        if (prop.SetMethod is { IsPublic: true })
        {
            prop.SetValue(evt, value);
            return;
        }

        var field = typeof(Event).GetField(
            $"<{nameof(Event.AvailableSeats)}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field is null)
            throw new InvalidOperationException(
                "AvailableSeats has no public setter and no backing field — " +
                "задавай значение через доменный метод.");

        field.SetValue(evt, value);
    }
}