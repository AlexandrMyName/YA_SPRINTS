using CommunityToolkit.Mvvm.ComponentModel;
using Sprints.Desktop.Api; 


namespace Booking.Desktop.Client.ViewModels;


public partial class UserCardVm : ObservableObject
{
    public UserInfoDto Source { get; }
    public UserCardVm(UserInfoDto dto) => Source = dto;

    [ObservableProperty] private bool _isSelected;

    public string Login => Source.Login;
    public bool IsAdmin => Source.Role == UserRole.Admin;
    public string RoleLabel => Source.Role.ToString();
    public string CreatedLabel => Source.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public partial class EventCardVm : ObservableObject
{
    public EventInfoDto Source { get; }
    public EventCardVm(EventInfoDto dto) => Source = dto;

    [ObservableProperty] private bool _isSelected;

    public string Title => Source.Title;
    public string Description => string.IsNullOrWhiteSpace(Source.Description) ? "—" : Source.Description;
    public string WhenLabel => $"{Source.StartAt.ToLocalTime():yyyy-MM-dd HH:mm} → {Source.EndAt.ToLocalTime():HH:mm}";
    public string SeatsLabel => $"Мест: {Source.AvailableSeats} / {Source.TotalSeats}";
}

public partial class BookingCardVm : ObservableObject
{
    public BookingInfoDto Source { get; }
    public BookingCardVm(BookingInfoDto dto) => Source = dto;

    [ObservableProperty] private bool _isSelected;

    public string ShortId => Source.Id.ToString()[..8];
    public string EventLabel => $"Event: {Source.EventId.ToString()[..8]}";
    public string UserLabel => $"User:  {Source.UserId.ToString()[..8]}";
    public string CreatedLabel => Source.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string StatusLabel => Source.Status.ToString();

    public bool IsPending => Source.Status == BookingStatus.Pending;
    public bool IsConfirmed => Source.Status == BookingStatus.Confirmed;
    public bool IsRejected => Source.Status == BookingStatus.Rejected;
    public bool IsCancelled => Source.Status == BookingStatus.Cancelled;
}
