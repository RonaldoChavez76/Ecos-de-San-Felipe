using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Controlador y Diseñador Visual de la Pantalla de Inicio de «Ecos de San Felipe».
/// Presenta:
///  • Fondo panorámico Pixel-Art colonial de San Felipe de noche: cielo estrellado, luna llena,
///    silueta de la Parroquia de las Torres Mochas, atrio de cantera, faroles coloniales cálidos y niebla.
///  • 5 Orbes luminosos de los Ecos (Fe, Tradición, Memoria, Naturaleza, Coraje) con animación flotante viva.
///  • Marco Central Colonial Sacro con doble ribete de oro y cantera novohispana.
///  • Indicador dinámico de Dificultad sincronizado con SaveStateManager y OptionsModalUI.
///  • 5 Botones estilizados con relieve colonial, biseles 9-slice y efectos hover/sonido:
///      1. [▶ INICIAR AVENTURA (HISTORIA)]
///      2. [⚔ JUGAR DIRECTO AL NIVEL 1]
///      3. [⚙ AJUSTES Y OPCIONES] -> Abre modal OptionsModalUI
///      4. [📜 MAPA DE TECLAS Y CONTROLES]
///      5. [✕ SALIR DEL JUEGO]
///  • Subpanel de Controles colonial ampliado y Panel de Historia de 4 imágenes sacras.
/// </summary>
public class PantallaInicioManager : MonoBehaviour
{
    [Header("1. Las 4 Imágenes de la Historia Inicial (Opcionales)")]
    [Tooltip("Arrastra aquí tus 4 imágenes. Si las dejas vacías, el código dibuja 4 ilustraciones Pixel-Art automáticamente")]
    public Texture2D[] cuatroImagenesHistoria = new Texture2D[4];

    [Header("2. Nombre de la Escena del Nivel 1")]
    public string nombreEscenaNivel1 = "SampleScene";

    [Header("3. Música y Sonido del Menú")]
    [Tooltip("Arrastra aquí tu archivo de música. Si lo dejas vacío, genera una melodía mística de fondo en Re menor automáticamente")]
    public AudioClip musicaMenu;

    [Range(0f, 1f)]
    [Tooltip("Volumen de la música de fondo")]
    public float volumenMusica = 0.38f;

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

    [Header("5. Referencias a la UI en Escena")]
    public GameObject panelMenuInicio;
    public GameObject panelControles;
    public GameObject panelHistoriaCuatroImagenes;
    public RawImage imgSlideActual;
    public Text txtSlideDialogo;
    public Text txtIndicadorSlide;

    [Header("6. Referencias del Nuevo Diseño Colonial")]
    public RawImage fondoAtmosphere;
    public Text txtPlacaDificultad;
    public List<RectTransform> orbesEcosTransforms = new List<RectTransform>();
    public List<Vector2> orbesEcosBasePos = new List<Vector2>();

    private int indiceSlideActual = 0;
    private Texture2D[] slidesGeneradas = new Texture2D[4];
    private Coroutine corrutinaEscritura;
    private AudioSource audioSource;
    private AudioSource sfxSource;

    private bool escribiendoTexto = false;
    private string mensajeObjetivo = "";

    void Awake()
    {
        VerificarYActualizarUI();
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
    }

    void Start()
    {
        Time.timeScale = 1f;

        for (int i = 0; i < 4; i++)
        {
            slidesGeneradas[i] = GenerarIlustracionHistoria(i);
        }

        InicializarAudio();
        VerificarYActualizarUI();
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
        ActualizarEtiquetaDificultad();
        MostrarMenuPrincipal();
    }

    void Update()
    {
        // 1. Avanzar la historia de 4 imágenes
        if (panelHistoriaCuatroImagenes != null && panelHistoriaCuatroImagenes.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                SiguienteImagenHistoria();
            }
        }

        // 2. Animar orbes de los Ecos en vivo
        AnimarEcosFlotantes();

