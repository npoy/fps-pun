using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    public TMP_Text overheatedMessage;
    public Slider weaponTempSlider; 

    public GameObject deathScreen;
    public TMP_Text deathText;

    public Slider healthSlider;

    public TMP_Text killsText, deathsText;

    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayer;

    public GameObject endScreen;

    public TMP_Text timerText;

    public GameObject optionsScreen;

    private void Awake() {
        instance = this;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ToggleOptions();
        }

        if (optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ToggleOptions() {
        if (!optionsScreen.activeInHierarchy) {
            optionsScreen.SetActive(true);
        } else {
            optionsScreen.SetActive(false);
        }
    }

    public void ReturnToMainMenu() {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame() {
        Application.Quit();
    }
}
