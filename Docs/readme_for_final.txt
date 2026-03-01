==========================================================================
=                              Rhythm Dash                               =
=                            Pistachio Studio                            =
= Jiayi Chen, Yuxuan Liao, Lindsey Zhang, Xinyi Zhou, Xuanhan Elena Zhou =
==========================================================================

Start Scene Name: "Init"

How to Play:
- Hit the beat using the Space key or the Right Trigger to accelerate.
- Steer the vehicle using the A/D keys or the Right Stick.
- Collect power-ups by colliding with them.
- Avoid obstacles and getting bumped by AI opponents.
- Finish the track as quickly as possible.

Technical Implementation Requirements:
**It must be a 3D Game Feel game!**
  -RhythmDash is a fully 3D racing game with continuous player control and responsive interaction, 
  immersive environment simulation, and cute&squishy-vibed polish that achieve "game feel" quality.
  -We highly recommend using controllers to best experience input and vibration feedback, 
  highlighting the gameplay with a aesthetic sensation of control.
  -The game communicates player progress and outcome through gameplay feedback, UI, and interaction responses.
  -The player can replay/reset the experience after failure or after finishing thought the menu.

**Precursors to Fun Gameplay**
  -The goal of the game is communicated to the player in our tutorial level,
  the player can also choose from a variety of play styles and complete the game however they like.
  -The player must make moment-to-moment choices such as how to steer and position themselves, 
  whether to collect power-ups, and how to respond to AI opponents and collisions.
  -Consequences of actions include reward for correct beat hit and strategic use of power-ups, and punishment
  from collisions.
  -We included a tutorial to help players learn core controls before full play.
  -We tuned the paramters through playtesting so the game is well-paced and feels balanced. 
  Mind you the AI opponents are competitive! Do not assume they can be beaten trivially ><.

**3D Character/Vehicle with Real-Time Control**
  -Our character design is fully 3D and only using model & animation clips. All other aspects
  are self-implemented (including animation controllers, state machines, and various other inputs).
  -Fluid animation transitions from idle, running, flying/boosted, stunned, knocked over.
  -Audio and visual feedback are tied to player actions and interactions such as beat hit and pickups.
  -Our character uses customed-designed vehicle steering movement controller with motion blurring camera and low-latency inputs.

**3D World with Physics and Spatial Simulation**
  -The world includes physics-based interaction through colliders, rigidbody behavior, contact handling, and impact responses.
  -Graphics & effects & audio cues aligned with gameplay.
  -Character 3D simulation with 6dof.
  -Interactive map for the player to explore.
  -Physical interactions with AI enemies, environment & obstacles, and state-changing power-ups (ainmated with Mecanim).

**Real-time NPC Steering Behaviors / Artificial Intelligence**
  -Custom implemenation of AI unicorn enemies that race the player using state machine implementation.
  -Interacts with the player and environment through racing, collision, and power-ups.
  -Sensory feedback from different states with animation (e.g. being knocked over).
  -Difficulty is appropriate and not trivial!

**Polish**
  -Racing themed UI
  -Main menu for pause/quit/restart
  -Minimap and player progress on screen HUD.
  -Transparent direction arrow to indicate correct track navigation.
  -Overall cute & squishy aesthetic.


======================
=    Jiayi Chen      =
======================

Implement rhythm-based gameplay and visual feedback effects, music import
and playback with beat detection, speed/time indicator UI, 3D checkpoint
arrows, notification system, rhythm-based VFX feedback and environment
animations, gameplay-based post processing, beat indication UI, sound effects,
telemetry, controller input and vibration feedback support, minimap UI, level
transitions, debugging utilities, CI/CD with automated build support, runtime
setting system, and other small gameplay and interface tweaks.

Scripts:
EventType.cs, AudioAnalyzer.cs, AudioReactiveAlpha.cs,
AudioReactiveScale.cs, AudioReactiveTranslation.cs, BeatDetector.cs,
BeatHitDetector.cs, BeatIndicator.cs, BeatReactiveColor.cs, BeatSettings.cs,
EdgeGlow.cs, EqualizerSettings.cs, ForcePlay.cs, GlowSettings.cs,
IndicatorSettings.cs, MusicNotification.cs, MusicPlayer.cs, LevelManager.cs,
PauseController.cs, PauseManager.cs, ButtonHover.cs, IntroMovie.cs,
MainMenuUI.cs, NotificationCenter.cs, PauseMenuUI.cs, CommandInitializer.cs,
GameInitializer.cs, GlobalConfig.cs, EdgeGlow.shader,
BeatControllerVibration.cs, BeatSoundTrigger.cs, SoundManager.cs,
SoundSettings.cs, MenuAutoSelect.cs, SpeedIndicatorUI.cs, TimeIndicatorUI.cs,
CheckpointIndicator.cs, GameplayClock.cs, Helper.cs, RuntimeSettings.cs,
Telemetry.cs, RenderingController.cs

Assets: 
Level Manager.prefab, MainMenuUI.prefab, Notification Center.prefab,
PauseManager.prefab, PauseMenuUI.prefab, Rhythm Module.prefab,
Rhythm UI.prefab, TransitionManager.prefab,
CheckpointIndicator.prefab, PauseController.prefab,
SoundManager.prefab, Speed Indicator UI.prefab, Time Indicator UI.prefab


