using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Services.Intercepts;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Sprints_Project_ASP_NetCore_API.Services.DataServices;
using System.Collections.Concurrent;
using Xunit;

namespace Tests
{
    /// <summary>
    /// Тесты для <see cref="BookingService"/>.
    /// Проверяют создание бронирований, изменение статусов, освобождение мест и конкурентное резервирование.
    /// </summary>
    public class BookingServiceTests
    {
        private readonly Mock<IRepository<IBooking>> _mockBookingRepo;
        private readonly Mock<IRepository<IEvent>> _mockEventRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BookingService>> _mockLogger_BookingService;
        private readonly Mock<ILogger<EventsService>> _mockLogger_EventsService;
        private readonly IInterceptLockings _interceptLockings;
        private readonly BookingService _bookingService;
        private readonly EventsService _eventService;

        public BookingServiceTests()
        {
            _mockBookingRepo = new Mock<IRepository<IBooking>>();
            _mockEventRepo = new Mock<IRepository<IEvent>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger_BookingService = new Mock<ILogger<BookingService>>();
            _mockLogger_EventsService = new Mock<ILogger<EventsService>>();
            _interceptLockings = new InterceptLockings();

            _bookingService = new BookingService(
                interceptLockings: _interceptLockings,
                repository: _mockBookingRepo.Object,
                eventRepository: _mockEventRepo.Object,
                logger: _mockLogger_BookingService.Object,
                mapper: _mockMapper.Object
            );

            _eventService = new EventsService(
                scopeFactory: null,
                interceptLockings: _interceptLockings,
                repository: _mockEventRepo.Object,
                logger: _mockLogger_EventsService.Object,
                mapper: _mockMapper.Object
            );
        }

        /// <summary>
        /// Вспомогательный метод для создания события с заданным количеством мест.
        /// Настраивает моки репозитория событий для возврата сущности и обновления её состояния.
        /// </summary>
        private async Task<Guid> CreateTestEvent(int totalSeats)
        {
            var eventId = Guid.NewGuid();
            var eventEntity = Event.Create( eventId, "Test", "Test", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), totalSeats);


          

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(ResultEntity<IEvent>.Ok(eventEntity, "Found"));

            _mockEventRepo.Setup(r => r.UpdateAsync(It.Is<IEvent>(e => e.Id == eventId)))
                .ReturnsAsync((IEvent e) =>
                {
                    // Обновляем состояние в замыкании – сохраняем переданную сущность
                    eventEntity = (Event)e;
                    return ResultEntity<IEvent>.Ok(e, "Updated");
                });

            return eventId;
        }

        // ===================== УСПЕШНЫЕ СЦЕНАРИИ =====================

