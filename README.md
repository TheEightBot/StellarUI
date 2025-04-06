# StellarUI

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/Stellar.svg)](https://www.nuget.org/packages/Stellar/)

A comprehensive cross-platform .NET application framework built on ReactiveUI for creating reactive MVVM applications.

## Overview

StellarUI is a reactive UI framework that provides a structured and consistent approach to building applications across different platforms including MAUI, Blazor, and Avalonia. It implements the MVVM (Model-View-ViewModel) pattern and enhances it with reactive programming paradigms, making it easier to manage application state, handle UI events, and implement complex workflows.

The framework is designed to support multiple platforms while maintaining a consistent programming model, allowing developers to share business logic and UI patterns across different .NET UI frameworks.

## Key Features

- **Cross-platform support**: Build applications for MAUI (iOS, Android, Windows, macOS), Blazor (WebAssembly, Server), and Avalonia (Windows, macOS, Linux)
- **Reactive programming model**: Built on ReactiveUI to provide robust reactive programming paradigms
- **Consistent lifecycle management**: Standardized lifecycle events (Activated, Deactivated, IsAppearing, IsDisappearing) across all platforms
- **Dependency injection**: First-class support for registering and resolving services with the `ServiceRegistrationAttribute`
- **Hot reload support**: Enhanced development experience with built-in hot reload capabilities
- **View infrastructure**: Base classes for all view types with consistent patterns for setup and binding
- **Navigation abstractions**: Platform-agnostic navigation APIs to simplify cross-platform navigation
- **Data binding**: Reactive two-way data binding with support for validation
- **Popup and modal support**: Unified APIs for displaying popups and modals across platforms

## Project Structure

StellarUI is composed of several projects, each serving a specific purpose in the framework:

### Core Libraries

- **Stellar**: Core library containing shared interfaces, base classes, and utilities used by all platform implementations
- **Stellar.Maui**: Implementation for .NET MAUI platform, providing base classes for MAUI UI components
- **Stellar.Blazor**: Implementation for Blazor WebAssembly and Server, enabling reactive UI for web applications
- **Stellar.Avalonia**: Implementation for Avalonia UI, supporting desktop applications on Windows, macOS, and Linux

### Extensions and Utilities

- **Stellar.Maui.PopUp**: Extended popup functionality for MAUI applications
- **Stellar.FluentValidation**: Integration with FluentValidation for input validation across platforms
- **Stellar.DiskDataCache**: Disk-based caching implementation for persistent storage

### Samples

- **Stellar.MauiSample**: Example application demonstrating MAUI features
- **Stellar.BlazorSample**: Example application demonstrating Blazor features
- **Stellar.AvaloniaSample**: Example application demonstrating Avalonia features
- **Stellar.MauiBlazorHybridSample**: Example application demonstrating hybrid MAUI Blazor approach

## Getting Started

### Prerequisites

- .NET 6.0 or later
- For MAUI development: .NET MAUI workload installed
- For Blazor development: ASP.NET Core Blazor workload installed
- For Avalonia development: Avalonia UI dependencies

### Installation

To use StellarUI in your project, add the relevant package references:

```xml
<!-- For MAUI applications -->
<PackageReference Include="Stellar" Version="latest" />
<PackageReference Include="Stellar.Maui" Version="latest" />

<!-- For Blazor applications -->
<PackageReference Include="Stellar" Version="latest" />
<PackageReference Include="Stellar.Blazor" Version="latest" />

<!-- For Avalonia applications -->
<PackageReference Include="Stellar" Version="latest" />
<PackageReference Include="Stellar.Avalonia" Version="latest" />
```

### Basic Setup

StellarUI simplifies application setup across all platforms with a focus on minimizing boilerplate code. Just add these two key method calls to your existing app setup:

#### MAUI Application Setup

```csharp
// In your MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        // Just add these two lines to enable StellarUI:
        .UseStellarComponents<App>() // Register all StellarUI components automatically
        .EnableHotReload();          // Enable hot reload capability (optional but recommended)
    
    return builder.Build();
}
```

#### Blazor Application Setup

```csharp
// In your Program.cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Just add this line to enable StellarUI:
builder.Services.UseStellarComponents<App>(); // Register all StellarUI components automatically

await builder.Build().RunAsync();
```

#### Avalonia Application Setup

```csharp
// In your Program.cs
public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        // Just add these two lines to enable StellarUI:
        .UseStellarComponents<App>() // Register all StellarUI components automatically
        .EnableHotReload()           // Enable hot reload capability (optional but recommended)
        .UseReactiveUI();
```

That's it! The `UseStellarComponents<App>()` method handles all the necessary registration of services, views, and view models. The framework auto-discovers and registers all classes in your application that have the `[ServiceRegistration]` attribute.

## Core Concepts

### Base Classes and Views

StellarUI provides base classes for each type of view component across platforms:

#### MAUI Base Classes

- `ContentPageBase<TViewModel>`: Base class for MAUI ContentPage
- `GridBase<TViewModel>`: Base class for MAUI Grid
- `StackLayoutBase<TViewModel>`: Base class for MAUI StackLayout  
- `ContentViewBase<TViewModel>`: Base class for MAUI ContentView
- `ShellBase<TViewModel>`: Base class for MAUI Shell
- `PopupPageBase<TViewModel>`: Base class for popup pages

#### Blazor Base Classes

- `ComponentBase<TViewModel>`: Base class for Blazor components
- `LayoutComponentBase<TViewModel>`: Base class for Blazor layouts
- `InjectableComponentBase<TViewModel>`: Base class for components with DI

#### Avalonia Base Classes

- `WindowBase<TViewModel>`: Base class for Avalonia windows
- `UserControlBase<TViewModel>`: Base class for Avalonia user controls

### View Implementation Pattern

All views follow a consistent pattern for setup and binding:

```csharp
[ServiceRegistration] // Optional: Register for dependency injection
public class SampleView : ContentViewBase<SampleViewModel>
{
    private Label _label;
    
    // Constructor - initialize with the view model
    public SampleView(SampleViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }
    
    // Setup the UI elements
    public override void SetupUserInterface()
    {
        Content = new Label()
            .Assign(out _label);
    }
    
    // Bind the UI elements to the view model
    public override void Bind(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, vm => vm.Text, v => v._label.Text)
            .DisposeWith(disposables);
            
        // Add other bindings as needed
    }
}
```

### ViewModels

ViewModels in StellarUI inherit from `ViewModelBase` and use the `[Reactive]` attribute from ReactiveUI for reactive properties:

```csharp
[ServiceRegistration] // Register for dependency injection
public partial class SampleViewModel : ViewModelBase
{
    [Reactive]
    public string Text { get; set; } = "Hello, World!";
    
    // Initialize method for setup that happens at creation time
    protected override void Initialize()
    {
        // Setup initial values, etc.
    }
    
    // Bind method for setting up reactive bindings
    protected override void Bind(CompositeDisposable disposables)
    {
        // Setup observables, commands, etc.
        this.WhenAnyValue(x => x.Text)
            .Subscribe(text => Console.WriteLine($"Text changed: {text}"))
            .DisposeWith(disposables);
    }
}
```

### Dependency Injection

StellarUI provides a `ServiceRegistrationAttribute` to automatically register classes with the dependency injection system:

```csharp
// Register as transient (default)
[ServiceRegistration]
public class TransientService : IService { }

// Register as singleton
[ServiceRegistration(Lifetime.Singleton)]
public class SingletonService : IService { }

// Register as scoped
[ServiceRegistration(Lifetime.Scoped)]
public class ScopedService : IService { }
```

Services can be injected into ViewModels through constructor injection:

```csharp
[ServiceRegistration]
public partial class SampleViewModel : ViewModelBase
{
    private readonly IService _service;
    
    // Constructor injection
    public SampleViewModel(IService service)
    {
        _service = service;
    }
    
    // Rest of the ViewModel...
}
```

### Lifecycle Management

StellarUI provides a consistent set of lifecycle events across all platforms:

- **Initialized**: Called when the view is first initialized
- **Activated**: Called when the view is activated
- **Deactivated**: Called when the view is deactivated
- **IsAppearing**: Called when the view is appearing on screen
- **IsDisappearing**: Called when the view is disappearing from screen
- **Disposed**: Called when the view is being disposed

These events are exposed as `IObservable<Unit>` properties on all view base classes, allowing you to react to them with reactive extensions:

```csharp
public class SampleView : ContentViewBase<SampleViewModel>
{
    public SampleView(SampleViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
        
        // Subscribe to lifecycle events
        this.IsAppearing
            .Subscribe(_ => Console.WriteLine("View is appearing"))
            .DisposeWith(disposables);
            
        this.IsDisappearing
            .Subscribe(_ => Console.WriteLine("View is disappearing"))
            .DisposeWith(disposables);
    }
    
    // Rest of the view...
}
```

ViewModels can implement the `ILifecycleEventAware` interface to be notified of lifecycle events:

```csharp
[ServiceRegistration]
public partial class SampleViewModel : ViewModelBase, ILifecycleEventAware
{
    public void OnLifecycleEvent(LifecycleEvent lifecycleEvent)
    {
        switch (lifecycleEvent)
        {
            case LifecycleEvent.IsAppearing:
                // Handle appearing
                break;
            case LifecycleEvent.IsDisappearing:
                // Handle disappearing
                break;
            // Handle other lifecycle events...
        }
    }
    
    // Rest of the ViewModel...
}
```

### Navigation

StellarUI provides platform-agnostic navigation abstractions to simplify navigation across different platforms:

#### MAUI Navigation

```csharp
// Navigate to a page
Observable.Return(Unit.Default)
    .NavigateToPage<SimpleSamplePage>(this)
    .DisposeWith(disposables);

// Navigate to a page with parameters
Observable.Return(42)  // The parameter value
    .NavigateToPage<int, ParameterizedPage>(
        this,
        queryParameters: (value, dict) =>
        {
            dict.Add("ParameterValue", value);
        })
    .DisposeWith(disposables);

// Show popup
this.BindCommand(ViewModel, vm => vm.ShowPopupCommand, v => v._popupButton)
    .DisposeWith(disposables);

// In ViewModel
_showPopupCommand = ReactiveCommand.CreateFromTask(async () =>
{
    await PopupNavigation.Instance.PushAsync(
        new SamplePopupPage(new SampleViewModel()));
});
```

#### Blazor Navigation

```csharp
// Navigate in a Blazor component
Navigation.NavigateTo("/details/42");

// In the target page class, receive the parameter
[Parameter]
public string Id { get; set; }

// Or with query parameters
[QueryParameter]
public int ParameterValue { get; set; }
```

#### Avalonia Navigation

```csharp
// Navigate in an Avalonia application
var window = new DetailWindow(new DetailViewModel());
window.Show();
```

## Advanced Features

### Validation

StellarUI integrates with FluentValidation for model validation across all platforms:

```csharp
// Define a validator
public class UserValidator : AbstractValidator<UserViewModel>
{
    public UserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).GreaterThan(0).LessThan(120);
    }
}

// Use the validator in a view model
[ServiceRegistration]
public partial class UserViewModel : ViewModelBase, IProvideValidation
{
    [Reactive]
    public string Username { get; set; }
    
    [Reactive]
    public string Email { get; set; }
    
    [Reactive]
    public int Age { get; set; }
    
    // Implementation of IProvideValidation
    public IEnumerable<ValidationResult> Validate()
    {
        var validator = new UserValidator();
        var results = validator.Validate(this);
        
        return results.Errors
            .Select(x => new ValidationResult(x.PropertyName, x.ErrorMessage));
    }
    
    // Track validity in the view model
    [Reactive]
    public bool IsValid { get; private set; }
    
    protected override void Bind(CompositeDisposable disposables)
    {
        this.WhenAnyValue(x => x.Username, x => x.Email, x => x.Age)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => 
            {
                IsValid = !Validate().Any();
            })
            .DisposeWith(disposables);
    }
}
```

### Data Caching

The `Stellar.DiskDataCache` package provides disk-based caching capabilities:

```csharp
// Register the cache in your DI setup
services.AddSingleton<IDataCache, DiskCache>();

// Use the cache in a view model
[ServiceRegistration]
public partial class CachedDataViewModel : ViewModelBase
{
    private readonly IDataCache _cache;
    
    public CachedDataViewModel(IDataCache cache)
    {
        _cache = cache;
    }
    
    [Reactive]
    public ObservableCollection<string> Items { get; private set; }
    
    protected override async void Initialize()
    {
        // Try to load from cache first
        var cachedItems = await _cache.GetAsync<List<string>>("items");
        if (cachedItems != null)
        {
            Items = new ObservableCollection<string>(cachedItems);
        }
        else
        {
            Items = new ObservableCollection<string>();
        }
    }
    
    public async Task SaveItems()
    {
        await _cache.SetAsync("items", Items.ToList());
    }
}
```

### Hot Reload

StellarUI has built-in support for hot reload during development:

```csharp
// Enable hot reload in a MAUI app
builder.EnableHotReload();

// Enable hot reload in an Avalonia app
AppBuilder.Configure<App>()
    // ... other configuration ...
    .EnableHotReload()
    .UseReactiveUI();
```

## Contributing

Contributions to StellarUI are welcome! Here's how you can contribute:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin feature/my-new-feature`
5. Submit a pull request

### Development Setup

1. Clone the repository
2. Open `Stellar.sln` in Visual Studio or your preferred IDE
3. Build the solution
4. Run one of the sample applications to test your changes

## License

StellarUI is licensed under the MIT License. See the LICENSE file for details.
