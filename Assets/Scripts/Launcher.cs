using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    // Start is called before the first frame update
    
    void Awake() {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;
    
    public GameObject menuButtons;

    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;

    public GameObject roomScreen;
    public TMP_Text roomNameText, playerNameLabel;
    private List<TMP_Text> playerNames = new List<TMP_Text>();

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton roomButton;
    private List<RoomButton> roomButtons = new List<RoomButton>();

    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    public static bool hasSetNick;

    public string levelToPlay;
    public GameObject startButton;

    public GameObject roomTestButton;

    public string[] maps;
    public bool changeMapBetweenRounds = true;

    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network...";
        if (!PhotonNetwork.IsConnected) {
            PhotonNetwork.ConnectUsingSettings();
        }
        roomScreen.SetActive(false);

        #if UNITY_EDITOR
            roomTestButton.SetActive(true);
        #endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenus(){
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if (!hasSetNick) {
            CloseMenus();
            nameInputScreen.SetActive(true);

            if (PlayerPrefs.HasKey("playerName")) {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        } else {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public void OpenRoomCreateScreen() {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom() {
        if (!string.IsNullOrEmpty(roomNameInput.text)) {
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = 8;
            PhotonNetwork.CreateRoom(roomNameInput.text, roomOptions);
            CloseMenus();
            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true)        ;
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListPlayers();

        if (PhotonNetwork.IsMasterClient) {
            startButton.SetActive(true);
        } else {
            startButton.SetActive(false);
        }
    }

    private void ListPlayers() {
        foreach (TMP_Text playerName in playerNames)
        {
            Destroy(playerName.gameObject);
        }
        playerNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            TMP_Text playerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            playerLabel.text = players[i].NickName;
            playerLabel.gameObject.SetActive(true);
            playerNames.Add(playerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text playerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        playerLabel.text = newPlayer.NickName;
        playerLabel.gameObject.SetActive(true);
        playerNames.Add(playerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Failed To Create Room: " + message;
        CloseMenus();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen() {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
        CloseMenus();
        loadingText.text = "Leaving Room";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser() {
        CloseMenus();
        roomBrowserScreen.SetActive(true);
    }

    public void CloseRoomBrowser() {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomButton roomButton in roomButtons)
        {
            Destroy(roomButton.gameObject);
        }

        roomButtons.Clear();
        roomButton.gameObject.SetActive(false);

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList) {
                RoomButton updatedRoomButton = Instantiate(roomButton, roomButton.transform.parent);
                updatedRoomButton.SetButtonDetails(roomList[i]);
                updatedRoomButton.gameObject.SetActive(true);

                roomButtons.Add(updatedRoomButton);
            }
        }
    }

    public void JoinRoom(RoomInfo roomInfo) {
        PhotonNetwork.JoinRoom(roomInfo.Name);
        CloseMenus();
        loadingText.text = "Joining Room";
        loadingScreen.SetActive(true);
    }

    public void SetNickName() {
        if (!string.IsNullOrEmpty(nameInput.text)) {
            PhotonNetwork.NickName = nameInput.text;
            PlayerPrefs.SetString("playerName", nameInput.text);
            CloseMenus();
            menuButtons.SetActive(true);
            hasSetNick = true;
        }
    }

    public void StartGame() {
        PhotonNetwork.LoadLevel(maps[Random.Range(0, maps.Length)]);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient) {
            startButton.SetActive(true);
        } else {
            startButton.SetActive(false);
        }
    }

    public void QuickJoin() {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test", roomOptions);
        CloseMenus();
        loadingText.text = "Creating Room";
        loadingScreen.SetActive(true);
    }

    public void QuitGame() {
        Application.Quit();
    }
}
