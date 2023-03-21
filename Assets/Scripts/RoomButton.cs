using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    public TMP_Text buttonText;
    private RoomInfo roomInfo;
    public void SetButtonDetails(RoomInfo inputInfo) {
        roomInfo = inputInfo;
        buttonText.text = roomInfo.Name;
    }

    public void OpenRoom() {
        Launcher.instance.JoinRoom(roomInfo);
    }
}
