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

Known Problem Areas:
- Ending condition bugged to not trigger game finish/leaderboard in Windows Build.

======================
=    Jiayi Chen      =
======================

Implement rhythm-based gameplay and visual feedback effects, music import
and playback with beat detection, the main and pause menus, notification UI,
level transitions, debugging utilities, and CI/CD with automated build support.

Scripts:
EventType.cs, AudioAnalyzer.cs, AudioReactiveAlpha.cs,
AudioReactiveScale.cs, AudioReactiveTranslation.cs, BeatDetector.cs,
BeatHitDetector.cs, BeatIndicator.cs, BeatReactiveColor.cs, BeatSettings.cs,
EdgeGlow.cs, EqualizerSettings.cs, ForcePlay.cs, GlowSettings.cs,
IndicatorSettings.cs, MusicNotification.cs, MusicPlayer.cs, LevelManager.cs,
PauseController.cs, PauseManager.cs, ButtonHover.cs, IntroMovie.cs,
MainMenuUI.cs, NotificationCenter.cs, PauseMenuUI.cs, CommandInitializer.cs,
GameInitializer.cs, GlobalConfig.cs, EdgeGlow.shader

Assets: 
Level Manager.prefab, MainMenuUI.prefab, Notification Center.prefab,
PauseManager.prefab, PauseMenuUI.prefab, Rhythm Module.prefab,
Rhythm UI.prefab, TransitionManager.prefab


======================
=    Yuxuan Liao     =
======================
Implemented the 3C (character, camera and controller) framework. 

Scripts:
  - Assets/Scripts/Animation/EnemyAnimationController.cs
  - Assets/Scripts/Character/CharacterController.cs
  - Assets/Scripts/Character/PlayerCharacterController.cs
  - Assets/Scripts/Vehicle/Vehicle.cs
  - Assets/Scripts/Vehicle/VehicleMovement.cs
  - Assets/Scripts/Vehicle/VehicleSettings.cs

Assets:
  - Assets/InputSystem_Actions.inputactions
  - Assets/Materials/PhysicsMaterial/FrictionlessVehicle.physicMaterial
  - Assets/Prefabs/Vehicle.prefab
  - Assets/Resources/Vehicle/DefaultVehicleSettings.asset

======================
=   Lindsey Zhang    =
======================

Implement session management (i.e. pre-start, lobby, countdown, racing, finished 
states), leaderboard/time-based scoring and finish conditions, and racetrack 
checkpoints for racing gameplay and game state logic. 

Scripts: 
Written: Checkpoint.cs, LeaderboardManager.cs, SessionBasedBehavior.cs, 
SessionManager.cs, SessionState.cs, VehicleProgress.cs
Modified: PlayerCharacterController.cs, EventType.cs, BeatIndicator.cs, 
VehicleMovement.cs

Assets: Floor Line.prefab, StartandFinishLine.prefab

======================
=    Xinyi Zhou      =
======================

Implemented environment (race track and barriers) and Game AI.

Scripts: AIVehicleMovement.cs, PlayerVehicleMovement.cs, VehicleMovement.cs, Vehicle.cs

Assets: Race track and prop_cone.prefab modified based on [Cartoon Race Track](https://assetstore.unity.com/packages/3d/environments/roadways/cartoon-race-track-oval-175061)

======================
= Xuanhan Elena Zhou =
======================

Implemented animation controllers for player and AI enemy; helped with implementing movement logic and states;
implemented powerup functionalities.

Scripts:
EnemyAnimationController.cs, PlayerAnimationController.cs, 
AIVehicleMovement.cs, Vehicle.cs, VehicleMovement.vs,
UnicornMovementController.cs, PlayerCharacterController.cs
StarCollector.cs

Assests:
star.prefab, Star (Animation Controller), GlowingStar.anim
UnicornPlayer1.prefab, Unicorn_dash (Animation Controller), Unicorn_Enemy_dash (Animation Controller)
