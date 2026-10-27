using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Domain.Entities;
using AutoMapper;
using Moq;


namespace Sprints.Events.UnitTests.TestData;


internal sealed class EventsServiceMocks
{

    public Mock<IRepository<Event>> Repository { get; } = new();
    public Mock<IEventCache> Cache { get; } = new();
    public Mock<IInterceptLockings> Locks { get; } = new();
    public Mock<IMapper> Mapper { get; } = new();
    public Mock<ITransaction> Transaction { get; } = new();

    public EventsServiceMocks()
    {
        Locks.Setup(l => l.GetOrAddByEventId(It.IsAny<Guid>()))
             .Returns(() => new SemaphoreSlim(1, 1));

        Repository
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Transaction.Object);

        Transaction
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Transaction
            .Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Transaction
            .Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        // single: Map<EventInfoDto>(Event)  
        Mapper
            .Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
            .Returns<Event>(e => EventBuilder.CreateDto(
                id: e.Id,
                title: e.Title,
                description: e.Description,
                startAt: e.StartAt,
                endAt: e.EndAt,
                totalSeats: e.TotalSeats,
                availableSeats: e.AvailableSeats));

        // reverse: Map<Event>(EventInfoDto)  
        Mapper
            .Setup(m => m.Map<Event>(It.IsAny<EventInfoDto>()))
            .Returns<EventInfoDto>(d => EventBuilder.Create(
                id: d.Id,
                title: d.Title,
                description: d.Description,
                totalSeats: d.TotalSeats,
                availableSeats: d.AvailableSeats));


        Mapper
            .Setup(m => m.Map<List<EventInfoDto>>(It.IsAny<object>()))
            .Returns<object>(src => ((IEnumerable<Event>)src)
                .Select(e => EventBuilder.CreateDto(
                    id: e.Id,
                    title: e.Title,
                    description: e.Description,
                    startAt: e.StartAt,
                    endAt: e.EndAt,
                    totalSeats: e.TotalSeats,
                    availableSeats: e.AvailableSeats))
                .ToList());
    }
}  

internal sealed class BookingRequestedHandlerMocks
{
    public Mock<IRepository<Event>> Events { get; } = new();
    public Mock<IRepository<InboxMessage>> Inbox { get; } = new();
    public Mock<IOutboxWriter> Outbox { get; } = new();
    public Mock<IInterceptLockings> Locks { get; } = new();
    public Mock<IEventCache> Cache { get; } = new();
    public Mock<ITransaction> Transaction { get; } = new();

    public BookingRequestedHandlerMocks()
    {
        Locks.Setup(l => l.GetOrAddByEventId(It.IsAny<Guid>()))
             .Returns(() => new SemaphoreSlim(1, 1));

        Events
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Transaction.Object);

        Transaction
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Transaction
            .Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Transaction
            .Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);
    }
}