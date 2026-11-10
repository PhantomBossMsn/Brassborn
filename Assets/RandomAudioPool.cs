using UnityEngine;

public class RandomAudioPool : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip[] audioClips;         // put your random clips here
    public int poolSize = 10;              // how many AudioSources to pre-create
    public float minPitch = 1f;            // optional pitch variation
    public float maxPitch = 1f;

    private AudioSource[] pool;
    private int nextIndex = 0;

    void Awake()
    {
        // Build pool
        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("AudioSource_" + i);
            go.transform.parent = transform; // keep it organized under this object
            pool[i] = go.AddComponent<AudioSource>();
        }
    }

    public void PlayRandom()
    {
        if (audioClips.Length == 0) return;

        // Pick a random clip
        AudioClip clip = audioClips[Random.Range(0, audioClips.Length)];

        // Grab next AudioSource in pool
        AudioSource source = pool[nextIndex];
        nextIndex = (nextIndex + 1) % poolSize;

        // Optional pitch variation
        source.pitch = Random.Range(minPitch, maxPitch);

        // Play without cutting others
        source.PlayOneShot(clip);
    }
}
