using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tema visual compartido para toda la interfaz (IMGUI): tamaños de fuente escalados a la resolución,
/// colores de alto contraste con transparencia (Alpha 150-180 aprox.) para no bloquear la vista del mapa,
/// y texto con sombra/contorno para que siga siendo legible sobre cualquier fondo.
/// </summary>
public static class UITheme
{
    // ---- Paleta de colores con transparencia equilibrada (Alpha 165/255 ≈ 0.65f) ----
    public static readonly Color TextoPrincipal = new Color(1f, 1f, 1f, 1f);
    public static readonly Color TextoSecundario = new Color(0.86f, 0.90f, 0.96f, 1f);
    public static readonly Color Dorado = new Color(1f, 0.88f, 0.42f, 1f);
    public static readonly Color Turquesa = new Color(0.55f, 0.97f, 1f, 1f);
    public static readonly Color Verde = new Color(0.60f, 1f, 0.68f, 1f);
    public static readonly Color Alerta = new Color(1f, 0.45f, 0.40f, 1f);

    // Fondo oscuro translúcido: canal Alpha = 165 aprox. (entre 150 y 180) para ver el terreno a través
    public static readonly Color FondoPanel = new Color(0.04f, 0.05f, 0.09f, 0.65f);
    public static readonly Color BordeDorado = new Color(0.90f, 0.72f, 0.30f, 0.75f);

    private static Texture2D blanca;
    private static readonly Dictionary<string, GUIStyle> cache = new Dictionary<string, GUIStyle>();

    public static Texture2D Blanca
    {
        get
        {
            if (blanca == null)
            {
                blanca = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                blanca.SetPixel(0, 0, Color.white);
                blanca.Apply();
                blanca.hideFlags = HideFlags.HideAndDontSave;
            }
            return blanca;
        }
    }

    /// <summary>Factor de escala según la altura de la pantalla (720p = 1.0).</summary>
    public static float Escala => Mathf.Clamp(Screen.height / 720f, 1f, 2.5f);

    /// <summary>Tamaño de fuente escalado (mínimo 10 px para admitir etiquetas compactas y legibles).</summary>
    public static int Fuente(int basePx) => Mathf.Max(10, Mathf.RoundToInt(basePx * Escala));

    public static float Px(float basePx) => basePx * Escala;

    public static GUIStyle Estilo(int basePx, FontStyle fontStyle = FontStyle.Bold, TextAnchor anchor = TextAnchor.MiddleLeft,
                                  Color? color = null, bool wordWrap = false)
    {
        Color c = color ?? TextoPrincipal;
        int size = Fuente(basePx);
        string key = $"{size}|{(int)fontStyle}|{(int)anchor}|{ColorUtility.ToHtmlStringRGBA(c)}|{wordWrap}";
        if (cache.TryGetValue(key, out GUIStyle s) && s != null) return s;

        s = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = fontStyle,
            alignment = anchor,
            wordWrap = wordWrap,
            richText = true,
            clipping = TextClipping.Overflow,
            padding = new RectOffset(0, 0, 0, 0)
        };
        s.normal.textColor = c;
        cache[key] = s;
        return s;
    }

    /// <summary>Texto con contorno oscuro sutil: legible sobre cualquier fondo translúcido o mapa.</summary>
    public static void EtiquetaConSombra(Rect r, string texto, GUIStyle estilo)
    {
        Color previo = GUI.color;
        Color colorOriginal = estilo.normal.textColor;
        float o = Mathf.Max(1f, Escala);

        estilo.normal.textColor = new Color(0f, 0f, 0f, 0.90f);
        GUI.Label(new Rect(r.x - o, r.y, r.width, r.height), texto, estilo);
        GUI.Label(new Rect(r.x + o, r.y, r.width, r.height), texto, estilo);
        GUI.Label(new Rect(r.x, r.y - o, r.width, r.height), texto, estilo);
        GUI.Label(new Rect(r.x, r.y + o, r.width, r.height), texto, estilo);

        estilo.normal.textColor = colorOriginal;
        GUI.Label(r, texto, estilo);
        GUI.color = previo;
    }

    /// <summary>Panel translúcido compacto con borde elegante de color.</summary>
    public static void Panel(Rect r, Color? borde = null, float grosor = 1.5f)
    {
        Color previo = GUI.color;
        GUI.color = FondoPanel;
        GUI.DrawTexture(r, Blanca);

        GUI.color = borde ?? BordeDorado;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, grosor), Blanca);
        GUI.DrawTexture(new Rect(r.x, r.yMax - grosor, r.width, grosor), Blanca);
        GUI.DrawTexture(new Rect(r.x, r.y, grosor, r.height), Blanca);
        GUI.DrawTexture(new Rect(r.xMax - grosor, r.y, grosor, r.height), Blanca);
        GUI.color = previo;
    }

    /// <summary>Dibuja únicamente el marco o contorno de un rectángulo con el grosor especificado.</summary>
    public static void Borde(Rect r, Color color, float grosor = 1.5f)
    {
        Color previo = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, grosor), Blanca);
        GUI.DrawTexture(new Rect(r.x, r.yMax - grosor, r.width, grosor), Blanca);
        GUI.DrawTexture(new Rect(r.x, r.y, grosor, r.height), Blanca);
        GUI.DrawTexture(new Rect(r.xMax - grosor, r.y, grosor, r.height), Blanca);
        GUI.color = previo;
    }

    /// <summary>Banner de una línea centrado horizontalmente (avisos de zona, puertas, hordas).</summary>
    public static void Banner(float y, string texto, Color colorTexto, float anchoBase = 620f, int fuenteBase = 16)
    {
        float w = Mathf.Min(Px(anchoBase), Screen.width - 24f);
        float h = Px(fuenteBase + 16f);
        Rect r = new Rect((Screen.width - w) * 0.5f, y, w, h);
        Panel(r, colorTexto, 1.5f);
        GUI.Label(r, texto, Estilo(fuenteBase, FontStyle.Bold, TextAnchor.MiddleCenter, colorTexto));
    }

    /// <summary>Estilo de botón grande y legible.</summary>
    public static GUIStyle Boton(int basePx = 16)
    {
        GUIStyle b = new GUIStyle(GUI.skin.button)
        {
            fontSize = Fuente(basePx),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        b.normal.textColor = TextoPrincipal;
        b.hover.textColor = Dorado;
        b.active.textColor = Dorado;
        return b;
    }
}
