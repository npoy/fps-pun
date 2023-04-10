using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

    void Awake() {
        instance = this;
    }
    
    private const int maxCodeNumber = 200;

    public enum EventCode : byte {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public List<PlayerInfo> players = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();

    public enum GameState {
        Waiting,
        Playing,
        Ending
    }

    public int killsToWin = 3;
    public Transform mapCamPoint;
    public GameState currentGameState = GameState.Waiting;
    public float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLength = 180f;
    private float currentMatchTime;
    private float sendTimer;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected) {
            SceneManager.LoadScene(0);
        } else {
            NewPlayerSend(PhotonNetwork.NickName);
            currentGameState = GameState.Playing;
            SetupTimer();

            if (!PhotonNetwork.IsMasterClient) {
                UIController.instance.timerText.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && currentGameState != GameState.Ending) {
            if (UIController.instance.leaderboard.activeInHierarchy) {
                UIController.instance.leaderboard.SetActive(false);
            } else {
                ShowLeaderboard();
            }
        }

        if (PhotonNetwork.IsMasterClient) {
            if (currentMatchTime > 0 && currentGameState == GameState.Playing) {
                currentMatchTime -= Time.deltaTime;
                if (currentMatchTime <= 0f) {
                    currentMatchTime = 0;
                    currentGameState = GameState.Ending;
                    ListPlayersSend();
                    StateCheck();
                }

                UpdateTimerDisplay();
                sendTimer -= Time.deltaTime;
                if (sendTimer <= 0) {
                    sendTimer += 1f;
                    TimerSend();
                }
            }
        }
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code < maxCodeNumber) {
            EventCode eventCode = (EventCode)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch (eventCode)
            {
                case EventCode.NewPlayer:
                    NewPlayerReceive(data);
                    break;
                case EventCode.ListPlayers:
                    ListPlayersReceive(data);
                    break;
                case EventCode.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
                case EventCode.NextMatch:
                    NextMatchReceive();
                    break;
                case EventCode.TimerSync:
                    TimerReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string userName) {
        object[] package = new object[4];
        package[0] = userName;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCode.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void NewPlayerReceive(object[] dataReceived) {
        PlayerInfo playerInfo = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);
        players.Add(playerInfo);
        ListPlayersSend();
    }

    public void ListPlayersSend() {
        object[] package = new object[players.Count + 1];

        package[0] = currentGameState;

        for (int i = 0; i < players.Count; i++)
        {
            object[] item = new object[4];
            item[0] = players[i].name;
            item[1] = players[i].actor;
            item[2] = players[i].kills;
            item[3] = players[i].deaths;

            package[i + 1] = item;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCode.ListPlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ListPlayersReceive(object[] dataReceived) {
        players.Clear();

        currentGameState = (GameState)dataReceived[0];

        for (int i = 1; i < dataReceived.Length; i++)
        {
            object[] item = (object[])dataReceived[i];

            PlayerInfo playerInfo = new PlayerInfo(
                (string)item[0],
                (int)item[1],
                (int)item[2],
                (int)item[3]
            );

            players.Add(playerInfo);

            if (PhotonNetwork.LocalPlayer.ActorNumber == playerInfo.actor) {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange) {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCode.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void UpdateStatsReceive(object[] dataReceived) {
        int updatedActor = (int)dataReceived[0];
        int updatedStat = (int)dataReceived[1];
        int updatedAmount = (int)dataReceived[2];

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].actor == updatedActor) {
                switch (updatedStat)
                {
                    case 0:
                        players[i].kills += updatedAmount;
                        break;
                    case 1:
                        players[i].deaths += updatedAmount;
                        break;
                }

                if (i == index) {
                    UpdateStatsDisplay();
                }

                if (UIController.instance.leaderboard.activeInHierarchy) {
                    ShowLeaderboard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void UpdateStatsDisplay() {
        if (players.Count > index) {
            UIController.instance.killsText.text = "Kills: " + players[index].kills;
            UIController.instance.deathsText.text = "Deaths: " + players[index].deaths;
        } else {
            UIController.instance.killsText.text = "Kills: 0";
            UIController.instance.deathsText.text = "Deaths: 0";
        }
    }

    void ShowLeaderboard() {
        UIController.instance.leaderboard.SetActive(true);
        foreach (LeaderboardPlayer leaderboardPlayer in leaderboardPlayers)
        {
            Destroy(leaderboardPlayer.gameObject);
        }

        leaderboardPlayers.Clear();
        UIController.instance.leaderboardPlayer.gameObject.SetActive(false);

        List<PlayerInfo> sortedPlayers = SortPlayers(players);

        foreach (PlayerInfo playerInfo in sortedPlayers)
        {
            LeaderboardPlayer newLeaderboardPlayer = Instantiate(UIController.instance.leaderboardPlayer, UIController.instance.leaderboardPlayer.transform.parent);
            newLeaderboardPlayer.SetDetails(playerInfo.name, playerInfo.kills, playerInfo.deaths);
            newLeaderboardPlayer.gameObject.SetActive(true);
            leaderboardPlayers.Add(newLeaderboardPlayer);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players) {
        List<PlayerInfo> sortedPlayers = new List<PlayerInfo>();
        
        while (sortedPlayers.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo playerInfo in players)
            {
                if (!sortedPlayers.Contains(playerInfo)) {
                    if (playerInfo.kills > highest) {
                        selectedPlayer = playerInfo;
                        highest = playerInfo.kills;
                    }
                }
            }

            sortedPlayers.Add(selectedPlayer);
        }

        return sortedPlayers;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    void ScoreCheck() {
        bool winnerFound = false;

        foreach (PlayerInfo playerInfo in players)
        {
            if (playerInfo.kills > killsToWin && killsToWin < 0) {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound) {
            if (PhotonNetwork.IsMasterClient && currentGameState != GameState.Ending) {
                currentGameState = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    void StateCheck() {
        if (currentGameState == GameState.Ending) {
            EndGame();
        }
    }

    void EndGame() {
        currentGameState = GameState.Ending;
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Before ending game but after destroying the player
        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;
        
        StartCoroutine(EndGameCo());
    }

    private IEnumerator EndGameCo() {
        yield return new WaitForSeconds(waitAfterEnding);

        if (!perpetual) {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        } else {
            if (PhotonNetwork.IsMasterClient) {
                if (!Launcher.instance.changeMapBetweenRounds) {
                    NextMatchSend();
                } else {
                    int newLevel = Random.Range(0, Launcher.instance.maps.Length);
                    if (Launcher.instance.maps[newLevel] == SceneManager.GetActiveScene().name) {
                        NextMatchSend();
                    } else {
                        PhotonNetwork.LoadLevel(Launcher.instance.maps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMatchSend() {
        PhotonNetwork.RaiseEvent(
            (byte)EventCode.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void NextMatchReceive() {
        currentGameState = GameState.Playing;
        
        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach (PlayerInfo playerInfo in players)
        {
            playerInfo.kills = 0;
            playerInfo.deaths = 0;
        }

        UpdateStatsDisplay();
        PlayerSpawner.instance.SpawnPlayer();
        SetupTimer();
    }

    public void SetupTimer() {
        if (matchLength > 0) {
            currentMatchTime = matchLength;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay() {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);
        UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend() {
        object[] package = new object[] { (int)currentMatchTime, currentGameState };

        PhotonNetwork.RaiseEvent(
            (byte)EventCode.TimerSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void TimerReceive(object[] dataReceived) {
        currentMatchTime = (int)dataReceived[0];
        currentGameState = (GameState)dataReceived[1];
        UpdateTimerDisplay();
        UIController.instance.timerText.gameObject.SetActive(true);
    }
}

[System.Serializable]
public class PlayerInfo {
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo (string _name, int _actor, int _kills, int _deaths) {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}