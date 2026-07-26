using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Data.Entities;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Xunit;
using Moq;


namespace Tests
{
    public class BookingServiceTests
    {
        private readonly Mock<IRepository<IBooking>> _mockBookingRepo;
        private readonly Mock<IRepository<IEvent>> _mockEventRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<BookingService>> _mockLogger;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _mockBookingRepo = new Mock<IRepository<IBooking>>();
            _mockEventRepo = new Mock<IRepository<IEvent>>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<BookingService>>();

            _bookingService = new BookingService( 
                repository: _mockBookingRepo.Object,
                eventRepository: _mockEventRepo.Object,
                logger: _mockLogger.Object,
                mapper: _mockMapper.Object
            );
        } 

           
        // ===================== Успешные сценарии =====================

        [Fact]
        public async Task CreateBookingAsync_WithExistingEvent_ReturnsPendingBooking()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Title = "TestTitle",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(2)
            };

            var bookingEntity = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var bookingDto = new BookingInfoDto
            {
                Id = bookingEntity.Id,
                EventId = bookingEntity.EventId,
                Status = bookingEntity.Status
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(ResultEntity<IEvent>.Ok(eventEntity, "Found"));

            // Настройка маппера: DTO → сущность
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => new Booking
                {
                    Id = dto.Id,
                    EventId = dto.EventId,
                    Status = dto.Status
                });

            // Настройка маппера: сущность → DTO (для результата)
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

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_AllHaveUniqueIds()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var eventEntity = new Event
            {
                Id = eventId,
                Title = "TestTitle",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(10)
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(ResultEntity<IEvent>.Ok(eventEntity, "Found"));

            // Настройка маппера
            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => new Booking
                {
                    Id = dto.Id,
                    EventId = dto.EventId,
                    Status = dto.Status
                });

            _mockMapper.Setup(m => m.Map<BookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status
                });

            // При каждом вызове AddAsync возвращаем ту же сущность, но с уникальным Id
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

        [Fact]
        public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBooking()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var bookingEntity = new Booking
            {
                Id = bookingId,
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(ResultEntity<IBooking>.Ok(bookingEntity, "Found"));

            // Настройка маппера: IBooking -> IBookingInfoDto (возвращаем BookingInfoDto)
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

        [Fact]
        public async Task GetBookingByIdAsync_AfterStatusChange_ReflectsUpdatedStatus()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var bookingEntity = new Booking
            {
                Id = bookingId,
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var updatedBookingEntity = new Booking
            {
                Id = bookingId,
                EventId = bookingEntity.EventId,
                Status = BookingStatus.Confirmed,
                CreatedAt = bookingEntity.CreatedAt
            };

            var updateDto = new BookingInfoDto
            {
                Id = bookingId,
                EventId = bookingEntity.EventId,
                Status = BookingStatus.Confirmed,
                CreatedAt = bookingEntity.CreatedAt
            };

            // Текущее состояние (будет меняться)
            var currentBooking = bookingEntity;

            // Настройка GetByIdAsync возвращать текущее состояние
            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(() => ResultEntity<IBooking>.Ok(currentBooking, "Found"));

            // Настройка UpdateAsync: обновляем currentBooking и возвращаем его
            _mockBookingRepo.Setup(r => r.UpdateAsync(It.Is<IBooking>(b => b.Id == bookingId)))
                .Callback<IBooking>(b => currentBooking = (Booking)b) // сохраняем обновлённую сущность
                .ReturnsAsync(() => ResultEntity<IBooking>.Ok(currentBooking, "Updated"));

            // Мапперы...
            _mockMapper.Setup(m => m.Map<IBookingInfoDto>(It.IsAny<IBooking>()))
                .Returns((IBooking entity) => new BookingInfoDto
                {
                    Id = entity.Id,
                    EventId = entity.EventId,
                    Status = entity.Status,
                    CreatedAt = entity.CreatedAt
                });

            _mockMapper.Setup(m => m.Map<IBooking>(It.IsAny<IBookingInfoDto>()))
                .Returns((IBookingInfoDto dto) => new Booking
                {
                    Id = dto.Id,
                    EventId = dto.EventId,
                    Status = dto.Status,
                    CreatedAt = dto.CreatedAt 
                });

            // Act
            var initial = await _bookingService.GetBookingByIdAsync(bookingId);
            await _bookingService.UpdateBookingAsync(updateDto);
            var afterConfirm = await _bookingService.GetBookingByIdAsync(bookingId);

            // Assert
            Assert.Equal(BookingStatus.Pending, initial.Data.Status);
            Assert.Equal(BookingStatus.Confirmed, afterConfirm.Data.Status);
        }
        // ===================== Неуспешные сценарии =====================

        [Fact]
        public async Task CreateBookingAsync_WithNonExistentEvent_ReturnsFail()
        {
            // Arrange
            var eventId = Guid.NewGuid();

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(ResultEntity<IEvent>.Fail("Event not found"));

            // Act
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.False(result.IsSuccesfuly);
            Assert.Equal("Event not found", result.Reason);
            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<IBooking>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_WithDeletedEvent_ReturnsFail()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var deletedEvent = new Event
            {
                Id = eventId,
                Title = "TestTitle",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(10), 
            };

            _mockEventRepo.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(ResultEntity<IEvent>.Fail("Event not found"));

            // Act
            var result = await _bookingService.CreateBookingAsync(eventId);

            // Assert
            Assert.False(result.IsSuccesfuly);
            Assert.Equal("Event not found", result.Reason);
            _mockBookingRepo.Verify(r => r.AddAsync(It.IsAny<IBooking>()), Times.Never);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithNonExistentId_ReturnsFail()
        {
            // Arrange
            var bookingId = Guid.NewGuid();

            _mockBookingRepo.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(ResultEntity<IBooking>.Fail("Booking not found"));

            // Act
            var result = await _bookingService.GetBookingByIdAsync(bookingId);

            // Assert
            Assert.False(result.IsSuccesfuly);
            Assert.Equal("Booking not found", result.Reason);
        }
    }
}