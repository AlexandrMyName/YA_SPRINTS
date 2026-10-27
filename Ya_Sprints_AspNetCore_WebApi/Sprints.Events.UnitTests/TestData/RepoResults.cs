using Moq;
using Sprints.Events.Application.Internal;

namespace Sprints.Events.UnitTests.TestData;

/// <summary>
/// Создание IResultEntity<T> через Moq — без привязки к конкретной фабрике
/// (ResultEntity.Ok / Result.Ok / и т.п.). Работает с любым IResultEntity.
/// </summary>
internal static class RepoResults
{
    public static IResultEntity<T> Ok<T>(T data, string? message = null) where T : class
    {
        var mock = new Mock<IResultEntity<T>>();
        mock.SetupGet(r => r.IsSuccesfuly).Returns(true);
        mock.SetupGet(r => r.Data).Returns(data);
        mock.SetupGet(r => r.Message).Returns(message);
        mock.SetupGet(r => r.Reason).Returns((string?)null);
        return mock.Object;
    }

    public static IResultEntity<T> Fail<T>(string reason) where T : class
    {
        var mock = new Mock<IResultEntity<T>>();
        mock.SetupGet(r => r.IsSuccesfuly).Returns(false);
        mock.SetupGet(r => r.Data).Returns((T?)null);
        mock.SetupGet(r => r.Message).Returns((string?)null);
        mock.SetupGet(r => r.Reason).Returns(reason);
        return mock.Object;
    }
}