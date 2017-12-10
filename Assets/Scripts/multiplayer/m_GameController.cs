using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class m_GameController : MonoBehaviour {

	public static m_GameController data;
	
	public GameObject completePanel;
	public GameObject pausePanel;
	private m_Shooter shooter;
	public bool isPlaying;
	public enum State {InGame, Paused, Complete, StartUp}
	public State gameState;

	
	void Awake () {
		data = this;
		Time.timeScale = 1.5f;
		shooter = GameObject.Find("Shooter").GetComponent<m_Shooter>();
        //StartPlay();
        gameState = State.StartUp;
		AudioListener.volume = PlayerPrefs.GetInt("sound", 1);
	}
	
	void Start(){
		shooter.inverseAim = PlayerPrefs.GetInt("inverseAim", 0) == 1 ? true : false;
	}	
    
    	
	public void StartPlay(){       
        isPlaying = true;
        gameState = State.InGame;
		shooter.spawnBall();
		m_AdaptiveCamera.extraMode = false;
	}
	
	public void Complete(){
		completePanel.SetActive(true);
		isPlaying = false;
		gameState = State.Complete;
		SoundController.data.playGameOver();
	}
	
	public void Restart(){
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
	
	public void togglePause(){
		if(gameState == State.StartUp || gameState == State.Complete)
			return;
		isPlaying = !isPlaying;
		pausePanel.SetActive(!isPlaying);
		Time.timeScale = isPlaying ? 1.5f : 0;
		gameState = isPlaying ? State.InGame : State.Paused;
	}
	
	public void loadMenu(){
		Time.timeScale = 1.5f;
        SceneManager.LoadScene("Menu");
	}
	
	public void switchAim(){
		shooter.inverseAim =! shooter.inverseAim;
	}
	
	public void ClearPlayerPrefs(){
		PlayerPrefs.DeleteAll();
		 SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
	
}