        // 3. Mantener actualizada la placa de dificultad
        ActualizarEtiquetaDificultad();
    }

    public void AbrirMenuOpciones()
    {
        ReproducirSonidoClick();
        OptionsModalUI.EnsureInstance()?.AbrirModal();
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
        Debug.Log("<color=cyan>[Ecos de San Felipe] Cargando nivel de juego: " + nombreEscenaNivel1 + "...</color>");
        SceneManager.LoadScene(nombreEscenaNivel1);
    }

    public void SalirDelJuego()
    {
        Debug.Log("[Ecos de San Felipe] Saliendo del juego...");
        Application.Quit();
    }

    public void AlternarPanelControles()
    {
        if (panelControles != null)
        {
            panelControles.SetActive(!panelControles.activeSelf);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANIMACIÓN Y ACTUALIZACIÓN DINÁMICA
    // ─────────────────────────────────────────────────────────────────────────
    private void AnimarEcosFlotantes()
    {
        if (orbesEcosTransforms == null || orbesEcosTransforms.Count == 0) return;

        float t = Time.time;
        for (int i = 0; i < orbesEcosTransforms.Count; i++)
        {
            RectTransform rt = orbesEcosTransforms[i];
            if (rt == null) continue;

            Vector2 basePos = (orbesEcosBasePos != null && i < orbesEcosBasePos.Count) ? orbesEcosBasePos[i] : Vector2.zero;
            float offY = Mathf.Sin(t * 1.8f + i * 1.25f) * 12f;
            float offX = Mathf.Cos(t * 1.1f + i * 0.95f) * 6f;
            rt.anchoredPosition = new Vector2(basePos.x + offX, basePos.y + offY);

            float scale = 1f + Mathf.Sin(t * 2.4f + i * 1.15f) * 0.10f;
            rt.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void ActualizarEtiquetaDificultad()
    {
        if (txtPlacaDificultad == null) return;

        string difNombre = "NORMAL";
        Color colDif = new Color(0.95f, 0.85f, 0.40f);

        if (SaveStateManager.Instance != null)
        {
            switch (SaveStateManager.Instance.dificultadActual)
            {
                case SaveStateManager.GameDifficulty.Facil:
                    difNombre = "FÁCIL";
                    colDif = new Color(0.40f, 0.95f, 0.55f);
                    break;
                case SaveStateManager.GameDifficulty.Normal:
                    difNombre = "NORMAL";
                    colDif = new Color(0.95f, 0.85f, 0.40f);
                    break;
                case SaveStateManager.GameDifficulty.Dificil:
                    difNombre = "DIFÍCIL";
                    colDif = new Color(1.0f, 0.35f, 0.40f);
                    break;
            }
        }
        else
        {
            int savedDif = PlayerPrefs.GetInt("GameDifficulty", 1);
            if (savedDif == 0) { difNombre = "FÁCIL"; colDif = new Color(0.40f, 0.95f, 0.55f); }
            else if (savedDif == 2) { difNombre = "DIFÍCIL"; colDif = new Color(1.0f, 0.35f, 0.40f); }
        }

        string hexCol = ColorUtility.ToHtmlStringRGB(colDif);
        txtPlacaDificultad.text = $"⚔  DIFICULTAD: <color=#{hexCol}><b>[ {difNombre} ]</b></color>  ⚔";
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
            txtIndicadorSlide.text = $"Crónica {indiceSlideActual + 1} de 4 — Presiona [ESPACIO] o clic en 'Siguiente'";
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

        if (musicaMenu != null)
        {
            audioSource.clip = musicaMenu;
        }
        else
        {
            audioSource.clip = GenerarMusicaMenuProcedural();
        }

        audioSource.Play();

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
            sfxSource.PlayOneShot(sfxClick, 0.70f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CONEXIÓN Y DETECCIÓN AUTOMÁTICA DE UI VIEJA VS NUEVA
    // ─────────────────────────────────────────────────────────────────────────
    public void VerificarYActualizarUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas_PantallaInicio");
        bool esIncompleto = false;

        if (canvasObj != null)
        {
            Transform fondo = canvasObj.transform.Find("Fondo_SanFelipe_Atmosphere");
            Button[] btns = canvasObj.GetComponentsInChildren<Button>(true);
            bool tieneOpciones = false;
            foreach (var b in btns)
            {
                if (b.gameObject.name == "Boton_Opciones") { tieneOpciones = true; break; }
            }

            if (fondo == null || !tieneOpciones)
            {
                esIncompleto = true;
            }
        }
        else
        {
            esIncompleto = true;
        }

        if (esIncompleto)
        {
            GenerarUIEnEscena();
        }
    }

    public void BuscarOConectarReferenciasExistentes()
    {
        GameObject canvasObj = GameObject.Find("Canvas_PantallaInicio");
        if (canvasObj == null) return;

        Transform tCanvas = canvasObj.transform;

        Transform tFondo = tCanvas.Find("Fondo_SanFelipe_Atmosphere");
        if (tFondo != null) fondoAtmosphere = tFondo.GetComponent<RawImage>();

        Transform tMenu = tCanvas.Find("Panel_MenuInicio");
        if (tMenu != null)
        {
            panelMenuInicio = tMenu.gameObject;
            Transform tCtrl = tMenu.Find("Subpanel_Controles");
            if (tCtrl != null) panelControles = tCtrl.gameObject;

            Transform tPlaca = tMenu.Find("Marco_Central_Colonial/Placa_Dificultad/Texto_Dificultad");
            if (tPlaca == null) tPlaca = tMenu.Find("Placa_Dificultad/Texto_Dificultad");
            if (tPlaca != null) txtPlacaDificultad = tPlaca.GetComponent<Text>();
        }

        Transform tContEcos = tCanvas.Find("Contenedor_Ecos_Flotantes");
        if (tContEcos != null)
        {
            orbesEcosTransforms.Clear();
            orbesEcosBasePos.Clear();
            for (int i = 0; i < tContEcos.childCount; i++)
            {
                RectTransform rt = tContEcos.GetChild(i) as RectTransform;
                if (rt != null)
                {
                    orbesEcosTransforms.Add(rt);
                    orbesEcosBasePos.Add(rt.anchoredPosition);
                }
            }
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
            AsignarListenerBoton(t, "Placa_Dificultad", AbrirMenuOpciones);
            AsignarListenerBoton(t, "Boton_CerrarControles", () => { if (panelControles != null) panelControles.SetActive(false); });
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
        Button[] btns = parent.GetComponentsInChildren<Button>(true);
        foreach (Button btn in btns)
        {
            if (btn.gameObject.name == nombreBoton)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ReproducirSonidoClick);
                btn.onClick.AddListener(accion);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GENERADOR VISUAL EN ESCENA (DISEÑO COLONIAL COMPLETO)
    // ─────────────────────────────────────────────────────────────────────────
    [ContextMenu("Generar UI en Escena (Hierarchy)")]
    public void GenerarUIEnEscena()
    {
        // 1. Limpiar canvas previo si ya existiera
        GameObject canvasViejo = GameObject.Find("Canvas_PantallaInicio");
        if (canvasViejo != null)
        {
            DestroyImmediate(canvasViejo);
        }

        // 2. EventSystem
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
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fuente == null) fuente = Font.CreateDynamicFontFromOSFont("Arial", 18);

        // Sprites Procedurales de Oro y Cantera
        Sprite spPanel = GenerarSpritePanelColonial(64, 64);
        Sprite spBotonAzul = GenerarSpriteBotonColonial(new Color(0.10f, 0.38f, 0.52f), new Color(0.95f, 0.78f, 0.30f), 64, 32);
        Sprite spBotonVerde = GenerarSpriteBotonColonial(new Color(0.12f, 0.40f, 0.24f), new Color(0.95f, 0.78f, 0.30f), 64, 32);
        Sprite spBotonDorado = GenerarSpriteBotonColonial(new Color(0.50f, 0.36f, 0.12f), new Color(1f, 0.88f, 0.42f), 64, 32);
        Sprite spBotonMarron = GenerarSpriteBotonColonial(new Color(0.34f, 0.22f, 0.18f), new Color(0.90f, 0.72f, 0.30f), 64, 32);
        Sprite spBotonRojo = GenerarSpriteBotonColonial(new Color(0.42f, 0.12f, 0.14f), new Color(0.95f, 0.65f, 0.30f), 64, 32);

        // 4. FONDO PANORÁMICO COLONIAL DE SAN FELIPE
        GameObject fondoObj = new GameObject("Fondo_SanFelipe_Atmosphere");
        fondoObj.transform.SetParent(canvasObj.transform, false);
        fondoAtmosphere = fondoObj.AddComponent<RawImage>();
        fondoAtmosphere.texture = GenerarFondoSanFelipeNocturno(640, 360);
        RectTransform rtFondo = fondoObj.GetComponent<RectTransform>();
        rtFondo.anchorMin = Vector2.zero;
        rtFondo.anchorMax = Vector2.one;
        rtFondo.offsetMin = Vector2.zero;
        rtFondo.offsetMax = Vector2.zero;

        // 5. ORBES LUMINOSOS FLOTANTES DE LOS 5 ECOS
        GameObject contEcos = new GameObject("Contenedor_Ecos_Flotantes");
        contEcos.transform.SetParent(canvasObj.transform, false);
        orbesEcosTransforms.Clear();
        orbesEcosBasePos.Clear();

        Color[] coloresEcos = {
            new Color(0f, 0.90f, 1f, 0.95f),     // 1. Fe (Cyan místico)
            new Color(1f, 0.85f, 0.30f, 0.95f),   // 2. Tradición (Dorado)
            new Color(0.80f, 0.55f, 0.95f, 0.95f), // 3. Memoria (Amatista)
            new Color(0.35f, 0.95f, 0.65f, 0.95f), // 4. Naturaleza (Esmeralda)
            new Color(1f, 0.30f, 0.35f, 0.95f)    // 5. Coraje (Rubí)
        };

        Vector2[] posEcos = {
            new Vector2(-380f, 120f),
            new Vector2(380f, 140f),
            new Vector2(-430f, -70f),
            new Vector2(420f, -90f),
            new Vector2(0f, 250f)
        };

        for (int i = 0; i < coloresEcos.Length; i++)
        {
            GameObject orbe = new GameObject($"Eco_Flotante_{i + 1}");
            orbe.transform.SetParent(contEcos.transform, false);
            Image imgOrbe = orbe.AddComponent<Image>();
            imgOrbe.sprite = GenerarSpriteOrbe(coloresEcos[i], 32);
            RectTransform rtOrbe = orbe.GetComponent<RectTransform>();
            rtOrbe.sizeDelta = new Vector2(34f, 34f);
            rtOrbe.anchoredPosition = posEcos[i];
            orbesEcosTransforms.Add(rtOrbe);
            orbesEcosBasePos.Add(posEcos[i]);
        }

        // 6. PANEL MENÚ DE INICIO
        panelMenuInicio = new GameObject("Panel_MenuInicio");
        panelMenuInicio.transform.SetParent(canvasObj.transform, false);
        RectTransform rtMenu = panelMenuInicio.AddComponent<RectTransform>();
        rtMenu.anchorMin = Vector2.zero;
        rtMenu.anchorMax = Vector2.one;
        rtMenu.offsetMin = Vector2.zero;
        rtMenu.offsetMax = Vector2.zero;

        // MARCO CENTRAL COLONIAL SACRO (Cantera y Ribete de Oro)
        GameObject marcoCentral = new GameObject("Marco_Central_Colonial");
        marcoCentral.transform.SetParent(panelMenuInicio.transform, false);
        Image imgMarco = marcoCentral.AddComponent<Image>();
        imgMarco.sprite = spPanel;
        imgMarco.type = Image.Type.Sliced;
        RectTransform rtMarco = marcoCentral.GetComponent<RectTransform>();
        rtMarco.sizeDelta = new Vector2(530f, 620f);
        rtMarco.anchoredPosition = new Vector2(0f, 0f);

        Shadow shadowMarco = marcoCentral.AddComponent<Shadow>();
        shadowMarco.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadowMarco.effectDistance = new Vector2(4f, -4f);

        // TÍTULO: ★ ECOS DE SAN FELIPE ★
        Text tTitulo = CrearTexto(marcoCentral.transform, "Titulo_EcosDeSanFelipe", "★  ECOS DE SAN FELIPE  ★", 34, new Color(1f, 0.88f, 0.42f), new Vector2(0, 238), new Vector2(500, 48), fuente);
        tTitulo.fontStyle = FontStyle.Bold;
        Outline outTitulo = tTitulo.gameObject.AddComponent<Outline>();
        outTitulo.effectColor = new Color(0.20f, 0.12f, 0.04f, 0.95f);
        outTitulo.effectDistance = new Vector2(2f, -2f);

        // SUBTÍTULO
        Text tSub = CrearTexto(marcoCentral.transform, "Subtitulo_SanFelipe", "Torres Mochas — San Felipe, Guanajuato", 16, new Color(0.55f, 0.95f, 1f), new Vector2(0, 198), new Vector2(480, 28), fuente);
        tSub.fontStyle = FontStyle.Italic;
        Outline outSub = tSub.gameObject.AddComponent<Outline>();
        outSub.effectColor = new Color(0.04f, 0.08f, 0.14f, 0.95f);
        outSub.effectDistance = new Vector2(1.5f, -1.5f);

        // DIVISOR DORADO
        CrearPanel(marcoCentral.transform, "Divisor_Dorado", new Color(0.92f, 0.74f, 0.28f, 0.65f), new Vector2(0, 172), new Vector2(380, 2));

        // PLACA DE DIFICULTAD INTERACTIVA
        GameObject placaDifObj = CrearBotonConSprite(marcoCentral.transform, "Placa_Dificultad", "", new Vector2(0, 142), new Vector2(390, 36), spBotonDorado, fuente, AbrirMenuOpciones);
        txtPlacaDificultad = CrearTexto(placaDifObj.transform, "Texto_Dificultad", "⚔  DIFICULTAD: [ NORMAL ]  ⚔", 15, Color.white, Vector2.zero, new Vector2(380, 34), fuente);
        txtPlacaDificultad.fontStyle = FontStyle.Bold;
        Outline outDif = txtPlacaDificultad.gameObject.AddComponent<Outline>();
        outDif.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f);
        outDif.effectDistance = new Vector2(1.2f, -1.2f);

        // LOS 5 BOTONES ESTILIZADOS
        CrearBotonConSprite(marcoCentral.transform, "Boton_IniciarAventura", "▶  INICIAR AVENTURA (HISTORIA)", new Vector2(0, 78), new Vector2(400, 48), spBotonAzul, fuente, ComenzarHistoriaCuatroImagenes);
        CrearBotonConSprite(marcoCentral.transform, "Boton_JugarDirecto", "⚔  JUGAR DIRECTO AL NIVEL 1", new Vector2(0, 22), new Vector2(400, 46), spBotonVerde, fuente, CargarEscenaJuego);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Opciones", "⚙  AJUSTES Y OPCIONES", new Vector2(0, -34), new Vector2(400, 46), spBotonDorado, fuente, AbrirMenuOpciones);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Controles", "📜  MAPA DE TECLAS Y CONTROLES", new Vector2(0, -90), new Vector2(400, 46), spBotonMarron, fuente, AlternarPanelControles);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Salir", "✕  SALIR DEL JUEGO", new Vector2(0, -146), new Vector2(400, 44), spBotonRojo, fuente, SalirDelJuego);

        // PIE DEL MARCO (CRÉDITOS Y VERSIÓN)
        Text tCred = CrearTexto(marcoCentral.transform, "Texto_Creditos", "UTNG — Creación de Videojuegos | Santiago Ronaldo Chávez Piñón", 13, new Color(0.85f, 0.82f, 0.78f), new Vector2(0, -222), new Vector2(490, 24), fuente);
        Outline outCred = tCred.gameObject.AddComponent<Outline>();
        outCred.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outCred.effectDistance = new Vector2(1f, -1f);

        Text tVers = CrearTexto(marcoCentral.transform, "Texto_Version", "★ Edición Expandida — San Felipe, Guanajuato ★", 12, new Color(0.95f, 0.78f, 0.35f, 0.90f), new Vector2(0, -246), new Vector2(490, 22), fuente);
        tVers.fontStyle = FontStyle.Italic;

        // 7. SUBPANEL DE CONTROLES COLONIAL
        panelControles = new GameObject("Subpanel_Controles");
        panelControles.transform.SetParent(panelMenuInicio.transform, false);
        Image imgCtrl = panelControles.AddComponent<Image>();
        imgCtrl.sprite = spPanel;
        imgCtrl.type = Image.Type.Sliced;
        RectTransform rtCtrl = panelControles.GetComponent<RectTransform>();
        rtCtrl.sizeDelta = new Vector2(640f, 480f);
        rtCtrl.anchoredPosition = new Vector2(0f, 0f);

        Shadow shadowCtrl = panelControles.AddComponent<Shadow>();
        shadowCtrl.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadowCtrl.effectDistance = new Vector2(5f, -5f);

        Text tCtrlTitle = CrearTexto(panelControles.transform, "Titulo_Controles", "★ CONTROLES DE COMBATE Y AVENTURA ★", 22, new Color(1f, 0.88f, 0.42f), new Vector2(0, 205), new Vector2(580, 36), fuente);
        tCtrlTitle.fontStyle = FontStyle.Bold;
        Outline outCtrlT = tCtrlTitle.gameObject.AddComponent<Outline>();
        outCtrlT.effectColor = new Color(0.18f, 0.10f, 0.02f, 0.95f);
        outCtrlT.effectDistance = new Vector2(1.5f, -1.5f);

        CrearPanel(panelControles.transform, "Divisor_Controles", new Color(0.92f, 0.74f, 0.28f, 0.60f), new Vector2(0, 182), new Vector2(520, 2));

        string controlesDetalle =
            "<b>[W, A, S, D]</b> : Moverse en 8 direcciones\n" +
            "<b>[J] / [Clic Izq]</b> : Atacar con el arma activa (Espada, Flechas, Magia)\n" +
            "<b>[Q] / [1, 2, 3]</b> : Selector de arma activa (Espada / Arco / Báculo)\n" +
            "<b>[K] / [Clic Der]</b> : Bloqueo y <b>Contraataque instantáneo (0.20s)</b>\n" +
            "<b>[ESPACIO]</b> : Rodar / Esquivar con invulnerabilidad temporal\n" +
            "<b>[L]</b> : Desatar <b>Modo Asalto</b> con la energía al máximo\n" +
            "<b>[E]</b> : Hablar con habitantes / Leer inscripciones sagradas\n" +
            "<b>[ESC]</b> : Pausar la partida y abrir Ajustes / Opciones";

        Text tCtrlBody = CrearTexto(panelControles.transform, "Texto_Controles_Detallado", controlesDetalle, 15, new Color(0.95f, 0.93f, 0.88f), new Vector2(0, 35), new Vector2(580, 260), fuente);
        tCtrlBody.alignment = TextAnchor.MiddleLeft;
        tCtrlBody.lineSpacing = 1.25f;
        Outline outCtrlB = tCtrlBody.gameObject.AddComponent<Outline>();
        outCtrlB.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outCtrlB.effectDistance = new Vector2(1f, -1f);

        CrearBotonConSprite(panelControles.transform, "Boton_CerrarControles", "ENTENDIDO / VOLVER AL MENÚ", new Vector2(0, -188), new Vector2(360, 44), spBotonDorado, fuente, () => panelControles.SetActive(false));
        panelControles.SetActive(false);

        // 8. PANEL HISTORIA 4 IMÁGENES
        panelHistoriaCuatroImagenes = new GameObject("Panel_Historia4Imagenes");
        panelHistoriaCuatroImagenes.transform.SetParent(canvasObj.transform, false);
        Image imgHist = panelHistoriaCuatroImagenes.AddComponent<Image>();
        imgHist.color = new Color(0.04f, 0.03f, 0.06f, 0.98f);
        RectTransform rtHist = panelHistoriaCuatroImagenes.GetComponent<RectTransform>();
        rtHist.anchorMin = Vector2.zero;
        rtHist.anchorMax = Vector2.one;
        rtHist.offsetMin = Vector2.zero;
        rtHist.offsetMax = Vector2.zero;

        // Marco Imagen
        GameObject marcoImg = new GameObject("Marco_Imagen");
        marcoImg.transform.SetParent(panelHistoriaCuatroImagenes.transform, false);
        Image imgMarcoHist = marcoImg.AddComponent<Image>();
        imgMarcoHist.sprite = spPanel;
        imgMarcoHist.type = Image.Type.Sliced;
        RectTransform rtMarcoImg = marcoImg.GetComponent<RectTransform>();
        rtMarcoImg.sizeDelta = new Vector2(660f, 380f);
        rtMarcoImg.anchoredPosition = new Vector2(0f, 90f);

        GameObject objRaw = new GameObject("Imagen_Historia_Actual");
        objRaw.transform.SetParent(marcoImg.transform, false);
        imgSlideActual = objRaw.AddComponent<RawImage>();
        RectTransform rtImgRaw = objRaw.GetComponent<RectTransform>();
        rtImgRaw.sizeDelta = new Vector2(640f, 360f);
        rtImgRaw.anchoredPosition = Vector2.zero;

        // Caja Diálogo
        GameObject cajaTextoIntro = new GameObject("Caja_Dialogo_Intro");
        cajaTextoIntro.transform.SetParent(panelHistoriaCuatroImagenes.transform, false);
        Image imgCaja = cajaTextoIntro.AddComponent<Image>();
        imgCaja.sprite = spPanel;
        imgCaja.type = Image.Type.Sliced;
        RectTransform rtCaja = cajaTextoIntro.GetComponent<RectTransform>();
        rtCaja.sizeDelta = new Vector2(940f, 150f);
        rtCaja.anchoredPosition = new Vector2(0f, -185f);

        txtSlideDialogo = CrearTexto(cajaTextoIntro.transform, "Texto_Dialogo", "", 21, new Color(0.96f, 0.94f, 0.90f), new Vector2(0, 5), new Vector2(880, 120), fuente);
        txtSlideDialogo.alignment = TextAnchor.MiddleLeft;

        txtIndicadorSlide = CrearTexto(panelHistoriaCuatroImagenes.transform, "Texto_Indicador_Slide", "Crónica 1 de 4", 17, new Color(0.55f, 0.95f, 1f), new Vector2(-240, -295), new Vector2(520, 35), fuente);

        CrearBotonConSprite(panelHistoriaCuatroImagenes.transform, "Boton_OmitirHistoria", "OMITIR HISTORIA >>", new Vector2(-420, 305), new Vector2(230, 42), spBotonMarron, fuente, CargarEscenaJuego);
        CrearBotonConSprite(panelHistoriaCuatroImagenes.transform, "Boton_SiguienteImagen", "SIGUIENTE >>", new Vector2(340, -295), new Vector2(230, 45), spBotonAzul, fuente, SiguienteImagenHistoria);

        panelHistoriaCuatroImagenes.SetActive(false);

        // Vincular referencias y listeners
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
        ActualizarEtiquetaDificultad();

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(canvasObj, "Generar UI Pantalla Inicio Colonial");
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("<color=cyan>[Ecos de San Felipe] ¡Nueva UI Colonial Sacra generada exitosamente en la Hierarchy!</color>");
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
        fondoAtmosphere = null;
        txtPlacaDificultad = null;
        orbesEcosTransforms.Clear();
        orbesEcosBasePos.Clear();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("<color=yellow>[Ecos de San Felipe] UI de Pantalla de Inicio limpiada de la Hierarchy.</color>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GENERADORES PROCEDURALES DE TEXTURAS Y SPRITES COLONIALES
    // ─────────────────────────────────────────────────────────────────────────
    public static Texture2D GenerarFondoSanFelipeNocturno(int w = 640, int h = 360)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[w * h];

        Vector2 lunaPos = new Vector2(w * 0.82f, h * 0.80f);
        float radioLuna = 18f;

        Vector2 farolIzq = new Vector2(75f, 75f);
        Vector2 farolDer = new Vector2(w - 75f, 75f);

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w;
                Color col;

                // Cielo Nocturno
                if (ny > 0.16f)
                {
                    float tCielo = Mathf.Clamp01((ny - 0.16f) / 0.84f);
                    col = Color.Lerp(
                        new Color(0.12f, 0.14f, 0.26f),
                        new Color(0.03f, 0.04f, 0.10f),
                        Mathf.Pow(tCielo, 0.75f)
                    );

                    int hash = (x * 7919 + y * 6271 + (x ^ y) * 104729) & 0x7FFFFFFF;
                    if (hash % 650 == 0)
                    {
                        float brillo = 0.5f + (hash % 100) / 200f;
                        Color starCol = (hash % 3 == 0) ? new Color(1f, 0.95f, 0.8f, brillo) : new Color(0.8f, 0.9f, 1f, brillo);
                        col = Color.Lerp(col, starCol, brillo);
                    }
                    else if (hash % 3800 == 0)
                    {
                        col = Color.Lerp(col, new Color(1f, 1f, 0.9f), 0.95f);
                    }

                    float distLuna = Vector2.Distance(new Vector2(x, y), lunaPos);
                    if (distLuna <= radioLuna)
                    {
                        float crater = ((x * 13 + y * 17) % 7) / 40f;
                        col = new Color(0.96f - crater, 0.95f - crater, 0.88f - crater, 1f);
                    }
                    else if (distLuna < radioLuna * 2.8f)
                    {
                        float tHalo = 1f - (distLuna - radioLuna) / (radioLuna * 1.8f);
                        Color haloCol = new Color(0.45f, 0.65f, 0.95f, tHalo * 0.35f);
                        col = Color.Lerp(col, haloCol, tHalo * 0.45f);
                    }
                }
                else
                {
                    float tSuelo = ny / 0.16f;
                    col = Color.Lerp(new Color(0.07f, 0.05f, 0.08f), new Color(0.14f, 0.10f, 0.13f), tSuelo);
                    if (y % 8 == 0 || (x + (y / 8) * 12) % 24 == 0)
                    {
                        col *= 0.85f;
                    }
                }

                // Sierra del Fraile lejana
                float montaniaY = 55f + Mathf.Sin(nx * 5.2f) * 16f + Mathf.Cos(nx * 11.5f) * 8f;
                if (y < montaniaY && ny >= 0.14f)
                {
                    col = Color.Lerp(col, new Color(0.06f, 0.07f, 0.15f), 0.88f);
                }

                // Parroquia de San Felipe (Torres Mochas)
                bool esParroquia = false;
                Color colCantera = new Color(0.14f, 0.10f, 0.13f);
                Color colDetalle = new Color(0.22f, 0.16f, 0.20f);

                if (x >= 195 && x <= 250 && y >= 45 && y <= 185)
                {
                    esParroquia = true;
                    if (x >= 210 && x <= 235 && y >= 125 && y <= 165)
                        colCantera = new Color(0.05f, 0.04f, 0.06f);
                    if (y >= 180 || y == 120 || y == 75)
                        colCantera = colDetalle;
                }
                else if (x >= 390 && x <= 445 && y >= 45 && y <= 185)
                {
                    esParroquia = true;
                    if (x >= 405 && x <= 430 && y >= 125 && y <= 165)
                        colCantera = new Color(0.05f, 0.04f, 0.06f);
                    if (y >= 180 || y == 120 || y == 75)
                        colCantera = colDetalle;
                }
                else if (x > 250 && x < 390 && y >= 45 && y <= 155)
                {
                    float distCentroX = Mathf.Abs(x - 320);
                    float frontonY = 155f - (distCentroX / 70f) * 25f;
                    if (y <= frontonY)
                    {
                        esParroquia = true;
                        float distRoseton = Vector2.Distance(new Vector2(x, y), new Vector2(320, 115));
                        if (distRoseton <= 15f)
                        {
                            float tRos = distRoseton / 15f;
                            colCantera = Color.Lerp(new Color(0.15f, 0.90f, 1f, 0.95f), new Color(0.05f, 0.35f, 0.55f), tRos);
                        }
                        else if (x >= 295 && x <= 345 && y <= 90)
                        {
                            colCantera = (distCentroX < 20 && y < 80) ? new Color(0.04f, 0.03f, 0.05f) : colDetalle;
                        }
                        else if (y == 135 || y == 95)
                        {
                            colCantera = colDetalle;
                        }
                    }
                }
                else if (Mathf.Abs(x - 320) <= 2 && y >= 155 && y <= 170)
                {
                    esParroquia = true;
                    colCantera = colDetalle;
                }
                else if (y >= 163 && y <= 166 && Mathf.Abs(x - 320) <= 7)
                {
                    esParroquia = true;
                    colCantera = colDetalle;
                }

                if (esParroquia)
                {
                    col = colCantera;
                }

                // Halos de Faroles
                float dFarol1 = Vector2.Distance(new Vector2(x, y), farolIzq);
                if (dFarol1 < 60f)
                {
                    float tFarol = 1f - (dFarol1 / 60f);
                    col = Color.Lerp(col, new Color(1f, 0.72f, 0.22f), tFarol * 0.38f);
                }
                float dFarol2 = Vector2.Distance(new Vector2(x, y), farolDer);
                if (dFarol2 < 60f)
                {
                    float tFarol = 1f - (dFarol2 / 60f);
                    col = Color.Lerp(col, new Color(1f, 0.72f, 0.22f), tFarol * 0.38f);
                }

                if ((Mathf.Abs(x - farolIzq.x) <= 2 || Mathf.Abs(x - farolDer.x) <= 2) && y >= 25 && y <= 80)
                {
                    col = new Color(0.05f, 0.04f, 0.05f);
                }
                if (dFarol1 <= 4f || dFarol2 <= 4f)
                {
                    col = new Color(1f, 0.90f, 0.45f);
                }

                // Bruma mística
                if (y >= 35 && y <= 80)
                {
                    float tBruma = Mathf.Sin((y - 35) / 45f * Mathf.PI);
                    float ondulacion = Mathf.Sin(x * 0.035f + y * 0.05f) * 0.5f + 0.5f;
                    Color brumaCol = new Color(0.10f, 0.50f, 0.75f, 0.18f * tBruma * ondulacion);
                    col = Color.Lerp(col, brumaCol, tBruma * 0.22f);
                }

                pixels[y * w + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Sprite GenerarSpriteBotonColonial(Color baseColor, Color bordeColor, int w = 64, int h = 32)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float ny = (float)y / (h - 1);
            for (int x = 0; x < w; x++)
            {
                bool esBordeExt = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                bool esBordeInt = (x == 1 || x == w - 2 || y == 1 || y == h - 2);

                if (esBordeExt)
                {
                    if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1))
                        px[y * w + x] = Color.clear;
                    else
                        px[y * w + x] = bordeColor;
                }
                else if (esBordeInt)
                {
                    px[y * w + x] = Color.Lerp(bordeColor, baseColor, 0.45f);
                }
                else
                {
                    Color colGrad = Color.Lerp(baseColor * 0.78f, baseColor * 1.22f, ny);
                    if (y == h - 3) colGrad = Color.Lerp(colGrad, Color.white, 0.28f);
                    px[y * w + x] = colGrad;
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
    }

    public static Sprite GenerarSpritePanelColonial(int w = 64, int h = 64)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[w * h];
        Color bordeOroExt = new Color(0.92f, 0.74f, 0.28f, 0.95f);
        Color bordeOroInt = new Color(0.68f, 0.52f, 0.18f, 0.85f);
        Color fondoOscuro = new Color(0.06f, 0.05f, 0.08f, 0.94f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int distBorde = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                if (distBorde == 0)
                {
                    if ((x == 0 || x == w - 1) && (y == 0 || y == h - 1))
                        px[y * w + x] = Color.clear;
                    else
                        px[y * w + x] = bordeOroExt;
                }
                else if (distBorde == 1)
                {
                    px[y * w + x] = bordeOroExt;
                }
                else if (distBorde == 2)
                {
                    px[y * w + x] = new Color(0.03f, 0.02f, 0.04f, 0.95f);
                }
                else if (distBorde == 3)
                {
                    px[y * w + x] = bordeOroInt;
                }
                else
                {
                    px[y * w + x] = fondoOscuro;
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
    }

    public static Sprite GenerarSpriteOrbe(Color col, int size = 32)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[size * size];
        Vector2 centro = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radio = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro);
                if (d <= radio)
                {
                    float t = d / radio;
                    Color c = Color.Lerp(Color.white, col, t * t);
                    c.a = Mathf.Clamp01(1f - t * 0.85f);
                    px[y * size + x] = c;
                }
                else
                {
                    px[y * size + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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

    private GameObject CrearBotonConSprite(Transform parent, string nombre, string etiqueta, Vector2 pos, Vector2 size, Sprite spBoton, Font fuente, UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = new GameObject(nombre);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = spBoton;
        img.type = Image.Type.Sliced;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.18f, 1.15f, 1.05f, 1f);
        cb.pressedColor = new Color(0.82f, 0.80f, 0.75f, 1f);
        btn.colors = cb;

        btn.onClick.AddListener(ReproducirSonidoClick);
        btn.onClick.AddListener(accion);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Shadow sh = btnObj.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.65f);
        sh.effectDistance = new Vector2(2f, -2f);

        if (!string.IsNullOrEmpty(etiqueta))
        {
            Text txt = CrearTexto(btnObj.transform, "Texto", etiqueta, 17, Color.white, Vector2.zero, size, fuente);
            txt.fontStyle = FontStyle.Bold;
            Outline outTxt = txt.gameObject.AddComponent<Outline>();
            outTxt.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outTxt.effectDistance = new Vector2(1.2f, -1.2f);
        }

        return btnObj;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SÍNTESIS PROCEDURAL DE MÚSICA Y SFX
    // ─────────────────────────────────────────────────────────────────────────
    private AudioClip GenerarMusicaMenuProcedural()
    {
        int sampleRate = 44100;
        float duracion = 12.0f;
        int totalSamples = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[totalSamples];

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

            float env = Mathf.Exp(-2.5f * tNota);
            float m = (Mathf.Sin(2f * Mathf.PI * freqM * tNota) + 
                       0.35f * Mathf.Sin(4f * Mathf.PI * freqM * tNota) + 
                       0.12f * Mathf.Sin(6f * Mathf.PI * freqM * tNota)) * env;

            if (tNota > 0.22f)
            {
                float tEcho = tNota - 0.22f;
                float echoEnv = Mathf.Exp(-3.5f * tEcho);
                m += 0.25f * Mathf.Sin(2f * Mathf.PI * (freqM * 1.5f) * tEcho) * echoEnv;
            }

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
            EditorGUILayout.HelpBox("✅ Los elementos de la UI (Canvas, Fondo Panorámico, Marco Central, 5 Botones) están presentes en la Hierarchy.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠️ La UI no está en Hierarchy. Haz clic en el botón de abajo para generarla con el nuevo diseño colonial.", MessageType.Warning);
        }

        GUI.backgroundColor = new Color(0.2f, 0.85f, 1f);
        if (GUILayout.Button(tieneUI ? "🔄 Regenerar / Actualizar UI Colonial en Hierarchy" : "🎨 Generar UI Colonial en Hierarchy", GUILayout.Height(40)))
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
            GameObject canvas = GameObject.Find("Canvas_PantallaInicio");
            bool necesitaRegenerar = false;

            if (canvas != null)
            {
                Transform fondo = canvas.transform.Find("Fondo_SanFelipe_Atmosphere");
                Button[] btns = canvas.GetComponentsInChildren<Button>(true);
                bool tieneOpciones = false;
                foreach (var b in btns)
                {
                    if (b.gameObject.name == "Boton_Opciones") { tieneOpciones = true; break; }
                }

                if (fondo == null || !tieneOpciones)
                {
                    necesitaRegenerar = true;
                }
            }
            else
            {
                necesitaRegenerar = true;
            }

            if (necesitaRegenerar)
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
