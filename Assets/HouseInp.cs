using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HouseInp : MonoBehaviour
{
    public GameObject ControllsUIEXP, punchui, kickui;
    public GameObject ispunchobj, ischainobj, isaxeobj;
    public CombatInputHandler combatInputHandler;

    private int weaponstats;

    void Start()
    {
        weaponstats = PlayerPrefs.GetInt("WeaponIDPREF", 1); // default to 1
        changeweaponUI();
    }

    void Update()
    {
        if (ControllsUIEXP.activeSelf)
        {
            if (Input.GetKey(KeyCode.A))
            {
                punchui.SetActive(true);
                kickui.SetActive(false);
            }
            else if (Input.GetKey(KeyCode.D))
            {
                punchui.SetActive(false);
                kickui.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                NextWeapon();
            }
        }

        // Toggle Controls UI (W+K)
        if (Input.GetKey(KeyCode.W) && Input.GetKeyDown(KeyCode.K))
        {
            ToggleControlsUI();
        }

        // Resume game (S+K)
        if (Input.GetKey(KeyCode.S) && Input.GetKeyDown(KeyCode.K))
        {
            SceneManager.LoadSceneAsync("Map");
        }
    }

    void ToggleControlsUI()
    {
        ControllsUIEXP.SetActive(!ControllsUIEXP.activeSelf);
        combatInputHandler.enabled = !ControllsUIEXP.activeSelf;
    }

    void NextWeapon()
    {
        weaponstats++;

        if (weaponstats > 3) // only 3 weapons
            weaponstats = 1;

        PlayerPrefs.SetInt("WeaponIDPREF", weaponstats);
        changeweaponUI();
    }

    void changeweaponUI()
    {
        ispunchobj.SetActive(weaponstats == 1);
        isaxeobj.SetActive(weaponstats == 2);
        ischainobj.SetActive(weaponstats == 3);
    }
}
