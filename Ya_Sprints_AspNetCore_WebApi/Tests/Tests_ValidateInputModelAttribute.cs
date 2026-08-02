using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using SprintASP_NetCore_API.Filters.ActionFilters;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    /// <summary>
    /// Тесты для валидационного атрибута <see cref="ValidateInputModelAttribute"/>.
    /// Проверяют как стандартную ModelState-валидацию, так и бизнес-правила,
    /// реализованные в <see cref="ValidatorHelper"/>.
    /// </summary>
    public class Tests_ValidateInputModelAttribute
    {
        private readonly ValidateInputModelAttribute _filter;
        private readonly EventInfoDto _validEventDto;
        private readonly EventFilterDto _validFilter;

        public Tests_ValidateInputModelAttribute()
        {
            _filter = new ValidateInputModelAttribute();

            _validEventDto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "Test Event",
                Description = "Test Description",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 10
            };

            _validFilter = new EventFilterDto
            {
                Title = "Test",
                From = DateTime.Now.AddDays(-1),
                To = DateTime.Now,
                Page = 1,
                PageSize = 10
            };
        }

        #region Вспомогательные методы

        /// <summary>
        /// Извлекает список сообщений об ошибках из <see cref="IActionResult"/>,
        /// который должен быть <see cref="BadRequestObjectResult"/>.
        /// Поддерживает как <see cref="ValidationProblemDetails"/>, так и анонимный объект с полем Errors.
        /// </summary>
        private List<string> GetErrorMessages(IActionResult result)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var value = badRequest.Value;

            if (value is ValidationProblemDetails details)
                return details.Errors.SelectMany(kvp => kvp.Value).ToList();

            var type = value.GetType();
            var prop = type.GetProperty("Errors");
            if (prop != null)
            {
                var errors = prop.GetValue(value) as IEnumerable<string>;
                return errors?.ToList() ?? new List<string>();
            }

            return new List<string>();
        }

        #endregion

        #region Тесты для CreateEventDto

        /// <summary>
        /// Проверяет, что валидный <see cref="CreateEventDto"/> не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithValidCreateEventDto_DoesNotSetResult()
        {
            var dto = new CreateEventDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                Description = "Description",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 10
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dto);
            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что <see cref="CreateEventDto"/> с TotalSeats = 0 возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCreateEventDto_TotalSeatsZero_ReturnsBadRequest()
        {
            var dto = new CreateEventDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = 0
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dto);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Общее количество мест должно быть больше 0", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="CreateEventDto"/> с отрицательным TotalSeats возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCreateEventDto_TotalSeatsNegative_ReturnsBadRequest()
        {
            var dto = new CreateEventDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                StartAt = DateTime.Now.AddHours(1),
                EndAt = DateTime.Now.AddHours(2),
                TotalSeats = -5
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dto);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Общее количество мест должно быть больше 0", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="CreateEventDto"/> с StartAt > EndAt возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCreateEventDto_StartAtAfterEndAt_ReturnsBadRequest()
        {
            var dto = new CreateEventDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 10
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dto);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="CreateEventDto"/> с StartAt == EndAt возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCreateEventDto_StartAtEqualsEndAt_ReturnsBadRequest()
        {
            var now = DateTime.Now;
            var dto = new CreateEventDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                StartAt = now,
                EndAt = now,
                TotalSeats = 10
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dto);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        /// <summary>
        /// Проверяет, что коллекция <see cref="CreateEventDto"/> с дублирующимися названиями возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCreateEventDtoCollection_DuplicateTitles_ReturnsBadRequest()
        {
            var dtos = new List<CreateEventDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Duplicate", StartAt = DateTime.Now.AddHours(1), EndAt = DateTime.Now.AddHours(2), TotalSeats = 10 },
                new() { Id = Guid.NewGuid(), Title = "Duplicate", StartAt = DateTime.Now.AddHours(3), EndAt = DateTime.Now.AddHours(4), TotalSeats = 10 }
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dtos);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains(errors, msg => msg.Contains("название не должно повторяться"));
        }

        #endregion

        #region Тесты для ModelState

        /// <summary>
        /// Проверяет, что при невалидном ModelState возвращается BadRequest с ошибками из ModelState.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WhenModelStateIsInvalid_ReturnsBadRequest()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("Title", "Title is required");

            var context = FilterTestHelper.CreateActionExecutingContext(
                new object(),
                _validEventDto,
                modelState: modelState
            );

            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Title is required", errors);
        }

        #endregion

        #region Тесты для EventInfoDto (одиночные)

        /// <summary>
        /// Проверяет, что валидный <see cref="EventInfoDto"/> не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithValidEventDto_DoesNotSetResult()
        {
            var context = FilterTestHelper.CreateActionExecutingContext(
                new object(),
                _validEventDto
            );

            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что <see cref="EventInfoDto"/> с StartAt > EndAt возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithEventDto_StartAtAfterEndAt_ReturnsBadRequest()
        {
            var invalidEventDto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "Invalid Event",
                Description = "Description",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 10
            };

            var context = FilterTestHelper.CreateActionExecutingContext(
                new object(),
                invalidEventDto
            );

            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="EventInfoDto"/> с StartAt == EndAt возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithEventDto_StartAtEqualsEndAt_ReturnsBadRequest()
        {
            var now = DateTime.Now;
            var invalidEventDto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "Invalid Event",
                Description = "Description",
                StartAt = now,
                EndAt = now,
                TotalSeats = 10
            };

            var context = FilterTestHelper.CreateActionExecutingContext(
                new object(),
                invalidEventDto
            );

            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        #endregion

        #region Тесты для коллекций EventInfoDto

        /// <summary>
        /// Проверяет, что валидная коллекция <see cref="EventInfoDto"/> не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithValidEventDtoCollection_DoesNotSetResult()
        {
            var dtos = new List<EventInfoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", StartAt = DateTime.Now.AddHours(1), EndAt = DateTime.Now.AddHours(2), TotalSeats = 10 },
                new() { Id = Guid.NewGuid(), Title = "Event 2", StartAt = DateTime.Now.AddHours(3), EndAt = DateTime.Now.AddHours(4), TotalSeats = 10 }
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dtos);
            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что коллекция <see cref="EventInfoDto"/> с неверными датами возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCollection_StartAtAfterEndAt_ReturnsBadRequest()
        {
            var dtos = new List<EventInfoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Invalid Event", StartAt = DateTime.Now.AddHours(2), EndAt = DateTime.Now.AddHours(1), TotalSeats = 10 },
                new() { Id = Guid.NewGuid(), Title = "Valid Event", StartAt = DateTime.Now.AddHours(3), EndAt = DateTime.Now.AddHours(4), TotalSeats = 10 }
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dtos);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains(errors, msg => msg.Contains("Дата начала не может быть позже"));
        }

        /// <summary>
        /// Проверяет, что коллекция <see cref="EventInfoDto"/> с дублирующимися названиями возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithCollection_DuplicateTitles_ReturnsBadRequest()
        {
            var dtos = new List<EventInfoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Duplicate", StartAt = DateTime.Now.AddHours(1), EndAt = DateTime.Now.AddHours(2), TotalSeats = 10 },
                new() { Id = Guid.NewGuid(), Title = "Duplicate", StartAt = DateTime.Now.AddHours(3), EndAt = DateTime.Now.AddHours(4), TotalSeats = 10 }
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), dtos);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains(errors, msg => msg.Contains("название не должно повторяться"));
        }

        #endregion

        #region Тесты для EventFilterDto

        /// <summary>
        /// Проверяет, что валидный <see cref="EventFilterDto"/> не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithValidEventFilterDto_DoesNotSetResult()
        {
            var context = FilterTestHelper.CreateActionExecutingContext(new object(), _validFilter);
            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что <see cref="EventFilterDto"/> с From > To возвращает ошибку.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithFilter_FromAfterTo_ReturnsBadRequest()
        {
            var invalidFilter = new EventFilterDto
            {
                From = DateTime.Now,
                To = DateTime.Now.AddDays(-1)
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), invalidFilter);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="EventFilterDto"/> с невалидным Page (меньше 1) возвращает ошибку.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public void OnActionExecuting_WithFilter_InvalidPage_ReturnsBadRequest(int invalidPage)
        {
            var invalidFilter = new EventFilterDto
            {
                Page = invalidPage,
                PageSize = 10,
                From = DateTime.Now.AddDays(-1),
                To = DateTime.Now.AddDays(1)
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), invalidFilter);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Номер страницы должен быть больше 0", errors);
        }

        /// <summary>
        /// Проверяет, что <see cref="EventFilterDto"/> с невалидным PageSize (0 или >100) возвращает ошибку.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(200)]
        public void OnActionExecuting_WithFilter_InvalidPageSize_ReturnsBadRequest(int invalidPageSize)
        {
            var invalidFilter = new EventFilterDto
            {
                Page = 1,
                PageSize = invalidPageSize,
                From = DateTime.Now.AddDays(-1),
                To = DateTime.Now.AddDays(1)
            };

            var context = FilterTestHelper.CreateActionExecutingContext(new object(), invalidFilter);
            _filter.OnActionExecuting(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Размер страницы должен быть от 1 до 100", errors);
        }

        #endregion

        #region Тесты для выходных данных (OnActionExecuted)

        /// <summary>
        /// Проверяет, что валидный выходной объект не изменяется фильтром.
        /// </summary>
        [Fact]
        public void OnActionExecuted_WithValidOutput_DoesNotChangeResult()
        {
            var result = new OkObjectResult(_validEventDto);
            var context = FilterTestHelper.CreateActionExecutedContext(new object(), result);
            _filter.OnActionExecuted(context);

            Assert.NotNull(context.Result);
            var okResult = Assert.IsType<OkObjectResult>(context.Result);
            Assert.Equal(_validEventDto, okResult.Value);
        }

        /// <summary>
        /// Проверяет, что невалидный выходной объект приводит к BadRequest с ошибками.
        /// </summary>
        [Fact]
        public void OnActionExecuted_WithInvalidOutput_ReturnsBadRequest()
        {
            var invalidDto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "Invalid",
                StartAt = DateTime.Now.AddHours(2),
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 10
            };

            var result = new OkObjectResult(invalidDto);
            var context = FilterTestHelper.CreateActionExecutedContext(new object(), result);
            _filter.OnActionExecuted(context);

            Assert.NotNull(context.Result);
            var errors = GetErrorMessages(context.Result);
            Assert.Contains("Дата начала не может быть позже или равна дате окончания", errors);
        }

        /// <summary>
        /// Проверяет, что при наличии исключения фильтр не меняет результат.
        /// </summary>
        [Fact]
        public void OnActionExecuted_WhenExceptionOccurred_DoesNotChangeResult()
        {
            var context = FilterTestHelper.CreateActionExecutedContext(new object(), new OkResult());
            context.Exception = new Exception("Test");
            _filter.OnActionExecuted(context);

            Assert.NotNull(context.Exception);
            Assert.IsType<OkResult>(context.Result);
        }

        #endregion

        #region Краевые случаи

        /// <summary>
        /// Проверяет, что null-аргумент не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithNullArgument_DoesNotSetResult()
        {
            var context = FilterTestHelper.CreateActionExecutingContext(new object(), null);
            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что пустая коллекция не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuting_WithEmptyCollection_DoesNotSetResult()
        {
            var context = FilterTestHelper.CreateActionExecutingContext(new object(), new List<EventInfoDto>());
            _filter.OnActionExecuting(context);
            Assert.Null(context.Result);
        }

        /// <summary>
        /// Проверяет, что null-результат в OnActionExecuted не вызывает ошибок.
        /// </summary>
        [Fact]
        public void OnActionExecuted_WithNullResult_DoesNotChangeResult()
        {
            var context = FilterTestHelper.CreateActionExecutedContext(new object(), null!);
            _filter.OnActionExecuted(context);
            Assert.Null(context.Result);
        }

        #endregion
    }
}