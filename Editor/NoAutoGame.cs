using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Reflection;

namespace DreadScripts.NoAutoGame
{
    internal static class NoAutoGame
    {
        #region Constants
        private const string GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME = "UnityEditor.GameView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        private const string GAME_WINDOW_OPEN_SESS_KEY = "NoAutoGameWindowWasOpen";
        private const string LAST_WINDOWS_SESS_KEY = "NoAutoGameLastWindowsIDs";

        private const string CLOSE_GAME_WINDOW_PREF_KEY = "NoAutoGameCloseGameWindow";
        private const string UNFOCUS_GAME_WINDOW_PREF_KEY = "NoAutoGameUnfocusGameView";
        private const int GAME_WINDOW_HANDLE_DELAY = 100;
        #endregion
        
        private static Type _gameWindowType;
        private static Type gameWindowType => _gameWindowType ?? (_gameWindowType = Type.GetType(GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME));
        private static readonly PropertyInfo hasFocusProperty = typeof(EditorWindow).GetProperty("hasFocus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        private static CancellationTokenSource cts;

        private static bool closeGameWindow;
        private static bool unfocusGameWindow;
        private static EditorWindow[] previouslyFocusedWindows;
        private static void HandlePlaymodeChange(PlayModeStateChange change)
        {
            cts?.Cancel();

            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    RememberWindows();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    try
                    {
                        cts = new CancellationTokenSource();
                        _ = HandleGameWindow(cts.Token);
                    }
                    catch(OperationCanceledException){}
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    SessionState.EraseString(LAST_WINDOWS_SESS_KEY);
                    if (!closeGameWindow && unfocusGameWindow && !SessionState.GetBool(GAME_WINDOW_OPEN_SESS_KEY, true))
                    {
                        var gameWindows = (EditorWindow[])Resources.FindObjectsOfTypeAll(gameWindowType);
                        if (gameWindows.Any() && !gameWindows.Any(w => previouslyFocusedWindows.Contains(w)))
                            foreach(var w in gameWindows)
                                w.Close();
                    }
                        
                    previouslyFocusedWindows = null;
                    break;
            }
        }

        private static void RememberWindows()
        {
            if (unfocusGameWindow && hasFocusProperty != null)
            {
                var idList = string.Join(",", Resources.FindObjectsOfTypeAll<EditorWindow>()
                                                       .Where(w => (bool) hasFocusProperty.GetValue(w))
                                                       .Select(w => w.GetInstanceID().ToString()));
                
                SessionState.SetString(LAST_WINDOWS_SESS_KEY, idList);
            }
            
            if (gameWindowType == null) return;
            var gameWindows = Resources.FindObjectsOfTypeAll(gameWindowType);
            SessionState.SetBool(GAME_WINDOW_OPEN_SESS_KEY, gameWindows.Length > 0); }

        private static async Task HandleGameWindow(CancellationToken ct)
        {
            if (!closeGameWindow && !unfocusGameWindow) return;
            var gameWindowWasOpen = SessionState.GetBool(GAME_WINDOW_OPEN_SESS_KEY, true);
            await Task.Delay(GAME_WINDOW_HANDLE_DELAY, ct);

            if (closeGameWindow && !gameWindowWasOpen)
            {
                var windows = Resources.FindObjectsOfTypeAll(gameWindowType);
                foreach (var w in windows)
                {
                    var ew = (EditorWindow)w;
                    ew.Close();
                }
            } else if (unfocusGameWindow)
            {
                var previousWindowsFocusList = SessionState.GetString(LAST_WINDOWS_SESS_KEY, string.Empty);
                if (string.IsNullOrWhiteSpace(previousWindowsFocusList)) return;
                
                previouslyFocusedWindows = previousWindowsFocusList
                                           .Split(',').Select(int.Parse)
                                           .Select(EditorUtility.InstanceIDToObject)
                                           .OfType<EditorWindow>().Where(w => w != null).ToArray();
                foreach (var w in previouslyFocusedWindows)
                    w.Focus();
            }
        }
        

        #region Initialization
        [InitializeOnLoadMethod]
        private static void Initialize() => LoadVariables();
        private static void LoadVariables()
        {
            closeGameWindow = EditorPrefs.GetBool(CLOSE_GAME_WINDOW_PREF_KEY, false);
            unfocusGameWindow = EditorPrefs.GetBool(UNFOCUS_GAME_WINDOW_PREF_KEY, true);
            RefreshHook();
        }

        private static void RefreshHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlaymodeChange;
            if (closeGameWindow || unfocusGameWindow)
                EditorApplication.playModeStateChanged += HandlePlaymodeChange;
        }
        #endregion
        
        #region Menu Items
        [MenuItem("DreadTools/Utility/NoAutoGame/AutoUnfocus")]
        private static void ToggleUnfocusGameWindow()
        {
            bool newValue = !EditorPrefs.GetBool(UNFOCUS_GAME_WINDOW_PREF_KEY, true);
            EditorPrefs.SetBool(UNFOCUS_GAME_WINDOW_PREF_KEY, newValue);
            Debug.Log($"[NoAutoGame] AutoUnfocus is now {(newValue ? "enabled" : "disabled")}.");
            unfocusGameWindow = newValue;
            RefreshHook();
        }
        
        [MenuItem("DreadTools/Utility/NoAutoGame/AutoClose")]
        private static void ToggleCloseGameWindow()
        {
            bool newValue = !EditorPrefs.GetBool(CLOSE_GAME_WINDOW_PREF_KEY, false);
            EditorPrefs.SetBool(CLOSE_GAME_WINDOW_PREF_KEY, newValue);
            Debug.Log($"[NoAutoGame] AutoClose is now {(newValue ? "enabled" : "disabled")}.");
            closeGameWindow = newValue;
            RefreshHook();
        }
        #endregion

    }
    
    
}
