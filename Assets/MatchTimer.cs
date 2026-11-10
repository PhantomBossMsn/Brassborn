using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class MatchTimer : MonoBehaviour
{
    public bool isREDJACK;
    public string stagename;
    [Header("Timer Settings")]
    public float matchTime = 60f; // total match time

    [Header("Health Scripts")]
    public CombatInputHandler scriptA;
    public CombatBotHandler scriptB;

    [Header("UI")]
    public TextMeshProUGUI timerText, ftimerText;
    public GameObject afterstatus, KOBp, KOBn, TU, finishedafterstats, FKOBP, FKOBN, FTU;

    [Header("Pause Options")]
    public float pauseInputDelay = 2f; // delay before K/L allowed
    public float resumeDelay = 1.5f;   // adjustable delay before unpausing gameplay

    [Header("Post-Game Input Timeout")]
    public float postGameInputTimeout = 3f; // timeout after death/time over before allowing any input

    [Header("Objects not affected by pause")]
    public GameObject[] ignorePauseObjects;

    private float timer;
    private bool isGameOver = false;
    private bool isPaused = false;
    private bool killed = false;
    private bool isInPostGameTimeout = false; // new flag for post-game input blocking

    private float pauseTimer = 0f; // track input after pause
    private bool isResuming = false; // flag for delayed resume
    private float postGameTimer = 0f; // timer for post-game input timeout

    public Animator animator;

    public AudioSource bgmusic;
    void Start()
    {
        timer = matchTime;
        Time.timeScale = 1f;
        isInPostGameTimeout = false;
        postGameTimer = 0f;
    }

    void Update()
    {
        // Handle post-game input timeout
        if (isInPostGameTimeout)
        {
            postGameTimer += Time.unscaledDeltaTime;

            if (postGameTimer >= postGameInputTimeout)
            {
                isInPostGameTimeout = false;
                Debug.Log("Post-game input timeout ended - inputs now allowed");
            }

            return; // block all input during timeout
        }
        if(!isInPostGameTimeout&& postGameTimer >= postGameInputTimeout)
        {
            if (Input.GetKeyDown(KeyCode.A)) // Restart
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
            else if (Input.GetKeyDown(KeyCode.L)) // Quit
            {
                if(isREDJACK)
                {
                    SceneManager.LoadSceneAsync("REDESC");


                }
                else
                {
                    SceneManager.LoadSceneAsync("Map");

                }
            }
        }

        if (isGameOver) return;

        // Pause (S + K pressed together) - only allow if game is not over
        if (!isPaused && !isGameOver && Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.K))
        {
            TogglePause();
        }

        // While paused
        if (isPaused)
        {
            bgmusic.Pause();
            // Keep main animator paused indicator
            animator.SetBool("ispuased", true);

            pauseTimer += Time.unscaledDeltaTime;

            // Manually update ignored animators every frame
            foreach (var obj in ignorePauseObjects)
            {
                if (obj == null) continue;
                Animator a = obj.GetComponent<Animator>();
                if (a != null && a.enabled)
                {
                    a.updateMode = AnimatorUpdateMode.UnscaledTime;
                    a.Update(Time.unscaledDeltaTime); // advance animation even when paused
                }
            }

            // Allow pause menu inputs after delay
            if (pauseTimer >= pauseInputDelay && !isResuming)
            {
                if (Input.GetKeyDown(KeyCode.A)) // Restart
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                    );
                }
                else if (Input.GetKeyDown(KeyCode.L)) // Quit
                {
                    SceneManager.LoadSceneAsync("Map");
                }
                else if (Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.K)) // Resume request
                {
                    StartCoroutine(ResumeAfterDelay());
                }
            }

            return; // stop ticking timer
        }
        else
        {
            bgmusic.UnPause();


            animator.SetBool("ispuased", false);
        }

        // Normal countdown
        timer -= Time.deltaTime;
        timer = Mathf.Max(timer, 0f);

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(timer).ToString();
            ftimerText.text = Mathf.CeilToInt(timer).ToString();
        }

        // Check for death conditions
        if (scriptA.health <= 0 || scriptB.health <= 0)
        {
            StartCoroutine(HandleDeath());
        }
        else if (timer <= 0)
        {
            HandleImmediateDeath();
        }
    }

    private IEnumerator HandleDeath()
    {
        if (isGameOver) yield break;
        isGameOver = true;
        scriptA.enabled = false;

        // Start post-game input timeout
        StartPostGameInputTimeout();

        yield return new WaitForSeconds(1f);
        afterstatus.SetActive(true);

        if (scriptA.health <= 0)
        {
            KOBp.SetActive(true);
            FinisherAfterstats(2);
        }
        else if (scriptB.health <= 0)
        {
            FinisherAfterstats(1);
            KOBn.SetActive(true);
        }
    }

    private void HandleImmediateDeath()
    {
        if (isGameOver) return;
        if (!killed)
        {
            isGameOver = true;
            killed = true;

            // Start post-game input timeout
            StartPostGameInputTimeout();

            afterstatus.SetActive(true);
            TU.SetActive(true);
            scriptA.Die();
            scriptA.enabled = false;
            Debug.Log("Time Over — Dead");
            FinisherAfterstats(0);
        }
    }

    private void StartPostGameInputTimeout()
    {
        isInPostGameTimeout = true;
        postGameTimer = 0f;
        Debug.Log("Post-game input timeout started for " + postGameInputTimeout + " seconds");

        // Force unpause if currently paused
        if (isPaused)
        {
            isPaused = false;
            isResuming = false;
            Time.timeScale = 1f;
            animator.SetBool("ispuased", false);
        }
    }

    void FinisherAfterstats(int stat)
    {
        bgmusic.Pause();

        scriptA.enabled = false;
        scriptB.enabled = false;

        finishedafterstats.SetActive(true);
        if (stat == 0)
        {
            FTU.SetActive(true);
        }
        if (stat == 2)
        {
            FKOBN.SetActive(true);
        }
        if (stat == 1)
        {
            FKOBP.SetActive(true);
            PlayerPrefs.SetInt(stagename, 1);
            Debug.Log(stagename);
        }
    }

    private void TogglePause()
    {
        // Prevent pausing if game is over or in post-game timeout
        if (isGameOver || isInPostGameTimeout) return;

        isPaused = true;
        Time.timeScale = 0f;
        pauseTimer = 0f;
        Debug.Log("Game Paused");

        // Set ignored animators to unscaled time so they keep animating
        foreach (var obj in ignorePauseObjects)
        {
            if (obj == null) continue;
            Animator a = obj.GetComponent<Animator>();
            if (a != null) a.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    private IEnumerator ResumeAfterDelay()
    {
        isResuming = true;
        Debug.Log("Resuming in " + resumeDelay + "s...");

        // Wait in real time while animators continue in unscaled time
        yield return new WaitForSecondsRealtime(resumeDelay);

        isPaused = false;
        isResuming = false;
        Time.timeScale = 1f;
        Debug.Log("Game Resumed");

        // Animators in ignorePauseObjects remain in UnscaledTime as requested
        animator.SetBool("ispuased", false);

        foreach (var obj in ignorePauseObjects)
        {
            if (obj == null) continue;
            Animator a = obj.GetComponent<Animator>();
            if (a != null && a.enabled)
            {
                a.updateMode = AnimatorUpdateMode.UnscaledTime;
                a.Update(Time.unscaledDeltaTime); // advance animation even when paused
            }
        }
    }
}