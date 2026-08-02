using Sprints_Project_ASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Xunit;
using Moq;
 

namespace Tests;

/// <summary>
/// Тесты для <see cref="EventsService"/>.
/// Проверяют CRUD-операции, фильтрацию, сортировку и пагинацию событий.
/// </summary>
public class Tests_EventsService
{

    private readonly Mock<IRepository<IEvent>> _mockRepository;
    private readonly Mock<ILogger<EventsService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly EventsService _service;
    private readonly Event _testEvent;
    private readonly EventInfoDto _testEventDto;
    private readonly List<IEvent> _testEvents;


    public Tests_EventsService()
    {
        _mockRepository = new Mock<IRepository<IEvent>>();
        _mockLogger = new Mock<ILogger<EventsService>>();
        _mockMapper = new Mock<IMapper>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _service = new EventsService(
            _mockScopeFactory.Object,
            _mockRepository.Object,
            _mockLogger.Object,
            _mockMapper.Object
        );

        _testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        _testEventDto = new EventInfoDto
        {
            Id = _testEvent.Id,
            Title = _testEvent.Title,
            Description = _testEvent.Description,
            StartAt = _testEvent.StartAt,
            EndAt = _testEvent.EndAt
        };

        // Тестовые данные для сортировки и пагинации
        _testEvents = new List<IEvent>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Alpha Event",
                Description = "First event",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now.AddDays(-1)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Beta Event",
                Description = "Second event",
                StartAt = DateTime.Now.AddDays(-1),
                EndAt = DateTime.Now.AddDays(0)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Gamma Event",
                Description = "Third event",
                StartAt = DateTime.Now.AddDays(0),
                EndAt = DateTime.Now.AddDays(1)
            }
        };
    }

    #region GetAllAsync Tests

    /// <summary>
    /// Проверяет, что GetAllAsync возвращает все события из репозитория.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents()
    {
        // Arrange
        var events = new List<IEvent> { _testEvent };
        var expectedDtos = new List<EventInfoDto> { _testEventDto };

        _mockRepository.Setup(r => r.GetAllAsync())
                       .ReturnsAsync(events);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(_testEvent))
                   .Returns(_testEventDto);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(_testEventDto.Id, result.First().Id);
        Assert.Equal(_testEventDto.Title, result.First().Title);

        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetAllAsync возвращает пустой список, если репозиторий пуст.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var events = new List<IEvent>();
        _mockRepository.Setup(r => r.GetAllAsync())
                      .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает событие по валидному ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEvent()
    {
        // Arrange
        var id = _testEvent.Id;
        var resultEntity = ResultEntity<IEvent>.Ok(_testEvent, "Success");

        _mockRepository.Setup(r => r.GetByIdAsync(id))
                      .ReturnsAsync(resultEntity);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(_testEvent))
                  .Returns(_testEventDto);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.NotNull(result.Data);
        Assert.Equal(_testEventDto.Id, result.Data.Id);
        Assert.Equal(_testEventDto.Title, result.Data.Title);

        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    /// <summary>
    /// Проверяет, что GetByIdAsync возвращает ошибку для несуществующего ID.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnFail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultEntity = ResultEntity<IEvent>.Fail("Event not found");

        _mockRepository.Setup(r => r.GetByIdAsync(id))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccesfuly);
        Assert.Null(result.Data);
        Assert.Equal("Event not found", result.Reason);

        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region AddAsync Tests

    /// <summary>
    /// Проверяет, что AddAsync успешно добавляет событие.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidDto_ShouldAddEvent()
    {
        // Arrange
        var newEventDto = new EventInfoDto
        {
            Id = Guid.NewGuid(),
            Title = "New Event",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        var newEvent = new Event
        {
            Id = newEventDto.Id,
            Title = newEventDto.Title,
            StartAt = newEventDto.StartAt,
            EndAt = newEventDto.EndAt
        };

        var resultEntity = ResultEntity<IEvent>.Ok(newEvent, "Added successfully");

        _mockMapper.Setup(m => m.Map<Event>(newEventDto))
                  .Returns(newEvent);

        _mockRepository.Setup(r => r.AddAsync(newEvent))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.AddAsync(newEventDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("Added successfully", result.Message);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<IEvent>()), Times.Once);
        _mockMapper.Verify(m => m.Map<Event>(newEventDto), Times.Once);
    }

    /// <summary>
    /// Проверяет, что AddAsync возвращает ошибку при неудачном добавлении.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenAddFails_ShouldReturnFail()
    {
        // Arrange
        var newEventDto = new EventInfoDto
        {
            Id = Guid.NewGuid(),
            Title = "New Event",
            StartAt = DateTime.Now,
            EndAt = DateTime.Now.AddHours(1)
        };

        var newEvent = new Event
        {
            Id = newEventDto.Id,
            Title = newEventDto.Title,
            StartAt = newEventDto.StartAt,
            EndAt = newEventDto.EndAt
        };

        var resultEntity = ResultEntity<IEvent>.Fail("Failed to add event");

        _mockMapper.Setup(m => m.Map<Event>(newEventDto))
                  .Returns(newEvent);

        _mockRepository.Setup(r => r.AddAsync(newEvent))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.AddAsync(newEventDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccesfuly);
        Assert.Equal("Failed to add event", result.Reason);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<IEvent>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    /// <summary>
    /// Проверяет, что UpdateAsync успешно обновляет событие.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithValidDto_ShouldUpdateEvent()
    {
        // Arrange
        var updatedEventDto = new EventInfoDto
        {
            Id = _testEventDto.Id,
            Title = "Updated Event",
            StartAt = _testEventDto.StartAt,
            EndAt = _testEventDto.EndAt
        };

        var updatedEvent = new Event
        {
            Id = updatedEventDto.Id,
            Title = updatedEventDto.Title,
            StartAt = updatedEventDto.StartAt,
            EndAt = updatedEventDto.EndAt
        };

        var resultEntity = ResultEntity<IEvent>.Ok(updatedEvent, "Updated successfully");

        _mockMapper.Setup(m => m.Map<Event>(updatedEventDto))
                  .Returns(updatedEvent);

        _mockRepository.Setup(r => r.UpdateAsync(updatedEvent))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.UpdateAsync(updatedEventDto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("Updated successfully", result.Message);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<IEvent>()), Times.Once);
    }

    /// <summary>
    /// Проверяет, что UpdateAsync возвращает ошибку при неудачном обновлении.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenUpdateFails_ShouldReturnFail()
    {
        // Arrange
        var updatedEventDto = new EventInfoDto
        {
            Id = _testEventDto.Id,
            Title = "Updated Event",
            StartAt = _testEventDto.StartAt,
            EndAt = _testEventDto.EndAt
        };

        var updatedEvent = new Event
        {
            Id = updatedEventDto.Id,
            Title = updatedEventDto.Title,
            StartAt = updatedEventDto.StartAt,
            EndAt = updatedEventDto.EndAt
        };

        var resultEntity = ResultEntity<IEvent>.Fail("Failed to update event");

        _mockMapper.Setup(m => m.Map<Event>(updatedEventDto))
                  .Returns(updatedEvent);

        _mockRepository.Setup(r => r.UpdateAsync(updatedEvent))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.UpdateAsync(updatedEventDto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccesfuly);
        Assert.Equal("Failed to update event", result.Reason);
    }

    #endregion

    #region DeleteAsync Tests

    /// <summary>
    /// Проверяет, что DeleteAsync успешно удаляет событие по ID.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldDeleteEvent()
    {
        // Arrange
        var id = _testEvent.Id;
        var resultEntity = ResultEntity<IEvent>.Ok("Deleted successfully");

        _mockRepository.Setup(r => r.DeleteAsync(id))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("Deleted successfully", result.Message);

        _mockRepository.Verify(r => r.DeleteAsync(id), Times.Once);
    }

    /// <summary>
    /// Проверяет, что DeleteAsync возвращает ошибку при неудачном удалении.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenDeleteFails_ShouldReturnFail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var resultEntity = ResultEntity<IEvent>.Fail("Failed to delete event");

        _mockRepository.Setup(r => r.DeleteAsync(id))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccesfuly);
        Assert.Equal("Failed to delete event", result.Reason);
    }

    #endregion

    #region AddRangeAsync Tests

    /// <summary>
    /// Проверяет, что AddRangeAsync успешно добавляет коллекцию событий.
    /// </summary>
    [Fact]
    public async Task AddRangeAsync_WithValidDtos_ShouldAddAllEvents()
    {
        // Arrange
        var eventDtos = new List<EventInfoDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Event 1", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "Event 2", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) }
        };

        var events = eventDtos.Select(d => new Event
        {
            Id = d.Id,
            Title = d.Title,
            StartAt = d.StartAt,
            EndAt = d.EndAt
        }).ToList();

        var resultEntity = ResultEntity<IEvent>.Ok("Added successfully");

        _mockMapper.Setup(m => m.Map<Event>(It.IsAny<EventInfoDto>()))
                  .Returns((EventInfoDto dto) => new Event
                  {
                      Id = dto.Id,
                      Title = dto.Title,
                      StartAt = dto.StartAt,
                      EndAt = dto.EndAt
                  });

        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<IEvent>>()))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.AddRangeAsync(eventDtos);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("Added successfully", result.Message);

        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<IEvent>>()), Times.Once);
    }

    #endregion

    #region UpdateRangeAsync Tests

    /// <summary>
    /// Проверяет, что UpdateRangeAsync успешно обновляет коллекцию событий.
    /// </summary>
    [Fact]
    public async Task UpdateRangeAsync_WithValidDtos_ShouldUpdateAllEvents()
    {
        // Arrange
        var eventDtos = new List<EventInfoDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Updated Event 1", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) },
            new() { Id = Guid.NewGuid(), Title = "Updated Event 2", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) }
        };

        var events = eventDtos.Select(d => new Event
        {
            Id = d.Id,
            Title = d.Title,
            StartAt = d.StartAt,
            EndAt = d.EndAt
        }).ToList();

        var resultEntity = ResultEntity<IEvent>.Ok("Updated successfully");

        _mockMapper.Setup(m => m.Map<Event>(It.IsAny<EventInfoDto>()))
                  .Returns((EventInfoDto dto) => new Event
                  {
                      Id = dto.Id,
                      Title = dto.Title,
                      StartAt = dto.StartAt,
                      EndAt = dto.EndAt
                  });

        _mockRepository.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<IEvent>>()))
                      .ReturnsAsync(resultEntity);

        // Act
        var result = await _service.UpdateRangeAsync(eventDtos);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("Updated successfully", result.Message);

        _mockRepository.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<IEvent>>()), Times.Once);
    }

    #endregion

    #region IsExisted Tests

    /// <summary>
    /// Проверяет, что IsExisted возвращает true для существующего ID.
    /// </summary>
    [Fact]
    public void IsExisted_WithValidId_ShouldReturnTrue()
    {
        // Arrange
        var id = _testEvent.Id;
        _mockRepository.Setup(r => r.IsExisted(id)).Returns(true);

        // Act
        var result = _service.IsExisted(id);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.IsExisted(id), Times.Once);
    }

    /// <summary>
    /// Проверяет, что IsExisted возвращает false для несуществующего ID.
    /// </summary>
    [Fact]
    public void IsExisted_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.IsExisted(id)).Returns(false);

        // Act
        var result = _service.IsExisted(id);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.IsExisted(id), Times.Once);
    }

    #endregion

    #region IsExistedByTitle Tests

    /// <summary>
    /// Проверяет, что IsExistedByTitle возвращает true для существующего названия.
    /// </summary>
    [Fact]
    public void IsExistedByTitle_WithValidTitle_ShouldReturnTrue()
    {
        // Arrange
        var title = "Test Event";
        _mockRepository.Setup(r => r.IsExistedByTitle(title)).Returns(true);

        // Act
        var result = _service.IsExistedByTitle(title);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.IsExistedByTitle(title), Times.Once);
    }

    /// <summary>
    /// Проверяет, что IsExistedByTitle возвращает false для несуществующего названия.
    /// </summary>
    [Fact]
    public void IsExistedByTitle_WithInvalidTitle_ShouldReturnFalse()
    {
        // Arrange
        var title = "Non-existent Event";
        _mockRepository.Setup(r => r.IsExistedByTitle(title)).Returns(false);

        // Act
        var result = _service.IsExistedByTitle(title);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.IsExistedByTitle(title), Times.Once);
    }

    #endregion

    #region GetFilteredAsync Tests - Filtering

    /// <summary>
    /// Проверяет фильтрацию по названию (Title) с частичным совпадением (contains).
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_ShouldReturnFilteredEvents()
    {
        // Arrange
        var filter = new EventFilterDto { Title = "Test" };
        var events = new List<IEvent> { _testEvent };
        var expectedDtos = new List<EventInfoDto> { _testEventDto };

        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(_testEvent))
                  .Returns(_testEventDto);

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(_testEventDto.Id, result.Items.First().Id);
        Assert.Equal(_testEventDto.Title, result.Items.First().Title);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    /// <summary>
    /// Проверяет фильтрацию по дате начала (From): возвращает события, начинающиеся после указанной даты.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithDateFromFilter_ShouldReturnEventsAfterDate()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-3);
        var filter = new EventFilterDto { From = fromDate };

        var events = new List<IEvent>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Past Event",
                StartAt = DateTime.Now.AddDays(-5),
                EndAt = DateTime.Now.AddDays(-4)
            },
            _testEvent // StartAt = DateTime.Now (после fromDate)
        };

        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(_testEvent.Title, result.Items.First().Title);
        Assert.Equal(1, result.TotalCount); // Только 1 событие после fromDate
    }

    /// <summary>
    /// Проверяет фильтрацию по дате окончания (To): возвращает события, заканчивающиеся до указанной даты.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithDateToFilter_ShouldReturnEventsBeforeDate()
    {
        // Arrange
        var toDate = DateTime.Now.AddDays(-3);
        var filter = new EventFilterDto { To = toDate };

        var pastEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Past Event",
            StartAt = DateTime.Now.AddDays(-5),
            EndAt = DateTime.Now.AddDays(-4)
        };

        var events = new List<IEvent>
        {
            pastEvent,
            _testEvent // StartAt = DateTime.Now (после toDate)
        };

        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Past Event", result.Items.First().Title);
        Assert.Equal(1, result.TotalCount); // Только 1 событие до toDate
    }

    /// <summary>
    /// Проверяет комбинацию нескольких фильтров (Title + From).
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithMultipleFilters_ShouldReturnFilteredEvents()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-1);
        var filter = new EventFilterDto
        {
            Title = "Alpha",
            From = fromDate
        };

        var events = new List<IEvent>
        {
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Alpha Event",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now.AddDays(-1)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Alpha Event 2",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1)
            },
            new Event
            {
                Id = Guid.NewGuid(),
                Title = "Beta Event",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddDays(1)
            }
        };

        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Alpha Event 2", result.Items.First().Title);
        Assert.Equal(1, result.TotalCount);
    }

    /// <summary>
    /// Проверяет, что при отсутствии подходящих событий возвращается пустой PaginatedResult.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WhenNoEventsMatchFilter_ShouldReturnEmptyPaginatedResult()
    {
        // Arrange
        var filter = new EventFilterDto { Title = "NonExistent" };
        var events = new List<IEvent> { _testEvent };
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    #endregion

    #region GetFilteredAsync Tests - Sorting

    /// <summary>
    /// Проверяет сортировку по названию (Title) по возрастанию.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithSortByTitleAscending_ShouldReturnSortedEvents()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            SortBy = "Title",
            SortDesc = false,
            PageSize = 10 // Чтобы получить все элементы
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(3, result.TotalCount);

        var titles = result.Items.Select(e => e.Title).ToList();
        Assert.Equal(new[] { "Alpha Event", "Beta Event", "Gamma Event" }, titles);
    }

    /// <summary>
    /// Проверяет сортировку по названию (Title) по убыванию.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithSortByTitleDescending_ShouldReturnSortedEvents()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            SortBy = "Title",
            SortDesc = true,
            PageSize = 10 // Чтобы получить все элементы
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(3, result.TotalCount);

        var titles = result.Items.Select(e => e.Title).ToList();
        Assert.Equal(new[] { "Gamma Event", "Beta Event", "Alpha Event" }, titles);
    }

    /// <summary>
    /// Проверяет сортировку по дате начала (StartAt) по возрастанию.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithSortByStartAtAscending_ShouldReturnSortedEvents()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            SortBy = "StartAt",
            SortDesc = false,
            PageSize = 10 // Чтобы получить все элементы
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(3, result.TotalCount);

        var startDates = result.Items.Select(e => e.StartAt).ToList();
        Assert.Equal(startDates.OrderBy(d => d), startDates);
    }

    #endregion

    #region GetFilteredAsync Tests - Pagination

    /// <summary>
    /// Проверяет пагинацию – первая страница.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithPagination_ShouldReturnFirstPage()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            Page = 1,
            PageSize = 2
        };

        var events = _testEvents; // 3 события
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count()); // 2 элемента на первой странице
        Assert.Equal(3, result.TotalCount); // Всего 3 элемента
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages); // 3 элемента / 2 = 2 страницы
        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal("Alpha Event", result.Items.First().Title);
        Assert.Equal("Beta Event", result.Items.Last().Title);
    }

    /// <summary>
    /// Проверяет пагинацию – вторая страница.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithPagination_ShouldReturnSecondPage()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            Page = 2,
            PageSize = 2
        };

        var events = _testEvents; // 3 события
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items); // 1 элемент на второй странице
        Assert.Equal(3, result.TotalCount); // Всего 3 элемента
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Equal("Gamma Event", result.Items.First().Title);
    }

    /// <summary>
    /// Проверяет пагинацию вместе с сортировкой.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithPaginationAndSorting_ShouldReturnCorrectPage()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            SortBy = "Title",
            SortDesc = true,
            Page = 2,
            PageSize = 1
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal("Beta Event", result.Items.First().Title);
    }

    /// <summary>
    /// Проверяет пагинацию вместе с фильтрацией.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithPaginationAndFiltering_ShouldReturnCorrectPage()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            Title = "Event",
            Page = 2,
            PageSize = 1
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal("Beta Event", result.Items.First().Title);
    }

    /// <summary>
    /// Проверяет, что при отсутствии явных параметров пагинации используются значения по умолчанию (Page=1, PageSize=10).
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        var filter = new EventFilterDto(); // Page=1, PageSize=10 по умолчанию

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count()); // Все элементы, так как PageSize=10
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    #endregion

    #region GetFilteredAsync Tests - Combination

    /// <summary>
    /// Проверяет комбинацию фильтрации, сортировки и пагинации одновременно.
    /// </summary>
    [Fact]
    public async Task GetFilteredAsync_WithFilterSortAndPagination_ShouldReturnCorrectResult()
    {
        // Arrange
        var filter = new EventFilterDto
        {
            Title = "Event",
            SortBy = "Title",
            SortDesc = true,
            Page = 2,
            PageSize = 1
        };

        var events = _testEvents;
        var queryable = events.AsQueryable();

        _mockRepository.Setup(r => r.GetQueryAsync())
                      .ReturnsAsync(queryable);

        _mockMapper.Setup(m => m.Map<EventInfoDto>(It.IsAny<Event>()))
                  .Returns((Event e) => new EventInfoDto
                  {
                      Id = e.Id,
                      Title = e.Title,
                      StartAt = e.StartAt,
                      EndAt = e.EndAt
                  });

        // Act
        var result = await _service.GetFilteredAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Beta Event", result.Items.First().Title);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    #endregion
}