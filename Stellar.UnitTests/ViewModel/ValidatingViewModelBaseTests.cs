using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.UnitTests.ViewModel;

/// <summary>
/// Exercises the validation pipeline end to end. The pipeline hops through
/// RxSchedulers.TaskpoolScheduler, which is global mutable state, so this class
/// swaps it for an immediate scheduler and restores it afterwards. The collection
/// attribute keeps that swap from racing other test classes.
/// </summary>
[Collection(nameof(SchedulerMutatingCollection))]
public sealed class ValidatingViewModelBaseTests : IDisposable
{
    private readonly IScheduler _originalTaskpool;

    public ValidatingViewModelBaseTests()
    {
        _originalTaskpool = RxSchedulers.TaskpoolScheduler;
        RxSchedulers.TaskpoolScheduler = ImmediateScheduler.Instance;
    }

    public void Dispose() => RxSchedulers.TaskpoolScheduler = _originalTaskpool;

    [Fact]
    public void IsValid_DefaultsToTrueBeforeAnyValidationRuns()
    {
        var viewModel = new TestViewModel(new StubValidator());

        Assert.True(viewModel.IsValid);
        Assert.Empty(viewModel.ValidationErrors);
    }

    [Fact]
    public void RegisterValidation_RunsTheValidatorAndPublishesTheResult()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation("Name", "Name is required") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        using var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);

        Assert.False(viewModel.IsValid);
        var error = Assert.Single(viewModel.ValidationErrors);
        Assert.Equal("Name", error.PropertyName);
        Assert.Equal("Name is required", error.ErrorMessage);
    }

    [Fact]
    public void RegisterValidation_ValidResult_LeavesIsValidTrueAndErrorsEmpty()
    {
        var validator = new StubValidator { Result = ValidationResult.DefaultValidationResult };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        using var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);

        Assert.True(viewModel.IsValid);
        Assert.Empty(viewModel.ValidationErrors);
    }

    [Fact]
    public void SubsequentValidation_ReplacesErrorsPositionallyAndTrimsTheExcess()
    {
        // UpdateValidationState overwrites in place and then trims, rather than
        // clearing and re-adding, so that the collection raises minimal change
        // events. Going from three errors to one must still leave exactly one.
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[]
                {
                    new ValidationInformation("A", "a"),
                    new ValidationInformation("B", "b"),
                    new ValidationInformation("C", "c"),
                },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        using var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);
        Assert.Equal(3, viewModel.ValidationErrors.Count);

        validator.Result = new ValidationResult(
            new[] { new ValidationInformation("Z", "z") },
            isValid: false);
        trigger.OnNext(Unit.Default);

        var remaining = Assert.Single(viewModel.ValidationErrors);
        Assert.Equal("Z", remaining.PropertyName);
    }

    [Fact]
    public void GrowingTheErrorSet_AddsBeyondTheExistingCount()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation("A", "a") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        using var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);
        Assert.Single(viewModel.ValidationErrors);

        validator.Result = new ValidationResult(
            new[]
            {
                new ValidationInformation("A", "a"),
                new ValidationInformation("B", "b"),
            },
            isValid: false);
        trigger.OnNext(Unit.Default);

        Assert.Equal(2, viewModel.ValidationErrors.Count);
    }

    [Fact]
    public void BecomingValidAgain_ClearsThePreviousErrors()
    {
        var validator = new StubValidator
        {
            Result = new ValidationResult(
                new[] { new ValidationInformation("A", "a") },
                isValid: false),
        };
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        using var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);
        Assert.NotEmpty(viewModel.ValidationErrors);

        validator.Result = ValidationResult.DefaultValidationResult;
        trigger.OnNext(Unit.Default);

        Assert.True(viewModel.IsValid);
        Assert.Empty(viewModel.ValidationErrors);
    }

    [Fact]
    public void DisposingTheRegistration_StopsFurtherValidation()
    {
        var validator = new StubValidator();
        var viewModel = new TestViewModel(validator);
        var trigger = new Subject<Unit>();

        var registration = viewModel.Validate(trigger);
        trigger.OnNext(Unit.Default);
        var callsBefore = validator.ValidateCallCount;

        registration.Dispose();
        trigger.OnNext(Unit.Default);

        Assert.Equal(callsBefore, validator.ValidateCallCount);
    }

    private sealed class StubValidator : IProvideValidation<TestViewModel>
    {
        public ValidationResult Result { get; set; } = ValidationResult.DefaultValidationResult;

        public int ValidateCallCount { get; private set; }

        public ValidationResult Validate(TestViewModel validation)
        {
            ValidateCallCount++;
            return Result;
        }
    }

    private sealed class TestViewModel : ValidatingViewModelBase<TestViewModel>
    {
        public TestViewModel(IProvideValidation<TestViewModel> validator)
            : base(validator)
        {
        }

        /// <summary>
        /// Exposes the protected registration with a zero throttle and an immediate
        /// observation scheduler, so tests observe results synchronously.
        /// </summary>
        public IDisposable Validate(IObservable<Unit> trigger) =>
            RegisterValidation(trigger, ImmediateScheduler.Instance, TimeSpan.Zero);

        protected override void Bind(WeakCompositeDisposable disposables)
        {
        }
    }
}

[CollectionDefinition(nameof(SchedulerMutatingCollection), DisableParallelization = true)]
public class SchedulerMutatingCollection
{
}
