#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LightType = UnityEngine.LightType;

public class LevelEditor : EditorWindow
{
    private bool _isSnapEnabled = true;
    private float _snapValue = 1.0f;
    private int _sceneNumber = 1;

    [MenuItem("Tools/Level Editor")]
    private static void ShowWindow()
    {
        LevelEditor window = GetWindow<LevelEditor>();
        window.titleContent = new GUIContent("Level Editor");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Editor", EditorStyles.boldLabel);

        _isSnapEnabled = EditorGUILayout.Toggle("Enable Snap", _isSnapEnabled);

        if (_isSnapEnabled)
        {
            _snapValue = EditorGUILayout.FloatField("Snap Value", _snapValue);
        }
        else
        {
            EditorSnapSettings.move = new Vector3(0, 0, 0);
        }

        if (GUILayout.Button(_isSnapEnabled ? "Disable Snap" : "Enable Snap"))
        {
            _isSnapEnabled = !_isSnapEnabled;

            SceneView.RepaintAll();
        }

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }

        EditorSnapSettings.move = new Vector3(_snapValue, _snapValue, _snapValue);
        
        //New Level Creation
        if (GUILayout.Button("Create New Level"))
        {
            CreateNewLevel();
        }

        //Create Light
        if (GUILayout.Button("Create Light"))
        {
            CreateLight();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    #region Level Generation Region
    public void CreateNewLevel()
    {
        int sceneCount = EditorSceneManager.sceneCountInBuildSettings;
        _sceneNumber = sceneCount+1;
        string scenePath = "Assets/_Archers/_Scenes/LevelScenes/" + "Level" + _sceneNumber + ".unity";

        // Проверяем, существует ли сцена с таким путем
        if (AssetDatabase.LoadAssetAtPath(scenePath, typeof(SceneAsset)) != null)
        {
            Debug.LogWarning("Scene already exists at path " + scenePath + ". New scene was not created.");
        }
        else
        {
            // Создаем новую сцену
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            _sceneNumber++;
        }
    }
    
    public void CreateLight()
    {
        // Создание нового GameObject
        GameObject lightObject = new GameObject("Light");
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Добавление компонента Light на новый GameObject
        Light lightComponent = lightObject.AddComponent<Light>();

        // Настройка параметров компонента Light (опционально)
        lightComponent.type = LightType.Directional;
        lightComponent.color = Color.white;
        lightComponent.intensity = 1.0f;
    }

    #region NavMesh Region


   

    #endregion
    
    #endregion
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (_isSnapEnabled && Selection.activeTransform)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.green; 
            // Отображение текущего значения сетки снапинга на сцене
            Handles.Label(Selection.activeTransform.gameObject.transform.position, "Snap Value: " + _snapValue, style);
        }
    }
}

#endif