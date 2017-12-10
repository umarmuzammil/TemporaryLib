using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;

public class m_TurnController : PunBehaviour {
    



    public enum Turn {
        none = 0,local, remote
    }


    public Turn turn;

    private float time = 0;
    public float turnDuration = 30;
    
    public Text turntext;
    //Room room;
    private void Start() {

        //room = PhotonNetwork.room;
        //room.CustomProperties.Add("Turn", turn);
        turntext.text = turnDuration.ToString();
        turn = Turn.local;
        
    }



    private void Update() {
                
        time += Time.deltaTime;   
        turntext.text = (turnDuration - (int)time).ToString();

        if(time > turnDuration) {
            time = 0;
            turn = switchTurn(turn);            
        }
    }

    public Turn switchTurn(Turn _turn) {
        _turn = (_turn == Turn.local) ? _turn = Turn.remote : _turn = Turn.local;
        return _turn;
    }


    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.isWriting) {
            stream.SendNext(turn);
        }
        else {
            turn = (Turn)stream.ReceiveNext();
        }
    }
}
