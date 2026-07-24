using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.UnitTests.ViewModel;

/// <summary>
/// MonitorValidationInformationFor projects the error collection down to whatever concerns
/// a single property, so a field can bind straight to its own message.
/// </summary>
[Collection(nameof(SchedulerMutatingCollection))]
public sealed class MonitorValidationInformationTests : IDisposable
{
    private readonly IScheduler _originalTaskpool;

    public MonitorValidationInformationTests()
    {
        _originalTaskpool = RxSchedulers.TaskpoolScheduler;
        RxSchedulers.TaskpoolScheduler = ImmediateScheduler.Instance;
    }

    public void Dispose() => RxSchedulers.TaskpoolScheduler = _originalTaskpool;

    [Fact]
    public void StartsWithAnUnflaggedEntryForTheProperty()
    {
        var viewModel = new TestViewModel(new StubValidator());
        var seen = new List<ValidationInformation>();

        using var subscription = viewModel
            .MonitorValidationInformationFor(static x => x.Name)
            .Subscribe(seen.Add);

        var initial = Assert.Single(seen);
        Assert.Equal(nameof(TestViewModel.Name), initial.PropertyName);
        Assert.True(initial.IsError);
    }

    [Fact]
    public void EmitsTheErrorRaisedForThatProperty()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation(nameof(TestViewModel.Name), "Name is required") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();
        var seen = new List<ValidationInformation>();

        using var monitor = viewModel
            .MonitorValidationInformationFor(static x => x.Name)
            .Subscribe(seen.Add);
        using var registration = viewModel.Validate(trigger);

        trigger.OnNext(Unit.Default);

        Assert.Contains(seen, i => i.ErrorMessage == "Name is required");
    }

    [Fact]
    public void IgnoresErrorsRaisedForOtherProperties()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation(nameof(TestViewModel.Other), "Other is required") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();
        var seen = new List<ValidationInformation>();

        using var monitor = viewModel
            .MonitorValidationInformationFor(static x => x.Name)
            .Subscribe(seen.Add);
        using var registration = viewModel.Validate(trigger);

        trigger.OnNext(Unit.Default);

        // Only the seeded default, because nothing concerning Name ever arrived.
        Assert.All(seen, i => Assert.NotEqual("Other is required", i.ErrorMessage));
    }

    [Fact]
    public void DoesNotRepeatAnUnchangedValue()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation(nameof(TestViewModel.Name), "Name is required") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();
        var seen = new List<ValidationInformation>();

        using var monitor = viewModel
            .MonitorValidationInformationFor(static x => x.Name)
            .Subscribe(seen.Add);
        using var registration = viewModel.Validate(trigger);

        trigger.OnNext(Unit.Default);
        var afterFirst = seen.Count;

        // Same failure again: DistinctUntilChanged should swallow the duplicate.
        trigger.OnNext(Unit.Default);

        Assert.Equal(afterFirst, seen.Count);
    }

    [Fact]
    public void AnExpressionThatIsNotAMemberAccess_YieldsAnEmptyStream()
    {
        var viewModel = new TestViewModel(new StubValidator());
        var completed = false;

        using var subscription = viewModel
            .MonitorValidationInformationFor(static _ => "not a member")
            .Subscribe(_ => { }, () => completed = true);

        Assert.True(completed);
    }

    private sealed class StubValidator : IProvideValidation<TestViewModel>
    {
        public ValidationResult Result { get; set; } = ValidationResult.DefaultValidationResult;

        public ValidationResult Validate(TestViewModel validation) => Result;
    }

    private sealed class TestViewModel : ValidatingViewModelBase<TestViewModel>
    {
        public TestViewModel(IProvideValidation<TestViewModel> validator)
            : base(validator)
        {
        }

        public string? Name { get; set; }

        public string? Other { get; set; }

        public IDisposable Validate(IObservable<Unit> trigger) =>
            RegisterValidation(trigger, ImmediateScheduler.Instance, TimeSpan.Zero);

        protected override void Bind(WeakCompositeDisposable disposables)
        {
        }
    }
}
