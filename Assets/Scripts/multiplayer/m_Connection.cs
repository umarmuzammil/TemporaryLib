using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
namespace com.Basket.Graystork {

    public class m_Connection : PunBehaviour {

        //Hide Panels

        public GameObject[] PanelsToHide;
        public GameObject connectionPanel;

        bool isConnecting = false;
        string _gameVersion = "1";
        public PhotonLogLevel Loglevel = PhotonLogLevel.Informational;
        public byte MaxPlayersPerRoom = 2;

        private void Start() {
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;
  
        }

        private void Update() {           

        }
        private void Awake() {
            PhotonNetwork.logLevel = Loglevel;
        }
        
        public void OnMultiplayerClick() {
           for(int i=0; i<PanelsToHide.Length; i++) {
                PanelsToHide[i].SetActive(false);
            }
            connectionPanel.SetActive(true);
        }

        public void Connect() {
            isConnecting = true;
            if (PhotonNetwork.connected) {
                PhotonNetwork.JoinRandomRoom();
            }
            else {
                PhotonNetwork.ConnectUsingSettings(_gameVersion);
            }
        }

        private void OnFailedToConnect(NetworkConnectionError error) {
            Debug.Log(error);
        }

        // CallBacks 

        public override void OnConnectedToMaster() {
            
            if (isConnecting) {
                PhotonNetwork.JoinRandomRoom();      
            }
        }

        public override void OnDisconnectedFromPhoton() {
        }

        /*public override void OnCreatedRoom() {
            Debug.Log("I created the room but should I load the level ?");
        }*/ //test callBack

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg) {
            Debug.Log("creating Room");
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = MaxPlayersPerRoom }, null);
        }
        public override void OnPhotonJoinRoomFailed(object[] codeAndMsg) {
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = MaxPlayersPerRoom }, null);

        }
                
        public override void OnJoinedRoom() {
            Debug.Log("I Joined the room");  //Join Room Stuff Goes Here.
            if (PhotonNetwork.room.PlayerCount == 2 ) {
                photonView.RPC("Loadlevel", PhotonTargets.All, null);
            }
        }

        [PunRPC]
        void Loadlevel() {            
            PhotonNetwork.LoadLevel("MultiplayerGameScene");
        }
    }

}