---
on:
  schedule: weekly on monday
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: read

engine: copilot

network:
  allowed:
    - defaults
    - dotnet

# global.json pins the SDK to 10.0.100 with latestFeature roll-forward, so the
# runner needs a .NET 10 SDK before any dotnet command will run.
runtimes:
  dotnet:
    version: "10.x"

tools:
  github:
    toolsets: [context, repos]
  bash:
    - "dotnet *"
    - "git status*"
    - "git diff*"
  edit:

safe-outputs:
  create-pull-request:
    max: 1
    draft: false
  add-labels:
    max: 2
  missing-tool:
---

# Dependency update

Check whether any NuGet package used by StellarUI has a newer stable release, and
open a single pull request updating the ones that are safe to take.

Every package version in this repository lives in `Directory.Packages.props`.
Central package management is enabled, so **that is the only file whose versions
you should edit**. Do not add `Version=` attributes to any `.csproj`.

## Rules about what may be updated

These are not suggestions. Each one exists because taking the newest version
broke something real.

**Never raise `Microsoft.CodeAnalysis.*` above 5.0.0.** This includes
`Microsoft.CodeAnalysis.CSharp`. `Stellar.SourceGenerators` is a Roslyn component,
and a source generator only loads in a host whose Roslyn is at least the version it
references. The .NET 10 SDK feature bands ship Roslyn 5.0 on 10.0.1xx, 5.3 on
10.0.2xx and 5.6 on 10.0.3xx. Referencing 5.6 would silently stop the generator
loading for every consumer below SDK 10.0.3xx, and nothing would fail the build —
they would just get no generated code. For analyzers and generators the reference
is a compatibility floor, not a version to maximise. Leave it alone.

**Keep Avalonia on the 11.3.x line.** Avalonia itself has shipped 12.x, but
`Avalonia.ReactiveUI` has no 12.x release and `Stellar.Avalonia` depends on it.
Taking Avalonia 12 while that package sits at 11.3.9 produces a build that
compiles and then fails at runtime. Patch updates within 11.3.x are fine. If you
find that `Avalonia.ReactiveUI` has finally published a 12.x version, do not
attempt the upgrade yourself — say so in the pull request body and leave the
versions unchanged, because it also needs the ReactiveUI initialisation in
`Stellar.Avalonia/Extensions/AppBuilderExtensions.cs` revisited.

**Stable releases only.** No previews, betas or release candidates, even when they
are the newest version on nuget.org. `global.json` sets `allowPrerelease: false`.
The one exception already in the file is `stylecop.analyzers`, which has no current
stable release; keep it on its `1.2.0-beta.*` line and update within that line.

**Do not migrate xunit to v3.** `xunit.v3` is a different package id, not an
upgrade, and it changes how tests are hosted. Keep `xunit` on its 2.x line.

**Do not change target frameworks**, `global.json`, or anything in a `.csproj`.
If a package update appears to require a framework change, stop and describe the
problem in the pull request body instead.

## How to verify before opening anything

Run these and make sure they pass. Do not open a pull request if they do not.

```
dotnet restore Stellar.slnf
dotnet build Stellar.slnf --configuration Release --no-restore
dotnet test Stellar.UnitTests/Stellar.UnitTests.csproj --configuration Release --no-build
```

`Stellar.slnf` is the subset that builds without the MAUI workload. The MAUI
packages and the sample apps are verified by CI on the pull request, not here.

If the build or tests fail after an update, remove that package from the batch,
re-verify, and mention in the pull request body which package you dropped and what
the failure was. A smaller correct pull request is much better than a large broken
one.

## Labelling

Apply the `dependencies` label to every pull request you open.

Also apply `skip-samples` **only when the batch contains at least one major version
bump**. That label makes CI skip the two MAUI sample app builds, which take about
twelve minutes. Batches of patch and minor updates must not carry it, so those get
the full sample coverage.

## The pull request

Open exactly one pull request containing all the updates you took. In the body:

- List each package as `id: old -> new`, grouped into major, minor and patch.
- State plainly which packages you deliberately did not update and why, especially
  any you skipped because of the rules above.
- Note anything you dropped from the batch because it failed verification.
- Say which verification commands you ran and that they passed.

Do not merge the pull request. Do not enable auto-merge. A human reviews and merges.

If every package is already current, do not open a pull request and do not open an
issue. Finishing quietly is the correct outcome.
