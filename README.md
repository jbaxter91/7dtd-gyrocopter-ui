# Gyrocopter Attitude Indicator Mod

Adds a vertical attitude gauge and pitch readout to the HUD when piloting the gyrocopter in 7 Days to Die. The UI is fully configurable in-game via console commands.

## Features
- Vertical attitude indicator bar above the gyrocopter HUD (fuel/health).
- Numeric pitch display above the bar.
- Console command `attui` (alias `attitudeui`) for live UI adjustments.
- Change scale, position, colors (RGB or hex), font, and text visibility without rebuilding.

## Installation
1. Build the project (DLL will be placed in `Mod/AttitudeIndicator`).
2. Copy the `Mod/AttitudeIndicator` folder into your game's `Mods` directory.
3. Launch 7 Days to Die. Enter a gyrocopter to see the UI.

## Console Commands
All commands are entered in the in-game console (F1).

### Show Current Settings
```
attui show
```
Displays all current UI settings (scale, position, colors, font, text).

### Scale the UI
```
attui scale <float>
```
- Example: `attui scale 1.2` (makes the gauge 20% larger)

### Move the UI
```
attui movex <float>
attui movey <float>
```
- `movex` moves horizontally: positive = right, negative = left.
- `movey` moves vertically: positive = down, negative = up.
- Example: `attui movex 20` (moves gauge 20 pixels right)
- Example: `attui movey -10` (moves gauge 10 pixels up)

### Change Colors
You can use either RGB(A) values (0-255 or 0-1) or hex codes (`#RRGGBB` or `#RRGGBBAA`).

Targets:
- `base` — border and level line
- `level` — pitch indicator bar
- `font` — text color
- `bg` — background rectangle

Examples:
```
attui color base 218 165 32        # goldenrod border
attui color level 255 0 0          # red pitch bar
attui color font #ffffff           # white text
attui color bg #000000AA           # semi-opaque black background
```

### Change Font Size
```
attui font size <int>
```
- Example: `attui font size 22`

### Toggle Text On/Off
```
attui text on
attui text off
```
- Example: `attui text off` (hides pitch text)

### Reset to Defaults
```
attui reset
```
Restores all UI settings to their default values.

## Troubleshooting
- If the UI does not appear, ensure the mod is installed on the client and you are piloting a gyrocopter.
- Use `attui show` to verify current settings.
- All changes take effect immediately; no restart required.

## License
MIT

## Credits
Mod by Beefy Nugget / jbaxter91
