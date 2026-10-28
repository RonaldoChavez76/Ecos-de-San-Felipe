using System;
using UnityEngine;

/// <summary>
/// Gestiona la zona activa actual (Parroquia Exterior, Parroquia Interior,
/// Mercado, Cerro del Fraile), sus límites de cámara y puntos de reaparición.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public enum ZoneId
    {
        ParroquiaExterior,
        ParroquiaInterior,
        Mercado,
        CerroDelFraile
    }

    [Header("Zona Activa")]
    public ZoneId currentZone = ZoneId.ParroquiaExterior;

    [Header("Límites de Cámara por Zona (Opcionales)")]
    public Collider2D limitesParroquiaExterior;
    public Collider2D limitesParroquiaInterior;
    public Collider2D limitesMercado;
    public Collider2D limitesCerroDelFraile;

    [Header("Spawn Points de Zonas")]
    public Transform spawnParroquiaExterior;
    public Transform spawnParroquiaInterior;
    public Transform spawnMercado;
    public Transform spawnCerroDelFraile;

    public event Action<ZoneId> OnZoneChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void CambiarZona(ZoneId nuevaZona, Vector2? puntoSpawnManual = null)
    {
        currentZone = nuevaZona;
        SaveStateManager.Instance?.GuardarCheckpoint(nuevaZona.ToString(), puntoSpawnManual ?? ObtenerSpawnPointPorDefecto(nuevaZona));

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
        {
            Vector2 destino = puntoSpawnManual ?? ObtenerSpawnPointPorDefecto(nuevaZona);
            player.transform.position = destino;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = destino;
        }

        AjustarLimitesCamara(nuevaZona);
        OnZoneChanged?.Invoke(nuevaZona);

        Debug.Log($"<color=#66FFCC>[LevelManager] Zona cambiada a: {nuevaZona}</color>");
    }

    public Vector2 ObtenerSpawnPointPorDefecto(ZoneId zona)
    {
        switch (zona)
        {
            case ZoneId.ParroquiaExterior:
                return spawnParroquiaExterior != null ? (Vector2)spawnParroquiaExterior.position : Vector2.zero;
            case ZoneId.ParroquiaInterior:
                return spawnParroquiaInterior != null ? (Vector2)spawnParroquiaInterior.position : new Vector2(0f, 60f);
            case ZoneId.Mercado:
                return spawnMercado != null ? (Vector2)spawnMercado.position : new Vector2(35f, 0f);
            case ZoneId.CerroDelFraile:
                return spawnCerroDelFraile != null ? (Vector2)spawnCerroDelFraile.position : new Vector2(0f, 40f);
            default:
                return Vector2.zero;
        }
    }

    private void AjustarLimitesCamara(ZoneId zona)
    {
        // Si hay una cámara con seguimiento suave o Cinemachine Confiner, actualizar los límites
        Collider2D col = null;
        switch (zona)
        {
            case ZoneId.ParroquiaExterior: col = limitesParroquiaExterior; break;
            case ZoneId.ParroquiaInterior: col = limitesParroquiaInterior; break;
            case ZoneId.Mercado: col = limitesMercado; break;
            case ZoneId.CerroDelFraile: col = limitesCerroDelFraile; break;
        }

        if (col != null)
        {
            // Opcional: configurar límites del script de cámara
        }
    }
}
