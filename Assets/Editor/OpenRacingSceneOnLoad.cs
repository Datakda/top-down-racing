using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDownRacing.EditorTools
{
    /// <summary>
    /// Одноразовая помощь: открывает сцену гонки в редакторе (после перезапуска Unity
    /// редактор мог остаться на пустой сцене). Срабатывает ровно один раз.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenRacingSceneOnLoad
    {
        private const string FlagKey = "TopDownRacing.SceneOpenedOnce";

        static OpenRacingSceneOnLoad()
        {
            if (SessionState.GetBool(FlagKey, false))
            {
                return;
            }

            SessionState.SetBool(FlagKey, true);

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorSceneManager.OpenScene("Assets/Scenes/Racing.unity", OpenSceneMode.Single);
                    Debug.Log("[Racing] Открыта сцена гонки: Assets/Scenes/Racing.unity");
                };
            }
        }
    }
}
