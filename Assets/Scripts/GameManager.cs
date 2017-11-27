using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon;

namespace com.Basket.Graystork {
    public class GameManager : PunBehaviour {

        //CallBacks
        public void LeftRoom() {
            SceneManager.LoadScene(0);
        }

        public override void OnPhotonPlayerConnected(PhotonPlayer other) {
            Debug.Log(other.NickName);
            if (PhotonNetwork.isMasterClient) {
                LoadMultiplayerScene();
            }
        }

        public override void OnPhotonPlayerDisconnected(PhotonPlayer other) {
            if (PhotonNetwork.isMasterClient) {
                LoadMultiplayerScene();
            }
        }
        void LoadMultiplayerScene() {
            if (!PhotonNetwork.isMasterClient) {
                Debug.Log("You are not Master Client");
            }

            PhotonNetwork.LoadLevel("MultiplayerGameScene");
        }
        //CallBacks End

        public void LeaveRoom() {
            PhotonNetwork.LeaveRoom();
        }

        
      
    }
}
