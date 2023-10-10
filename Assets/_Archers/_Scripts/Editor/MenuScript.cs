using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MenuScript : MonoBehaviour
{
    private const string MAIN_SCENE = "Assets/_Archers/_Scenes/MainScene.unity";
    private const string SPLASH_SCENE = "Assets/_Archers/_Scenes/SplashScene.unity";

    [MenuItem("Scenes/Splash - RUN #_0", false,1)]
    public static void SplashSceneRun()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(SPLASH_SCENE);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Scenes/Splash - Edit #_1", false, 2)]
    public static void SplashSceneEdit()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(SPLASH_SCENE);
    }

    [MenuItem("Scenes/Main - Edit #2", false, 3)]
    public static void MainSceneEdit()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(MAIN_SCENE);
    }

}