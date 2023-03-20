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
    public TMP_Text roomNameText;

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton roomButton;
    private List<RoomButton> roomButtons = new List<RoomButton>();

    void Start()
    {
        CloseMenus();
        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network...";
        PhotonNetwork.ConnectUsingSettings();
        roomScreen.SetActive(false);
    }

    void CloseMenus(){
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);
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
        
    }
}
