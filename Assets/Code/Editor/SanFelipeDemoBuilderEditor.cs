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

        int missingRemoved = LimpiarScriptsFaltantesEnEscena(activeScene);
        bool tieneCorazonSinScript = ExisteObjetoSinComponente<HeartPickup>("Corazon_Curativo");

        if (activeScene.name == "SampleScene")
        {
            SanFelipeDemoBuilder builder = Object.FindAnyObjectByType<SanFelipeDemoBuilder>();
            if (builder == null) return;

            if (GameObject.Find("Marca_GranMapa_SanFelipe_V4") == null ||
                GameObject.Find("Zona_Parroquia_Exterior_Interactivos") == null ||
                missingRemoved > 0 ||
                tieneCorazonSinScript)
            {
                if (GameObject.Find("Marca_GranMapa_SanFelipe_V4") == null)
                {
                    builder.GenerarTodoEnEditor();
                }
                else
                {
                    ParroquiaExteriorBuilder.ConstruirElementosExteriorParroquia();
                }
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("<color=cyan><b>[San Felipe - Nivel 1]</b> Escena verificada, scripts de corazones/pedestales enlazados y referencias faltantes limpiadas.</color>");
            }
        }
        else if (activeScene.name == "ParroquiaInterior")
        {
            bool tienePedestalSinScript = ExisteObjetoSinComponente<BellPedestalNode>("Pedestal_Campana_1");
            if (GameObject.Find("Marca_Parroquia_Interior_V4") == null ||
                missingRemoved > 0 ||
                tieneCorazonSinScript ||
                tienePedestalSinScript)
            {
                ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("<color=cyan><b>[San Felipe - Parroquia Interior V4]</b> Nave de Cruz Latina (33x43), Pedestales, Corazones y Horda Interior actualizados sin scripts faltantes.</color>");
            }
        }
    }

    private static bool ExisteObjetoSinComponente<T>(string nombreObjeto) where T : Component
    {
        GameObject go = GameObject.Find(nombreObjeto);
        return go != null && go.GetComponent<T>() == null;
    }

    public static int LimpiarScriptsFaltantesEnEscena(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;
        int totalRemoved = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t != null && t.gameObject != null)
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    if (count > 0)
                    {
                        totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }
                }
            }
        }
        return totalRemoved;
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
            SanFelipeSceneAutoSetup.LimpiarScriptsFaltantesEnEscena(builder.gameObject.scene);
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
            SanFelipeSceneAutoSetup.LimpiarScriptsFaltantesEnEscena(builder.gameObject.scene);
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
        Scene activeScene = SceneManager.GetActiveScene();
        SanFelipeSceneAutoSetup.LimpiarScriptsFaltantesEnEscena(activeScene);

        SanFelipeDemoBuilder builder = Object.FindAnyObjectByType<SanFelipeDemoBuilder>();
        if (builder != null)
        {
            builder.GenerarTodoEnEditor();
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
        else
        {
            ParroquiaInteriorBuilder interior = Object.FindAnyObjectByType<ParroquiaInteriorBuilder>();
            if (interior != null)
            {
                ParroquiaInteriorBuilder.ConstruirInteriorParroquia();
                EditorSceneManager.MarkSceneDirty(activeScene);
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