======================
=    Yuxuan Liao     =
======================
Implemented the 3C (character, camera and controller) framework. 
Implemented the tutorial level.

Scripts:
  - Assets/Scripts/Animation/EnemyAnimationController.cs
  - Assets/Scripts/Character/CharacterController.cs
  - Assets/Scripts/Character/PlayerCharacterController.cs
  - Assets/Scripts/Vehicle/Vehicle.cs
  - Assets/Scripts/Vehicle/VehicleMovement.cs
  - Assets/Scripts/Vehicle/VehicleSettings.cs
  - Assets/Scripts/Tutorial/TutorialEnemyMovementController.cs
  - Assets/Scripts/Tutorial/TutorialLevel.cs

Assets:
  - Assets/InputSystem_Actions.inputactions
  - Assets/Materials/PhysicsMaterial/FrictionlessVehicle.physicMaterial
  - Assets/Prefabs/Vehicle.prefab
  - Assets/Resources/Vehicle/DefaultVehicleSettings.asset
  - Assets/Prefabs/PlayerController.prefab
  - Assets/Prefabs/TutorialLevel.prefab

======================
=   Lindsey Zhang    =
======================

Implement session management (i.e. pre-start, lobby, countdown, racing, finished 
states), leaderboard/time-based scoring and finish conditions, and racetrack 
checkpoints for racing gameplay and game state logic. 

Scripts: 
Written: Checkpoint.cs, CountdownUI.cs, LeaderboardManager.cs, RaceProgressUI.cs,
SessionBasedBehavior.cs, SessionManager.cs, SessionState.cs, VehicleProgress.cs
Modified: PlayerCharacterController.cs, EventType.cs, BeatIndicator.cs, 
VehicleMovement.cs

Assets: Countdown UI.prefab, Floor Line.prefab, Leaderboard Entry.prefab, 
Leaderboard UI.prefab, Race Progress UI.prefab, StartandFinishLine.prefab

======================
=    Xinyi Zhou      =
======================

Set up environment (race track and barriers).
Implemented a configurable game AI system generated via scripts, supporting multiple difficulty levels.

Scripts: AIManager.cs, AIVehicleMovement.cs, PlayerVehicleMovement.cs, VehicleMovement.cs, Vehicle.cs

Assets: prop_cone.prefab, prop_plastic_block.prefab

======================
= Xuanhan Elena Zhou =
======================

Technical Contributions:
Character design, model & animation
Implemented animation controllers for Player and AI Enemy
Improved character movement/steering logic and AI state machine
Implemented Power-Ups: invincibility/speedboost/stun, visual & sound effects
Tutorial scene script & characters


Scripts & Assests:
Power-Ups:
Star.prefab, Star (Animation Controller), GlowingStar.anim, StarCollector.cs
SpeedBoost.prefab, Wings (Animation Controller) WingAnimation.anim, WingCollector.cs
Stun.prefab, BombStun.anim, StunCollector.cs
Character:
UnicornPlayer.prefab, UnicornEnemy.prefab
Unicorn_dash (Animation Controller), Unicorn_Enemy_dash (Animation Controller)
PlayerAnimationController.cs, EnemyAnimationController.cs
Vehicle.cs, VehicleMovement.cs, VehicleSettings.cs, PlayerVehicleMovement.cs, AIVehicleMovement.cs
Tutorial:
TutorialEnemyAnimationController.cs, TutorialEnemyMovementController.cs


======================
= Third-party Assets =
======================

Music from "Tron: Legacy" by Daft Punk (https://music.apple.com/us/album/tron-legacy-original-motion-picture-soundtrack/1440617583)

DOTween (HOTween v2) by Demigiant from Unity Asset Store (https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)

In-game Debug Console by yasirkula from Unity Asset Store (https://assetstore.unity.com/packages/tools/gui/in-game-debug-console-68068)

Easy Transitions by fen4flo from Unity Asset Store (https://assetstore.unity.com/packages/tools/gui/easy-transitions-225607)

FREE Skybox Extended Shader by BOXOPHOBIC from Unity Asset Store (https://assetstore.unity.com/packages/vfx/shaders/free-skybox-extended-shader-107400)

Race drag Start and Finish Line by RohitArt from Sketchfab (https://sketchfab.com/3d-models/race-drag-start-and-finish-line-f30a73703bb74d1fa4beacf731d1baf6)

Start Finish Race Checkered Line by ahmedsaeedmirza from SketchFab (https://sketchfab.com/3d-models/start-finish-race-checkered-line-b3c601819d214e77b983fe23b12879cc)

Cartoon Race Track by RCC Design from Unity Asset Store (https://assetstore.unity.com/packages/3d/environments/roadways/cartoon-race-track-oval-175061)

Hyper Casual FX by Lana Studio from Unity Asset Store (https://assetstore.unity.com/packages/vfx/particles/hyper-casual-fx-200333)

Cute Unicorn by SURIYUN from Unity Assert Store (https://assetstore.unity.com/packages/3d/characters/animals/mammals/cute-unicorn-114979)