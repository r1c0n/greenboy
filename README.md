# GreenBoy

A continuation of [CoreBoy Green (CBG)](https://gitlab.com/coreboy-green/emu), the .NET Game Boy emulator that started life as a port of [Coffee-GB](https://github.com/trekawek/coffee-gb).

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

Integration tests use the bundled Blargg and Mooneye ROMs. Some ROMs expose existing emulation failures or reach their test timeout.

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

- GreenBoy continues [CoreBoy Green (CBG)](https://gitlab.com/coreboy-green/emu), maintained by OfficialB.
- Originally based on Coffee-GB (https://github.com/trekawek/coffee-gb).
- Ported to .NET Core by David Whitney
- Avalonia UI contributed by Bogdan Bara (https://github.com/fknzxlegend1)
- CoreBoy originally created by David Whitney (https://github.com/davidwhitney/CoreBoy)
- [Fixed interpolation by samstalhandske](https://github.com/davidwhitney/CoreBoy/pull/5)
- [Windows audio support by wcabus](https://github.com/davidwhitney/CoreBoy/pull/6)
