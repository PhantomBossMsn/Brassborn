using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class REDESCcutscenemrg : MonoBehaviour
{
    [System.Serializable]
    public class TargetPoint
    {
        public Transform target;   // position + rotation to move to
        public bool useLerp = true; // should this one lerp?
        public float delay = 0.5f; // wait before moving to this target
    }

    [Header("References")]
    public GameObject objectToMove;
    public GameObject RedJack;

    [Header("Targets")]
    public TargetPoint[] targets;

    [Header("General Settings")]
    public float moveSpeed = 3f; // how fast we lerp

    private bool isMoving = false;

    private void Start()
    {
        // auto-start for testing
        StartMove();
    }

    public void StartMove()
    {
        if (objectToMove != null && targets.Length > 0)
        {
            StartCoroutine(MoveRoutine());
        }
        else
        {
            Debug.LogWarning("MoveToTargets: Missing objectToMove or targets!");
        }
    }

    private IEnumerator MoveRoutine()
    {
        isMoving = true;

        foreach (TargetPoint point in targets)
        {
            if (point.target == null) continue;

            // wait before this target
            yield return new WaitForSeconds(point.delay);

            if (!point.useLerp)
            {
                // Snap instantly
                objectToMove.transform.position = point.target.position;
                objectToMove.transform.rotation = point.target.rotation;
            }
            else
            {
                // Smooth move with MoveTowards (more reliable than pure Lerp)
                while (Vector3.Distance(objectToMove.transform.position, point.target.position) > 0.01f ||
                       Quaternion.Angle(objectToMove.transform.rotation, point.target.rotation) > 0.5f)
                {
                    objectToMove.transform.position =
                        Vector3.MoveTowards(objectToMove.transform.position, point.target.position, moveSpeed * Time.deltaTime);

                    objectToMove.transform.rotation =
                        Quaternion.RotateTowards(objectToMove.transform.rotation, point.target.rotation, moveSpeed * 50 * Time.deltaTime);

                    yield return null;
                }

                // snap at the end
                objectToMove.transform.position = point.target.position;
                objectToMove.transform.rotation = point.target.rotation;
            }
        }

        isMoving = false;
    }
    public void GlobalUexFunc(int id)
    { 
        if (id ==1)
        {
            RedJack.SetActive(false);


        }
        if (id == 2)
        {
            SceneManager.LoadSceneAsync("Congrat");
        }
        if (id == 3)
        {
            SceneManager.LoadSceneAsync("HomeScene");
        }
    }
}
