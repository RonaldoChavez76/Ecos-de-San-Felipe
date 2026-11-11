using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nodo individual de Sello de la Cruz Azul en la cima del Cerro del Fraile.
/// Puede ser purificado por Ronan [E / J] o corrompido por La Sombra Guardiana en Fase 2.
/// Los sellos corruptos emiten chispas y orbes espectrales sin alterar el movimiento de Ronan.
/// </summary>
public class BlueCrossSealNode : MonoBehaviour
{
    public string nombreSello = "Sello del Viento";
    public BlueCrossShrinePuzzle puzzlePadre;
    public SpriteRenderer flamaRenderer;
    public bool purificado = false;

    private SpriteRenderer baseRenderer;
    private Transform playerTransform;
    private ParticleSystem chispas;
    private readonly List<GameObject> orbesDefensivos = new List<GameObject>();
    private readonly List<float> nacimientoOrbes = new List<float>();
    private float siguientePulsoTiempo;
    private float siguienteOrbeTiempo;
    private bool jugadorEnRango;

    private void Start()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        CrearChispas();
        ActualizarVisual(purificado);
        siguientePulsoTiempo = Time.time + 4f;
        siguienteOrbeTiempo = Time.time + 4f;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerTransform = pm.transform;
        }

        if (purificado)
        {
            ActualizarOrbesDefensivos();
            return;
        }

        if (Time.time >= siguienteOrbeTiempo)
        {
            CrearOrbeDefensivo();
            siguienteOrbeTiempo = Time.time + 4f;
        }
        ActualizarOrbesDefensivos();

        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);

            if (dist <= 2.2f)
            {
                if (!jugadorEnRango)
                {
                    jugadorEnRango = true;
                    siguientePulsoTiempo = Time.time + 4f;
                }
                else if (Time.time >= siguientePulsoTiempo)
                {
                    siguientePulsoTiempo = Time.time + 4f;
                    PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
                    if (pm != null) pm.RecibirDanio(4f);
                }
            }
            else jugadorEnRango = false;

            // Interacción de purificación
            if (dist <= 2.2f)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
                {
                    Purificar();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (purificado) return;

        ProjectileBehaviour proj = other.GetComponent<ProjectileBehaviour>() ?? other.GetComponentInParent<ProjectileBehaviour>();
        PlayerProjectile playerProj = other.GetComponent<PlayerProjectile>();
        if ((proj != null && proj.IsDeflected) || playerProj != null)
        {
            Purificar();
        }
    }

    public void Purificar()
    {
        if (purificado) return;
        purificado = true;
        foreach (GameObject orb in orbesDefensivos)
        {
            if (orb != null) Destroy(orb);
        }
        orbesDefensivos.Clear();
        nacimientoOrbes.Clear();
        if (chispas != null) chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ActualizarVisual(true);
        puzzlePadre?.NotificarSelloPurificado(this);
    }

    public void Corromper()
    {
        if (puzzlePadre != null && puzzlePadre.acertijoResuelto) return;
        purificado = false;
        ActualizarVisual(false);
        siguientePulsoTiempo = Time.time + 4f;
        siguienteOrbeTiempo = Time.time + 4f;
        puzzlePadre?.NotificarSelloCorrompido(this);
    }

    private void ActualizarVisual(bool limpio)
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = limpio
                ? new Color(0.20f, 0.90f, 1.0f, 1f)
                : new Color(0.55f, 0.15f, 0.65f, 1f);
        }

        if (flamaRenderer != null)
        {
            flamaRenderer.color = limpio
                ? new Color(0f, 0.9f, 1f, 0.95f)
                : new Color(0.62f, 0.08f, 0.9f, 0.95f);
            flamaRenderer.transform.localScale = Vector3.one * (limpio ? 1.9f : 2.6f);
        }

        if (chispas != null)
        {
            var main = chispas.main;
            main.startColor = limpio ? new Color(0f, 0.9f, 1f, 0.9f) : new Color(0.7f, 0.15f, 1f, 0.9f);
            if (limpio) chispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else if (!chispas.isPlaying) chispas.Play();
        }
    }

    private void CrearChispas()
    {
        GameObject sparks = new GameObject("Chispas_Sello_Corrompido");
        sparks.transform.SetParent(transform, false);
        sparks.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        chispas = sparks.AddComponent<ParticleSystem>();
        var main = chispas.main;
        main.loop = true;
        main.startLifetime = 0.65f;
        main.startSpeed = 0.65f;
        main.startSize = 0.07f;
        main.startColor = new Color(0.7f, 0.15f, 1f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = chispas.emission;
        emission.rateOverTime = 6f;
        var shape = chispas.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;
    }

    private void CrearOrbeDefensivo()
    {
        GameObject orb = new GameObject("Orbe_Espectral_Defensivo");
        orb.transform.SetParent(transform, false);
        SpriteRenderer rendererOrb = orb.AddComponent<SpriteRenderer>();
        rendererOrb.sprite = ProjectileBehaviour.ObtenerSpriteOrbeAbisal();
        rendererOrb.color = new Color(0.65f, 0.2f, 1f, 0.9f);
        rendererOrb.sortingOrder = baseRenderer != null ? baseRenderer.sortingOrder + 5 : 10;
        orb.transform.localScale = Vector3.one * 0.24f;
        orbesDefensivos.Add(orb);
        nacimientoOrbes.Add(Time.time);
    }

    private void ActualizarOrbesDefensivos()
    {
        for (int i = orbesDefensivos.Count - 1; i >= 0; i--)
        {
            GameObject orb = orbesDefensivos[i];
            if (orb == null || purificado || Time.time - nacimientoOrbes[i] >= 3.8f)
            {
                if (orb != null) Destroy(orb);
                orbesDefensivos.RemoveAt(i);
                nacimientoOrbes.RemoveAt(i);
                continue;
            }

            float angulo = Time.time * 2.4f + i * Mathf.PI * 2f / Mathf.Max(1, orbesDefensivos.Count);
            orb.transform.localPosition = new Vector3(Mathf.Cos(angulo) * 0.72f, 0.7f + Mathf.Sin(angulo) * 0.3f, 0f);
        }
    }

    private void OnGUI()
    {
        if (purificado || playerTransform == null) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > 2.2f) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.25f);
        if (screenPos.z <= 0) return;

        float w = UITheme.Px(340f);
        float h = UITheme.Px(34f);
        Rect r = new Rect(screenPos.x - w * 0.5f, Screen.height - screenPos.y - h * 0.5f, w, h);
        Color bordeColor = new Color(0.85f, 0.25f, 1.0f, 0.95f);
        UITheme.Panel(r, bordeColor, 2f);
        GUIStyle lblStyle = UITheme.Estilo(12, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        lblStyle.wordWrap = false;
        GUI.Label(r, $"[E / J] Purificar {nombreSello}", lblStyle);
    }
}
