# GreenBoy

GreenBoy is a rebrand of CoreBoy Green (CBG), a continuation of [CoreBoy](https://github.com/davidwhitney/CoreBoy) by David Whitney. CoreBoy began as a .NET port of [Coffee-GB](https://github.com/trekawek/coffee-gb).

## Features

* Runs Gameboy and Gameboy Color games.
* Has a headless CLI mode
* Has a Windows-Only WinForms UI
* Can be used as a library in your own software

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* An editor or IDE with .NET 10 support

CLI image decoding uses [SkiaSharp](https://www.nuget.org/packages/SkiaSharp), licensed under MIT. BMP frame encoding and Windows screenshots require no additional imaging package.

## Usage

Build the solution:

```sh
dotnet build src/GreenBoy.sln
```

The WinForms project requires Windows. On macOS or Linux, build or run the Avalonia or CLI project directly.

### Windows

```sh
dotnet run --project src/GreenBoy.Windows
```

Load a ROM from the Emulator menu. To publish a self-contained Windows build, run `src/self-contained.cmd`.

### Mac / Linux

```sh
dotnet run --project src/GreenBoy.Avalonia
```

Load a ROM from the Emulator menu.

### CLI

```sh
dotnet run --project src/GreenBoy.Cli -- game.gb
```

Add `--interactive` to play in the console.

## Tests

```sh
dotnet test src/GreenBoy.Test.Unit
dotnet test src/GreenBoy.Test.Integration
```

Integration tests use the bundled Blargg and Mooneye ROMs. Five existing boot ROM failures are tagged `KnownBootFailure`; the full integration command above still runs them.

To run the ROM regression checks used by CI:

```sh
dotnet test src/GreenBoy.Test.Integration --filter "TestCategory!=KnownBootFailure"
```

### Continuous integration

GitHub Actions builds the Release solution and runs unit tests and ROM regressions on Windows and Ubuntu for pushes and pull requests. The Linux build uses `-p:EnableWindowsTargeting=true` to compile the WinForms project alongside the portable projects. The SDK selection is limited to stable .NET 10 releases by `global.json`.

Known boot failures run in a separate, non-blocking step and remain visible in the logs and uploaded TRX reports. Remove each ROM from `KnownBootFailureRoms` in `src/GreenBoy.Test.Integration/Mooneye/GeneralTest.cs` when it is fixed so it becomes a required regression check. Every other active test is required to pass; tests already marked ignored retain that status.

Each OS uploads a `test-results-<os>` artifact, including reports from failed test runs, retained for 14 days. The workflow can also be started manually from the Actions tab.

## Controls

	LeftArrow = Left
	RightArrow = Right
	UpArrow = Up
	DownArrow = Down
	Z = A
	X = B
	Enter = Start
	Backspace = Select

## Roadmap

- [ ] Emulation for all Game Boy consoles
	- [x] Game Boy Classic
	- [x] Game Boy Color
	- [ ] Game Boy Advance
- [ ] All necessary features have been added `(Subject to change)`
	- [x] Audio (Windows)
	- [ ] Savegames
	- [ ] Controller input (XBOX, PlayStation)
	- [ ] Customizable keybinds

## Lineage and Contributors

- CoreBoy Green (CBG) continued CoreBoy and is now rebranded as GreenBoy.
- Originally based on Coffee-GB (https://github.com/trekawek/coffee-gb).
- [CoreBoy](https://github.com/davidwhitney/CoreBoy) created and ported to .NET Core by David Whitney.
- Avalonia UI contributed by Bogdan Bara (https://github.com/fknzxlegend1)
- [Fixed interpolation by samstalhandske](https://github.com/davidwhitney/CoreBoy/pull/5)
- [Windows audio support by wcabus](https://github.com/davidwhitney/CoreBoy/pull/6)
