# Ballance

**A 3D physics-based balancing game built with Unity for CS583: 3D Game Programming.**

In Ballance, you guide Earth across a tiltable gravitational grid toward the Sun. The challenge is to manage Earth's momentum, avoid black holes, and optionally recover satellites on the way. You control **the platform, not Earth directly**.

## Gameplay

- **Tilt the grid** on two axes to make Earth roll through the level using Rigidbody physics.
- **Avoid black holes.** They deform the surface into physical gravity wells with openings; falling in ends the attempt. Falling off the platform also causes a loss.
- **Reach the Sun** to complete a level. A short absorption sequence places Earth into orbit before showing the results.
- **Recover up to three optional satellites per level** to earn a rating from zero to three stars. All three satellites are *not* required to finish.
- **Progress through five levels** of increasing difficulty. Completing a level unlocks the next, and your best satellite rating for each level is saved locally.

## Controls

| Input | Action |
| --- | --- |
| `W` / `Up Arrow` | Tilt the grid forward |
| `S` / `Down Arrow` | Tilt the grid backward |
| `A` / `Left Arrow` | Tilt the grid left |
| `D` / `Right Arrow` | Tilt the grid right |
| `Esc` | Pause / resume gameplay |

While paused, you can resume, restart the level, or return to the main menu. Restart and navigation options are also available on result screens.

## Features

- Five 3D levels, each with its own arrangement of gravitational obstacles.
- Deformed 3D grid whose collision mesh follows the visible surface.
- Depth-colored grid lines to make gravitational wells easier to read.
- Animated photon-like particles traveling along the grid and spiraling into black holes.
- Earth, Sun, orbiting planets, and space visuals.
- Three collectible satellites per level and persistent best-star ratings using Unity `PlayerPrefs`.
- Main menu, level selection, illustrated **How to Play** pages, pause menu, and win/loss screens.
- Gameplay sound effects and movement-responsive rolling audio; no continuous background soundtrack.

## Running the Project

1. Clone the repository:

   `git clone https://github.com/gugatchanturia/Ballance.git`

2. Open **Unity Hub**, choose **Add project from disk**, and select the cloned `Ballance` folder.
3. Open the project with **Unity 6.5 (6000.5.9f1)**, or install that editor version through Unity Hub if prompted. The project uses the **Universal Render Pipeline (URP)** and the **Input System** package.
4. Open `Assets/Scenes/GameScenes/MainMenu.unity` and press **Play** in the Unity Editor.

For a standalone build, include `MainMenu` and all five `LevelXX-3D` scenes in **File > Build Profiles > Scene List**, with `MainMenu` first.

### Main Scenes

| Scene | Purpose |
| --- | --- |
| `Assets/Scenes/GameScenes/MainMenu.unity` | Main menu, level selection, and instructions |
| `Assets/Scenes/GameScenes/Level01-3D.unity` | Level 1 |
| `Assets/Scenes/GameScenes/Level02-3D.unity` | Level 2 |
| `Assets/Scenes/GameScenes/Level03-3D.unity` | Level 3 |
| `Assets/Scenes/GameScenes/Level04-3D.unity` | Level 4 |
| `Assets/Scenes/GameScenes/Level05-3D.unity` | Level 5 |

### Original 2D Prototype

`Assets/Scenes/TestScenes/Level00.unity` contains an early playable prototype of the **original 2D one-pager design**. It demonstrates the basic platform-balancing mechanics but does not contain the finished visuals or level designs.

The original concept used a 2D ball and a platform whose **left and right ends could be raised and lowered independently**. During development, the project evolved into the final fixed-camera 3D gravitational-grid game while retaining the central idea of using platform movement and physics to guide an object around hazards.

## Project Notes

- The five main levels use the exact scene names shown above (including hyphens).
- Unlocks and star ratings are stored locally on the player's machine via `PlayerPrefs`; they do not sync between devices.
- The **How to Play** screen explains the updated 3D controls and mechanics.
- Art and audio assets used in the project may have their own licenses. Ownership of third-party assets is not claimed by this repository.

## Course Assignment

Created for **CS583: 3D Game Programming**. The project is an implementation and extension of the Ballance one-page design, with a separate design-changes description submitted for the assignment.