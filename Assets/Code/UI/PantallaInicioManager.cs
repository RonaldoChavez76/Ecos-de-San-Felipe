using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class PantallaInicioManager : MonoBehaviour
{
    [Header("1. Las 4 Imágenes de la Historia Inicial (Opcionales)")]
    [Tooltip("Arrastra aquí tus 4 imágenes. Si las dejas vacías, el código dibuja 4 ilustraciones Pixel-Art automáticamente")]
    public Texture2D[] cuatroImagenesHistoria = new Texture2D[4];

    [Header("2. Nombre de la Escena del Nivel 1")]
    public string nombreEscenaNivel1 = "SampleScene";

    [Header("3. Música y Sonido del Menú")]
    [Tooltip("Arrastra aquí tu archivo de música (WAV, MP3, OGG). Si lo dejas vacío, el juego genera una melodía ambiental mística de fondo automáticamente")]
    public AudioClip musicaMenu;

    [Range(0f, 1f)]
    [Tooltip("Volumen de la música de fondo")]
    public float volumenMusica = 0.35f;

    [Tooltip("Efecto de sonido al presionar botones (opcional, se genera uno automáticamente si está vacío)")]
    public AudioClip sfxClick;

    [Header("4. Textos de la Historia Inicial")]
    [TextArea(2, 4)]
    public string[] dialogosIntro = new string[4]
    {
        "1/4 — EL SILENCIO AL AMANECER:\nDurante la noche, una niebla desconocida cubrió las calles de cantera y adobe de San Felipe, Guanajuato...",
        "2/4 — LOS ECOS DISPERSOS:\nLos recuerdos del pueblo se desprendieron en forma de cinco Ecos luminosos, dejando a sus habitantes sin memoria ni pasado.",
        "3/4 — LAS SOMBRAS CUSTODIAS:\nCriaturas nacidas de la oscuridad tomaron la Parroquia, el Mercado y el Mirador, confundiendo los Ecos con amenazas.",
        "4/4 — EL DEBER DE RONAN:\nArmado con su espada corta y el brazal capaz de devolver los ataques enemigos, Ronan debe recuperar los Ecos antes de que San Felipe se borre por completo."
    };

    [Header("5. Referencias a la UI en Escena (Generadas en Hierarchy)")]
    public GameObject panelMenuInicio;
    public GameObject panelControles;
    public GameObject panelHistoriaCuatroImagenes;
    public RawImage imgSlideActual;
    public Text txtSlideDialogo;
    public Text txtIndicadorSlide;

    public void AbrirMenuOpciones()
    {
        OptionsModalUI.EnsureInstance()?.AbrirModal();
    }

    private int indiceSlideActual = 0;
    private Texture2D[] slidesGeneradas = new Texture2D[4];
    private Coroutine corrutinaEscritura;
    private AudioSource audioSource;
    private AudioSource sfxSource;

    private bool escribiendoTexto = false;
    private string mensajeObjetivo = "";

    void Awake()
    {
        if (panelMenuInicio == null)
        {
            BuscarOConectarReferenciasExistentes();
        }
        VincularBotonesYListeners();
    }

    void Start()
    {
        // 1. Asegurar que el tiempo esté corriendo normalmente
        Time.timeScale = 1f;

        // 2. Generar ilustraciones de respaldo si no hay asignadas
        for (int i = 0; i < 4; i++)
        {
            slidesGeneradas[i] = GenerarIlustracionHistoria(i);
        }

        // 3. Inicializar Música
        InicializarAudio();

        // 4. Si no estaban conectadas las referencias, conectarlas o construirlas
        if (panelMenuInicio == null)
        {
            BuscarOConectarReferenciasExistentes();
        }

        if (panelMenuInicio == null)
        {
            GenerarUIEnEscena();
        }

        // 5. Vincular listeners y mostrar menú
        VincularBotonesYListeners();
        MostrarMenuPrincipal();
    }

    void Update()
    {
        // Permitir avanzar o completar la historia con Espacio, Enter o tecla E
        if (panelHistoriaCuatroImagenes != null && panelHistoriaCuatroImagenes.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                SiguienteImagenHistoria();
            }
        }
    }

    public void MostrarMenuPrincipal()
    {
        if (panelMenuInicio != null) panelMenuInicio.SetActive(true);
        if (panelControles != null) panelControles.SetActive(false);
        if (panelHistoriaCuatroImagenes != null) panelHistoriaCuatroImagenes.SetActive(false);
    }

    public void ComenzarHistoriaCuatroImagenes()
    {
        if (panelMenuInicio != null) panelMenuInicio.SetActive(false);
        if (panelHistoriaCuatroImagenes != null) panelHistoriaCuatroImagenes.SetActive(true);
        indiceSlideActual = 0;
        ActualizarSlideHistoria();
    }

    public void SiguienteImagenHistoria()
    {
        // Si aún está escribiendo letra por letra, completar el texto al primer clic o pulsación
        if (escribiendoTexto)
        {
            if (corrutinaEscritura != null) StopCoroutine(corrutinaEscritura);
            if (txtSlideDialogo != null) txtSlideDialogo.text = mensajeObjetivo;
            escribiendoTexto = false;
            return;
        }

        indiceSlideActual++;
        if (indiceSlideActual < 4)
        {
            ActualizarSlideHistoria();
        }
        else
        {
            CargarEscenaJuego();
        }
    }

    public void CargarEscenaJuego()
    {
        Time.timeScale = 1f;
        Debug.Log("<color=cyan>Cargando nivel de juego: " + nombreEscenaNivel1 + "...</color>");
        SceneManager.LoadScene(nombreEscenaNivel1);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo de Ecos de San Felipe...");
        Application.Quit();
    }

    public void AlternarPanelControles()
    {
        if (panelControles != null)
        {
            panelControles.SetActive(!panelControles.activeSelf);
        }
    }

    private void ActualizarSlideHistoria()
    {
        Texture2D texMostrar = (cuatroImagenesHistoria != null &&
                                cuatroImagenesHistoria.Length > indiceSlideActual &&
                                cuatroImagenesHistoria[indiceSlideActual] != null)
            ? cuatroImagenesHistoria[indiceSlideActual]
            : slidesGeneradas[indiceSlideActual];

        if (imgSlideActual != null) imgSlideActual.texture = texMostrar;
        if (txtIndicadorSlide != null)
        {
            txtIndicadorSlide.text = $"Imagen {indiceSlideActual + 1} de 4 — Presiona [ESPACIO] o clic en 'Siguiente'";
        }

        if (txtSlideDialogo != null && dialogosIntro != null && indiceSlideActual < dialogosIntro.Length)
        {
            EscribirTextoAnimado(txtSlideDialogo, dialogosIntro[indiceSlideActual]);
        }
    }

    private void EscribirTextoAnimado(Text componenteTexto, string mensajeCompleto)
    {
        mensajeObjetivo = mensajeCompleto;
        if (corrutinaEscritura != null) StopCoroutine(corrutinaEscritura);
        corrutinaEscritura = StartCoroutine(RutinaMaquinaDeEscribir(componenteTexto, mensajeCompleto));
    }

    private IEnumerator RutinaMaquinaDeEscribir(Text componenteTexto, string mensaje)
    {
        escribiendoTexto = true;
        componenteTexto.text = "";
        foreach (char letra in mensaje)
        {
            componenteTexto.text += letra;
            yield return new WaitForSecondsRealtime(0.016f);
        }
        escribiendoTexto = false;
    }

    private void InicializarAudio()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            audioSource = sources[0];
            sfxSource = sources[1];
        }
        else if (sources.Length == 1)
        {
            audioSource = sources[0];
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.volume = volumenMusica;
        audioSource.playOnAwake = false;

        // Si el usuario asignó una canción la reproduce; si no, sintetiza la música ambiental de San Felipe
        if (musicaMenu != null)
        {
            audioSource.clip = musicaMenu;
        }
        else
        {
            audioSource.clip = GenerarMusicaMenuProcedural();
        }

        audioSource.Play();

        // Generar SFX de clic si no hay asignado
        if (sfxClick == null)
        {
            sfxClick = GenerarSonidoClick();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    public void ReproducirSonidoClick()
    {
        if (sfxSource != null && sfxClick != null)
        {
            sfxSource.PlayOneShot(sfxClick, 0.65f);
        }
    }

    // =========================================================================
    // CONEXIÓN Y VINCULACIÓN DE ELEMENTOS DE UI
    // =========================================================================
    public void BuscarOConectarReferenciasExistentes()
    {
        GameObject canvasObj = GameObject.Find("Canvas_PantallaInicio");
        if (canvasObj == null) return;

        Transform tCanvas = canvasObj.transform;

        Transform tMenu = tCanvas.Find("Panel_MenuInicio");
        if (tMenu != null)
        {
            panelMenuInicio = tMenu.gameObject;
            Transform tCtrl = tMenu.Find("Subpanel_Controles");
            if (tCtrl != null) panelControles = tCtrl.gameObject;
        }

        Transform tHist = tCanvas.Find("Panel_Historia4Imagenes");
        if (tHist != null)
        {
            panelHistoriaCuatroImagenes = tHist.gameObject;

            Transform tImg = tHist.Find("Marco_Imagen/Imagen_Historia_Actual");
            if (tImg != null) imgSlideActual = tImg.GetComponent<RawImage>();

            Transform tTxt = tHist.Find("Caja_Dialogo_Intro/Texto_Dialogo");
            if (tTxt != null) txtSlideDialogo = tTxt.GetComponent<Text>();

            Transform tInd = tHist.Find("Texto_Indicador_Slide");
            if (tInd != null) txtIndicadorSlide = tInd.GetComponent<Text>();
        }
    }

    public void VincularBotonesYListeners()
    {
        if (panelMenuInicio != null)
        {
            Transform t = panelMenuInicio.transform;
            AsignarListenerBoton(t, "Boton_IniciarAventura", ComenzarHistoriaCuatroImagenes);
            AsignarListenerBoton(t, "Boton_JugarDirecto", CargarEscenaJuego);
            AsignarListenerBoton(t, "Boton_Opciones", AbrirMenuOpciones);
            AsignarListenerBoton(t, "Boton_Controles", AlternarPanelControles);
            AsignarListenerBoton(t, "Boton_Salir", SalirDelJuego);
        }

        if (panelHistoriaCuatroImagenes != null)
        {
            Transform t = panelHistoriaCuatroImagenes.transform;
            AsignarListenerBoton(t, "Boton_OmitirHistoria", CargarEscenaJuego);
            AsignarListenerBoton(t, "Boton_SiguienteImagen", SiguienteImagenHistoria);
        }
    }

    private void AsignarListenerBoton(Transform parent, string nombreBoton, UnityEngine.Events.UnityAction accion)
    {
        Transform btnTr = parent.Find(nombreBoton);
        if (btnTr != null)
        {
            Button btn = btnTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ReproducirSonidoClick);
                btn.onClick.AddListener(accion);
            }
        }
    }

    // =========================================================================
    // GENERADOR EN ESCENA (HIERARCHY) PARA MODO EDICIÓN
    // =========================================================================
    [ContextMenu("Generar UI en Escena (Hierarchy)")]
    public void GenerarUIEnEscena()
    {
        // 1. Limpiar canvas previo si ya existiera
        GameObject canvasViejo = GameObject.Find("Canvas_PantallaInicio");
        if (canvasViejo != null)
        {
            DestroyImmediate(canvasViejo);
        }

        // 2. Asegurar EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
#endif
        }

        // 3. Crear Canvas_PantallaInicio
        GameObject canvasObj = new GameObject("Canvas_PantallaInicio");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        canvasObj.AddComponent<GraphicRaycaster>();

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Font.CreateDynamicFontFromOSFont("Arial", 18);

        // 4. Panel Menú de Inicio
        panelMenuInicio = CrearPanel(canvasObj.transform, "Panel_MenuInicio", new Color(0.08f, 0.06f, 0.05f, 0.98f), Vector2.zero, new Vector2(1280, 720));

        CrearTexto(panelMenuInicio.transform, "Titulo_EcosDeSanFelipe", "ECOS DE SAN FELIPE", 56, new Color(0f, 0.92f, 1f), new Vector2(0, 170), new Vector2(900, 80), fuente);
        CrearTexto(panelMenuInicio.transform, "Subtitulo_SanFelipe", "Aventura y Acción RPG 2D — San Felipe, Guanajuato", 22, new Color(0.88f, 0.78f, 0.65f), new Vector2(0, 110), new Vector2(800, 40), fuente);

        CrearBoton(panelMenuInicio.transform, "Boton_IniciarAventura", "INICIAR AVENTURA (HISTORIA)", new Vector2(0, 35), new Vector2(380, 50), new Color(0.15f, 0.55f, 0.68f), fuente, ComenzarHistoriaCuatroImagenes);
        CrearBoton(panelMenuInicio.transform, "Boton_JugarDirecto", "JUGAR DIRECTO AL NIVEL 1", new Vector2(0, -25), new Vector2(380, 46), new Color(0.20f, 0.50f, 0.30f), fuente, CargarEscenaJuego);
        CrearBoton(panelMenuInicio.transform, "Boton_Opciones", "AJUSTES Y OPCIONES", new Vector2(0, -80), new Vector2(380, 46), new Color(0.65f, 0.45f, 0.15f), fuente, AbrirMenuOpciones);
        CrearBoton(panelMenuInicio.transform, "Boton_Controles", "MAPA DE TECLAS / CONTROLES", new Vector2(0, -135), new Vector2(380, 46), new Color(0.38f, 0.25f, 0.18f), fuente, AlternarPanelControles);
        CrearBoton(panelMenuInicio.transform, "Boton_Salir", "SALIR DEL JUEGO", new Vector2(0, -190), new Vector2(380, 46), new Color(0.45f, 0.15f, 0.15f), fuente, SalirDelJuego);

        CrearTexto(panelMenuInicio.transform, "Texto_Creditos", "UTNG — Creación de Videojuegos | Desarrollado por Santiago Ronaldo Chávez Piñón", 16, new Color(0.6f, 0.55f, 0.5f), new Vector2(0, -300), new Vector2(900, 30), fuente);

        // Subpanel Controles
        panelControles = CrearPanel(panelMenuInicio.transform, "Subpanel_Controles", new Color(0.06f, 0.05f, 0.08f, 0.98f), new Vector2(420, -50), new Vector2(380, 280));
        CrearTexto(panelControles.transform, "Texto_Controles", "CONTROLES DE RONAN:\n\n[W, A, S, D] : Mover en 8 direcciones\n[J] o [Clic Izq] : Espadazo\n[K] o [Clic Der] : Bloqueo / Contraataque (0.20s)\n[ESPACIO] : Esquive con invulnerabilidad\n[L] : Activar Modo Asalto\n[E] : Hablar / Leer Pistas", 18, Color.white, Vector2.zero, new Vector2(340, 250), fuente);
        panelControles.SetActive(false);

        // 5. Panel Historia 4 Imágenes
        panelHistoriaCuatroImagenes = CrearPanel(canvasObj.transform, "Panel_Historia4Imagenes", new Color(0.05f, 0.04f, 0.07f, 1f), Vector2.zero, new Vector2(1280, 720));

        GameObject marcoImg = CrearPanel(panelHistoriaCuatroImagenes.transform, "Marco_Imagen", new Color(0f, 0.85f, 1f, 0.8f), new Vector2(0, 85), new Vector2(650, 370));
        GameObject objRaw = new GameObject("Imagen_Historia_Actual");
        objRaw.transform.SetParent(marcoImg.transform, false);
        imgSlideActual = objRaw.AddComponent<RawImage>();
        imgSlideActual.rectTransform.sizeDelta = new Vector2(640, 360);

        GameObject cajaTextoIntro = CrearPanel(panelHistoriaCuatroImagenes.transform, "Caja_Dialogo_Intro", new Color(0.14f, 0.10f, 0.08f, 0.95f), new Vector2(0, -185), new Vector2(920, 145));
        txtSlideDialogo = CrearTexto(cajaTextoIntro.transform, "Texto_Dialogo", "", 22, new Color(0.96f, 0.93f, 0.88f), new Vector2(0, 5), new Vector2(870, 115), fuente);
        txtIndicadorSlide = CrearTexto(panelHistoriaCuatroImagenes.transform, "Texto_Indicador_Slide", "Imagen 1 de 4", 18, new Color(0f, 0.9f, 1f), new Vector2(-240, -295), new Vector2(520, 35), fuente);

        CrearBoton(panelHistoriaCuatroImagenes.transform, "Boton_OmitirHistoria", "OMITIR HISTORIA >>", new Vector2(-420, 300), new Vector2(220, 42), new Color(0.35f, 0.25f, 0.20f), fuente, CargarEscenaJuego);
        CrearBoton(panelHistoriaCuatroImagenes.transform, "Boton_SiguienteImagen", "SIGUIENTE >>", new Vector2(340, -295), new Vector2(220, 45), new Color(0.15f, 0.55f, 0.68f), fuente, SiguienteImagenHistoria);

        panelHistoriaCuatroImagenes.SetActive(false);

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(canvasObj, "Generar UI Pantalla Inicio");
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("<color=cyan>[Ecos de San Felipe] ¡UI generada exitosamente en la ventana Hierarchy!</color>");
    }

    [ContextMenu("Limpiar UI de la Escena")]
    public void LimpiarUIEnEscena()
    {
        GameObject canvasObj = GameObject.Find("Canvas_PantallaInicio");
        if (canvasObj != null)
        {
            DestroyImmediate(canvasObj);
        }

        panelMenuInicio = null;
        panelControles = null;
        panelHistoriaCuatroImagenes = null;
        imgSlideActual = null;
        txtSlideDialogo = null;
        txtIndicadorSlide = null;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("<color=yellow>[Ecos de San Felipe] UI de Pantalla de Inicio limpiada de la Hierarchy.</color>");
    }

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
                    col = Color.Lerp(new Color(0.85f, 0.42f, 0.20f), new Color(0.16f, 0.10f, 0.28f), ny);
                    bool esSiluetaPueblo = (y < 28) || (x > 65 && x < 95 && y < 58) || (x > 55 && x < 65 && y < 70) || (x > 95 && x < 105 && y < 70);
                    if (esSiluetaPueblo) col = new Color(0.12f, 0.07f, 0.06f);
                }
                else if (indice == 1)
                {
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

    private Text CrearTexto(Transform parent, string nombre, string contenido, int fontSize, Color colorTexto, Vector2 pos, Vector2 size, Font fuente)
    {
        GameObject txtObj = new GameObject(nombre);
        txtObj.transform.SetParent(parent, false);
        Text t = txtObj.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = fontSize;
        t.color = colorTexto;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    private void CrearBoton(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size, Color colorBtn, Font fuente, UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = CrearPanel(parent, nombre, colorBtn, pos, size);
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(ReproducirSonidoClick);
        btn.onClick.AddListener(accion);
        CrearTexto(btnObj.transform, "Texto", etiqueta, 20, Color.white, Vector2.zero, size, fuente);
    }

    // =========================================================================
    // SÍNTESIS PROCEDURAL DE MÚSICA Y SFX (CALIDAD RPG FANTASY)
    // =========================================================================
    private AudioClip GenerarMusicaMenuProcedural()
    {
        int sampleRate = 44100;
        float duracion = 12.0f;
        int totalSamples = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[totalSamples];

        // Progresión ambiental en Re menor: Dm -> Bb -> C -> Am
        float[] notasMelodia = {
            293.66f, 349.23f, 440.00f, 587.33f,
            466.16f, 440.00f, 349.23f, 293.66f,
            261.63f, 329.63f, 392.00f, 523.25f,
            440.00f, 392.00f, 329.63f, 220.00f
        };

        float[] notasBajo = {
            146.83f, 146.83f, 146.83f, 146.83f,
            116.54f, 116.54f, 116.54f, 116.54f,
            130.81f, 130.81f, 130.81f, 130.81f,
            110.00f, 110.00f, 110.00f, 110.00f
        };

        int samplesPorNota = totalSamples / notasMelodia.Length;

        for (int i = 0; i < totalSamples; i++)
        {
            int idx = Mathf.Min(i / samplesPorNota, notasMelodia.Length - 1);
            float freqM = notasMelodia[idx];
            float freqB = notasBajo[idx];
            float tGlobal = i / (float)sampleRate;
            float tNota = (i % samplesPorNota) / (float)sampleRate;

            // Melodía principal tipo campana / arpa
            float env = Mathf.Exp(-2.5f * tNota);
            float m = (Mathf.Sin(2f * Mathf.PI * freqM * tNota) + 
                       0.35f * Mathf.Sin(4f * Mathf.PI * freqM * tNota) + 
                       0.12f * Mathf.Sin(6f * Mathf.PI * freqM * tNota)) * env;

            // Eco armónico místico ("Ecos")
            if (tNota > 0.22f)
            {
                float tEcho = tNota - 0.22f;
                float echoEnv = Mathf.Exp(-3.5f * tEcho);
                m += 0.25f * Mathf.Sin(2f * Mathf.PI * (freqM * 1.5f) * tEcho) * echoEnv;
            }

            // Colchón ambiental de bajo
            float b = 0.2f * Mathf.Sin(2f * Mathf.PI * freqB * tGlobal) + 
                      0.1f * Mathf.Sin(2f * Mathf.PI * (freqB * 1.5f) * tGlobal);

            samples[i] = (m * 0.45f + b * 0.25f) * 0.65f;
        }

        AudioClip clip = AudioClip.Create("Musica_PantallaInicio_Procedural", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerarSonidoClick()
    {
        int sampleRate = 44100;
        float duracion = 0.08f;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float progreso = i / (float)count;
            float freq = Mathf.Lerp(620f, 280f, progreso);
            float env = 1f - progreso;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * env * 0.4f;
        }

        AudioClip clip = AudioClip.Create("SFX_MenuClick", count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PantallaInicioManager))]
public class PantallaInicioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PantallaInicioManager manager = (PantallaInicioManager)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🎮 Elementos en Hierarchy (Modo Edición)", EditorStyles.boldLabel);

        bool tieneUI = GameObject.Find("Canvas_PantallaInicio") != null;

        if (tieneUI)
        {
            EditorGUILayout.HelpBox("✅ Los elementos de la UI (Canvas, Paneles, Botones) están presentes en la Hierarchy y visibles en Scene.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠️ La UI no está en Hierarchy (solo se generaría al dar Play). Haz clic en el botón de abajo para generarla permanentemente.", MessageType.Warning);
        }

        GUI.backgroundColor = new Color(0.2f, 0.85f, 1f);
        if (GUILayout.Button(tieneUI ? "🔄 Regenerar / Actualizar UI en Hierarchy" : "🎨 Generar Todos los Elementos de UI en Hierarchy", GUILayout.Height(40)))
        {
            manager.GenerarUIEnEscena();
        }

        if (tieneUI)
        {
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("🗑️ Limpiar UI de Hierarchy", GUILayout.Height(28)))
            {
                manager.LimpiarUIEnEscena();
            }
        }

        GUI.backgroundColor = Color.white;
    }
}

[InitializeOnLoad]
public static class PantallaInicioAutoSetup
{
    static PantallaInicioAutoSetup()
    {
        EditorApplication.delayCall += EjecutarAutoSetup;
        EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            if (scene.name == "PantallaInicio")
            {
                EditorApplication.delayCall += EjecutarAutoSetup;
            }
        };
    }

    private static void EjecutarAutoSetup()
    {
        if (Application.isPlaying) return;

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name == "PantallaInicio")
        {
            if (GameObject.Find("Canvas_PantallaInicio") == null)
            {
                PantallaInicioManager mgr = Object.FindAnyObjectByType<PantallaInicioManager>();
                if (mgr != null)
                {
                    mgr.GenerarUIEnEscena();
                }
            }
        }
    }

    [MenuItem("Ecos de San Felipe/🎨 Generar UI en Hierarchy (PantallaInicio)")]
    public static void MenuGenerarUI()
    {
        PantallaInicioManager mgr = Object.FindAnyObjectByType<PantallaInicioManager>();
        if (mgr != null)
        {
            mgr.GenerarUIEnEscena();
        }
        else
        {
            EditorUtility.DisplayDialog("Aviso", "Abre la escena 'PantallaInicio' para generar su UI.", "OK");
        }
    }
}
#endif
