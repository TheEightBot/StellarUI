global using System;
global using System.Reactive;
global using System.Reactive.Concurrency;
global using System.Reactive.Disposables;

// System.Reactive 6.1 moved the CompositeDisposable overload of DisposeWith
// into this namespace; without it the call resolves to a SerialDisposable
// overload and fails.
global using System.Reactive.Disposables.Fluent;
global using System.Reactive.Linq;
global using System.Reactive.Subjects;
global using ReactiveUI;

// Stellar's own MAUI view wrappers (ReactiveStackLayout, ReactiveGrid, ReactiveViewCell)
// live in the ReactiveUI.Maui namespaces for historical compatibility; the ReactiveUI 24
// platform types now come from ReactiveUI.Reactive.Maui.
global using ReactiveUI.Maui;
global using ReactiveUI.Reactive;
global using ReactiveUI.Reactive.Maui;
