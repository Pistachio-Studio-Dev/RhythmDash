using System.Collections;
using UnityEngine;

public static class GameplayClock
{
    public static bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsGameplayPaused;
    public static float Time => (float)TimeAsDouble;
    public static double TimeAsDouble => PauseManager.Instance != null
        ? PauseManager.Instance.CurrentGameplayTimeAsDouble
        : UnityEngine.Time.timeAsDouble;

    public static IEnumerator WaitForSeconds(double seconds)
    {
        double targetTime = TimeAsDouble + seconds;
        while (TimeAsDouble < targetTime)
        {
            yield return null;
        }
    }
}
