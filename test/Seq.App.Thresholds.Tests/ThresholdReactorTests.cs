﻿using System;

using Moq;

using NUnit.Framework;

using Seq.App.Thresholds.Tests.Support;
using Seq.Apps;
using Seq.Apps.LogEvents;

using Serilog;

namespace Seq.App.Thresholds.Tests
{
    internal class ThresholdReactorTester : ThresholdReactor
    {
        public DateTime CurrentBucketSecond
        {
            get
            {
                return _currentBucketSecond;
            }
        }
    }

    [TestFixture]
    public class ThresholdReactorTests
    {
        private Mock<ILogger> _logger;
        private DateTime _testStartTime;

        [SetUp]
        public void SetUp()
        {
            _logger = new Mock<ILogger>();
            _testStartTime = Some.UtcTimestamp();
        }

        [Test]
        public void when_wrapped_before_threshold_reached_should_not_log()
        {
            const int SecondsBetweenLogs = 45;

            // arrange
            var sut = GetThresholdReactor(5);

            // act
            SendXEventsNSecondsApart(sut, 7, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [Test]
        public void when_threshold_exceeded_over_time_should_log_only_1_message()
        {
            const int SecondsBetweenLogs = 2;

            // arrange
            var sut = GetThresholdReactor(5);

            // act
            SendXEventsNSecondsApart(sut, 7, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once());
        }

        [Test]
        public void when_threshold_exceeded_should_log_only_1_message()
        {
            const int SecondsBetweenLogs = 0;

            // arrange
            var sut = GetThresholdReactor(5);

            // act
            SendXEventsNSecondsApart(sut, 7, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once());
        }

        [Test]
        public void when_reset_disabled_and_threshold_exceeded_should_only_3_message()
        {
            const int ExpectLogs = 3;
            const int SecondsBetweenLogs = 0;

            // arrange
            var sut = GetThresholdReactor(5, false);

            // act
            SendXEventsNSecondsApart(sut, 7, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Exactly(ExpectLogs));
        }

        [Test]
        public void when_event_time_before_attach_time_should_not_throw()
        {
            const int SecondsBetweenLogs = 0;

            // arrange
            var sut = GetThresholdReactor(5);

            _testStartTime = sut.CurrentBucketSecond - TimeSpan.FromSeconds(1);

            // act
            SendXEventsNSecondsApart(sut, 1, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }


        [Test]
        public void when_threshold_tripled_over_time_should_get_3_logs()
        {
            const int ExpectLogs = 3;
            const int Threshold = 5;
            const int SecondsBetweenLogs = 2;
            var sut = GetThresholdReactor(Threshold);

            // act
            SendXEventsNSecondsApart(sut, Threshold * ExpectLogs, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Exactly(ExpectLogs));
        }

        [Test]
        public void when_threshold_tripled_should_get_3_logs()
        {
            const int ExpectLogs = 3;
            const int Threshold = 5;
            const int SecondsBetweenLogs = 2;
            var sut = GetThresholdReactor(Threshold);

            // act
            SendXEventsNSecondsApart(sut, Threshold * ExpectLogs, SecondsBetweenLogs);

            // assert
            _logger.Verify(l => l.Information(It.IsAny<string>(), It.IsAny<object[]>()), Times.Exactly(ExpectLogs));
        }

        private void SendXEventsNSecondsApart(ISubscribeTo<LogEventData> sut, int numberOfEvents, int secondsBetweenEvents = 0)
        {
            for (var i = 0; i < numberOfEvents; i++)
            {
                var eventTime = _testStartTime + TimeSpan.FromSeconds(i * secondsBetweenEvents);
                var @event = Some.LogEvent(timestamp: eventTime);
                sut.On(@event);
            }
        }

        private ThresholdReactorTester GetThresholdReactor(int threshold, bool resetOnThresholdReached = true)
        {
            var sut = new ThresholdReactorTester()
                {
                    EventsInWindowThreshold = threshold, 
                    ThresholdName = Guid.NewGuid().ToString(), 
                    WindowSeconds = 120,
                    ResetOnThresholdReached = resetOnThresholdReached
                };

            var appHost = new Mock<IAppHost>();
            appHost.SetupGet(h => h.Host).Returns(new Host(new[] { "localhost" }, "test"));
            appHost.SetupGet(h => h.Logger).Returns(_logger.Object);

            sut.Attach(appHost.Object);

            return sut;
        }
    }
}