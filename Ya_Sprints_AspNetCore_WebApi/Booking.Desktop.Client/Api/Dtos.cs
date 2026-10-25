using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sprints.Desktop.Api;

// ============ AUTH ============

public sealed record LoginRequest
{
    [JsonPropertyName("login")] public string Login { get; init; } = "";
    [JsonPropertyName("password")] public string Password { get; init; } = "";
}

public sealed record RefreshRequest
{
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; init; } = "";
}

public sealed record TokenResponse
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("accessToken")] public string AccessToken { get; init; } = "";
    [JsonPropertyName("refreshToken")] public string RefreshToken { get; init; } = "";
    [JsonPropertyName("expiresAtUtc")] public DateTime ExpiresAtUtc { get; init; }
}

// ============ PAGINATION ============

public sealed class PaginatedResult<T>
{
    [JsonPropertyName("items")] public List<T> Items { get; set; } = new();
    [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("pageSize")] public int PageSize { get; set; }
    [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
}

// ============ USERS ============

public enum UserRole { User = 0, Admin = 1 }

public sealed class UserInfoDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("login")] public string Login { get; set; } = "";
    [JsonPropertyName("role")] public UserRole Role { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
}

// ============ EVENTS ============

public sealed class EventInfoDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("startAt")] public DateTime StartAt { get; set; }
    [JsonPropertyName("endAt")] public DateTime EndAt { get; set; }
    [JsonPropertyName("totalSeats")] public int TotalSeats { get; set; }
    [JsonPropertyName("availableSeats")] public int AvailableSeats { get; set; }
}

public sealed record CreateEventDto
{
    [JsonPropertyName("id")] public Guid Id { get; init; }
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("startAt")] public DateTime StartAt { get; init; }
    [JsonPropertyName("endAt")] public DateTime EndAt { get; init; }
    [JsonPropertyName("totalSeats")] public int? TotalSeats { get; init; }
}

// ============ BOOKINGS ============

public enum BookingStatus { Pending = 0, Confirmed = 1, Rejected = 2, Cancelled = 3 }

public sealed class BookingInfoDto
{
    [JsonPropertyName("id")] public Guid Id { get; set; }
    [JsonPropertyName("eventId")] public Guid EventId { get; set; }
    [JsonPropertyName("userId")] public Guid UserId { get; set; }
    [JsonPropertyName("status")] public BookingStatus Status { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("processedAt")] public DateTime? ProcessedAt { get; set; }
}