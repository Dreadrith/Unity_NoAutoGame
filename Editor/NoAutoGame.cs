using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace DreadScripts.NoAutoGame
{
    internal static class NoAutoGame
    {
        private const bool isEnabled = true;
        private const bool closeGameWindow = true;

        private const string GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME = "UnityEditor.GameView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        private const string GAME_WINDOW_OPEN_SESS_KEY = "NoAutoGameWindowWasOpen";
        //private const string LAST_WINDOW_SESS_KEY = "NoAutoGameLastWindowAQN";
        private const int GAME_WINDOW_HANDLE_DELAY = 100;

        private static System.Type gameWindowType => _gameWindowType ?? (_gameWindowType = System.Type.GetType(GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME));
        private static System.Type _gameWindowType;
        private static CancellationTokenSource cts;

        //private static EditorWindow lastWindow;

        [InitializeOnLoadMethod]
        static void Register()
        {
            if (!isEnabled) return;
            EditorApplication.playModeStateChanged -= HandlePlaymodeChange;
            EditorApplication.playModeStateChanged += HandlePlaymodeChange;

            /*EditorApplication.update -= RememberCurrentWindow;
            EditorApplication.update += RememberCurrentWindow;*/
        }

        static void HandlePlaymodeChange(PlayModeStateChange change)
        {
            cts?.Cancel();

            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    RememberGameWindow();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    try
                    {
                        cts = new CancellationTokenSource();
                        _ = HandleGameWindow(cts.Token);
                    }
                    catch(OperationCanceledException){}
                    break;


                /*case PlayModeStateChange.ExitingPlayMode:
                    SessionState.EraseString(LAST_WINDOW_SESS_KEY);
                    break;*/
            }
        }

        static void RememberGameWindow()
        {
            var t = Type.GetType(GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME);
            if (t == null) return;
            SessionState.SetBool(GAME_WINDOW_OPEN_SESS_KEY, Resources.FindObjectsOfTypeAll(t).Length > 0);
        }

        static async Task HandleGameWindow(CancellationToken ct)
        {
            var gameWindowWasOpen = SessionState.GetBool(GAME_WINDOW_OPEN_SESS_KEY, true);
            if (gameWindowWasOpen) return;
            await Task.Delay(GAME_WINDOW_HANDLE_DELAY, ct);

            if (closeGameWindow)
            {
                var windows = Resources.FindObjectsOfTypeAll(gameWindowType);
                foreach (var w in windows)
                {
                    var ew = w as EditorWindow;
                    ew?.Close();
                }
            }

            //Attempt to remember the last focused window and load it when in play mode
            //This worked if last focused window was docked next to game view, otherwise it didn't
            /*else
            {

                var sessValue = SessionState.GetString(LAST_WINDOW_SESS_KEY, string.Empty);
                if (sessValue != string.Empty)
                {
                    Debug.Log("LOADING");
                    if (int.TryParse(sessValue[0].ToString(), out int i))
                    {
                        Debug.Log(i);
                        var AQN = sessValue.Substring(1, sessValue.Length - 1);
                        var t = System.Type.GetType(AQN);
                        if (t != null)
                        {
                            var allWindows = Resources.FindObjectsOfTypeAll(t);
                            if (i < allWindows.Length)
                            {
                                var w = allWindows[i] as EditorWindow;
                                w?.Focus();
                            }
                        }
                    }
                }
            }*/
        }

        /*static void RememberCurrentWindow()
        {
            var w = EditorWindow.focusedWindow;
            if (w == null || w == lastWindow) return;
            lastWindow = w;

            var AQN = w.GetType().AssemblyQualifiedName;
            if (AQN == GAME_WINDOW_TYPE_ASSEMBLY_FULL_NAME) return;

            var t = System.Type.GetType(AQN);
            if (t != null)
            {
                var allWindows = Resources.FindObjectsOfTypeAll(t);
                var i = allWindows.GetIndexOf(w2 => w2 == w);
                if (i != -1)
                    SessionState.SetString(LAST_WINDOW_SESS_KEY, $"{i}{AQN}");

            }
        }

        internal static int GetIndexOf<T>(this IEnumerable<T> collection, System.Func<T, bool> predicate)
        {
            int index = -1;
            using (var enumerator = collection.GetEnumerator())
                while (enumerator.MoveNext())
                {
                    checked { ++index; }
                    if (enumerator.Current == null) continue;

                    if (predicate(enumerator.Current))
                        return index;
                }
            return -1;
        }*/

    }
}