        /// <summary>
        /// Проверяет, что создание брони уменьшает AvailableSeats на 1.
        /// </summary>
        [Fact]
        public async Task CreateBooking_DecreasesAvailableSeats_ByOne()
        {
            // Arrange
            var eventId = await CreateTestEvent(10);
            var initialSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;

            // Настройка AddAsync для брони
            var bookingId = Guid.NewGuid();
            var bookingEntity =   Booking.Create(  bookingId,   eventId,   BookingStatus.Pending , DateTime.UtcNow);
            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Created"));

            // Мапперы
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt)); 
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            // Act
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.True(result.IsSuccesfuly);
            var updatedSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(initialSeats - 1, updatedSeats);
        }

        /// <summary>
        /// Проверяет создание нескольких броней до лимита – все успешны, у каждой уникальный Id.
        /// </summary>
        [Fact]
        public async Task CreateMultipleBookings_UpToLimit_AllSucceed_AndHaveUniqueIds()
        {
            // Arrange
            var eventId = await CreateTestEvent(3);
            var ids = new HashSet<Guid>();

            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            // Act
            for (int i = 0; i < 3; i++)
            {
                var result = await _bookingService.CreateBookingAsync(eventId);
                Assert.True(result.IsSuccesfuly);
                ids.Add(result.Data.Id);
            }

            // Assert
            Assert.Equal(3, ids.Count);
            var finalSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(0, finalSeats);
        }

        /// <summary>
        /// Проверяет, что после исчерпания мест следующая попытка выбрасывает NoAvailableSeatsException.
        /// </summary>
        [Fact]
        public async Task CreateBooking_WhenSeatsExhausted_ThrowsNoAvailableSeatsException()
        {
            // Arrange
            var eventId = await CreateTestEvent(1);
            // Первая бронь успешна
            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            await _bookingService.CreateBookingAsync(eventId);

            // Act & Assert – вторая бронь должна выбросить исключение
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(eventId));
        }

        // ===================== НЕУСПЕШНЫЕ СЦЕНАРИИ =====================

        /// <summary>
        /// Проверяет, что бронирование для несуществующего события выбрасывает KeyNotFoundException.  
        /// </summary>
        [Fact]
        public async Task CreateBooking_ForNonExistentEvent_ThrowsKeyNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdAsync(invalidId))
                .ReturnsAsync(ResultEntity<IEvent>.Fail("Not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                _bookingService.CreateBookingAsync(invalidId));

            Assert.True(exception is KeyNotFoundException, $"Expected KeyNotFoundException, got {exception.GetType()}");

        }

        /// <summary>
        /// Проверяет, что бронирование при нулевом количестве мест выбрасывает NoAvailableSeatsException.
        /// </summary>
        [Fact]
        public async Task CreateBooking_WhenNoSeats_ThrowsNoAvailableSeatsException()
        {
            // Arrange – событие с 0 мест
            var eventId = await CreateTestEvent(0);

            // Act & Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(eventId));
        }

        // ===================== СТАТУСЫ БРОНИ (через сервис) =====================

        /// <summary>
        /// Проверяет переход брони в статус Confirmed и заполнение ProcessedAt.
        /// </summary>
        [Fact]
        public async Task UpdateBooking_Confirm_ChangesStatusAndSetsProcessedAt()
        {
            // Arrange
            var eventId = await CreateTestEvent(5);
            var bookingId = Guid.NewGuid();
            var bookingEntity = Booking.Create( bookingId, eventId, BookingStatus.Pending, DateTime.UtcNow);
            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Found"));
            _mockBookingRepo.Setup(r => r.UpdateAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) =>
                {
                    bookingEntity = (Booking)b;
                    return ResultEntity<IBooking>.Ok(bookingEntity, "Updated");
                });
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status, ProcessedAt = entity.ProcessedAt });

            var bookingDto = new BookingInfoDto { Id = bookingId, EventId = eventId, Status = BookingStatus.Pending };
            bookingDto.Status = BookingStatus.Confirmed;
            bookingDto.ProcessedAt = DateTime.UtcNow;

            // Act
            var updateResult = await _bookingService.UpdateBookingAsync(bookingDto);

            // Assert
            Assert.True(updateResult.IsSuccesfuly);
            Assert.Equal(BookingStatus.Confirmed, updateResult.Data.Status);
            Assert.NotNull(updateResult.Data.ProcessedAt);
        }

        /// <summary>
        /// Проверяет переход брони в статус Rejected и заполнение ProcessedAt.
        /// </summary>
        [Fact]
        public async Task UpdateBooking_Reject_ChangesStatusAndSetsProcessedAt()
        {
            // Arrange
            var eventId = await CreateTestEvent(5);
            var bookingId = Guid.NewGuid();
            var bookingEntity = Booking.Create(bookingId, eventId, BookingStatus.Pending, DateTime.UtcNow);
            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Found"));
            _mockBookingRepo.Setup(r => r.UpdateAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) =>
                {
                    bookingEntity = (Booking)b;
                    return ResultEntity<IBooking>.Ok(bookingEntity, "Updated");
                });
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status, ProcessedAt = entity.ProcessedAt });

            var bookingDto = new BookingInfoDto { Id = bookingId, EventId = eventId, Status = BookingStatus.Pending };
            bookingDto.Status = BookingStatus.Rejected;
            bookingDto.ProcessedAt = DateTime.UtcNow;

            // Act
            var updateResult = await _bookingService.UpdateBookingAsync(bookingDto);

            // Assert
            Assert.True(updateResult.IsSuccesfuly);
            Assert.Equal(BookingStatus.Rejected, updateResult.Data.Status);
            Assert.NotNull(updateResult.Data.ProcessedAt);
        }

        /// <summary>
        /// Проверяет, что после Reject() количество свободных мест восстанавливается.
        /// </summary>
        [Fact]
        public async Task RejectBooking_ReleasesSeat()
        {
            // Arrange
            var eventId = await CreateTestEvent(3);
            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            var bookingResult = await _bookingService.CreateBookingAsync(eventId);
            var initialAfterCreate = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(2, initialAfterCreate);

            // Act – освобождаем место через eventService (имитируем отклонение в фоновом сервисе)
            await _eventService.ReleaseSeatsAndUpdateAsync(eventId, 1);

            // Assert
            var finalSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(3, finalSeats);
        }

        /// <summary>
        /// Проверяет, что после Reject() можно успешно создать новую бронь на то же место.
        /// </summary>
        [Fact]
        public async Task AfterReject_CanCreateNewBooking()
        {
            // Arrange
            var eventId = await CreateTestEvent(1);
            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            var booking1 = await _bookingService.CreateBookingAsync(eventId);
            Assert.True(booking1.IsSuccesfuly);

            // Освобождаем место
            await _eventService.ReleaseSeatsAndUpdateAsync(eventId, 1);

            // Act
            var booking2 = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.True(booking2.IsSuccesfuly);
            Assert.NotEqual(booking1.Data.Id, booking2.Data.Id);
        }

        // ===================== КОНКУРЕНТНОСТЬ =====================

        /// <summary>
        /// Проверяет защиту от овербукинга:
        /// 20 конкурентных запросов на событие с 5 местами – ровно 5 успешных броней, 15 исключений, AvailableSeats = 0.
        /// </summary>
        [Fact]
        public async Task ConcurrentBookings_20Requests_5Seats_Exactly5Success_15Exceptions_AvailableSeatsZero()
        {
            // Arrange
            var eventId = await CreateTestEvent(5);
            var successCount = 0;
            var exceptionCount = 0;
            var lockObj = new object();

            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            var tasks = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var result = await _bookingService.CreateBookingAsync(eventId);
                        if (result.IsSuccesfuly)
                        {
                            lock (lockObj) successCount++;
                        }
                    }
                    catch (NoAvailableSeatsException)
                    {
                        lock (lockObj) exceptionCount++;
                    }
                }));
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(5, successCount);
            Assert.Equal(15, exceptionCount);
            var finalSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(0, finalSeats);
        }

        /// <summary>
        /// Проверяет уникальность Id при конкурентных запросах.
        /// 10 одновременных запросов на событие с 10 местами – все успешны, все Id уникальны.
        /// </summary>
        [Fact]
        public async Task ConcurrentBookings_10Requests_10Seats_AllUniqueIds()
        {
            // Arrange
            var eventId = await CreateTestEvent(10);
            var ids = new ConcurrentBag<Guid>();

            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));
            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto { Id = entity.Id, EventId = entity.EventId, Status = entity.Status });

            var tasks = new List<Task<IResultDto<IBookingInfoDto>>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_bookingService.CreateBookingAsync(eventId));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            var distinctIds = results.Select(r => r.Data.Id).Distinct().Count();
            Assert.Equal(10, distinctIds);
            var finalSeats = (await _mockEventRepo.Object.GetByIdAsync(eventId)).Data.AvailableSeats;
            Assert.Equal(0, finalSeats);
        }

        // ===================== ДОПОЛНИТЕЛЬНЫЕ ТЕСТЫ =====================

        /// <summary>
        /// Проверяет успешное создание брони для существующего события с доступными местами.
        /// </summary>
        [Fact]
        public async Task CreateBookingAsync_WithExistingEvent_ReturnsPendingBooking()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity   = Event.Create(eventId, "TestTitle", null, DateTime.Now, DateTime.Now.AddDays(2), 5); 
            var bookingEntity = Booking.Create(Guid.NewGuid(), eventId, BookingStatus.Pending, DateTime.UtcNow);
            
            

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(() => ResultEntity<IEvent>.Ok(eventEntity, "Found"));

            _mockEventRepo.Setup(r => r.UpdateAsync(It.IsAny<IEvent>()))
                .ReturnsAsync((IEvent e) =>
                {
                    eventEntity.AvailableSeats = e.AvailableSeats;
                    return ResultEntity<IEvent>.Ok(e, "Updated");
                });

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));

            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status
                });

            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Created"));

            // Act
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.True(result.IsSuccesfuly);
            Assert.NotNull(result.Data);
            Assert.Equal(BookingStatus.Pending, result.Data.Status);
            Assert.Equal(eventId, result.Data.EventId);
            _mockEventRepo.Verify(r => r.GetByIdAsync(eventId), Times.Once);
            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<IBooking>()), Times.Once);
        }

        /// <summary>
        /// Проверяет, что несколько броней для одного события имеют уникальные Id.
        /// </summary>
        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = Event.Create(eventId, "TestTitle", null, DateTime.Now, DateTime.Now.AddDays(10), 10);

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(() => ResultEntity<IEvent>.Ok(eventEntity, "Found"));

            _mockEventRepo.Setup(r => r.UpdateAsync(It.IsAny<IEvent>()))
                .ReturnsAsync((IEvent e) =>
                {
                    eventEntity.AvailableSeats = e.AvailableSeats;
                    return ResultEntity<IEvent>.Ok(e, "Updated");
                });

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));

            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status
                });

            _mockBookingRepo.Setup(r => r.AddAsync(It.IsAny<IBooking>()))
                .ReturnsAsync((IBooking b) => ResultEntity<IBooking>.Ok(b, "Created"));

            // Act
            var result1 = await _bookingService.CreateBookingAsync(eventId);
            var result2 = await _bookingService.CreateBookingAsync(eventId);
            var result3 = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            var ids = new[] { result1.Data.Id, result2.Data.Id, result3.Data.Id };
            Assert.Equal(3, ids.Distinct().Count());
        }

        /// <summary>
        /// Проверяет получение брони по её Id.
        /// </summary>
        [Fact]
        public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBooking()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var bookingEntity = Booking.Create(bookingId, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);

            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Found"));

            _mockMapper.Setup(m => m.Map<IBookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status,
                    CreatedAt = entity.CreatedAt
                });

            // Act
            var result = await _bookingService.GetBookingByIdAsync(bookingId);

            // Assert
            Assert.True(result.IsSuccesfuly);
            Assert.NotNull(result.Data);
            Assert.Equal(bookingId, result.Data.Id);
            Assert.Equal(bookingEntity.EventId, result.Data.EventId);
            Assert.Equal(bookingEntity.Status, result.Data.Status);
        }

        /// <summary>
        /// Проверяет, что изменение статуса брони отражается при повторном получении.
        /// </summary>
        [Fact]
        public async Task GetBookingByIdAsync_AfterStatusChange_ReflectsUpdatedStatus()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var bookingEntity = Booking.Create(bookingId, Guid.NewGuid(), BookingStatus.Pending, DateTime.UtcNow);
             
            var currentBooking = bookingEntity;

            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(() => ResultEntity<IBooking>.Ok(currentBooking, "Found"));

            _mockBookingRepo.Setup(r => r.UpdateAsync(It.Is<IBooking>(b => b.Id == bookingId)))
                .Callback<IBooking>(b => currentBooking = (Booking)b)
                .ReturnsAsync(() => ResultEntity<IBooking>.Ok(currentBooking, "Updated"));

            _mockMapper.Setup(m => m.Map<IBookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status,
                    CreatedAt = entity.CreatedAt
                });

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => Booking.Create(dto.Id, dto.EventId, dto.Status, dto.CreatedAt, dto.ProcessedAt));

            var updateDto = new BookingInfoDto
            {
                Id = bookingId,
                EventId = bookingEntity.EventId,
                Status = BookingStatus.Confirmed,
                CreatedAt = bookingEntity.CreatedAt
            };

            // Act
            var initial = await _bookingService.GetBookingByIdAsync(bookingId);
            await _bookingService.UpdateBookingAsync(updateDto);
            var afterConfirm = await _bookingService.GetBookingByIdAsync(bookingId);

            // Assert
            Assert.Equal(BookingStatus.Pending, initial.Data.Status);
            Assert.Equal(BookingStatus.Confirmed, afterConfirm.Data.Status);
        }

        /// <summary>
        /// Проверяет, что бронирование для несуществующего события возвращает ошибку. 
        /// </summary>
        [Fact]
        public async Task CreateBookingAsync_WithNonExistentEvent_ReturnsFail()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                _bookingService.CreateBookingAsync(eventId));

            Assert.True(exception is KeyNotFoundException,
                $"Expected KeyNotFoundException , got {exception.GetType()}");

            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<IBooking>()), Times.Never);
        }

        /// <summary>
        /// Проверяет, что бронирование для удалённого (несуществующего) события возвращает ошибку.
        /// </summary>
        [Fact]
        public async Task CreateBookingAsync_WithDeletedEvent_ReturnsFail()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                _bookingService.CreateBookingAsync(eventId));

            Assert.True(exception is KeyNotFoundException, $"Expected KeyNotFoundException, got {exception.GetType()}");

            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<IBooking>()), Times.Never);
        }

        /// <summary>
        /// Проверяет, что получение брони по несуществующему Id выбрасывает KeyNotFoundException.
        /// </summary>
        [Fact]
        public async Task GetBookingByIdAsync_WithNonExistentId_ReturnsFail()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ThrowsAsync(new KeyNotFoundException("Booking not found"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _bookingService.GetBookingByIdAsync(bookingId));
            Assert.Equal("Booking not found", ex.Message);
        }
    }
}