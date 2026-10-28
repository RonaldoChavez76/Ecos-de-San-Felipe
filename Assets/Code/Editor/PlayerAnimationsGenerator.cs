#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public class PlayerAnimationsGenerator
{
    static PlayerAnimationsGenerator()
    {
        EditorApplication.delayCall += GenerarAnimacionesSiFaltan;
    }

    private static void GenerarAnimacionesSiFaltan()
    {
        // Si falta alguna de las animaciones nuevas del jugador (ej. Player_AttackDown), generarlas
        if (!System.IO.File.Exists("Assets/Animations/Player_AttackDown.anim") ||
            !System.IO.File.Exists("Assets/Animations/Player_Death.anim"))
        {
            GenerarTodasLasAnimacionesDelPlayer();
        }
    }

    [MenuItem("San Felipe/🎬 Generar Animaciones de Player en Assets-Animations")]
    public static void GenerarTodasLasAnimacionesDelPlayer()
    {
        string texturePath = "Assets/Sprites/Human-Soldier-Cyan.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite sp)
            {
                spriteMap[sp.name] = sp;
            }
        }

        if (spriteMap.Count == 0)
        {
            Debug.LogError("No se encontraron sprites recortados en " + texturePath);
            return;
        }

        System.IO.Directory.CreateDirectory("Assets/Animations");

        // Helper para obtener sprite por fila (0..7) y columna (0..23)
        // Fila 0: Sur (Abajo), Fila 2: Este (Derecha), Fila 4: Norte (Arriba), Fila 6: Oeste (Izquierda)
        Sprite GetSprite(int row, int col)
        {
            int index = row * 24 + col;
            string key = "Human-Soldier-Cyan_" + index;
            if (spriteMap.ContainsKey(key)) return spriteMap[key];
            return null;
        }

        // =====================================================================
        // 1. ANIMACIONES DE IDLE (SUR, NORTE, ESTE, OESTE)
        // =====================================================================
        CrearClipSprite("Assets/Animations/Player_IdleDown.anim", new[] { GetSprite(0, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/Player_IdleUp.anim", new[] { GetSprite(4, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/Player_IdleRight.anim", new[] { GetSprite(2, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/Player_IdleLeft.anim", new[] { GetSprite(6, 0) }, 1f, true);

        // Actualizar también los archivos Idle existentes para compatibilidad total
        CrearClipSprite("Assets/Animations/IdleDown.anim", new[] { GetSprite(0, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/IdleUp.anim", new[] { GetSprite(4, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/IdleRight.anim", new[] { GetSprite(2, 0) }, 1f, true);
        CrearClipSprite("Assets/Animations/IdleLeft.anim", new[] { GetSprite(6, 0) }, 1f, true);

        // =====================================================================
        // 2. ANIMACIONES DE CAMINATA / WALK (4 FRAMES, 8-10 FPS, LOOP)
        // =====================================================================
        Sprite[] walkDown = { GetSprite(0, 1), GetSprite(0, 2), GetSprite(0, 3), GetSprite(0, 4) };
        Sprite[] walkUp = { GetSprite(4, 1), GetSprite(4, 2), GetSprite(4, 3), GetSprite(4, 4) };
        Sprite[] walkRight = { GetSprite(2, 1), GetSprite(2, 2), GetSprite(2, 3), GetSprite(2, 4) };
        Sprite[] walkLeft = { GetSprite(6, 1), GetSprite(6, 2), GetSprite(6, 3), GetSprite(6, 4) };

        CrearClipSprite("Assets/Animations/Player_WalkDown.anim", walkDown, 10f, true);
        CrearClipSprite("Assets/Animations/Player_WalkUp.anim", walkUp, 10f, true);
        CrearClipSprite("Assets/Animations/Player_WalkRight.anim", walkRight, 10f, true);
        CrearClipSprite("Assets/Animations/Player_WalkLeft.anim", walkLeft, 10f, true);

        // Actualizar también los archivos Walk existentes para compatibilidad total
        CrearClipSprite("Assets/Animations/WalkDown.anim", walkDown, 10f, true);
        CrearClipSprite("Assets/Animations/WalkUp.anim", walkUp, 10f, true);
        CrearClipSprite("Assets/Animations/WalkRight.anim", walkRight, 10f, true);
        CrearClipSprite("Assets/Animations/WalkLeft.anim", walkLeft, 10f, true);

        // =====================================================================
        // 3. ANIMACIONES DE ATAQUE CON ESPADA (4 FRAMES, 16 FPS)
        // =====================================================================
        Sprite[] attackDown = { GetSprite(0, 5), GetSprite(0, 6), GetSprite(0, 7), GetSprite(0, 8) };
        Sprite[] attackUp = { GetSprite(4, 5), GetSprite(4, 6), GetSprite(4, 7), GetSprite(4, 8) };
        Sprite[] attackRight = { GetSprite(2, 5), GetSprite(2, 6), GetSprite(2, 7), GetSprite(2, 8) };
        Sprite[] attackLeft = { GetSprite(6, 5), GetSprite(6, 7), GetSprite(6, 7), GetSprite(6, 8) };

        CrearClipSprite("Assets/Animations/Player_AttackDown.anim", attackDown, 16f, false);
        CrearClipSprite("Assets/Animations/Player_AttackUp.anim", attackUp, 16f, false);
        CrearClipSprite("Assets/Animations/Player_AttackRight.anim", attackRight, 16f, false);
        CrearClipSprite("Assets/Animations/Player_AttackLeft.anim", attackLeft, 16f, false);

        // =====================================================================
        // 4. BLOQUEO / GUARDIA CON ESPADA
        // =====================================================================
        Sprite[] blockSprites = { GetSprite(0, 5) };
        CrearClipSprite("Assets/Animations/Player_Block.anim", blockSprites, 1f, true);

        // =====================================================================
        // 5. ESQUIVE / RODAR (DODGE)
        // =====================================================================
        Sprite[] dodgeSprites = { GetSprite(0, 1), GetSprite(0, 2), GetSprite(0, 3) };
        CrearClipSprite("Assets/Animations/Player_Dodge.anim", dodgeSprites, 12f, false);

        // =====================================================================
        // 6. CAÍDA Y MUERTE (DEATH, 4 FRAMES, VELOCIDAD SUAVE)
        // =====================================================================
        Sprite[] deathSprites = { GetSprite(0, 20), GetSprite(0, 21), GetSprite(0, 22), GetSprite(0, 23) };
        CrearClipSprite("Assets/Animations/Player_Death.anim", deathSprites, 6f, false);

        // =====================================================================
        // 7. ACTUALIZAR O CONFIGURAR PLAYER.CONTROLLER
        // =====================================================================
        ConfigurarPlayerAnimatorController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=cyan><b>[San Felipe]</b> ¡Todas las animaciones de Ronan (Human-Soldier-Cyan) guardadas con éxito en Assets/Animations/!</color>");
    }

    private static void CrearClipSprite(string path, Sprite[] frames, float fps, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool esNuevo = false;
        if (clip == null)
        {
            clip = new AnimationClip();
            esNuevo = true;
        }
        else
        {
            clip.ClearCurves();
        }

        clip.frameRate = fps;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // Filtrar nulos si alguno falta
        List<Sprite> validos = new List<Sprite>();
        foreach (var f in frames) if (f != null) validos.Add(f);
        if (validos.Count == 0) return;

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[validos.Count];
        for (int i = 0; i < validos.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = validos[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (esNuevo)
        {
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }
    }

    private static void ConfigurarPlayerAnimatorController()
    {
        string controllerPath = "Assets/Animations/Player.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) return;

        // Asegurar que existan los parámetros Attack, Die, Dodge, isBlocking
        AsegurarParametro(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AsegurarParametro(controller, "Die", AnimatorControllerParameterType.Trigger);
        AsegurarParametro(controller, "Dodge", AnimatorControllerParameterType.Trigger);
        AsegurarParametro(controller, "isBlocking", AnimatorControllerParameterType.Bool);

        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player_AttackDown.anim");
        AnimationClip deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player_Death.anim");
        AnimationClip blockClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player_Block.anim");
        AnimationClip dodgeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Player_Dodge.anim");

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Agregar o conectar estados de combate si no existen
        AsegurarEstadoConClip(sm, "Attack", attackClip, "Attack", true);
        AsegurarEstadoConClip(sm, "Die", deathClip, "Die", false);
        AsegurarEstadoConClip(sm, "Block", blockClip, "isBlocking", true);
        AsegurarEstadoConClip(sm, "Dodge", dodgeClip, "Dodge", true);

        EditorUtility.SetDirty(controller);
    }

    private static void AsegurarParametro(AnimatorController controller, string nombre, AnimatorControllerParameterType tipo)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == nombre) return;
        }
        controller.AddParameter(nombre, tipo);
    }

    private static void AsegurarEstadoConClip(AnimatorStateMachine sm, string nombreEstado, AnimationClip clip, string parametroTrigger, bool vuelveAIdle)
    {
        if (clip == null) return;

        // Verificar si ya existe el estado
        foreach (var child in sm.states)
        {
            if (child.state.name == nombreEstado)
            {
                child.state.motion = clip;
                return;
            }
        }

        AnimatorState state = sm.AddState(nombreEstado);
        state.motion = clip;

        // Transición desde AnyState
        AnimatorStateTransition trans = sm.AddAnyStateTransition(state);
        trans.hasExitTime = false;
        trans.duration = 0.05f;

        if (parametroTrigger == "isBlocking")
        {
            trans.AddCondition(AnimatorConditionMode.If, 0, parametroTrigger);
        }
        else
        {
            trans.AddCondition(AnimatorConditionMode.If, 0, parametroTrigger);
        }

        if (vuelveAIdle && sm.defaultState != null)
        {
            AnimatorStateTransition transVuelta = state.AddTransition(sm.defaultState);
            transVuelta.hasExitTime = true;
            transVuelta.exitTime = 0.9f;
            transVuelta.duration = 0.1f;
        }
    }
}
#endif
