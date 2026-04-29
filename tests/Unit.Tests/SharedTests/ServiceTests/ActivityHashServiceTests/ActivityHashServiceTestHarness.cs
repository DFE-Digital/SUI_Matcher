using System.Diagnostics;

using Shared.Models;
using Shared.Services;

namespace Unit.Tests.SharedTests.ServiceTests.ActivityHashServiceTests;

public sealed class ActivityHashServiceTestHarness
{
    public ActivityHashService Service { get; } = new();

    public static ActivityTestScope StartActivity() => ActivityTestScope.Start();

    public static PersonSpecification CreatePersonSpecification(
        string given = "John",
        string family = "Doe",
        string? gender = "male",
        DateOnly? birthDate = null,
        string addressPostalCode = "AB1 2CD"
    )
    {
        return new PersonSpecification
        {
            Given = given,
            Family = family,
            Gender = gender,
            BirthDate = birthDate ?? new DateOnly(1990, 1, 1),
            AddressPostalCode = addressPostalCode,
        };
    }

    public static MatchPersonResult CreateMatchPersonResult(
        string given = "Jane",
        string family = "Smith",
        string? gender = "female",
        DateOnly? birthDate = null,
        string addressPostalCode = "XY9 8ZW"
    )
    {
        return new MatchPersonResult
        {
            Given = given,
            Family = family,
            Gender = gender,
            BirthDate = birthDate ?? new DateOnly(2004, 5, 15),
            AddressPostalCode = addressPostalCode,
        };
    }

    public sealed class ActivityTestScope : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly ActivitySource _activitySource;
        private readonly Activity _activity;

        private ActivityTestScope(
            ActivityListener listener,
            ActivitySource activitySource,
            Activity activity
        )
        {
            _listener = listener;
            _activitySource = activitySource;
            _activity = activity;
        }

        public static ActivityTestScope Start()
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "TestScope",
                Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            };
            ActivitySource.AddActivityListener(listener);

            var activitySource = new ActivitySource("TestScope");
            var activity = activitySource.StartActivity(nameof(ActivityTestScope));

            Assert.NotNull(activity);

            return new ActivityTestScope(listener, activitySource, activity);
        }

        public void Dispose()
        {
            _activity.Stop();
            _activity.Dispose();
            _activitySource.Dispose();
            _listener.Dispose();
        }
    }
}
