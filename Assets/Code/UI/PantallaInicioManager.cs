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
///  • 6 Botones estilizados: continuar desde checkpoint, iniciar nueva aventura con confirmación,
///    ajustes, controles y salida.
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
    private Button botonContinuar;
    private Coroutine rutinaHistoria;
    private CanvasGroup grupoHistoria;
    private Image overlayTransicion;
    private bool transicionEnCurso;

    void Awake()
    {
        VerificarYActualizarUI();
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
    }

    void Start()
    {
        Time.timeScale = 1f;
        SaveStateManager.EnsureInstance();

        CargarImagenesHistoriaDesdeResources();
        for (int i = 0; i < 4; i++)
        {
            if (cuatroImagenesHistoria[i] == null)
                slidesGeneradas[i] = GenerarIlustracionHistoria(i);
        }

        InicializarAudio();
        VerificarYActualizarUI();
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
        ActualizarEtiquetaDificultad();
        ActualizarDisponibilidadContinuar();
        ActualizarBotonSalirPlataforma();
        ActualizarTextoControlesSegunPlataforma();
        LimpiarGlifosAntiguosEnCanvas();
        MostrarMenuPrincipal();
    }

    private void LimpiarGlifosAntiguosEnCanvas()
    {
        if (panelMenuInicio == null) return;
        foreach (Text tx in panelMenuInicio.GetComponentsInChildren<Text>(true))
        {
            if (tx == null || string.IsNullOrEmpty(tx.text)) continue;
            string s = tx.text;
            if (s.Contains("⚔") || s.Contains("✦") || s.Contains("⚙") || s.Contains("📜") || s.Contains("✕"))
            {
                tx.text = s
                    .Replace("⚔", "★")
                    .Replace("✦", "★")
                    .Replace("⚙", "★")
                    .Replace("📜", "•")
                    .Replace("✕", "X");
            }
        }
    }

    private void ActualizarBotonSalirPlataforma()
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer || panelMenuInicio == null) return;
        Button[] btns = panelMenuInicio.GetComponentsInChildren<Button>(true);
        foreach (Button btn in btns)
        {
            if (btn != null && btn.gameObject.name == "Boton_Salir")
            {
                btn.gameObject.SetActive(false);
                break;
            }
        }
    }

    private void ActualizarTextoControlesSegunPlataforma()
    {
        if (panelControles == null || !MobileInput.IsTouchControlsActive) return;
        Text[] textos = panelControles.GetComponentsInChildren<Text>(true);
        foreach (Text t in textos)
        {
            if (t != null && t.gameObject.name == "Texto_Controles_Detallado")
            {
                t.text =
                    "<b>[JOYSTICK IZQUIERDO]</b> : Mover a Ronan en 8 direcciones\n" +
                    "<b>[ATACAR]</b> : Atacar con el arma equipada (Espada / Arco / Báculo)\n" +
                    "<b>[ESCUDO (MANTENER)]</b> : Bloqueo y <b>Contraataque (0.26s)</b>\n" +
                    "<b>[ESQUIVE]</b> : Rodar con invulnerabilidad temporal\n" +
                    "<b>[ASALTO]</b> : Desatar <b>Modo Asalto</b> al llegar al 100%\n" +
                    "<b>[ARMA / TARJETAS]</b> : Cambiar de arma rápidamente\n" +
                    "<b>[BOTÓN INTERACTUAR]</b> : Hablar, abrir cofres, trueque o purificar\n" +
                    "<b>[PAUSA / TIENDA]</b> : Opciones y Tienda en la esquina superior derecha";
                break;
            }
        }
    }

    private void CargarImagenesHistoriaDesdeResources()
    {
        string[] rutas = { "Historia/escena1", "Historia/escena2", "Historia/escena3", "Historia/escena4" };
        if (cuatroImagenesHistoria == null || cuatroImagenesHistoria.Length != rutas.Length)
            cuatroImagenesHistoria = new Texture2D[rutas.Length];

        for (int i = 0; i < rutas.Length; i++)
        {
            if (cuatroImagenesHistoria[i] != null) continue;

            cuatroImagenesHistoria[i] = Resources.Load<Texture2D>(rutas[i]);
            if (cuatroImagenesHistoria[i] == null)
                Debug.LogWarning($"[Ecos de San Felipe] No se encontró la imagen de historia en Resources/{rutas[i]}; se usará la ilustración generada.");
        }
    }

    void Update()
    {
        // Animar orbes de los Ecos en vivo
        AnimarEcosFlotantes();

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
        if (rutinaHistoria != null) StopCoroutine(rutinaHistoria);
        SaveStateManager.EnsureInstance().ReiniciarProgreso();
        MostrarHistoriaNuevaAventura();
        rutinaHistoria = StartCoroutine(RutinaHistoriaAutomatica());
    }

    public void ComenzarNuevaAventuraDirecta()
    {
        SaveStateManager.EnsureInstance().ReiniciarProgreso();
        CargarEscenaJuego();
    }

    private void MostrarHistoriaNuevaAventura()
    {
        if (panelMenuInicio != null) panelMenuInicio.SetActive(false);
        if (panelHistoriaCuatroImagenes != null) panelHistoriaCuatroImagenes.SetActive(true);
        if (panelHistoriaCuatroImagenes != null)
        {
            grupoHistoria = panelHistoriaCuatroImagenes.GetComponent<CanvasGroup>();
            if (grupoHistoria == null) grupoHistoria = panelHistoriaCuatroImagenes.AddComponent<CanvasGroup>();
            grupoHistoria.alpha = 0f;
        }
        indiceSlideActual = 0;
        ActualizarSlideHistoria();
    }

    private IEnumerator RutinaHistoriaAutomatica()
    {
        for (indiceSlideActual = 0; indiceSlideActual < 4; indiceSlideActual++)
        {
            ActualizarSlideHistoria();
            yield return StartCoroutine(AnimarAlphaHistoria(0f, 1f, 0.55f));

            while (escribiendoTexto) yield return null;
            yield return new WaitForSecondsRealtime(4.4f);

            if (indiceSlideActual < 3)
                yield return StartCoroutine(AnimarAlphaHistoria(1f, 0f, 0.35f));
        }

        yield return StartCoroutine(RutinaTransicionANivel());
        rutinaHistoria = null;
    }

    private IEnumerator AnimarAlphaHistoria(float desde, float hasta, float duracion)
    {
        if (grupoHistoria == null) yield break;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            grupoHistoria.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }
        grupoHistoria.alpha = hasta;
    }

    private IEnumerator RutinaTransicionANivel()
    {
        if (transicionEnCurso) yield break;
        transicionEnCurso = true;

        if (overlayTransicion == null)
        {
            Canvas canvas = panelHistoriaCuatroImagenes != null
                ? panelHistoriaCuatroImagenes.GetComponentInParent<Canvas>()
                : null;
            if (canvas != null)
            {
                GameObject overlay = new GameObject("Overlay_TransicionANivel");
                overlay.transform.SetParent(canvas.transform, false);
                overlayTransicion = overlay.AddComponent<Image>();
                overlayTransicion.color = Color.black;
                overlayTransicion.raycastTarget = false;
                RectTransform rect = overlay.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                overlay.transform.SetAsLastSibling();
            }
        }

        if (overlayTransicion != null)
        {
            Color color = Color.black;
            color.a = 0f;
            overlayTransicion.color = color;
            float tiempo = 0f;
            while (tiempo < 0.7f)
            {
                tiempo += Time.unscaledDeltaTime;
                color.a = Mathf.Clamp01(tiempo / 0.7f);
                overlayTransicion.color = color;
                yield return null;
            }
        }

        CargarEscenaJuego();
    }

    public void ContinuarPartida()
    {
        if (!SaveStateManager.EnsureInstance().RegresarAlUltimoCheckpoint())
        {
            Debug.LogWarning("[PantallaInicioManager] No se pudo continuar: no hay un checkpoint válido guardado.");
        }
    }

    public void SiguienteImagenHistoria()
    {
        OmitirHistoria();
    }

    public void OmitirHistoria()
    {
        if (rutinaHistoria != null)
        {
            StopCoroutine(rutinaHistoria);
            rutinaHistoria = null;
        }
        StartCoroutine(RutinaTransicionANivel());
    }

    public void CargarEscenaJuego()
    {
        Time.timeScale = 1f;
        if (!Application.CanStreamedLevelBeLoaded(nombreEscenaNivel1))
        {
            Debug.LogError($"[PantallaInicioManager] No se puede cargar la escena '{nombreEscenaNivel1}'. Verifica que esté incluida en Build Settings.");
            return;
        }

        Debug.Log("<color=cyan>[Ecos de San Felipe] Cargando nivel de juego: " + nombreEscenaNivel1 + "...</color>");
        SceneManager.LoadScene(nombreEscenaNivel1, LoadSceneMode.Single);
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
        txtPlacaDificultad.text = $"★  DIFICULTAD: <color=#{hexCol}><b>[ {difNombre} ]</b></color>  ★";
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
            txtIndicadorSlide.text = $"CRÓNICA {indiceSlideActual + 1} DE 4  •  REPRODUCCIÓN AUTOMÁTICA";
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
            bool tieneContinuar = false;
            foreach (var b in btns)
            {
                if (b.gameObject.name == "Boton_Opciones") tieneOpciones = true;
                if (b.gameObject.name == "Boton_Continuar") tieneContinuar = true;
            }

            if (fondo == null || !tieneOpciones || !tieneContinuar)
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
            AsignarListenerBoton(t, "Boton_Continuar", ContinuarPartida);
            AsignarListenerBoton(t, "Boton_IniciarAventura", ComenzarHistoriaCuatroImagenes);
            AsignarListenerBoton(t, "Boton_JugarDirecto", ComenzarNuevaAventuraDirecta);
            AsignarListenerBoton(t, "Boton_Opciones", AbrirMenuOpciones);
            AsignarListenerBoton(t, "Boton_Controles", AlternarPanelControles);
            AsignarListenerBoton(t, "Boton_Salir", SalirDelJuego);
            AsignarListenerBoton(t, "Placa_Dificultad", AbrirMenuOpciones);
            AsignarListenerBoton(t, "Boton_CerrarControles", () => { if (panelControles != null) panelControles.SetActive(false); });
        }

        if (panelHistoriaCuatroImagenes != null)
        {
            Transform t = panelHistoriaCuatroImagenes.transform;
            AsignarListenerBoton(t, "Boton_OmitirHistoria", OmitirHistoria);
            AsignarListenerBoton(t, "Boton_SiguienteImagen", OmitirHistoria);
            AsignarListenerBoton(t, "Marco_Imagen", OmitirHistoria);
        }
    }

    private void ActualizarDisponibilidadContinuar()
    {
        if (panelMenuInicio == null) return;
        if (botonContinuar == null)
        {
            Button[] botones = panelMenuInicio.GetComponentsInChildren<Button>(true);
            foreach (Button boton in botones)
            {
                if (boton.gameObject.name == "Boton_Continuar")
                {
                    botonContinuar = boton;
                    break;
                }
            }
        }

        if (botonContinuar == null) return;
        bool hayCheckpoint = SaveStateManager.EnsureInstance().HayCheckpointGuardado;
        botonContinuar.interactable = hayCheckpoint;
        Text etiqueta = botonContinuar.GetComponentInChildren<Text>();
        if (etiqueta != null)
        {
            etiqueta.text = hayCheckpoint ? "▶  CONTINUAR PARTIDA" : "▶  CONTINUAR — SIN PARTIDA";
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
            if (Application.isPlaying)
            {
                canvasViejo.SetActive(false);
                canvasViejo.name = "Canvas_PantallaInicio_Obsoleto";
                Destroy(canvasViejo);
            }
            else DestroyImmediate(canvasViejo);
        }

        // 2. EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(es, "Crear EventSystem");
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
        Text tSub = CrearTexto(marcoCentral.transform, "Subtitulo_SanFelipe", "Torres Mochas — San Felipe, Guanajuato", 18, new Color(0.55f, 0.95f, 1f), new Vector2(0, 198), new Vector2(490, 30), fuente);
        tSub.fontStyle = FontStyle.Italic;
        Outline outSub = tSub.gameObject.AddComponent<Outline>();
        outSub.effectColor = new Color(0.04f, 0.08f, 0.14f, 0.95f);
        outSub.effectDistance = new Vector2(1.5f, -1.5f);

        // DIVISOR DORADO
        CrearPanel(marcoCentral.transform, "Divisor_Dorado", new Color(0.92f, 0.74f, 0.28f, 0.65f), new Vector2(0, 172), new Vector2(380, 2));

        // PLACA DE DIFICULTAD INTERACTIVA
        GameObject placaDifObj = CrearBotonConSprite(marcoCentral.transform, "Placa_Dificultad", "", new Vector2(0, 142), new Vector2(390, 36), spBotonDorado, fuente, AbrirMenuOpciones);
        txtPlacaDificultad = CrearTexto(placaDifObj.transform, "Texto_Dificultad", "★  DIFICULTAD: [ NORMAL ]  ★", 16, Color.white, Vector2.zero, new Vector2(380, 34), fuente);
        txtPlacaDificultad.fontStyle = FontStyle.Bold;
        Outline outDif = txtPlacaDificultad.gameObject.AddComponent<Outline>();
        outDif.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f);
        outDif.effectDistance = new Vector2(1.2f, -1.2f);

        // LOS 6 BOTONES ESTILIZADOS
        CrearBotonConSprite(marcoCentral.transform, "Boton_Continuar", "▶  CONTINUAR PARTIDA", new Vector2(0, 96), new Vector2(400, 42), spBotonVerde, fuente, ContinuarPartida);
        CrearBotonConSprite(marcoCentral.transform, "Boton_IniciarAventura", "★  NUEVA AVENTURA (HISTORIA)", new Vector2(0, 49), new Vector2(400, 42), spBotonAzul, fuente, ComenzarHistoriaCuatroImagenes);
        CrearBotonConSprite(marcoCentral.transform, "Boton_JugarDirecto", "▶  NUEVA AVENTURA — NIVEL 1", new Vector2(0, 2), new Vector2(400, 42), spBotonVerde, fuente, ComenzarNuevaAventuraDirecta);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Opciones", "★  AJUSTES Y OPCIONES", new Vector2(0, -45), new Vector2(400, 42), spBotonDorado, fuente, AbrirMenuOpciones);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Controles", "•  MAPA DE CONTROLES", new Vector2(0, -92), new Vector2(400, 42), spBotonMarron, fuente, AlternarPanelControles);
        CrearBotonConSprite(marcoCentral.transform, "Boton_Salir", "X  SALIR DEL JUEGO", new Vector2(0, -139), new Vector2(400, 42), spBotonRojo, fuente, SalirDelJuego);

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

        Text tCtrlBody = CrearTexto(panelControles.transform, "Texto_Controles_Detallado", controlesDetalle, 17, new Color(0.95f, 0.93f, 0.88f), new Vector2(0, 35), new Vector2(590, 268), fuente);
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

        // Marco Imagen (tocable en móvil para omitir/avanzar intro)
        GameObject marcoImg = new GameObject("Marco_Imagen");
        marcoImg.transform.SetParent(panelHistoriaCuatroImagenes.transform, false);
        Image imgMarcoHist = marcoImg.AddComponent<Image>();
        imgMarcoHist.sprite = spPanel;
        imgMarcoHist.type = Image.Type.Sliced;
        Button btnMarcoHist = marcoImg.AddComponent<Button>();
        btnMarcoHist.transition = Selectable.Transition.None;
        RectTransform rtMarcoImg = marcoImg.GetComponent<RectTransform>();
        rtMarcoImg.sizeDelta = new Vector2(980f, 560f);
        rtMarcoImg.anchoredPosition = new Vector2(0f, 40f);

        GameObject objRaw = new GameObject("Imagen_Historia_Actual");
        objRaw.transform.SetParent(marcoImg.transform, false);
        imgSlideActual = objRaw.AddComponent<RawImage>();
        RectTransform rtImgRaw = objRaw.GetComponent<RectTransform>();
        rtImgRaw.sizeDelta = new Vector2(960f, 540f);
        rtImgRaw.anchoredPosition = Vector2.zero;

        CrearTexto(panelHistoriaCuatroImagenes.transform, "Titulo_Cronica",
            "LA CRÓNICA DE SAN FELIPE", 23, UITheme.Dorado, new Vector2(0f, 330f), new Vector2(900f, 36f), fuente);

        // Caja Diálogo
        GameObject cajaTextoIntro = new GameObject("Caja_Dialogo_Intro");
        cajaTextoIntro.transform.SetParent(panelHistoriaCuatroImagenes.transform, false);
        Image imgCaja = cajaTextoIntro.AddComponent<Image>();
        imgCaja.sprite = spPanel;
        imgCaja.type = Image.Type.Sliced;
        RectTransform rtCaja = cajaTextoIntro.GetComponent<RectTransform>();
        rtCaja.sizeDelta = new Vector2(1040f, 132f);
        rtCaja.anchoredPosition = new Vector2(0f, -265f);

        txtSlideDialogo = CrearTexto(cajaTextoIntro.transform, "Texto_Dialogo", "", 21, new Color(0.96f, 0.94f, 0.90f), new Vector2(0, 0), new Vector2(980, 114), fuente);
        txtSlideDialogo.alignment = TextAnchor.MiddleLeft;

        txtIndicadorSlide = CrearTexto(panelHistoriaCuatroImagenes.transform, "Texto_Indicador_Slide", "Crónica 1 de 4", 15, new Color(0.55f, 0.95f, 1f), new Vector2(-250, -337), new Vector2(600, 30), fuente);

        CrearBotonConSprite(panelHistoriaCuatroImagenes.transform, "Boton_OmitirHistoria", "OMITIR INTRO >>", new Vector2(470, -337), new Vector2(230, 38), spBotonMarron, fuente, OmitirHistoria);

        panelHistoriaCuatroImagenes.SetActive(false);

        // Vincular referencias y listeners
        BuscarOConectarReferenciasExistentes();
        VincularBotonesYListeners();
        ActualizarEtiquetaDificultad();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Undo.RegisterCreatedObjectUndo(canvasObj, "Generar UI Pantalla Inicio Colonial");
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
        Debug.Log("<color=cyan>[Ecos de San Felipe] ¡Nueva UI Colonial Sacra generada exitosamente en la Hierarchy!</color>");
    }

    [ContextMenu("Limpiar UI de la Escena")]
    public void LimpiarUIEnEscena()
    {
        GameObject canvasObj = GameObject.Find("Canvas_PantallaInicio");
        if (canvasObj != null)
        {
            if (Application.isPlaying) Destroy(canvasObj);
            else DestroyImmediate(canvasObj);
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
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
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
        const int w = 320;
        const int h = 180;
        indice = Mathf.Clamp(indice, 0, 3);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[w * h];

        Color cieloArriba = indice == 0 ? new Color(0.10f, 0.11f, 0.27f) :
            indice == 1 ? new Color(0.035f, 0.055f, 0.16f) :
            indice == 2 ? new Color(0.075f, 0.025f, 0.11f) :
            new Color(0.09f, 0.24f, 0.36f);
        Color horizonte = indice == 0 ? new Color(0.83f, 0.34f, 0.20f) :
            indice == 1 ? new Color(0.12f, 0.16f, 0.34f) :
            indice == 2 ? new Color(0.30f, 0.055f, 0.12f) :
            new Color(0.97f, 0.60f, 0.29f);
        Color suelo = indice == 0 ? new Color(0.13f, 0.085f, 0.10f) :
            indice == 1 ? new Color(0.045f, 0.065f, 0.13f) :
            indice == 2 ? new Color(0.055f, 0.025f, 0.065f) :
            new Color(0.10f, 0.16f, 0.19f);

        for (int y = 0; y < h; y++)
        {
            float t = Mathf.Clamp01(y / 150f);
            Color row = Color.Lerp(horizonte, cieloArriba, t);
            if (y < 29) row = Color.Lerp(new Color(0.045f, 0.04f, 0.07f), suelo, y / 29f);
            for (int x = 0; x < w; x++)
            {
                int hash = (x * 73856093) ^ (y * 19349663) ^ (indice * 83492791);
                Color pixel = row;
                if (y > 86 && (hash & 255) < 14)
                {
                    float twinkle = 0.45f + ((hash >> 8) & 127) / 220f;
                    pixel = Color.Lerp(pixel, new Color(0.82f, 0.88f, 1f), twinkle);
                }
                pixels[y * w + x] = pixel;
            }
        }

        PintarMontanas(pixels, w, h, new Color(0.065f, 0.075f, 0.16f), indice * 17);
        PintarCirculo(pixels, w, h, indice == 0 ? 255 : 45, 136, 18,
            indice == 0 ? new Color(1f, 0.74f, 0.38f) : new Color(0.75f, 0.83f, 1f));
        PintarPueblo(pixels, w, h, indice == 2 ? new Color(0.035f, 0.022f, 0.045f) : new Color(0.13f, 0.095f, 0.12f));
        DibujarIglesia(pixels, w, h, indice == 2 ? new Color(0.09f, 0.045f, 0.07f) : new Color(0.43f, 0.31f, 0.28f));

        if (indice == 0)
        {
            PintarAura(pixels, w, h, 160, 82, 98, new Color(0.95f, 0.22f, 0.20f), 0.25f);
            for (int i = 0; i < 4; i++)
                PintarRect(pixels, w, h, 0, 39 + i * 10, w, 44 + i * 10, new Color(0.48f, 0.36f, 0.48f, 0.55f));
            PintarRect(pixels, w, h, 28, 27, 74, 30, new Color(0.82f, 0.43f, 0.22f));
        }
        else if (indice == 1)
        {
            Color[] ecos =
            {
                new Color(0.25f, 0.95f, 1f), new Color(1f, 0.78f, 0.28f),
                new Color(0.78f, 0.49f, 1f), new Color(0.35f, 1f, 0.66f),
                new Color(1f, 0.35f, 0.42f)
            };
            Vector2[] posiciones = { new Vector2(48, 132), new Vector2(105, 153), new Vector2(161, 119), new Vector2(221, 150), new Vector2(278, 126) };
            for (int i = 0; i < posiciones.Length; i++)
            {
                Vector2 punto = posiciones[i];
                PintarLinea(pixels, w, h, 160, 84, (int)punto.x, (int)punto.y, ecos[i] * 0.72f);
                PintarAura(pixels, w, h, (int)punto.x, (int)punto.y, 22, ecos[i], 0.85f);
                PintarCirculo(pixels, w, h, (int)punto.x, (int)punto.y, 5, Color.white);
                PintarCirculo(pixels, w, h, (int)punto.x, (int)punto.y, 2, ecos[i]);
            }
        }
        else if (indice == 2)
        {
            DibujarSombra(pixels, w, h, 58, 28, 0.9f);
            DibujarSombra(pixels, w, h, 260, 25, 1.15f);
            DibujarSombra(pixels, w, h, 164, 24, 1.7f);
            for (int i = 0; i < 3; i++)
                PintarRect(pixels, w, h, 0, 48 + i * 15, w, 52 + i * 15, new Color(0.22f, 0.08f, 0.16f, 0.55f));
        }
        else
        {
            PintarAura(pixels, w, h, 228, 107, 68, new Color(0.20f, 0.76f, 1f), 0.82f);
            DibujarRonan(pixels, w, h, 130, 28);
            DibujarCruz(pixels, w, h, 232, 93);
            DibujarEco(pixels, w, h, 223, 128, new Color(0.35f, 0.94f, 1f));
            DibujarEco(pixels, w, h, 258, 111, new Color(1f, 0.78f, 0.30f));
            DibujarEco(pixels, w, h, 271, 143, new Color(0.73f, 0.55f, 1f));
            PintarLinea(pixels, w, h, 151, 70, 193, 103, new Color(1f, 0.78f, 0.32f));
            PintarAura(pixels, w, h, 154, 67, 24, new Color(1f, 0.71f, 0.2f), 0.55f);
        }

        PintarRect(pixels, w, h, 0, 0, w, 3, new Color(0.88f, 0.69f, 0.29f));
        PintarRect(pixels, w, h, 0, h - 3, w, h, new Color(0.88f, 0.69f, 0.29f));
        tex.SetPixels(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static void PintarPixel(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            int index = y * width + x;
            pixels[index] = color.a >= 1f ? color : Color.Lerp(pixels[index], color, color.a);
        }
    }

    private static void PintarRect(Color[] pixels, int width, int height, int xMin, int yMin, int xMax, int yMax, Color color)
    {
        int left = Mathf.Clamp(xMin, 0, width);
        int right = Mathf.Clamp(xMax, 0, width);
        int bottom = Mathf.Clamp(yMin, 0, height);
        int top = Mathf.Clamp(yMax, 0, height);
        for (int y = bottom; y < top; y++)
        {
            int row = y * width;
            for (int x = left; x < right; x++)
            {
                int index = row + x;
                pixels[index] = color.a >= 1f ? color : Color.Lerp(pixels[index], color, color.a);
            }
        }
    }

    private static void PintarCirculo(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
    {
        int radiusSq = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= radiusSq) PintarPixel(pixels, width, height, x, y, color);
            }
        }
    }

    private static void PintarAura(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color, float intensidad)
    {
        for (int y = Mathf.Max(0, cy - radius); y < Mathf.Min(height, cy + radius); y++)
        {
            for (int x = Mathf.Max(0, cx - radius); x < Mathf.Min(width, cx + radius); x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) / radius;
                if (distancia >= 1f) continue;
                Color resplandor = color;
                resplandor.a = Mathf.Pow(1f - distancia, 2f) * intensidad;
                PintarPixel(pixels, width, height, x, y, resplandor);
            }
        }
    }

    private static void PintarLinea(Color[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        while (true)
        {
            PintarRect(pixels, width, height, x0 - 1, y0 - 1, x0 + 2, y0 + 2, color);
            if (x0 == x1 && y0 == y1) break;
            int dobleError = 2 * error;
            if (dobleError >= dy) { error += dy; x0 += sx; }
            if (dobleError <= dx) { error += dx; y0 += sy; }
        }
    }

    private static void PintarMontanas(Color[] pixels, int width, int height, Color color, int seed)
    {
        for (int x = 0; x < width; x++)
        {
            float perfil = Mathf.Sin((x + seed) * 0.027f) * 12f + Mathf.Cos((x - seed) * 0.061f) * 7f;
            int cima = 51 + Mathf.RoundToInt(perfil);
            PintarRect(pixels, width, height, x, 29, x + 1, cima, color);
        }
    }

    private static void PintarPueblo(Color[] pixels, int width, int height, Color color)
    {
        PintarRect(pixels, width, height, 0, 28, width, 35, new Color(0.20f, 0.13f, 0.15f));
        int[] posiciones = { 12, 49, 88, 222, 270, 294 };
        int[] alturas = { 22, 33, 25, 30, 23, 31 };
        for (int i = 0; i < posiciones.Length; i++)
        {
            int x = posiciones[i];
            int altura = alturas[i];
            PintarRect(pixels, width, height, x, 34, x + 28, 34 + altura, color);
            PintarLinea(pixels, width, height, x - 2, 34 + altura, x + 14, 42 + altura, color * 0.8f);
            PintarLinea(pixels, width, height, x + 14, 42 + altura, x + 30, 34 + altura, color * 0.8f);
            for (int wy = 40; wy < 34 + altura - 3; wy += 10)
            {
                PintarRect(pixels, width, height, x + 5, wy, x + 9, wy + 5, new Color(1f, 0.65f, 0.27f, 0.65f));
                PintarRect(pixels, width, height, x + 18, wy, x + 22, wy + 5, new Color(1f, 0.65f, 0.27f, 0.55f));
            }
        }
    }

    private static void DibujarIglesia(Color[] pixels, int width, int height, Color cantera)
    {
        Color sombra = new Color(0.07f, 0.06f, 0.10f);
        Color detalle = new Color(0.78f, 0.57f, 0.36f);
        PintarRect(pixels, width, height, 117, 33, 203, 87, cantera);
        PintarRect(pixels, width, height, 105, 34, 137, 126, cantera * 0.90f);
        PintarRect(pixels, width, height, 183, 34, 215, 126, cantera * 0.90f);
        PintarLinea(pixels, width, height, 101, 125, 121, 145, detalle);
        PintarLinea(pixels, width, height, 121, 145, 141, 125, detalle);
        PintarLinea(pixels, width, height, 179, 125, 199, 145, detalle);
        PintarLinea(pixels, width, height, 199, 145, 219, 125, detalle);
        PintarRect(pixels, width, height, 113, 126, 129, 137, detalle);
        PintarRect(pixels, width, height, 191, 126, 207, 137, detalle);
        PintarRect(pixels, width, height, 155, 87, 165, 104, detalle);
        PintarRect(pixels, width, height, 151, 99, 169, 102, detalle);
        PintarRect(pixels, width, height, 158, 95, 162, 108, detalle);
        PintarRect(pixels, width, height, 140, 39, 180, 91, cantera * 1.06f);
        PintarLinea(pixels, width, height, 134, 91, 160, 111, detalle);
        PintarLinea(pixels, width, height, 160, 111, 186, 91, detalle);
        PintarRect(pixels, width, height, 151, 34, 169, 63, sombra);
        PintarRect(pixels, width, height, 108, 96, 134, 99, sombra);
        PintarRect(pixels, width, height, 186, 96, 212, 99, sombra);
        PintarRect(pixels, width, height, 114, 60, 119, 70, new Color(1f, 0.72f, 0.3f, 0.78f));
        PintarRect(pixels, width, height, 201, 60, 206, 70, new Color(1f, 0.72f, 0.3f, 0.78f));
        PintarRect(pixels, width, height, 154, 76, 166, 80, detalle);
    }

    private static void DibujarSombra(Color[] pixels, int width, int height, int x, int baseY, float escala)
    {
        int cabeza = Mathf.RoundToInt(10f * escala);
        int torsoAncho = Mathf.RoundToInt(18f * escala);
        int torsoAlto = Mathf.RoundToInt(32f * escala);
        Color negro = new Color(0.018f, 0.012f, 0.035f);
        PintarAura(pixels, width, height, x, baseY + torsoAlto + cabeza, cabeza * 2, new Color(0.9f, 0.025f, 0.16f), 0.65f);
        PintarCirculo(pixels, width, height, x, baseY + torsoAlto + cabeza, cabeza, negro);
        PintarRect(pixels, width, height, x - torsoAncho, baseY, x + torsoAncho, baseY + torsoAlto, negro);
        PintarLinea(pixels, width, height, x - torsoAncho + 2, baseY + torsoAlto - 2, x - torsoAncho - 10, baseY + torsoAlto - 18, negro);
        PintarLinea(pixels, width, height, x + torsoAncho - 2, baseY + torsoAlto - 2, x + torsoAncho + 10, baseY + torsoAlto - 17, negro);
        PintarRect(pixels, width, height, x - 6, baseY + torsoAlto + cabeza - 2, x - 2, baseY + torsoAlto + cabeza + 1, new Color(1f, 0.06f, 0.18f));
        PintarRect(pixels, width, height, x + 2, baseY + torsoAlto + cabeza - 2, x + 6, baseY + torsoAlto + cabeza + 1, new Color(1f, 0.06f, 0.18f));
    }

    private static void DibujarRonan(Color[] pixels, int width, int height, int x, int baseY)
    {
        Color capa = new Color(0.18f, 0.34f, 0.49f);
        Color tunica = new Color(0.25f, 0.57f, 0.70f);
        PintarAura(pixels, width, height, x, baseY + 53, 34, new Color(0.24f, 0.78f, 1f), 0.42f);
        PintarRect(pixels, width, height, x - 15, baseY + 5, x + 14, baseY + 31, capa);
        PintarRect(pixels, width, height, x - 10, baseY + 20, x + 10, baseY + 42, tunica);
        PintarRect(pixels, width, height, x - 9, baseY + 1, x - 2, baseY + 12, new Color(0.18f, 0.22f, 0.31f));
        PintarRect(pixels, width, height, x + 3, baseY + 1, x + 10, baseY + 12, new Color(0.18f, 0.22f, 0.31f));
        PintarRect(pixels, width, height, x - 9, baseY + 43, x + 9, baseY + 59, new Color(0.72f, 0.44f, 0.28f));
        PintarRect(pixels, width, height, x - 11, baseY + 55, x + 11, baseY + 62, new Color(0.11f, 0.085f, 0.075f));
        PintarLinea(pixels, width, height, x - 12, baseY + 31, x - 22, baseY + 43, capa);
        PintarLinea(pixels, width, height, x + 12, baseY + 32, x + 25, baseY + 45, tunica);
        PintarRect(pixels, width, height, x + 21, baseY + 42, x + 28, baseY + 49, new Color(0.23f, 0.92f, 1f));
        PintarLinea(pixels, width, height, x - 2, baseY + 30, x - 20, baseY + 49, new Color(0.82f, 0.88f, 0.94f));
        PintarRect(pixels, width, height, x - 2, baseY + 29, x + 2, baseY + 34, new Color(0.94f, 0.77f, 0.35f));
        PintarRect(pixels, width, height, x - 4, baseY + 50, x + 5, baseY + 52, new Color(0.13f, 0.10f, 0.10f));
    }

    private static void DibujarEco(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        PintarAura(pixels, width, height, x, y, 15, color, 0.85f);
        PintarCirculo(pixels, width, height, x, y, 6, color);
        PintarCirculo(pixels, width, height, x, y, 3, Color.white);
    }

    private static void DibujarCruz(Color[] pixels, int width, int height, int x, int baseY)
    {
        Color azul = new Color(0.14f, 0.69f, 0.96f);
        PintarRect(pixels, width, height, x - 3, baseY, x + 4, baseY + 58, azul);
        PintarRect(pixels, width, height, x - 18, baseY + 40, x + 19, baseY + 47, azul);
        PintarRect(pixels, width, height, x - 7, baseY - 5, x + 8, baseY, new Color(0.59f, 0.39f, 0.22f));
        PintarAura(pixels, width, height, x, baseY + 44, 13, azul, 0.75f);
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
            Text txt = CrearTexto(btnObj.transform, "Texto", etiqueta, 19, Color.white, Vector2.zero, size, fuente);
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
