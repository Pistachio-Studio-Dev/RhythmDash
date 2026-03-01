// [ Written by Jiayi ]

using System.IO;
using IngameDebugConsole;
using UnityEngine;
using Event;

public static class CommandInitializer
{
    [RuntimeInitializeOnLoadMethod]
    public static void Init()
    {
        DebugLogConsole.AddCommand(
            "quit",
            "quit this application",
            Application.Quit
        );

        DebugLogConsole.AddCommand<int>(
            "load_scene_by_id",
            "Load a scene by ID",
            LevelManager.Instance.ForceLoadScene
        );

        DebugLogConsole.AddCommand<string>(
            "load_scene_by_name",
            "Load a scene by name",
            LevelManager.Instance.ForceLoadScene
        );

        DebugLogConsole.AddCommand(
            "print_scene_name",
            "Print name of the current active scene",
            () => { Debug.Log(LevelManager.Instance.CurrentLevelName); }
        );

        DebugLogConsole.AddCommand(
            "list_scenes",
            "List available scenes",
            LevelManager.Instance.PrintAllScenes
        );

        DebugLogConsole.AddCommand(
            "list_music",
            "List loaded music",
            () => { MusicPlayer.Instance?.PrintMusicNameList(); }
        );

        DebugLogConsole.AddCommand(
            "get_current_music",
            "Print current music name",
            () => { MusicPlayer.Instance?.PrintCurrentMusicName(); }
        );

        DebugLogConsole.AddCommand(
            "switch_music",
            "switch current playing music",
            (int index) => { MusicPlayer.Instance?.Play(index); }
        );
        
        DebugLogConsole.AddCommand(
            "refresh_music",
            "Refresh loaded music",
            () => { MusicPlayer.Instance?.Initialize(); }
        );
        
        DebugLogConsole.AddCommand(
            "start_playing",
            "Start playing music",
            () => { MusicPlayer.Instance?.Play(); }
        );
        
        DebugLogConsole.AddCommand(
            "stop_playing",
            "Stop playing music",
            () => { MusicPlayer.Instance?.Stop(); }
        );
        
        DebugLogConsole.AddCommand(
            "next_music",
            "Switch to the next music",
            () => { MusicPlayer.Instance?.Next(); }
        );
        
        DebugLogConsole.AddCommand(
            "main_menu",
            "Go to main menu",
            LevelManager.Instance.MainMenu
        );
        
        DebugLogConsole.AddCommand(
            "current_level",
            "Reload current level",
            LevelManager.Instance.CurrentLevel
        );
        
        DebugLogConsole.AddCommand(
            "next_level",
            "Go to next level",
            LevelManager.Instance.NextLevel
        );
        
        DebugLogConsole.AddCommand<string>(
            "show_notification",
            "Show notification",
            NotificationCenter.Instance.ShowNotification
        );
        
        DebugLogConsole.AddCommand(
            "finish_race",
            "Finish current race",
            () => {SessionManager.Instance?.SetState(SessionState.Finished);}
        );
    }
}