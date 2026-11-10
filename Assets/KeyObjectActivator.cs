using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class KeyObjectActivator : MonoBehaviour
{
    [Header("Assign Objects in Order: A, D, S, W")]
    public GameObject cogObject;
    public GameObject rockObject;
    public GameObject redObject;
    public GameObject kObject;
    public GameObject animatedhand;

    [Header("Icons")]
    public GameObject redJackicon, LockiconRED;
    public GameObject Rockjackicon, LockiconROCK;

    private GameObject[] objects;
    bool selectedSCENE;
    public AudioSource CantselectLVL;
    [Header("Scene Transition Delay (seconds)")]
    public float delay = 1f; // how long the hand anim plays before loading

    void Start()
    {
        Time.timeScale = 1f;

        // handle lock visuals only once
        if (PlayerPrefs.GetInt("COGJACK") == 1)
        {
            Rockjackicon.SetActive(true);
            if (PlayerPrefs.GetInt("ROCKJACK") == 1)
            {
                redJackicon.SetActive(true);
            }
            else
            {
                LockiconRED.SetActive(true);
            }
        }
        else
        {
            LockiconROCK.SetActive(true);
        }

        // Store them in an array for easy switching
        objects = new GameObject[] { cogObject, rockObject, redObject, kObject };

        // Start with all disabled
        SetActiveOnly(null);
    }

    void Update()
    {
        // --- Handle selection keys ---
        if (Input.GetKeyDown(KeyCode.A))
        {
            SetActiveOnly(cogObject);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            if (PlayerPrefs.GetInt("COGJACK") == 1)
                SetActiveOnly(rockObject);
            else
                CantselectLVL.Play(); // ?? NOW it works
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (PlayerPrefs.GetInt("ROCKJACK") == 1)
                SetActiveOnly(redObject);
            else if (LockiconRED.activeSelf) { CantselectLVL.Play(); }
                 // ?? NOW it works
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            SetActiveOnly(kObject);
        }

        // --- Handle scene transition ---
        if (Input.GetKeyDown(KeyCode.L) && !selectedSCENE)
        {
            if (cogObject.activeSelf || rockObject.activeSelf || redObject.activeSelf || kObject.activeSelf)
            {
                animatedhand.SetActive(true);
                StartCoroutine(HandleSceneTransition());
            }
        }
    }

    void SetActiveOnly(GameObject target)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
                obj.SetActive(obj == target);
        }
    }

    IEnumerator HandleSceneTransition()
    {
        selectedSCENE = true;

        yield return new WaitForSecondsRealtime(delay);

        string sceneName = "";
        if (cogObject.activeSelf) sceneName = "CogScene";
        else if (rockObject.activeSelf) sceneName = "RockScene";
        else if (redObject.activeSelf) sceneName = "RedScene";
        else if (kObject.activeSelf) sceneName = "HomeScene";

        if (!string.IsNullOrEmpty(sceneName))
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = true;
            while (!op.isDone)
                yield return null;
        }
    }
}
