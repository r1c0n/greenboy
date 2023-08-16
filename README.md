# CoreBoy Green

A continuation of the .NET Core Gameboy emulator that started life as a port of Coffee-GB (https://github.com/trekawek/coffee-gb).

## Features

* Runs Gameboy and Gameboy Color games.
* Has a headless CLI mode
* Has a Windows-Only WinForms UI
* Can be used as a library in your own software

## Prerequisites

*  Visual Studio 2022
* .NET Core 3.1

## Usage

### Windows

Just run `CoreBoy.Windows` and load a ROM from the file menu!

### Mac / Linux

Command line:

Just run `CoreBoy.Avalonia` and load a ROM from the file menu!

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

- [ ] Support emulation for all Game Boy consoles
	- [x] Game Boy Classic
	- [x] Game Boy Color
	- [ ] Game Boy Advance
- [ ] All necessary features have been added `(Subject to change)`
	- [x] Audio (Windows)
	- [ ] Savegames
	- [ ] Controller input (XBOX, PlayStation)

## Lineage and Contributors

- Originally based on Coffee-GB (https://github.com/trekawek/coffee-gb).
- Ported to .NET Core by David Whitney
- Avelonia UI contributed by Bogdan Bara (https://github.com/fknzxlegend1)
- CoreBoy originally created by David Whitney (https://github.com/davidwhitney/CoreBoy)
- [Fixed interpolation by samstalhandske](https://github.com/davidwhitney/CoreBoy/pull/5)
- [Windows audio support by wcabus](https://github.com/davidwhitney/CoreBoy/pull/6)
