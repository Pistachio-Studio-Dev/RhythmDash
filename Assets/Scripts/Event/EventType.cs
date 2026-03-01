using UnityEngine;
using UnityEngine.Events;

namespace Event
{
    public class Dummy : UnityEvent
    {
    }

    public class MusicStarted : UnityEvent<string>
    {
    }

    public class Beat : UnityEvent
    {
    }

    public class BeatWithAmplitude : UnityEvent<float>
    {
    }

    public class BeatAhead : UnityEvent
    {
    }

    public class BeatAheadWithAmplitude : UnityEvent<float>
    {
    }

    public class BeatHit : UnityEvent
    {
    }

    public class BeatHitFailed : UnityEvent
    {
    }

    public class LevelLoaded : UnityEvent<string>
    {
    }

    public class SessionStateChanged : UnityEvent<SessionState, SessionState>
    {
    }

    public class VehicleFinished : UnityEvent<VehicleProgress>
    {
    }

    public class VehicleLapFinished : UnityEvent<VehicleProgress>
    {
    }

    public class CollectPowerUp : UnityEvent<GameObject>
    {
    }

    public class Pause : UnityEvent
    {
    }

    public class Resume : UnityEvent
    {
    }

    public class TutorialEnemyHit : UnityEvent
    { }

}
