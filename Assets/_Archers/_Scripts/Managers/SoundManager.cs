using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundManager : HMSingleton<SoundManager>
{
    [SerializeField] private AudioSource _aud;
    [SerializeField] private AudioSource _runAud;
    
    [Space] 
    [SerializeField] private AudioClip[] _deathSounds;
    [SerializeField] private AudioClip   _arrowSound;
    [SerializeField] private AudioClip   _winSound;
    [SerializeField] private AudioClip   _woodSound;
    [SerializeField] private AudioClip   _bubbleSound;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlayDeathSound()
    {
        _aud.PlayOneShot(_deathSounds[Random.Range(0, _deathSounds.Length)]);
    }
    
    public void PlayArrowSound()
    {
        _aud.PlayOneShot(_arrowSound);
    }
    public void PlayBubbleSound()
    {
        _aud.PlayOneShot(_bubbleSound);
    }

    public void PlayWoodSound()
    {
        _aud.PlayOneShot(_woodSound);
    }
    
    public void PlayRunSound()
    {
        _runAud.Play();
    }

    public void StopRunSound()
    {
        _runAud.Stop();
    }
    
    public void PlayWinRoundSound()
    {
        _aud.PlayOneShot(_winSound);
    }
}
