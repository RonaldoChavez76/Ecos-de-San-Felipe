using UnityEngine;

/// <summary>
/// Sistema central de audio para Ecos de San Felipe.
/// Administra música dinámica y los 8 efectos sonoros esenciales del juego
/// con sintetizador procedural de respaldo para garantizar sonido 100% funcional.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Canales de Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips de Música (Opcionales)")]
    public AudioClip explorationMusic;
    public AudioClip combatMusic;
    public AudioClip bossMusic;

    [Header("Clips de SFX (Opcionales, se autogeneran si están vacíos)")]
    public AudioClip sfxSlash;
    public AudioClip sfxCounter;
    public AudioClip sfxEcho;
    public AudioClip sfxDodge;
    public AudioClip sfxHit;
    public AudioClip sfxAssault;
    public AudioClip sfxBell;
    public AudioClip sfxSecret;
    public AudioClip sfxVictoryFanfare;

    [Header("Volúmenes Maestros")]
    public float masterVolume = 1.0f;
    public float musicVolume = 0.5f;
    public float sfxVolume = 0.7f;

    private const string PREF_VOL_MASTER = "VolMaster";
    private const string PREF_VOL_MUSIC = "VolMusic";
    private const string PREF_VOL_SFX = "VolSFX";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CargarAjustesVolumen();
        AsegurarAudioSources();
        GenerarClipsProceduralesSiFaltan();
    }

    public void CargarAjustesVolumen()
    {
        masterVolume = PlayerPrefs.GetFloat(PREF_VOL_MASTER, 1.0f);
        musicVolume = PlayerPrefs.GetFloat(PREF_VOL_MUSIC, 0.75f);
        sfxVolume = PlayerPrefs.GetFloat(PREF_VOL_SFX, 0.85f);
        ActualizarVolumenes();
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_VOL_MASTER, masterVolume);
        ActualizarVolumenes();
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_VOL_MUSIC, musicVolume);
        ActualizarVolumenes();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat(PREF_VOL_SFX, sfxVolume);
        ActualizarVolumenes();
    }

    private void ActualizarVolumenes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    private void Start()
    {
        if (explorationMusic != null && musicSource != null && !musicSource.isPlaying)
        {
            PlayMusic(explorationMusic);
        }
    }

    public void PlayMusic(AudioClip clip, float volume = 0.5f)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = volume * musicVolume * masterVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 0.7f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }
    }

    public void PlaySlashSound() => PlaySFX(sfxSlash, 0.6f);
    public void PlayCounterSound() => PlaySFX(sfxCounter, 0.95f);
    public void PlayEchoSound() => PlaySFX(sfxEcho, 0.85f);
    public void PlayDodgeSound() => PlaySFX(sfxDodge, 0.5f);
    public void PlayHitSound() => PlaySFX(sfxHit, 0.7f);
    public void PlayAssaultSound() => PlaySFX(sfxAssault, 0.8f);
    public void PlayBellSound() => PlaySFX(sfxBell, 0.9f);
    public void PlaySecretSound() => PlaySFX(sfxSecret, 0.85f);
    public void PlayItemPickupSound() => PlaySFX(sfxSecret, 0.85f);
    public void PlayVictoryFanfare() => PlaySFX(sfxVictoryFanfare ?? sfxSecret, 1.0f);
    public void PlayButtonHover()   => PlaySFX(sfxDodge, 0.35f); // sonido suave de hover UI

    private void AsegurarAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    private void GenerarClipsProceduralesSiFaltan()
    {
        if (sfxSlash == null) sfxSlash = SintetizarSonido("SFX_Slash", 0.08f, 700f, 200f, 0.5f);
        if (sfxCounter == null) sfxCounter = SintetizarAcordeCounter("SFX_Counter");
        if (sfxEcho == null) sfxEcho = SintetizarCampanaMagica("SFX_Echo", 0.8f, 880f);
        if (sfxDodge == null) sfxDodge = SintetizarSonido("SFX_Dodge", 0.12f, 300f, 120f, 0.4f);
        if (sfxHit == null) sfxHit = SintetizarSonido("SFX_Hit", 0.14f, 220f, 60f, 0.7f);
        if (sfxAssault == null) sfxAssault = SintetizarAssaultPowerUp("SFX_Assault");
        if (sfxBell == null) sfxBell = SintetizarCampanaMagica("SFX_Bell", 1.8f, 330f);
        if (sfxSecret == null) sfxSecret = SintetizarSecuenciaZelda("SFX_Secret");
        if (sfxVictoryFanfare == null) sfxVictoryFanfare = SintetizarFanfarriaVictoria("SFX_VictoryFanfare");
    }

    private AudioClip SintetizarFanfarriaVictoria(string nombre)
    {
        int sampleRate = 44100;
        float duracion = 1.4f;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        // Notas de fanfarria triunfal en C Mayor: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
        float[] melodía = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float[] duraciones = { 0.22f, 0.22f, 0.22f, 0.74f };

        int sampleIndex = 0;
        for (int n = 0; n < melodía.Length; n++)
        {
            int noteSamples = Mathf.RoundToInt(duraciones[n] * sampleRate);
            float freq = melodía[n];
            for (int i = 0; i < noteSamples && sampleIndex < count; i++, sampleIndex++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-3.2f * (t / duraciones[n]));
                float onda1 = Mathf.Sin(2f * Mathf.PI * freq * t);
                float onda2 = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.35f;
                float onda3 = Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.25f;
                samples[sampleIndex] = (onda1 + onda2 + onda3) * env * 0.45f;
            }
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // =========================================================================
    // SINTETIZADORES PROCEDURALES DE AUDIO (PURA MATEMÁTICA EN MEMORIA)
    // =========================================================================

    private AudioClip SintetizarSonido(string nombre, float duracion, float freqInicio, float freqFin, float volumen)
    {
        int sampleRate = 44100;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(freqInicio, freqFin, (float)i / count);
            float envolvente = Mathf.Exp(-18f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envolvente * volumen;
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SintetizarAcordeCounter(string nombre)
    {
        int sampleRate = 44100;
        float duracion = 0.35f;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        // Acorde metálico brillante de contraataque (1200Hz + 1800Hz + 2400Hz)
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float envolvente = Mathf.Exp(-12f * t);
            float onda1 = Mathf.Sin(2f * Mathf.PI * 1200f * t);
            float onda2 = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.7f;
            float onda3 = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.4f;
            samples[i] = (onda1 + onda2 + onda3) * envolvente * 0.45f;
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SintetizarCampanaMagica(string nombre, float duracion, float freqBase)
    {
        int sampleRate = 44100;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-4f * t);
            float s = Mathf.Sin(2f * Mathf.PI * freqBase * t) * 0.6f +
                      Mathf.Sin(2f * Mathf.PI * freqBase * 2.05f * t) * 0.3f +
                      Mathf.Sin(2f * Mathf.PI * freqBase * 3.12f * t) * 0.15f;
            samples[i] = s * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SintetizarAssaultPowerUp(string nombre)
    {
        int sampleRate = 44100;
        float duracion = 0.5f;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float freq = Mathf.Lerp(250f, 950f, (float)i / count);
            float env = Mathf.PingPong(t * 4f, 1f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.4f;
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip SintetizarSecuenciaZelda(string nombre)
    {
        int sampleRate = 44100;
        float duracion = 0.7f;
        int count = Mathf.RoundToInt(sampleRate * duracion);
        float[] samples = new float[count];

        float[] notas = { 587.33f, 659.25f, 783.99f, 880f }; // D, E, G, A
        int subSamples = count / notas.Length;

        for (int n = 0; n < notas.Length; n++)
        {
            for (int i = 0; i < subSamples; i++)
            {
                int index = n * subSamples + i;
                if (index >= count) break;
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-8f * t);
                samples[index] = Mathf.Sin(2f * Mathf.PI * notas[n] * t) * env * 0.4f;
            }
        }

        AudioClip clip = AudioClip.Create(nombre, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
