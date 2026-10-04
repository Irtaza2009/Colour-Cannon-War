using Unity.Netcode;
using UnityEngine;

public class AudioManager : NetworkBehaviour
{
    public static AudioManager Instance;

[Header("Audio Source")]
[SerializeField] AudioSource MusicSource;
[SerializeField] AudioSource SFXSource;

[Header("Audio Clips")]
public AudioClip BackgroundMusic;
public AudioClip CannonSound;
public AudioClip ZapSound;

private void Awake()
    {
        Instance = this;
    }
private void Start()
    {
        MusicSource.clip = BackgroundMusic;
        MusicSource.Play();
    }

public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

public void PlayCannonSound()
    {
        PlaySFX(CannonSound);
    }

public void PlayZapSound()
    {
        PlaySFX(ZapSound);
    }





}
