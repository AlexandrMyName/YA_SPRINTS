using Refit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sprints.Desktop.Api;

public interface IAuthApi
{
    [Post("/api/v1/auth/login")]
    Task<TokenResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);

    [Post("/api/v1/auth/refresh")]
    Task<TokenResponse> RefreshAsync([Body] RefreshRequest request, CancellationToken ct = default);
}

public interface IUsersApi
{
    [Get("/api/v1/users")]
    Task<PaginatedResult<UserInfoDto>> GetUsersAsync(
        [Query] int page = 1, [Query] int pageSize = 100, CancellationToken ct = default);

    [Post("/api/v1/users/{login}/promote")]
    Task<UserInfoDto> PromoteAsync([AliasAs("login")] string login, CancellationToken ct = default);

    [Post("/api/v1/users/{login}/demote")]
    Task<UserInfoDto> DemoteAsync([AliasAs("login")] string login, CancellationToken ct = default);
}

public interface IEventsApi
{
    [Get("/api/v1/events")]
    Task<PaginatedResult<EventInfoDto>> GetEventsAsync(
        [Query] int page = 1, [Query] int pageSize = 100, CancellationToken ct = default);

    // Топ-10 популярных событий. На сервере кешируется в Redis (TTL 60 c).
    [Get("/api/v1/events/top")]
    Task<List<EventInfoDto>> GetTopEventsAsync(
        [Query] int take = 10, CancellationToken ct = default);

    [Post("/api/v1/events")]
    Task<string> CreateEventAsync([Body] CreateEventDto dto, CancellationToken ct = default);

    [Put("/api/v1/events/{id}")]
    Task<string> UpdateEventAsync(
        [AliasAs("id")] Guid id,
        [Body] EventInfoDto dto,
        CancellationToken ct = default);

    [Delete("/api/v1/events/{id}")]
    Task<string> DeleteEventAsync([AliasAs("id")] Guid id, CancellationToken ct = default);
}

public interface IBookingsApi
{
    [Get("/api/v1/bookings")]
    Task<PaginatedResult<BookingInfoDto>> GetBookingsAsync(
        [Query] int page = 1, [Query] int pageSize = 100, CancellationToken ct = default);

    [Post("/api/v1/bookings/events/{eventId}")]
    Task<BookingInfoDto> CreateBookingAsync(
        [AliasAs("eventId")] Guid eventId,
        CancellationToken ct = default);

    [Delete("/api/v1/bookings/{id}")]
    Task<BookingInfoDto> CancelBookingAsync([AliasAs("id")] Guid id, CancellationToken ct = default);
}