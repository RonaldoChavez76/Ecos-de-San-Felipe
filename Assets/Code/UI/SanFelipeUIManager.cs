using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SanFelipeUIManager : MonoBehaviour
{
    [Header("0. Configuración de Escenas")]
    [Tooltip("Si es true, la escena de juego arranca directamente sin pausar por el menú (el menú vive en la escena PantallaInicio)")]
    public bool iniciarDirectoEnJuego = true;
    public string nombreEscenaMenuPrincipal = "PantallaInicio";

    [Header("1. Las 4 Imágenes de la Historia Inicial (Opcionales)")]
    [Tooltip("Si las dejas vacías, el código dibuja 4 ilustraciones Pixel-Art de San Felipe automáticamente")]
    public Texture2D[] cuatroImagenesHistoria = new Texture2D[4];

    [Header("2.Los 4 Diálogos de la Historia Inicial (Sección 2.1 del Documento)")]
    [TextArea(2, 4)]
    public string[] dialogosIntro = new string[4]
    {
        "1/4 — EL SILENCIO AL AMANECER:\nDurante la noche, una niebla desconocida cubrió las calles de cantera y adobe de San Felipe, Guanajuato...",
        "2/4 — LOS ECOS DISPERSOS:\nLos recuerdos del pueblo se desprendieron en forma de cinco Ecos luminosos, dejando a sus habitantes sin memoria ni pasado.",
        "3/4 — LAS SOMBRAS CUSTODIAS:\nCriaturas nacidas de la oscuridad tomaron la Parroquia, el Mercado y el Mirador, confundiendo los Ecos con amenazas.",
        "4/4 — EL DEBER DE RONAN:\nArmado con su espada corta y el brazal capaz de devolver los ataques enemigos, Ronan debe recuperar los Ecos antes de que San Felipe se borre por completo."
    };

    [Header("3. Diálogos con Historia dentro del Pueblo (Presionar E cerca del cartel/NPC)")]
    [TextArea(2, 4)]
    public string[] dialogosPueblo = new string[]
    {
        "Anciano de la Plaza: «¡Ronan! Qué bueno que tú aún recuerdas tu nombre... Las sombras se llevaron el Eco de la Fe hacia el callejón de La Parroquia.»",
        "Anciano de la Plaza: «Recuerda usar tu brazal con [K] o [Clic Derecho] justo antes de que sus proyectiles te impacten (0.20s) para devolvérselos con el doble de fuerza.»",
        "Anciano de la Plaza: «Si logras reunir suficiente energía, desata el Modo Asalto con [L]. ¡Devuélvele la memoria a San Felipe!»"
    };

    [Header("4. Cinemática de Muerte en Cámara Lenta")]
    [Tooltip("Escala del tiempo al morir (ej. 0.20 = 20% de velocidad normal)")]
    public float camaraLentaMuerte = 0.20f;
    [Tooltip("Duración en segundos reales de la cámara lenta antes de mostrar la pantalla de Game Over")]
    public float duracionCamaraLenta = 1.6f;

    // Referencias internas
    private PlayerMovement player;
    private Canvas mainCanvas;

    // Paneles de UI
    private GameObject panelMenuInicio;
    private GameObject panelControles;
    private GameObject panelHistoriaCuatroImagenes;
    private GameObject panelDialogoInGame;
    private GameObject panelGameOver;
    private Button botonReaparecerCheckpoint;
    private Text textoBotonCheckpoint;

    // Elementos de la Historia de 4 Imágenes
    private RawImage imgSlideActual;
    private Text txtSlideDialogo;
    private Text txtIndicadorSlide;
    private int indiceSlideActual = 0;
    private Texture2D[] slidesGeneradas = new Texture2D[4];

    // Elementos de Diálogo In-Game
    private Text txtDialogoPueblo;
    private GameObject avisoInteractuarUI;
    private int indiceDialogoPueblo = 0;
    private bool dialogoInGameActivo = false;

    // Control de estado
    private bool enMenuOIntro = true;
    private bool gameOverMostrado = false;
    private Coroutine corrutinaEscritura;

    void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        // Generar las 4 ilustraciones de respaldo por si el usuario aún no arrastra las suyas
        for (int i = 0; i < 4; i++)
        {
            slidesGeneradas[i] = GenerarIlustracionHistoria(i);
        }

        ConstruirTodaLaInterfazPorCodigo();

        // Si el sistema nuevo de pausa/UI está presente, este manager solo gestiona Game Over.
        // Siempre inicia directo al juego y deja el ESC al nuevo sistema.
        if (PauseAndVictoryUI.Instance != null || iniciarDirectoEnJuego)
        {
            IniciarJuegoDirecto();
        }
        else
        {
            MostrarMenuPrincipal();
        }
    }

    void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        // 1. Detectar si Ronan murió (Vida <= 0) para activar la secuencia de Muerte en Cámara Lenta
        HealthComponent health = player != null ? player.GetComponent<HealthComponent>() : null;
        if (panelGameOver != null && panelGameOver.activeSelf)
        {
            ActualizarDisponibilidadCheckpoint();
        }

        if (!enMenuOIntro && player != null &&
            (player.currentHealth <= 0f || (health != null && health.isDead)) &&
            !gameOverMostrado)
        {
            IniciarSecuenciaMuerte();
        }

        // 2. Avanzar la historia de 4 imágenes con Espacio, Enter o Clic
        if (panelHistoriaCuatroImagenes != null && panelHistoriaCuatroImagenes.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                SiguienteImagenHistoria();
            }
        }

        // 3. Sistema de Diálogos con Historia dentro del juego (Tecla E)
        if (!enMenuOIntro && !gameOverMostrado && player != null)
        {
            // El punto de diálogo está en el centro de la Plaza Principal (cerca de (0,0) a (-2.5, 1.5))
            float distAlCartel = Vector2.Distance(player.transform.position, new Vector2(-2.2f, 1.2f));
            bool cercaDeNPC = distAlCartel < 2.8f;

            if (avisoInteractuarUI != null)
            {
                avisoInteractuarUI.SetActive(cercaDeNPC && !dialogoInGameActivo);
            }

            if (cercaDeNPC && Input.GetKeyDown(KeyCode.E))
            {
                AvanzarDialogoDelPueblo();
            }
            else if (dialogoInGameActivo && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                AvanzarDialogoDelPueblo();
            }
        }

        // 4. Pausar / Reanudar juego con Escape
        // NOTA: Si PauseAndVictoryUI (sistema nuevo de pausa) está presente,
        //       él gestiona el ESC — SanFelipeUIManager NO debe interferir.
        bool nuevoSistemaPausaActivo = PauseAndVictoryUI.Instance != null;
        if (!nuevoSistemaPausaActivo && !gameOverMostrado &&
            (panelHistoriaCuatroImagenes == null || !panelHistoriaCuatroImagenes.activeSelf) && !dialogoInGameActivo)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                AlternarPausa();
            }
        }    }

    // =========================================================================
    // FLUJO DE PANTALLAS: MENÚ -> 4 IMÁGENES DE HISTORIA -> JUEGO -> GAME OVER
    // =========================================================================
    public void IniciarJuegoDirecto()
    {
        enMenuOIntro = false;
        gameOverMostrado = false;
        Time.timeScale = 1f;

        if (panelMenuInicio != null) panelMenuInicio.SetActive(false);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelHistoriaCuatroImagenes != null) panelHistoriaCuatroImagenes.SetActive(false);
        if (panelDialogoInGame != null) panelDialogoInGame.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (avisoInteractuarUI != null) avisoInteractuarUI.SetActive(false);

        // Si el nuevo sistema de Pausa/Victoria esta presente, ocultar el Canvas raiz
        // para evitar que el overlay de este manager interfiera con PauseAndVictoryUI.
        if (PauseAndVictoryUI.Instance != null && mainCanvas != null)
            mainCanvas.gameObject.SetActive(false);
    }
    public void AlternarPausa()
    {
        if (panelMenuInicio == null) return;
        bool enPausa = panelMenuInicio.activeSelf;
        if (enPausa)
        {
            panelMenuInicio.SetActive(false);
            if (panelControles != null) panelControles.SetActive(false);
            Time.timeScale = 1f;
        }
        else
        {
            panelMenuInicio.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void VolverAlMenuPrincipal()
    {
        SaveStateManager.EnsureInstance().GuardarProgreso();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("<color=cyan>Regresando al Menú Principal: " + nombreEscenaMenuPrincipal + "...</color>");
        SceneManager.LoadScene(nombreEscenaMenuPrincipal);
    }

    public void MostrarMenuPrincipal()
    {
        enMenuOIntro = true;
        gameOverMostrado = false;
        Time.timeScale = 0f; // Pausar el mundo detrás del menú

        panelMenuInicio.SetActive(true);
        panelControles.SetActive(false);
        panelHistoriaCuatroImagenes.SetActive(false);
        panelDialogoInGame.SetActive(false);
        panelGameOver.SetActive(false);
        if (avisoInteractuarUI != null) avisoInteractuarUI.SetActive(false);
    }

    public void ComenzarHistoriaCuatroImagenes()
    {
        panelMenuInicio.SetActive(false);
        panelHistoriaCuatroImagenes.SetActive(true);
        indiceSlideActual = 0;
        ActualizarSlideHistoria();
    }

    public void SiguienteImagenHistoria()
    {
        indiceSlideActual++;
        if (indiceSlideActual < 4)
        {
            ActualizarSlideHistoria();
        }
        else
        {
            // Terminó las 4 imágenes: ¡Arranca la partida en San Felipe!
            panelHistoriaCuatroImagenes.SetActive(false);
            enMenuOIntro = false;
            Time.timeScale = 1f;

            // Mostrar automáticamente el primer diálogo al despertar en la Plaza
            indiceDialogoPueblo = 0;
            AvanzarDialogoDelPueblo();
        }
    }

    private void ActualizarSlideHistoria()
    {
        // Si el usuario arrastró su propia imagen en el Inspector, la usa; si no, usa la ilustración generada
        Texture2D texMostrar = (cuatroImagenesHistoria != null &&
                                cuatroImagenesHistoria.Length > indiceSlideActual &&
                                cuatroImagenesHistoria[indiceSlideActual] != null)
            ? cuatroImagenesHistoria[indiceSlideActual]
            : slidesGeneradas[indiceSlideActual];

        imgSlideActual.texture = texMostrar;
        txtIndicadorSlide.text = $"Imagen {indiceSlideActual + 1} de 4 — Presiona [ESPACIO] o clic en 'Siguiente'";

        EscribirTextoAnimado(txtSlideDialogo, dialogosIntro[indiceSlideActual]);
    }

    public void AvanzarDialogoDelPueblo()
    {
        if (!dialogoInGameActivo)
        {
            dialogoInGameActivo = true;
            panelDialogoInGame.SetActive(true);
            indiceDialogoPueblo = 0;
            Time.timeScale = 0f; // Pausar enemigos mientras el jugador lee la historia
            EscribirTextoAnimado(txtDialogoPueblo, dialogosPueblo[indiceDialogoPueblo]);
        }
        else
        {
            indiceDialogoPueblo++;
            if (indiceDialogoPueblo < dialogosPueblo.Length)
            {
                EscribirTextoAnimado(txtDialogoPueblo, dialogosPueblo[indiceDialogoPueblo]);
            }
            else
            {
                // Cerrar cuadro de diálogo y volver a la acción
                dialogoInGameActivo = false;
                panelDialogoInGame.SetActive(false);
                Time.timeScale = 1f;
            }
        }
    }

    public void IniciarSecuenciaMuerte()
    {
        if (gameOverMostrado) return;
        StartCoroutine(RutinaSecuenciaMuerte());
    }

    private IEnumerator RutinaSecuenciaMuerte()
    {
        gameOverMostrado = true;

        // 1. Entrar en cámara lenta cinemática para que se aprecie la caída
        Time.timeScale = camaraLentaMuerte;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. Destello/tinte carmesí sutil en Ronan indicando el golpe fatal
        if (player != null)
        {
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.45f, 0.45f, 1f);
        }

        // 3. Esperar en tiempo real mientras transcurre la animación de muerte (columnas 20 a 23)
        yield return new WaitForSecondsRealtime(duracionCamaraLenta);

        // 4. Congelar el mundo y mostrar la pantalla de Game Over con Fade In suave
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0.02f;

        if (panelGameOver != null)
        {
            // Reactivar el Canvas si fue ocultado por IniciarJuegoDirecto
            if (mainCanvas != null) mainCanvas.gameObject.SetActive(true);
            panelGameOver.SetActive(true);

            CanvasGroup cg = panelGameOver.GetComponent<CanvasGroup>();
            if (cg == null) cg = panelGameOver.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            float tiempoFade = 0.55f;
            float crono = 0f;
            while (crono < tiempoFade)
            {
                crono += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(crono / tiempoFade);
                yield return null;
            }
            cg.alpha = 1f;
        }
    }

    public void ReintentarPartida()
    {
        ReaparecerEnCheckpoint();
    }

    public void ReaparecerEnCheckpoint()
    {
        player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("[SanFelipeUIManager] No se encontró a Ronan para reaparecer en el checkpoint.");
            return;
        }

        LevelManager.EnsureInstance();
        SaveStateManager save = SaveStateManager.EnsureInstance();
        if (!save.RestaurarJugadorDesdeCheckpoint(player))
        {
            ReiniciarDesdeInicioNivel();
            return;
        }

        FinalizarRespawn();
    }

    public void ReiniciarDesdeInicioNivel()
    {
        player = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("[SanFelipeUIManager] No se encontró a Ronan para reiniciar el nivel.");
            return;
        }

        LevelManager levelManager = LevelManager.EnsureInstance();
        SaveStateManager.EnsureInstance().RestaurarJugadorInicioNivel(player, levelManager.currentZone);
        ZoneWaveSpawner.ReiniciarEncuentrosDeEscena();
        FinalizarRespawn();
    }

    private void FinalizarRespawn()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (player != null)
        {
            player.gameObject.SetActive(true);
            player.enabled = true;
            player.RestablecerEstadoTrasDerrota(player.transform.position);
            HealthComponent health = player.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.isDead = false;
                health.RestablecerTrasDerrota(player.maxHealth, player.currentHealth);
                health.ActivarInvulnerabilidadTemporal(1.5f);
            }
            player.ActivarInvulnerabilidadTrasRespawn(1.5f);
        }

        gameOverMostrado = false;
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.SetState(GameManager.GameState.Exploring);
    }

    private void ActualizarDisponibilidadCheckpoint()
    {
        if (botonReaparecerCheckpoint == null) return;

        bool disponible = SaveStateManager.EnsureInstance()
            .HayCheckpointParaZona(LevelManager.EnsureInstance().currentZone.ToString());
        botonReaparecerCheckpoint.interactable = disponible;
        if (textoBotonCheckpoint != null)
        {
            textoBotonCheckpoint.text = disponible
                ? "REAPARECER EN EL ÚLTIMO CHECKPOINT"
                : "CHECKPOINT NO DISPONIBLE EN ESTA ZONA";
        }
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo de Ecos de San Felipe...");
        Application.Quit();
    }

    private void EscribirTextoAnimado(Text componenteTexto, string mensajeCompleto)
    {
        if (corrutinaEscritura != null) StopCoroutine(corrutinaEscritura);
        corrutinaEscritura = StartCoroutine(RutinaMaquinaDeEscribir(componenteTexto, mensajeCompleto));
    }

    private IEnumerator RutinaMaquinaDeEscribir(Text componenteTexto, string mensaje)
    {
        componenteTexto.text = "";
        foreach (char letra in mensaje)
        {
            componenteTexto.text += letra;
            yield return new WaitForSecondsRealtime(0.018f);
        }
    }

    // =========================================================================
    // CONSTRUCTOR AUTOMÁTICO DE CANVAS Y PANELES UI (100% POR CÓDIGO)
    // =========================================================================
    private void ConstruirTodaLaInterfazPorCodigo()
    {
        // Asegurar que exista EventSystem para que los botones respondan al clic del mouse
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasObj = new GameObject("Canvas_EcosDeSanFelipe");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        canvasObj.AddComponent<GraphicRaycaster>();

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Font.CreateDynamicFontFromOSFont("Arial", 18);

        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------
        // 1. PANEL MENÚ DE INICIO / PAUSA EN JUEGO
        // ---------------------------------------------------------------------
        panelMenuInicio = CrearPanel(canvasObj.transform, "Panel_MenuInicio", new Color(0.11f, 0.08f, 0.06f, 0.96f), Vector2.zero, new Vector2(1280, 720));

        CrearTexto(panelMenuInicio.transform, "ECOS DE SAN FELIPE", 54, new Color(0f, 0.92f, 1f), new Vector2(0, 170), new Vector2(900, 80), fuente);
        CrearTexto(panelMenuInicio.transform, "Aventura y Acción RPG 2D — San Felipe, Guanajuato", 22, new Color(0.88f, 0.78f, 0.65f), new Vector2(0, 110), new Vector2(800, 40), fuente);

        CrearBoton(panelMenuInicio.transform, "CONTINUAR JUEGO", new Vector2(0, 30), new Vector2(360, 50), new Color(0.15f, 0.55f, 0.68f), fuente, AlternarPausa);
        CrearBoton(panelMenuInicio.transform, "MAPA DE TECLAS / CONTROLES", new Vector2(0, -35), new Vector2(360, 48), new Color(0.38f, 0.25f, 0.18f), fuente, () => panelControles.SetActive(!panelControles.activeSelf));
        CrearBoton(panelMenuInicio.transform, "VOLVER AL MENÚ PRINCIPAL", new Vector2(0, -100), new Vector2(360, 48), new Color(0.20f, 0.38f, 0.50f), fuente, VolverAlMenuPrincipal);
        CrearBoton(panelMenuInicio.transform, "SALIR DEL JUEGO", new Vector2(0, -165), new Vector2(360, 45), new Color(0.45f, 0.15f, 0.15f), fuente, SalirDelJuego);

        CrearTexto(panelMenuInicio.transform, "UTNG — Creación de Videojuegos | Desarrollado por Santiago Ronaldo Chávez Piñón", 16, new Color(0.82f, 0.78f, 0.72f), new Vector2(0, -290), new Vector2(900, 30), fuente);

        // Subpanel de Controles (Tabla 11 del documento)
        panelControles = CrearPanel(panelMenuInicio.transform, "Subpanel_Controles", new Color(0.06f, 0.05f, 0.08f, 0.98f), new Vector2(440, -40), new Vector2(540, 350));
        CrearTexto(panelControles.transform, "CONTROLES DE RONAN:\n\n[W, A, S, D] : Mover en 8 direcciones\n[J] o [Clic Izq] : Atacar con el arma equipada\n[Q] / [1-2-3] / Rueda : Cambiar arma\n[K] o [Clic Der] : Bloqueo / Contraataque (0.20s)\n[ESPACIO] : Esquive con invulnerabilidad\n[L] : Activar Modo Asalto\n[E] : Hablar / Leer Pistas\n[ESC] : Pausa", 17, Color.white, Vector2.zero, new Vector2(500, 330), fuente);
        panelControles.SetActive(false);

        // ---------------------------------------------------------------------
        // 2. PANEL HISTORIA DE 4 IMÁGENES Y SUS DIÁLOGOS
        // ---------------------------------------------------------------------
        panelHistoriaCuatroImagenes = CrearPanel(canvasObj.transform, "Panel_Historia4Imagenes", new Color(0.06f, 0.05f, 0.08f, 0.98f), Vector2.zero, new Vector2(1280, 720));

        // Marco para la Imagen de la Historia
        GameObject marcoImg = CrearPanel(panelHistoriaCuatroImagenes.transform, "Marco_Imagen", new Color(0f, 0.85f, 1f, 0.8f), new Vector2(0, 85), new Vector2(650, 370));
        GameObject objRaw = new GameObject("Imagen_Historia_Actual");
        objRaw.transform.SetParent(marcoImg.transform, false);
        imgSlideActual = objRaw.AddComponent<RawImage>();
        imgSlideActual.rectTransform.sizeDelta = new Vector2(640, 360);

        // Caja inferior para el diálogo de cada una de las 4 imágenes
        GameObject cajaTextoIntro = CrearPanel(panelHistoriaCuatroImagenes.transform, "Caja_Dialogo_Intro", new Color(0.14f, 0.10f, 0.08f, 0.95f), new Vector2(0, -185), new Vector2(920, 145));
        txtSlideDialogo = CrearTexto(cajaTextoIntro.transform, "", 22, new Color(0.96f, 0.93f, 0.88f), new Vector2(0, 5), new Vector2(870, 115), fuente);
        txtIndicadorSlide = CrearTexto(panelHistoriaCuatroImagenes.transform, "Imagen 1 de 4", 18, new Color(0f, 0.9f, 1f), new Vector2(-240, -295), new Vector2(520, 35), fuente);

        CrearBoton(panelHistoriaCuatroImagenes.transform, "SIGUIENTE >>", new Vector2(340, -295), new Vector2(220, 45), new Color(0.15f, 0.55f, 0.68f), fuente, SiguienteImagenHistoria);

        // ---------------------------------------------------------------------
        // 3. PANEL DIÁLOGOS CON HISTORIA DENTRO DEL JUEGO (NPC / PLACAS)
        // ---------------------------------------------------------------------
        panelDialogoInGame = CrearPanel(canvasObj.transform, "Panel_DialogoInGame", new Color(0.10f, 0.07f, 0.05f, 0.94f), new Vector2(0, -220), new Vector2(960, 170));
        txtDialogoPueblo = CrearTexto(panelDialogoInGame.transform, "", 22, Color.white, new Vector2(0, 12), new Vector2(900, 110), fuente);
        CrearTexto(panelDialogoInGame.transform, "Presiona [E] o [ESPACIO] para continuar...", 16, new Color(0f, 0.9f, 1f), new Vector2(310, -58), new Vector2(320, 28), fuente);

        // Aviso flotante cuando Ronan se acerca al letrero/NPC de la Plaza
        avisoInteractuarUI = CrearPanel(canvasObj.transform, "Aviso_PresionaE", new Color(0f, 0f, 0f, 0.78f), new Vector2(0, -140), new Vector2(440, 42));
        CrearTexto(avisoInteractuarUI.transform, "Presiona [E] para leer inscripción / hablar", 18, new Color(0f, 0.95f, 1f), Vector2.zero, new Vector2(420, 35), fuente);

        // ---------------------------------------------------------------------
        // 4. PANTALLA DE GAME OVER (CUANDO RONAN CAE DERROTADO)
        // ---------------------------------------------------------------------
        panelGameOver = CrearPanel(canvasObj.transform, "Panel_GameOver", new Color(0.035f, 0.02f, 0.025f, 0.94f), Vector2.zero, new Vector2(1280, 720));
        GameObject pergaminoDerrota = CrearPanel(panelGameOver.transform, "Pergamino_Derrota", new Color(0.13f, 0.09f, 0.055f, 0.98f), Vector2.zero, new Vector2(700, 500));
        Outline marcoDerrota = pergaminoDerrota.AddComponent<Outline>();
        marcoDerrota.effectColor = new Color(0.88f, 0.68f, 0.28f, 1f);
        marcoDerrota.effectDistance = new Vector2(4f, -4f);
        Font fuenteColonial = Font.CreateDynamicFontFromOSFont("Georgia", 20);
        if (fuenteColonial == null) fuenteColonial = fuente;

        CrearTexto(pergaminoDerrota.transform, "HAS CAÍDO EN LAS SOMBRAS", 42, new Color(0.95f, 0.75f, 0.34f), new Vector2(0, 172), new Vector2(650, 66), fuenteColonial);
        CrearTexto(pergaminoDerrota.transform, "Los ecos de San Felipe aún esperan ser liberados.", 20, new Color(0.9f, 0.85f, 0.75f), new Vector2(0, 116), new Vector2(620, 38), fuenteColonial);

        botonReaparecerCheckpoint = CrearBotonInteractivo(
            pergaminoDerrota.transform,
            "REAPARECER EN EL ÚLTIMO CHECKPOINT",
            new Vector2(0, 48),
            new Vector2(560, 56),
            new Color(0.48f, 0.32f, 0.12f),
            fuenteColonial,
            ReaparecerEnCheckpoint,
            out textoBotonCheckpoint);
        CrearBoton(pergaminoDerrota.transform, "REINICIAR DESDE EL INICIO DEL NIVEL", new Vector2(0, -28), new Vector2(560, 56), new Color(0.18f, 0.42f, 0.38f), fuenteColonial, ReiniciarDesdeInicioNivel);
        CrearBoton(pergaminoDerrota.transform, "VOLVER AL MENÚ PRINCIPAL", new Vector2(0, -104), new Vector2(560, 56), new Color(0.38f, 0.18f, 0.18f), fuenteColonial, VolverAlMenuPrincipal);
        ActualizarDisponibilidadCheckpoint();
    }

    // =========================================================================
    // GENERADOR DE LAS 4 ILUSTRACIONES DE LA HISTORIA (RESPALDO AUTOMÁTICO)
    // =========================================================================
    private Texture2D GenerarIlustracionHistoria(int indice)
    {
        int w = 160, h = 90;
        Texture2D tex = new Texture2D(w, h);
        tex.filterMode = FilterMode.Point;

        for (int y = 0; y < h; y++)
        {
            float ny = y / (float)h;
            for (int x = 0; x < w; x++)
            {
                Color col = Color.black;

                if (indice == 0)
                {
                    // Imagen 1: Amanecer sobre la Parroquia de San Felipe
                    col = Color.Lerp(new Color(0.85f, 0.42f, 0.20f), new Color(0.16f, 0.10f, 0.28f), ny);
                    bool esSiluetaPueblo = (y < 28) || (x > 65 && x < 95 && y < 58) || (x > 55 && x < 65 && y < 70) || (x > 95 && x < 105 && y < 70);
                    if (esSiluetaPueblo) col = new Color(0.12f, 0.07f, 0.06f);
                }
                else if (indice == 1)
                {
                    // Imagen 2: Los 5 Ecos luminosos dispersándose en la noche
                    col = Color.Lerp(new Color(0.05f, 0.08f, 0.18f), new Color(0.02f, 0.02f, 0.06f), ny);
                    Vector2[] ecos = { new Vector2(30, 55), new Vector2(60, 68), new Vector2(80, 42), new Vector2(105, 65), new Vector2(130, 50) };
                    foreach (Vector2 e in ecos)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), e);
                        if (d < 6f) col = Color.white;
                        else if (d < 13f) col = Color.Lerp(new Color(0f, 0.95f, 1f), col, (d - 6f) / 7f);
                    }
                }
                else if (indice == 2)
                {
                    // Imagen 3: Las Sombras Demoníacas con ojos carmesí
                    col = Color.Lerp(new Color(0.22f, 0.03f, 0.08f), new Color(0.04f, 0.02f, 0.06f), ny);
                    float dCentro = Vector2.Distance(new Vector2(x, y), new Vector2(80, 45));
                    if (dCentro < 28f) col = new Color(0.03f, 0.01f, 0.04f);
                    if ((Vector2.Distance(new Vector2(x, y), new Vector2(72, 50)) < 3.5f) ||
                        (Vector2.Distance(new Vector2(x, y), new Vector2(88, 50)) < 3.5f))
                    {
                        col = new Color(1f, 0.1f, 0.2f);
                    }
                }
                else
                {
                    // Imagen 4: El brazal de Ronan devolviendo un proyectil con luz azul
                    col = Color.Lerp(new Color(0.08f, 0.22f, 0.35f), new Color(0.04f, 0.06f, 0.12f), ny);
                    float dEscudo = Vector2.Distance(new Vector2(x, y), new Vector2(80, 45));
                    if (dEscudo > 18f && dEscudo < 24f) col = new Color(0f, 0.95f, 1f);
                    else if (dEscudo <= 18f) col = new Color(0.6f, 0.98f, 1f);
                }

                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return tex;
    }

    // Helpers de construcción UI
    private GameObject CrearPanel(Transform parent, string nombre, Color colorFondo, Vector2 pos, Vector2 size)
    {
        GameObject panel = new GameObject(nombre);
        panel.transform.SetParent(parent, false);
        Image img = panel.AddComponent<Image>();
        img.color = colorFondo;
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return panel;
    }

    private Text CrearTexto(Transform parent, string contenido, int fontSize, Color colorTexto, Vector2 pos, Vector2 size, Font fuente)
    {
        GameObject txtObj = new GameObject("Texto_UI");
        txtObj.transform.SetParent(parent, false);
        Text t = txtObj.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = Mathf.Max(fontSize, 18);
        Outline contorno = txtObj.AddComponent<Outline>();
        contorno.effectColor = new Color(0f, 0f, 0f, 0.9f);
        contorno.effectDistance = new Vector2(1.5f, -1.5f);
        t.color = colorTexto;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    private void CrearBoton(Transform parent, string etiqueta, Vector2 pos, Vector2 size, Color colorBtn, Font fuente, UnityEngine.Events.UnityAction accion)
    {
        CrearBotonInteractivo(parent, etiqueta, pos, size, colorBtn, fuente, accion, out _);
    }

    private Button CrearBotonInteractivo(
        Transform parent,
        string etiqueta,
        Vector2 pos,
        Vector2 size,
        Color colorBtn,
        Font fuente,
        UnityEngine.Events.UnityAction accion,
        out Text texto)
    {
        GameObject btnObj = CrearPanel(parent, "Boton_" + etiqueta, colorBtn, pos, size);
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(accion);
        texto = CrearTexto(btnObj.transform, etiqueta, 20, Color.white, Vector2.zero, size, fuente);
        return btn;
    }
}