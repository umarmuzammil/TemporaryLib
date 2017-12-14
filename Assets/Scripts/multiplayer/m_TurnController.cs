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
    }

    public float turnDuration = 60;    
    public Text turntext;

    private void Start() {

        myTurn = (PhotonNetwork.isMasterClient) ? Turn.local : Turn.remote;
        turntext.text = turnDuration.ToString();
        activeTurn = Turn.local;        
    }

    private void Update() {                   
        time += Time.deltaTime;

        float textTime = Mathf.Clamp((turnDuration - (int)time), 0, 60);
        turntext.text = textTime.ToString();
                
        if(time > turnDuration) {            
            switchTurn();
        }
    }

    public void switchTurn() {
        activeTurn = (activeTurn == Turn.local) ? Turn.remote : Turn.local; 
        time = 0;
    }
       

}
