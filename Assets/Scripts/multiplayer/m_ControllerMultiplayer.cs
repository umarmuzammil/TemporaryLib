using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(m_GameController))]
public class m_ControllerMultiplayer : PunBehaviour  {

    public int startBallsCount = 6;                 //Balls count we start with
    public int bonusRingClearCombo = 3;             //Count of clear goals in a row to get big rinf bonus
    public int bonusAimCombo = 5;                   //Count of goals in a row to get aim bonus
    public int bonusAimMinXpLevel = 5;              //Minimum xp level to be able get aim bonus
    public int bonusRingThrowsLimit = 3;            //When you get big ring bonus after this count of throws it will gone
    public int bonusAimThrowsLimit = 3;             //When you get aim bonus after this count of throws it will gone 
    public int xpScoreStep = 100;					//XP step. With help of this you can tweak the speed of getting xp level.

    //Test SCORES MULTIPLAYERS
    public Text LocalPlayerName;
    public Text RemotePlayerCount;
    public Text ballsRemoteCountTxt;
    public Text ballsLocalCountTxt;
    public Text scoreTxt;
    public Text plusScoreTxt;
    public Text plusBallTxt;
    public Text plusDotsTxt;
    public Transformer ballIcon;
    public BoxCollider spawnCollider;



    private GameObject ring;
    private m_Shooter shooter;
    private m_TurnController turnController;
    private AudioSource thisAudio;

    //multiplayer Score Variables
    private int currentLocalBallsCount;                  //Current amount of balls left to throw
    private int currentRemoteBallsCount;                

    private int score;                              //Current score
    private int xpLevel;                            //Int that defines current XP level 

    private bool hitRecord;							//Boolean that defines if we already hitted last best score or not

    Vector3 RandomPos;
    Hashtable hash = new Hashtable();


    void OnEnable() {
        m_Ball.OnGoal += Goal;
        m_Ball.OnFail += Fail;
    }

    void OnDisable() {
        m_Ball.OnGoal -= Goal;
        m_Ball.OnFail -= Fail;
    }

    void Awake() {
        m_Shooter.aimDotsNum = 60;
    }
   
    void Start() {        
        ring = GameObject.Find("ring");
        turnController = GameObject.Find("TurnController").GetComponent<m_TurnController>();
        shooter = GameObject.Find("Shooter").GetComponent<m_Shooter>();
        thisAudio = GetComponent<AudioSource>();
        currentLocalBallsCount = currentRemoteBallsCount = startBallsCount;

        transform.GetComponent<PhotonView>().viewID = Random.Range(0, 10);
        //customRoomProperties
        
        hash.Add("score", startBallsCount);
        PhotonNetwork.SetPlayerCustomProperties(hash);

        ResetData();

        if (PhotonNetwork.isMasterClient) {            
            RandomPos = GetRandomPosInCollider();
            photonView.RPC("AssignRandomValue", PhotonTargets.All, RandomPos);            
        }
    }

    [PunRPC]
    void AssignRandomValue(Vector3 _RandomPos) {    
        shooter.newBallPosition = _RandomPos;
        m_GameController.data.StartPlay();
    }
        
    void Goal(float distance, float height, bool floored, bool clear, bool special) {


        if (turnController._myTurn == turnController._activeTurn)
        {
            if (turnController._activeTurn == m_TurnController.Turn.local)
            {
                currentLocalBallsCount += 1;
                PhotonNetwork.player.CustomProperties["score"] = currentLocalBallsCount;
                PhotonNetwork.player.SetCustomProperties(hash);
            }
            else
            {
                
                currentRemoteBallsCount += 1;
                PhotonNetwork.player.CustomProperties["score"] = currentRemoteBallsCount;
                PhotonNetwork.player.SetCustomProperties(hash);
            }

        }

        if (clear) {   

            ballIcon.ScaleImpulse(new Vector3(1.3f, 1.3f, 1), 0.4f, 2);
            plusBallTxt.gameObject.SetActive(true);

            

            if (special)
                SoundController.data.playClearSpecialGoal();
            else
                SoundController.data.playClearGoal();
            
        }

        else {
            SoundController.data.playGoal();
        }


        if (special) {
            int heightScore = (int)height;
            plusScoreTxt.text += "\n+" + heightScore.ToString("F0");          
        }

        if (floored) {
            int flooredScore = (int)distance * 2;
            plusScoreTxt.text += "+" + flooredScore.ToString("F0");                     
        }

        plusScoreTxt.gameObject.SetActive(true);
        scoreTxt.gameObject.GetComponent<Transformer>().ScaleImpulse(new Vector3(1.3f, 1.3f, 1), 0.4f, 1);
        BallCompleted();
    }

