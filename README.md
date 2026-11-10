# Brassborn 🥊

> **🎮 Play the game live right now on itch.io:**  
> **👉 [https://sft-dev.itch.io/brassborn](https://sft-dev.itch.io/brassborn)**

---

## About The Game
**Brassborn** is a fast-paced 3D fighter game made in Unity (URP). Step into the arena, execute multi-hit combos, wield axes and melee attacks, and battle through challenging opponents backed by a driving soundtrack and punchy audio effects.

### Key Highlights
- **Basic Combat System**: Straightforward, responsive melee combat mechanics with attack chains, dash evasions, weapon stances (fists, axes), and locational damage (head, body, limbs).
- **Tactical Opponent AI**: Responsive bot behavior featuring adaptive distancing, anti-spam defense, and tactical counter-attacks.
- **Punchy Audio & Soundtrack**: Impactful randomized audio pools for hits and strikes, matched with energetic music tracks.
- **Stylized Visuals**: Universal Render Pipeline (URP) with comic-style hit VFX (`POW!`, `SMASH!`) and custom shaders.

---

## Game Architecture & Flow

### 🎬 Scene Structure
The game follows a narrative flow through the following scenes:

1. **`Menu.unity`** — Main title screen and entry point into the game.
2. **`WelcomeCutscene.unity`** — Cinematic introductory story sequence.
3. **`Map.unity`** — Overworld stage selector for choosing upcoming battles.
4. **`HomeScene.unity`** — Player hub / safehouse interior for story interactions.
5. **`CogScene.unity`** — Combat stage: Fighting Cog Jack at a gas pump in space.
6. **`RockScene.unity`** — Combat stage: Fighting Rock Jack in a town arena.
7. **`RedScene.unity`** — High-stakes arena battle against Red Jack.
8. **`REDESC.unity`** — Scripted story escape sequence and cutscene.
9. **`Congrat.unity`** — Victory screen displaying match results and return to hub.

---

### 📜 Script Breakdown

#### Combat & AI
- **`CombatInputHandler.cs`** — Core player fighter controller handling movement, jumping, crouching, dashing, weapon switching, and combo inputs.
- **`CombatBotHandler.cs`** — State-machine AI controller managing approaching, retreating, circling, and anti-spam defensive behaviors.
- **`NPCcombatInputHandler.cs`** — NPC-specific combat inputs and attack execution.
- **`PlayerHitbox.cs`** — Hitbox collision, damage calculation per body part (head, body, limbs), and trigger for visual hit effects.
- **`HealthBar.cs`** — Smooth health bar UI with animated damage transitions.
- **`MatchTimer.cs`** — Match round timer and time-out handling.

#### Audio & Effects
- **`RandomAudioPool.cs`** — Manages and randomizes strike and impact sounds for punchy, varied combat feedback.
- **`BeatManager.cs` & `PulseToTheBeat.cs`** — Samples the soundtrack's BPM to pulse UI elements and visual props in rhythm with the music.

#### Cutscenes & UI
- **`REDESCcutscenemrg.cs`** — Cinematic manager controlling camera movements, object lerping, and scripted events.
- **`TypeWriterEffect.cs`** — Typewriter-style dialog delivery with accompanying typing SFX.
- **`TextCycler.cs`** — Cycles text elements sequentially for cutscenes and UI screens.
- **`CongratENDSCRIPT.cs`** — Handles victory countdowns and seamless loop back to the home hub.
- **`HouseInp.cs` & `MenuINP.cs`** — Handles menu buttons and interactive hub prompts.
- **`KeyObjectActivator.cs`** — Quick utility to toggle game objects on key presses.

---

## Development Setup

### Requirements
- **Unity Version**: Recommended Unity 2022.3 LTS (URP)
- **Git**

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/PhantomBossMsn/Brassborn.git
   ```
2. Open **Unity Hub**.
3. Click **Add project from disk** and select the `Brassborn` folder.
4. Launch the project and start from `Assets/Scenes/Menu.unity`.

---

## Play in Browser
Don't want to build from source? Play the latest WebGL build in your browser:  
🔗 **[Play Brassborn on itch.io](https://sft-dev.itch.io/brassborn)**
