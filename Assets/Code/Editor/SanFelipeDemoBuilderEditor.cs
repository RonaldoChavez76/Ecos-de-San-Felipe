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
        EditorApplication.delayCall += AutoActualizarEscenaNivel1;
    }

    private static void AutoActualizarEscenaNivel1()
    {
        if (Application.isPlaying) return;
        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.name == "SampleScene")
        {
            SanFelipeDemoBuilder builder = Object.FindFirstObjectByType<SanFelipeDemoBuilder>();
            if (builder == null) return;

            if (GameObject.Find("Marca_GranMapa_SanFelipe_V3") == null || GameObject.Find("Parroquia_Exterior_Elementos") == null)
            {
                builder.GenerarTodoEnEditor();
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("<color=cyan><b>[San Felipe - Nivel 1]</b> Gran Mapa 81x65, Estructuras Reales, Puerta a Escena ParroquiaInterior y Sistema de Hordas por Zona actualizados en la Hierarchy.</color>");
            }
        }
        else if (activeScene.name == "ParroquiaInterior")
        {
            if (GameObject.Find("Parroquia_Interior_Zona") == null)
            {
                ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("<color=cyan><b>[San Felipe - Parroquia Interior]</b> Nave de Cruz Latina (33x43), Bancas, Altar, Acertijo de 3 Campanas y Horda Interior generados en la Hierarchy.</color>");
            }
        }
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
        EditorGUILayout.LabelField("⚔️ GENERADOR DE NIVEL 1 (GRAN VILLA Y PARROQUIA)", EditorStyles.boldLabel);

        GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
        if (GUILayout.Button("🌟 Generar TODO Nivel 1 (Gran Mapa 81x65 + Hordas + Puerta)", GUILayout.Height(36)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Todo Nivel 1");
            builder.GenerarTodoEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.5f);
        if (GUILayout.Button("🗺️ Regenerar Solo Gran Mapa 81x65 en Hierarchy", GUILayout.Height(32)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Mapa");
            builder.GenerarMapaEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        GUI.backgroundColor = new Color(0.95f, 0.4f, 0.7f);
        if (GUILayout.Button("👹 Regenerar Zonas de Hordas y NPCs en Hierarchy", GUILayout.Height(32)))
        {
            Undo.RecordObject(builder.gameObject, "Generar Hordas");
            builder.GenerarEnemigosEnEditor();
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        }

        EditorGUILayout.Space(6);
        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.9f);
        if (GUILayout.Button("🎬 Guardar Animaciones de Ronan en Assets/Animations", GUILayout.Height(30)))
        {
            PlayerAnimationsGenerator.GenerarTodasLasAnimacionesDelPlayer();
        }

        EditorGUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🗑️ Borrar Hordas/Enemigos", GUILayout.Height(24)))
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

    [MenuItem("San Felipe/🌟 Regenerar Nivel 1 Completo en Hierarchy")]
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
            ParroquiaInteriorBuilder interior = Object.FindAnyObjectByType<ParroquiaInteriorBuilder>();
            if (interior != null)
            {
                ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            else
            {
                Debug.LogWarning("Abre SampleScene o ParroquiaInterior para generar el nivel.");
            }
        }
    }

    [MenuItem("San Felipe/🚪 Abrir Escena: Nivel 1 Exterior (SampleScene)")]
    public static void AbrirEscenaExterior()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }
    }

    [MenuItem("San Felipe/⛪ Abrir Escena: Nivel 1 Interior (ParroquiaInterior)")]
    public static void AbrirEscenaInterior()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/Scenes/ParroquiaInterior.unity");
        }
    }
}
#endif
