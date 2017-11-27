using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class PlayerInputField : MonoBehaviour {

    static string playerNamePrefKey = "Player";
    void Start () {

        string defaultName = "";
        InputField inputField = GetComponent<InputField>();

        if(inputField.text != null) {
            if(PlayerPrefs.HasKey("playerNamePrefKey")) {
                defaultName = PlayerPrefs.GetString("playerNamePrefKey");
                inputField.text = defaultName;
            }
        }

        PhotonNetwork.playerName = defaultName;
	}

    public void SetPlayerName(string value) {
       
        PhotonNetwork.playerName = value + " "; 
        PlayerPrefs.SetString(playerNamePrefKey, value);
    }
}
