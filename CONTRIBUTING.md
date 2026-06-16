# Contributing to NavVolume

First off, thank you for taking the time to contribute!

This document explains how to set up the project, the conventions the codebase follows, and the workflow for getting a change merged.
By participating in this project you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Ways to contribute

- **Report a bug**: open a [bug report](https://github.com/javimanotas/NavVolume/issues/new/choose).
- **Request a feature**: open a [feature request](https://github.com/javimanotas/NavVolume/issues/new/choose).
- **Improve the docs**: typos, clarifications and examples are all welcome.
- **Submit code**: fixes and features via a pull request (see below).

If you are planning a large change, please open an issue to discuss it first so we can agree on the approach before you invest time in it.

## Prerequisites

- **Unity 6000.3** (Unity 6) or newer. Match the version in [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt) when possible.
- **[Git LFS](https://git-lfs.com/)**: binary assets are stored with LFS. See the [Installation section of the README](README.md#installation) for details.
- **[.NET SDK 8](https://dotnet.microsoft.com/download)**: only needed to run the code formatter locally (see below).

## Getting started

```bash
git lfs install
git clone https://github.com/javimanotas/NavVolume.git
```

Open the project with Unity Hub, then open `Assets/NavVolume/Demo/Scenes/Demo.unity` to verify everything works.

## Development workflow

1. **Fork** the repository and create a topic branch off `main`:
   ```bash
   git checkout -b feat/new-feature
   ```
2. Make your change (one logical change per pull request).
3. **Format** the code and **run the tests** locally (see below).
4. **Commit** using the Conventional Commits style (see below).
5. **Open a pull request** against `main`.

CI runs the formatter and the full test suite on every pull request, both must pass before a change can be merged.

## Code style & formatting

C# code is formatted with **[CSharpier](https://csharpier.com/)**.
CI fails if any file is not formatted, so please run it before pushing:

```bash
dotnet tool install csharpier --global

# Format the whole Assets folder
csharpier format Assets

# Or just check, like CI does
csharpier check Assets
```

Some coding conventions that can't be specified with `csharpier` can be found on the [styleguide](Docs/csharp-style-guide.md).

## Tests

Tests use the **Unity Test Framework** and are split into two assemblies:

- **EditMode**: `Assets/NavVolume/Tests/EditMode`
- **PlayMode**: `Assets/NavVolume/Tests/PlayMode`

Run them from the Unity Editor via **Window > General > Test Runner**, or from the command line:

```bash
# EditMode
Unity -batchmode -runTests -projectPath . -testPlatform editmode -logFile -

# PlayMode
Unity -batchmode -runTests -projectPath . -testPlatform playmode -logFile -
```

Please add or update tests for any behavior you change, and make sure both modes pass before opening a pull request.

## Commit messages

This project uses **[Conventional Commits](https://www.conventionalcommits.org/)**. Use a type prefix so the history stays readable:

```
feat: implement path replanning
fix: clamp navigable query when out of bounds
perf: optimize neighbor linking
refactor: split jobs into separate files
docs: document the avoidance parameters
test: add coverage for the min-heap
...
```

Make sure to use those prefixes in the branches names to describe their purpouses.
