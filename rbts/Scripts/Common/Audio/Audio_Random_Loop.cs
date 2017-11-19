using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Audio_Random_Loop : MonoBehaviour
{
	public AudioClip[] clips;
	public AudioMixerGroup output;

    [Header("Минимальная задержка")]
    public float minDelay = 1;
    [Header("Максимальная задержка")]
    public float maxDelay = 2;
    
    [Space]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Range(0.0f, 1.0f)]
    public float spatialBlend = 0;

    [Header("Залупить первый звук")]
    public bool loop = false;

    private AudioSource source;
    
    // Срабатывает один раз
    private void Start()
    {
        source = GetComponent<AudioSource>();

        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.outputAudioMixerGroup = output;
        source.spatialBlend = spatialBlend;
        StartCoroutine(LoopPlaying());
    }
	  
    //Корутина для зацикленного проигрывания рандомных звуков
    private IEnumerator LoopPlaying()
    {
        while (true)
        {
            yield return null;
            if (source.isPlaying)
            {
                continue;
            }

            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            MyPlayMethod();

            if (loop)
            {
                yield break;
            }
        }
    }
    
    //Воспроизведение случайного трека из списка
    private void MyPlayMethod()
    {
        int randomClip = Random.Range(0, clips.Length);
        float randomPitch = Random.Range(minPitch, maxPitch);
        source.clip = clips[randomClip];
        source.pitch = randomPitch;
        source.loop = loop;
        source.Play();
    }
}
