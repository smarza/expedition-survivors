using UnityEngine;

namespace ProjectExpedition
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (Object.FindAnyObjectByType<GameDirector>() != null) return;
            var root = new GameObject("Project Expedition — Runtime");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameDirector>();
        }
    }
}
