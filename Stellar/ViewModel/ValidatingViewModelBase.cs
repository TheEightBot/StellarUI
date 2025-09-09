using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Splat;

namespace Stellar.ViewModel;

public abstract partial class ValidatingViewModelBase<TNeedsValidation> : ViewModelBase
    where TNeedsValidation : class
{
    [IgnoreReactive]
    public static TimeSpan DefaultValidationChangeThrottleDuration { get; set; } = TimeSpan.FromMilliseconds(17 * 4);

    protected readonly IProvideValidation<TNeedsValidation> Validator;

    [Reactive]
    public partial bool IsValid { get; private set; }

    public ObservableCollection<ValidationInformation> ValidationErrors { get; } = new();

    public ValidatingViewModelBase(IProvideValidation<TNeedsValidation> validator)
    {
        IsValid = true;
        Validator = validator;
    }

    protected IDisposable RegisterValidation<TDoesntMatter>(IObservable<TDoesntMatter> validationTrigger, IScheduler? observationScheduler = null, TimeSpan? changeThrottleDuration = null)
    {
        return RegisterValidation(validationTrigger.Select(_ => Unit.Default), observationScheduler, changeThrottleDuration);
    }

    protected IDisposable RegisterValidation(IObservable<Unit>? validationTrigger = null, IScheduler? observationScheduler = null, TimeSpan? changeThrottleDuration = null)
    {
        var validatorDisposable = new SerialDisposable();

        // Create a weak reference to avoid a strong reference to 'this'
        var weakThis = new WeakReference<ValidatingViewModelBase<TNeedsValidation>>(this);

        // Store validator in a local variable to avoid capturing 'this.Validator'
        var validator = Validator;

        validationTrigger ??= CreatePropertyChangedObservable(weakThis);

        validatorDisposable.Disposable =
            validationTrigger
                .ObserveOn(RxApp.TaskpoolScheduler)
                .ThrottleFirst(changeThrottleDuration ?? DefaultValidationChangeThrottleDuration, RxApp.TaskpoolScheduler)
                .Select(_ => ValidateWithWeakReference(weakThis, validator))
                .ObserveOn(observationScheduler ?? RxApp.MainThreadScheduler)
                .Subscribe(validationResult => UpdateValidationState(weakThis, validationResult));

        return validatorDisposable;
    }

    private static IObservable<Unit> CreatePropertyChangedObservable(WeakReference<ValidatingViewModelBase<TNeedsValidation>> weakThis)
    {
        return Observable
            .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                static eventHandler =>
                {
                    void Handler(object? sender, PropertyChangedEventArgs e) => eventHandler?.Invoke(e);
                    return Handler;
                },
                handler =>
                {
                    if (weakThis.TryGetTarget(out var target))
                    {
                        target.PropertyChanged += handler;
                    }
                },
                handler =>
                {
                    if (weakThis.TryGetTarget(out var target))
                    {
                        target.PropertyChanged -= handler;
                    }
                },
                RxApp.TaskpoolScheduler)
            .Select(_ => Unit.Default)
            .StartWith(Unit.Default);
    }

    private static ValidationResult? ValidateWithWeakReference(
        WeakReference<ValidatingViewModelBase<TNeedsValidation>> weakThis,
        IProvideValidation<TNeedsValidation>? validator)
    {
        if (!weakThis.TryGetTarget(out var target) || validator == null)
        {
            return ValidationResult.DefaultValidationResult;
        }

        if (target is not TNeedsValidation needsValidation)
        {
            return ValidationResult.DefaultValidationResult;
        }

        return validator.Validate(needsValidation);
    }

    private static void UpdateValidationState(
        WeakReference<ValidatingViewModelBase<TNeedsValidation>> weakThis,
        ValidationResult? validationResult)
    {
        if (!weakThis.TryGetTarget(out var target))
        {
            return;
        }

        target.ValidationErrors.Clear();

        if (validationResult?.ValidationInformation != null)
        {
            foreach (var error in validationResult.ValidationInformation)
            {
                target.ValidationErrors.Add(error);
            }
        }

        target.IsValid = validationResult?.IsValid ?? false;
    }

    public IObservable<ValidationInformation> MonitorValidationInformationFor<TProperty>(Expression<Func<TNeedsValidation, TProperty>> property)
    {
        if (property?.Body is not MemberExpression member)
        {
            return Observable.Empty<ValidationInformation>();
        }

        var propertyName = member.Member.Name;
        var validInformation = new ValidationInformation(propertyName, true);

        // Create a weak reference to avoid a strong reference to 'this'
        var weakThis = new WeakReference<ValidatingViewModelBase<TNeedsValidation>>(this);
        var weakErrors = new WeakReference<ObservableCollection<ValidationInformation>>(ValidationErrors);

        return Observable
            .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                static eventHandler =>
                {
                    void Handler(object? sender, NotifyCollectionChangedEventArgs e) => eventHandler?.Invoke(e);
                    return Handler;
                },
                handler =>
                {
                    if (weakErrors.TryGetTarget(out var errors))
                    {
                        errors.CollectionChanged += handler;
                    }
                },
                handler =>
                {
                    if (weakErrors.TryGetTarget(out var errors))
                    {
                        errors.CollectionChanged -= handler;
                    }
                })
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(_ => GetValidationInformation(weakErrors, propertyName, validInformation))
            .StartWith(validInformation)
            .DistinctUntilChanged();
    }

    private static ValidationInformation GetValidationInformation(
        WeakReference<ObservableCollection<ValidationInformation>> weakErrors,
        string propertyName,
        ValidationInformation defaultValue)
    {
        if (!weakErrors.TryGetTarget(out var errors))
        {
            return defaultValue;
        }

        return errors.FirstOrDefault(ni => ni.PropertyName.Equals(propertyName, StringComparison.Ordinal)) ?? defaultValue;
    }
}
