using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NotificationConsumer.Application.Events;
using NotificationConsumer.Application.Notifications;
using Xunit;

namespace NotificationConsumer.UnitTests;

public class NotificationProcessorTests
{
    private readonly NotificationProcessor _sut = new(NullLogger<NotificationProcessor>.Instance);

    [Theory]
    [InlineData("FilmCreated")]
    [InlineData("FilmUpdated")]
    [InlineData("FilmDeleted")]
    public async Task ProcessAsync_WhenEventTypeIsKnown_CompletesSuccessfully(string eventType)
    {
        var message = new FilmEventMessage
        {
            Id = "1",
            Title = "Duna",
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            CorrelationId = "corr-1"
        };

        var act = () => _sut.ProcessAsync("film-created", message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessAsync_WhenEventTypeIsUnknown_CompletesWithoutThrowing()
    {
        var message = new FilmEventMessage
        {
            Id = "1",
            Title = "Duna",
            EventType = "SomethingElse",
            Timestamp = DateTime.UtcNow,
            CorrelationId = "corr-1"
        };

        var act = () => _sut.ProcessAsync("film-created", message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
