using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using Splat;

namespace Stellar.ViewModel;

public abstract partial class ValidatingViewModelBase<TNeedsValidation> : ViewModelBase
    where TNeedsValidation : class
{
    public static TimeSpan DefaultValidationChangeThrottleDuration { get; set; } = TimeSpan.FromMilliseconds(17 * 4);

    protected readonly IProvideValidation<TNeedsValidation> Validator;

    /*TODO: SetModifier = AccessModifier.Protected*/
    [Reactive]
    private bool _isValid;

    public ObservableCollection<ValidationInformation> ValidationErrors { get; } = new();

    public ValidatingViewModelBase(IProvideValidation<TNeedsValidation> validator)
    {
        Validator = validator;
    }

    protected IDisposable RegisterValidation<TDoesntMatter>(IObservable<TDoesntMatter> validationTrigger, IScheduler? observationScheduler = null, TimeSpan? changeThrottleDuration = null)
    {
        return RegisterValidation(validationTrigger.Select(_ => Unit.Default), observationScheduler, changeThrottleDuration);
    }

    protected IDisposable RegisterValidation(IObservable<Unit>? validationTrigger = null, IScheduler? observationScheduler = null, TimeSpan? changeThrottleDuration = null)
    {
        var validatorDisposable = new SerialDisposable();

        validationTrigger ??=
            Observable
                .FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, PropertyChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => this.PropertyChanged += x,
                    x => this.PropertyChanged -= x,
                    RxApp.TaskpoolScheduler)
                .Select(_ => Unit.Default)
                .StartWith(Unit.Default);

        validatorDisposable.Disposable =
            validationTrigger
                .ObserveOn(RxApp.TaskpoolScheduler)
                .ThrottleFirst(changeThrottleDuration ?? DefaultValidationChangeThrottleDuration, RxApp.TaskpoolScheduler)
                .Select(
                    _ =>
                    {
                        if (this is not TNeedsValidation nv || Validator is null)
                        {
                            return ValidationResult.DefaultValidationResult;
                        }

                        return Validator.Validate(nv);
                    })
                .ObserveOn(observationScheduler ?? RxApp.MainThreadScheduler)
                .Do(
                    validationResult =>
                    {
                        ValidationErrors.Clear();
                        foreach (var error in validationResult?.ValidationInformation ?? Enumerable.Empty<ValidationInformation>())
                        {
                            ValidationErrors.Add(error);
                        }

                        IsValid = validationResult?.IsValid ?? false;
                    })
                .Subscribe();

        return validatorDisposable;
    }

    public IObservable<ValidationInformation> MonitorValidationInformationFor<TProperty>(Expression<Func<TNeedsValidation, TProperty>> property)
    {
        if (property?.Body is not MemberExpression member)
        {
            return Observable.Empty<ValidationInformation>();
        }

        var propertyName = member.Member.Name;

        var validInformation = new ValidationInformation(propertyName, true);

        return
            Observable
                .FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => ValidationErrors.CollectionChanged += x,
                    x => ValidationErrors.CollectionChanged -= x)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(_ => ValidationErrors?.FirstOrDefault(ni => ni.PropertyName.Equals(propertyName, StringComparison.Ordinal)) ?? validInformation)
                .StartWith(validInformation)
                .DistinctUntilChanged();
    }
}
