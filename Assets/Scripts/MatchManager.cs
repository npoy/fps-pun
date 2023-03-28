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
    
    public enum EventCodes : byte {
        NewPlayer,
        ListPlayers,
        ChangeStat
    }

    public List<PlayerInfo> players = new List<PlayerInfo>();
    private int index;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected) {
            SceneManager.LoadScene(0);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnEvent(EventData photonEvent) {

    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
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