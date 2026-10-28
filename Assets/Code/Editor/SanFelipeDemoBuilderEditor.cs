#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SanFelipeSceneAutoSetup
{
    static SanFelipeSceneAutoSetup()
    {
        EditorApplication.delayCall += AutoGenerarEnemigosSiFaltan;
    }

    private static void AutoGenerarEnemigosSiFaltan()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "SampleScene") return;
        if (Application.isPlaying) return;

        SanFelipeDemoBuilder builder = Object.FindFirstObjectByType<SanFelipeDemoBuilder>();
        if (builder == null) return;

        // Si ya existen los enemigos, solo asegurar que están conectados
        if (GameObject.Find("Enemigos_SanFelipe") != null || GameObject.Find("Sombra_Lanzadora") != null)
        {
            builder.ConectarEnemigosExistentes();
            return;
        }

        // Generar enemigos en la escena automáticamente
        builder.ConstruirGrupoEnemigosYEco();
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("<color=magenta><b>[San Felipe]</b> Enemigos_SanFelipe generados automáticamente en la Hierarchy con sus Animators.</color>");
    }
}

[CustomEditor(typeof(SanFelipeDemoBuilder))]
public class SanFelipeDemoBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SanFelipeDemoBuilder builder = (SanFelipeDemoBuilder)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("⚔️ GENERADOR DE ESCENA Y HIERARCHY", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.95f, 0.4f, 0.7f);
        if (GUILayout.Button("👹 Generar Enemigos Permanentes en Hierarchy", GUILayout.Height(36)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Enemigos");
            builder.GenerarEnemigosEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
        if (GUILayout.Button("🗺️ Generar Mapa Permanente en Hierarchy", GUILayout.Height(32)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Mapa");
            builder.GenerarMapaEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
        if (GUILayout.Button("🌟 Generar TODO (Mapa + Enemigos)", GUILayout.Height(36)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Todo");
            builder.GenerarTodoEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.9f);
        if (GUILayout.Button("🎬 Guardar Animaciones de Ronan en Assets/Animations", GUILayout.Height(32)))
        {
            PlayerAnimationsGenerator.GenerarTodasLasAnimacionesDelPlayer();
        }

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🗑️ Borrar Enemigos", GUILayout.Height(24)))
        {
            builder.LimpiarEnemigosEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }
        if (GUILayout.Button("🗑️ Borrar Mapa", GUILayout.Height(24)))
        {
            builder.LimpiarMapaEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
    }

    [MenuItem("San Felipe/👹 Generar Enemigos en Hierarchy")]
    public static void MenuGenerarEnemigos()
    {
        SanFelipeDemoBuilder builder = Object.FindFirstObjectByType<SanFelipeDemoBuilder>();
        if (builder != null)
        {
            builder.GenerarEnemigosEnEditor();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        else
        {
            Debug.LogWarning("No se encontró SanFelipeDemoBuilder en la escena activa.");
        }
    }

    [MenuItem("San Felipe/🌟 Generar TODO en Hierarchy")]
    public static void MenuGenerarTodo()
    {
        SanFelipeDemoBuilder builder = Object.FindFirstObjectByType<SanFelipeDemoBuilder>();
        if (builder != null)
        {
            builder.GenerarTodoEnEditor();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        else
        {
            Debug.LogWarning("No se encontró SanFelipeDemoBuilder en la escena activa.");
        }
    }
}
#endif
