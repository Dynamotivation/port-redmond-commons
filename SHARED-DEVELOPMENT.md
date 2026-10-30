# Shared Redmond development

This repository contains the source-level libraries shared by the Redmond
Calculator and Notepad ports:

- `Redmond.Avalonia.Controls`
- `Redmond.Avalonia.Windowing`
- `Redmond.Shortcuts`

They remain normal .NET projects rather than published NuGet packages, keeping
local debugging and source navigation straightforward.

## Application integration

Each application pins this repository as a Git submodule named `redmond-commons`
at its repository root:

```text
port-redmond-calculator/
  redmond-commons/
  src/

port-redmond-notepad/
  redmond-commons/
  redmond/
```

Clone an application and its Commons revision with:

```sh
git clone --recurse-submodules <application-url>
git submodule update --init --recursive
```

The application project files reference projects below `redmond-commons`
directly. A commit in each application therefore records the exact Commons
revision it was built and tested against without requiring a package feed.

## Change workflow

1. Make and commit a change in `port-redmond-commons`.
2. Push the Commons commit.
3. Update the `redmond-commons` submodule in each consuming application.
4. Run `dotnet run --project Redmond.Commons.Verification`, then build and
   test both applications.
5. Commit and push the new submodule revisions.

Avoid Git subtree copies here: they duplicate shared source into both
application histories and make every common fix a reconciliation exercise.
