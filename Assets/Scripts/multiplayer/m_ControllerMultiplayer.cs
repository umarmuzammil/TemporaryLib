using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon;

[RequireComponent(typeof(m_GameController))]
public class m_ControllerMultiplayer : PunBehaviour  {

    public int startBallsCount = 10;                //Balls count we start with
    public int bonusRingClearCombo = 3;             //Count of clear goals in a row to get big rinf bonus
    public int bonusAimCombo = 5;                   //Count of goals in a row to get aim bonus
    public int bonusAimMinXpLevel = 5;              //Minimum xp level to be able get aim bonus
    public int bonusRingThrowsLimit = 3;            //When you get big ring bonus after this count of throws it will gone
    public int bonusAimThrowsLimit = 3;             //When you get aim bonus after this count of throws it will gone 
    public int xpScoreStep = 100;					//XP step. With help of this you can tweak the speed of getting xp level.

    public Text ballsCountTxt;
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

    private int currentBallsCount;                  //Current amount of balls left to throw
    private int score;                              //Current score
    private int comboScore;                         //Current amount of combo score. Increases when you have goals in a row. Resets when you fail a ball.
    private int comboGoals;                         //Current quantity of usual goals got in a row. Increases when you have goals in a row. Resets when you fail a ball.
    private int comboClearGoals;                    //Current quantity of clear goals got in a row. Increases when you have clear goals in a row. Resets when you fail a ball.
    private int comboGoals_bonusRing;               //Current quantity of clear goals got in a row to open ring bonus. Increases when you have clear goals in a row. Resets when you fail a ball.
    private bool bonusRingActive;                   //Boolean to determine if ring bonus currently active or not
    private int bonusRingThrows;                    //Current quantity of balls thrown during ring bonus active
    private int comboGoals_bonusAim;                //Current quantity of goals got in a row to open aim bonus. Increases when you have goals in a row. Resets when you fail a ball.
    private bool bonusAimActive;                    //Boolean to determine if aim bonus currently active or not
    private int bonusAimThrows;                     //Current quantity of balls thrown during aim bonus active or not
    private bool bonusSuperBallActive;              //Boolean to determine if superball bonus active or not
    private float superBallProgress;                //Float that keeps current superball progress value
    private int xpLevel;                            //Int that defines current XP level 

    private bool hitRecord;							//Boolean that defines if we already hitted last best score or not

    Vector3 RandomPos;

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
        currentBallsCount = startBallsCount;
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
        comboGoals += 1;
        superBallProgress += 0.01f;
        if (!bonusAimActive) {
            if (xpLevel > bonusAimMinXpLevel)
                comboGoals_bonusAim += 1;
            if (comboGoals_bonusAim == bonusAimCombo) {
                bonusAimActive = true;
                plusDotsTxt.gameObject.SetActive(true);
                thisAudio.PlayOneShot(SoundController.data.bonusOpen);
            }
        }
        else {
            bonusAimThrows += 1;
            if (bonusAimThrows == bonusAimThrowsLimit) {
                bonusAimThrows = comboGoals_bonusAim = 0;
                bonusAimActive = false;
            }
        }

        if (bonusRingActive) {
            bonusRingThrows += 1;
            if (bonusRingThrows == bonusRingThrowsLimit) {
                comboGoals_bonusRing = 0;
                StartCoroutine(ResetRing());
            }
        }

        if (clear) {
            currentBallsCount += 1;
            ballIcon.ScaleImpulse(new Vector3(1.3f, 1.3f, 1), 0.4f, 2);
            plusBallTxt.gameObject.SetActive(true);
            comboClearGoals += 1;

            if (!bonusRingActive) {
                comboGoals_bonusRing += 1;
                if (comboGoals_bonusRing == bonusRingClearCombo) {
                    StartCoroutine(GrowRing());
                }
            }

            if (special)
                SoundController.data.playClearSpecialGoal();
            else
                SoundController.data.playClearGoal();
            superBallProgress += 0.01f;
        }
        else {
            SoundController.data.playGoal();
            comboClearGoals = comboGoals_bonusRing = 0;
        }

        comboScore += (int)distance;
        plusScoreTxt.text = "+" + comboScore.ToString("F0");

        if (special) {
            int heightScore = (int)height;
            comboScore += heightScore;
            plusScoreTxt.text += "\n+" + heightScore.ToString("F0");
            superBallProgress += 0.01f;
        }

        if (floored) {
            int flooredScore = (int)distance * 2;
            plusScoreTxt.text += "+" + flooredScore.ToString("F0");
            superBallProgress += 0.01f;               
        }

        plusScoreTxt.gameObject.SetActive(true);
        scoreTxt.gameObject.GetComponent<Transformer>().ScaleImpulse(new Vector3(1.3f, 1.3f, 1), 0.4f, 1);
        AddScore(comboScore);
        BallCompleted();
    }

    void Fail() {
        comboGoals = comboClearGoals = comboGoals_bonusRing = comboGoals_bonusAim = comboScore = 0;
        currentBallsCount -= 1;

        if (bonusAimActive) {
            bonusAimThrows += 1;
            if (bonusAimThrows == bonusAimThrowsLimit) {
                bonusAimThrows = comboGoals_bonusAim = 0;
                bonusAimActive = false;
            }
        }
               
        BallCompleted();
    }

    void BallCompleted() {
        xpLevel = score > 2 * xpScoreStep ? score / xpScoreStep : 1;
        UpdateBallsCount();
        UpdateSpawnCollider();
        shooter.newBallPosition = GetRandomPosInCollider();
        shooter.spawnBall();
    }

    IEnumerator GrowRing() {
        yield return new WaitForSeconds(0.5f);
        bonusRingActive = true;
        ring.SetActive(false);
        ring.transform.localPosition = new Vector3(1.9f, 9.51f, 0);
        ring.transform.localScale = new Vector3(2, 2, 2);
        ring.SetActive(true);
        thisAudio.PlayOneShot(SoundController.data.bonusOpen);
    }

    IEnumerator ResetRing() {
        yield return new WaitForSeconds(0.5f);
        bonusRingActive = false;
        ring.SetActive(false);
        ring.transform.localPosition = new Vector3(1.2f, 9.51f, 0);
        ring.transform.localScale = new Vector3(1, 1, 1);
        ring.SetActive(true);
        bonusRingThrows = 0;
    }

    public void AddScore(int score) {
        this.score += score;
        scoreTxt.text = this.score.ToString();        

    }

    void UpdateBallsCount() {
        ballsCountTxt.text = currentBallsCount.ToString();
        if (currentBallsCount < 1) {
            m_GameController.data.Complete();
        }
    }  

    void UpdateSpawnCollider() {
        float colLenght = Mathf.Clamp(19 + xpLevel, 20, 35);
        spawnCollider.gameObject.transform.position = new Vector3(colLenght / 2, 4, 0);
        Vector3 tempSize = spawnCollider.size;
        tempSize.x = colLenght;
        spawnCollider.size = tempSize;
    }

    public void ResetData() {
        currentBallsCount = startBallsCount;
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
