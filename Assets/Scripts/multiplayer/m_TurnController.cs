using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;

public class m_TurnController : PunBehaviour {

    public enum Turn {
        none = 0,local, remote
    }

    [SerializeField]
    private Turn myTurn;
    [SerializeField]
    private Turn activeTurn;


    public Turn _myTurn {
        get { return myTurn; }
    }
       
    public Turn _activeTurn {
        get { return activeTurn; }
    }
    


    private float time = 0;

    public float _time {
        get { return time; }
        set { time = value; }
    }


    public float turnDuration = 30;    
    public Text turntext;

    private void Start() {     

        myTurn = (PhotonNetwork.isMasterClient) ? Turn.local : Turn.remote;
        turntext.text = turnDuration.ToString();
        activeTurn = Turn.local;
        
    }      
             


    private void Update() {
                
        time += Time.deltaTime;

        float textTime = Mathf.Clamp((turnDuration - (int)time), 0, 30);
        turntext.text = textTime.ToString();

        if(time > turnDuration) {
            time = 0;
            activeTurn = switchTurn(activeTurn);            
        }
    }

    public Turn switchTurn(Turn _turn) {
        _turn = (_turn == Turn.local) ? _turn = Turn.remote : _turn = Turn.local;
        return _turn;
    }


    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.isWriting) {
            stream.SendNext(activeTurn);
        }
        else {
            activeTurn = (Turn)stream.ReceiveNext();
        }
    }
}
