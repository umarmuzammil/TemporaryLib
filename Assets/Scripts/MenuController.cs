using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuController : MonoBehaviour {
	
	public GameObject panelSettings;
	public Toggle soundToggle;
	public Toggle inverseAimToggle;
	
	void Start(){
		soundToggle.isOn = PlayerPrefs.GetInt("sound", 1) == 1 ? true : false;
		AudioListener.volume = PlayerPrefs.GetInt("sound", 1);
		inverseAimToggle.isOn = PlayerPrefs.GetInt("inverseAim", 0) == 1 ? true : false;
	}
	
	void Update(){
		if (Input.GetKey(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
	
	public void StartArcadeMode(){
        SceneManager.LoadScene("gameArcade");
	}
	
	public void StartTimeAttackMode(){
        SceneManager.LoadScene("gameTimeAttack");
	}
	public void StartDistanceMode(){
        SceneManager.LoadScene("gameDistance");
	}
	
	public void switchSettings(){
		panelSettings.SetActive(!panelSettings.activeInHierarchy);
	}
	
	public void switchSound(){
		PlayerPrefs.SetInt("sound", PlayerPrefs.GetInt("sound", 1) == 1 ? 0 : 1);
		AudioListener.volume = PlayerPrefs.GetInt("sound", 1);
	}
	
	public void switchShadow(){
		PlayerPrefs.SetInt("inverseAim", PlayerPrefs.GetInt("inverseAim", 0) == 1 ? 0 : 1);
	}
}