    void Fail() {

        if (turnController._myTurn == turnController._activeTurn)
        {
            if (turnController._activeTurn == m_TurnController.Turn.local)
            {
                currentLocalBallsCount -= 1;
                PhotonNetwork.player.CustomProperties["score"] = currentLocalBallsCount;
                PhotonNetwork.player.SetCustomProperties(hash);
            }
            else
            {
                currentRemoteBallsCount -= 1;
                PhotonNetwork.player.CustomProperties["score"] = currentRemoteBallsCount;
                PhotonNetwork.player.SetCustomProperties(hash);
            }

        }

                   
        BallCompleted();
    }

    void BallCompleted() {
        xpLevel = score > 2 * xpScoreStep ? score / xpScoreStep : 1;
        UpdateBallsCount();
        UpdateSpawnCollider();
        turnController.switchTurn();

        if (turnController._activeTurn == turnController._myTurn) {
            Vector3 newBallPos = GetRandomPosInCollider();
            photonView.RPC("NextRandomPos", PhotonTargets.All, newBallPos);            
        }       


        if (turnController._activeTurn == turnController._myTurn) {
            Vector3 newBallPos = GetRandomPosInCollider();
            photonView.RPC("NextRandomPos", PhotonTargets.All, newBallPos);            
        }
        

    }
    
    [PunRPC]
    void NextRandomPos(Vector3 _newBallPos) {        
        shooter.newBallPosition = _newBallPos;
        shooter.spawnBall();
    }
        

    IEnumerator GrowRing() {
        yield return new WaitForSeconds(0.5f);
        ring.SetActive(false);
        ring.transform.localPosition = new Vector3(1.9f, 9.51f, 0);
        ring.transform.localScale = new Vector3(2, 2, 2);
        ring.SetActive(true);
        thisAudio.PlayOneShot(SoundController.data.bonusOpen);
    }

    IEnumerator ResetRing() {
        yield return new WaitForSeconds(0.5f);
        ring.SetActive(false);
        ring.transform.localPosition = new Vector3(1.2f, 9.51f, 0);
        ring.transform.localScale = new Vector3(1, 1, 1);
        ring.SetActive(true);
    }

    public void AddScore(int score) {
        this.score += score;
        scoreTxt.text = this.score.ToString();        

    }

    void UpdateBallsCount() {

        for (int i = 0; i < PhotonNetwork.playerList.Length; i++) {

            Debug.Log(PhotonNetwork.playerList[i].NickName);
            //Debug.Log(PhotonNetwork.playerList[i].CustomProperties["score"].ToString());

            if (PhotonNetwork.isMasterClient) {
                Debug.Log("I am master client");
                ballsLocalCountTxt.text = PhotonNetwork.playerList[i].CustomProperties["score"].ToString();
            }

            if (!PhotonNetwork.isMasterClient) {
                Debug.Log("I am not master client");
                ballsRemoteCountTxt.text = PhotonNetwork.playerList[i].CustomProperties["score"].ToString();
            }
        }
        

        /*if (currentBallsCount < 1) {
            m_GameController.data.Complete();
        }*/
    }  

    void UpdateSpawnCollider() {
        float colLenght = Mathf.Clamp(19 + xpLevel, 20, 35);
        spawnCollider.gameObject.transform.position = new Vector3(colLenght / 2, 4, 0);
        Vector3 tempSize = spawnCollider.size;
        tempSize.x = colLenght;
        spawnCollider.size = tempSize;
    }

    public void ResetData() {

        currentLocalBallsCount = currentRemoteBallsCount = startBallsCount;
        score = 0;
        xpLevel = 1; 
        UpdateBallsCount();
        UpdateSpawnCollider();
        scoreTxt.text = score.ToString();
    }

    private Vector3 GetRandomPosInCollider() {
        Vector3 center = spawnCollider.gameObject.transform.position;
        Vector3 randPos = center + new Vector3(Random.Range(-spawnCollider.bounds.size.x / 2, spawnCollider.bounds.size.x / 2), Random.Range(-spawnCollider.bounds.size.y / 2, spawnCollider.bounds.size.y / 2), 0);
        return randPos;

    }


}
