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
global using ReactiveUI.Maui;
