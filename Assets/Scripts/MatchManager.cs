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
        UpdateStat
    }

    public List<PlayerInfo> players = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> leaderboardPlayers = new List<LeaderboardPlayer>();

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected) {
            SceneManager.LoadScene(0);
        } else {
            NewPlayerSend(PhotonNetwork.NickName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (UIController.instance.leaderboard.activeInHierarchy) {
                UIController.instance.leaderboard.SetActive(false);
            } else {
                ShowLeaderboard();
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
        object[] package = new object[players.Count];
        for (int i = 0; i < players.Count; i++)
        {
            object[] item = new object[4];
            item[0] = players[i].name;
            item[1] = players[i].actor;
            item[2] = players[i].kills;
            item[3] = players[i].deaths;

            package[i] = item;
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
        for (int i = 0; i < dataReceived.Length; i++)
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
                index = i;
            }
        }
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