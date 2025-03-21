# Lumberjack Simulator

A tycoon/factory style game about moving some lumber around. Start by selling firewood and work your way up to manufacturing lumber in your own lumber mill. 

#### [v0.1 Demo Video](https://youtu.be/ZvO9VPx8GT0?si=jYTOtr3RS7Tjy18j)

## Get the game

[Downloads](https://drive.google.com/drive/folders/1RLs5GTHi1n1WWJeCwzsjZQ_pqcQiGAX6?usp=sharing)

Pre-compiled versions (for Windows) are served from the above link. Download the latest version, extract it, and run LumberSim.exe.<br>
Or you can compile the game yourself- see [build instructions](#build-instructions).

## How to play

Controls:
- WASD To move
- Shift to sprint
- Q to switch toolbelt item
- Mouse left for current toolbelt primary

The toolbelt is represented by the text in the bottom right of the screen. Here is a table of options and what you can do with them:

Tool | Primary
-----|--------
Hands | Picks up a log; must be hovering over a physics enabled log and the log must be under the weight limit.
Axe | Cuts a log; a log may take multiple swings to cut, keep cutting on the yellow dot to make progress in one spot. 

## Build Instructions

Because of how git stores large files, there are some extra steps you have to take before Lumberjack Simulator can be built locally.

The build instructions assume you have:
- Unity 2021.3.17f1 installed.
- The [EMullen packages](https://drive.google.com/file/d/1FLOMNzrzfUfUotHyN5g3abZDlzj5hizf/view?usp=sharing) file downloaded.
- The [large assets](https://drive.google.com/file/d/1WJ3VFdEEOhBZz_m-w06kim2S0X_U8ezq/view?usp=sharing) file downloaded; version __v1__.

1. Clone the Git repo:
```
git clone https://github.com/east-01/LumberSim.git
```
2. Place the __extracted__ (not .zip versions) in the Assets directory, your file structure should look like: 
```
<lumbersim install>\Assets\LargeAssetsvX
<lumbersim install>\Assets\EMullenPackages
```
3. The game can be built as normal now.
