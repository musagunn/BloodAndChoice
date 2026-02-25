using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Ses Kaynaklarý (Audio Source)")]
    [SerializeField] private AudioSource musicSource; // Müzik için (Döngüsel)
    [SerializeField] private AudioSource sfxSource;   // Efektler için (Tek seferlik)

    [Header("Ses Klipleri (Audio Clips)")]
    public AudioClip backgroundMusic;
    public AudioClip attackSound;
    public AudioClip hitSound; // Biz hasar alýnca veya yanlýþ yapýnca
    public AudioClip parrySound;
    public AudioClip monsterHitSound; // Canavara vurunca

    void Start()
    {
        // Oyun baþlar baþlamaz müziði çal
        PlayMusic(backgroundMusic);
    }

    // --- MÜZÝK FONKSÝYONU ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true; // Sürekli çalsýn
            musicSource.Play();
        }
    }

    // --- EFEKT FONKSÝYONLARI ---
    public void PlayAttack()
    {
        PlaySFX(attackSound);
    }

    public void PlayParry()
    {
        PlaySFX(parrySound);
    }

    public void PlayMonsterHit() // Canavar hasar alýnca
    {
        PlaySFX(monsterHitSound);
    }

    public void PlayPlayerHit() // Biz hasar alýnca
    {
        PlaySFX(hitSound);
    }

    // Yardýmcý fonksiyon (Tek seferlik ses çalar)
    private void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip); // PlayOneShot: Sesler üst üste biner, kesilmez
        }
    }
}