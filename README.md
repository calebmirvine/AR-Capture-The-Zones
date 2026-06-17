# AR Capture The Zones

AR Capture The Zones is an augmented reality game built with Unity and AR Foundation. Inspired by "Conquest" game modes, it transforms any physical, real-world room into an interactive spatial playfield. 

Players use their device to map their environment, after which the game dynamically anchors capture zones to the physical floor. To capture a zone, players must physically navigate their real-world environment and hold their position within the augmented zone boundaries.

## 🎥 Demo
[Watch the Gameplay Demo](https://www.youtube.com/watch?v=LVPtyxqMTok)

## ⚙️ Technical Features

* **Environmental Scanning:** Utilizes AR Foundation's plane detection to continuously map real-world flat surfaces and floors.
* **Dynamic Playfield Anchoring:** The system algorithmically identifies the largest valid detected plane and securely anchors the 3D playfield and capture zones to that physical coordinate.
* **Spatial Interaction:** Replaces traditional joystick movement with physical real-world navigation, requiring the user's device camera to physically enter and remain inside the AR zone coordinates to trigger capture events.

## 🚀 How It Works

1. **Scan:** The user slowly pans their device camera across the floor to build a spatial map of the room.
2. **Confirm:** The application highlights the largest continuous flat surface it detects. The user confirms this plane to set the stage.
3. **Spawn & Anchor:** The game spawns the capture zones, locking them permanently to the tracked physical floor. 
4. **Capture:** The player walks toward the zones in the real world. Entering a zone begins the capture process, all while avoiding enemy attacks (with extra help from powerups). 

## 🛠️ Built With
* **Engine:** Unity
* **Framework:** AR Foundation (ARCore / ARKit)
* **Language:** C#

---
*Project Origin: This application was originally developed as the final game project for ICS 223 at Camosun College.*
