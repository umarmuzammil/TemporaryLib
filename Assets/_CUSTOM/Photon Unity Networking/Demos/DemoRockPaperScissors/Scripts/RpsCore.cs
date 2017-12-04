using System;
using System.Collections;
using Photon;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// the Photon server assigns a ActorNumber (player.ID) to each player, beginning at 1
// for this game, we don't mind the actual number
// this game uses player 0 and 1, so clients need to figure out their number somehow
public class RpsCore : PunBehaviour, IPunTurnManagerCallbacks
{
    #region SerializedVariables

    [SerializeField]
	private RectTransform ConnectUiView;

	[SerializeField]
	private RectTransform GameUiView;

	[SerializeField]
	private CanvasGroup ButtonCanvasGroup;

	[SerializeField]
	private RectTransform TimerFillImage;

    [SerializeField]
    private Text TurnText;

    [SerializeField]
    private Text TimeText;

    [SerializeField]
    private Text RemotePlayerText;

    [SerializeField]
    private Text LocalPlayerText;
    
    [SerializeField]
    private Image WinOrLossImage;


    [SerializeField]
    private Image localSelectionImage;
    public Hand localSelection;

    [SerializeField]
    private Image remoteSelectionImage;
    public Hand remoteSelection;
    
    [SerializeField]
    private Sprite SelectedRock;

    [SerializeField]
    private Sprite SelectedPaper;

    [SerializeField]
    private Sprite SelectedScissors;

    [SerializeField]
    private Sprite SpriteWin;

    [SerializeField]
    private Sprite SpriteLose;

    [SerializeField]
    private Sprite SpriteDraw;


    [SerializeField]
    private RectTransform DisconnectedPanel;


    #endregion

    private ResultType result;

    private PunTurnManager turnManager;

    public Hand randomHand;    // used to show remote player's "hand" while local player didn't select anything



	// keep track of when we show the results to handle game logic.
	private bool IsShowingResults;
	
    public enum Hand
    {
        None = 0,
        Rock,
        Paper,
        Scissors
    }

    public enum ResultType
    {
        None = 0,
        Draw,
        LocalWin,
        LocalLoss
    }

    public void Start()
    {
		turnManager = gameObject.AddComponent<PunTurnManager>();
        turnManager.TurnManagerListener = this;
        turnManager.TurnDuration = 5f;
        

        localSelectionImage.gameObject.SetActive(false);
        remoteSelectionImage.gameObject.SetActive(false);
        StartCoroutine("CycleRemoteHandCoroutine");

		RefreshUIViews();
    }

    public void Update()
    {
		// Check if we are out of context, which means we likely got back to the demo hub.
		if (DisconnectedPanel ==null)
		{
			Destroy(gameObject);
		}

        // for debugging, it's useful to have a few actions tied to keys:
        if (Input.GetKeyUp(KeyCode.L))
        {
            PhotonNetwork.LeaveRoom();
        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            PhotonNetwork.ConnectUsingSettings(null);
            PhotonHandler.StopFallbackSendAckThread();
        }

	
        if ( ! PhotonNetwork.inRoom)
        {
			return;
		}

		// disable the "reconnect panel" if PUN is connected or connecting
		if (PhotonNetwork.connected && DisconnectedPanel.gameObject.GetActive())
		{
			DisconnectedPanel.gameObject.SetActive(false);
		}
		if (!PhotonNetwork.connected && !PhotonNetwork.connecting && !DisconnectedPanel.gameObject.GetActive())
		{
			DisconnectedPanel.gameObject.SetActive(true);
		}


		if (PhotonNetwork.room.PlayerCount>1)
		{
			if (turnManager.IsOver)
			{
				return;
			}

			/*
			// check if we ran out of time, in which case we loose
			if (turnEnd<0f && !IsShowingResults)
			{
					Debug.Log("Calling OnTurnCompleted with turnEnd ="+turnEnd);
					OnTurnCompleted(-1);
					return;
			}
		*/

            if (TurnText != null)
            {
                TurnText.text = turnManager.Turn.ToString();
            }

			if (turnManager.Turn > 0 && TimeText != null && ! IsShowingResults)
            {
                
				TimeText.text = turnManager.RemainingSecondsInTurn.ToString("F1") + " SECONDS";

				TimerFillImage.anchorMax = new Vector2(1f- turnManager.RemainingSecondsInTurn/turnManager.TurnDuration,1f);
            }

            
		}

		UpdatePlayerTexts();

        // show local player's selected hand
        Sprite selected = SelectionToSprite(localSelection);
        if (selected != null)
        {
            localSelectionImage.gameObject.SetActive(true);
            localSelectionImage.sprite = selected;
        }

        // remote player's selection is only shown, when the turn is complete (finished by both)
        if (turnManager.IsCompletedByAll)
        {
            selected = SelectionToSprite(remoteSelection);
            if (selected != null)
            {
                remoteSelectionImage.color = new Color(1,1,1,1);
                remoteSelectionImage.sprite = selected;
            }
        }
        else
        {
			ButtonCanvasGroup.interactable = PhotonNetwork.room.PlayerCount > 1;

            if (PhotonNetwork.room.PlayerCount < 2)
            {
                remoteSelectionImage.color = new Color(1, 1, 1, 0);
            }

            // if the turn is not completed by all, we use a random image for the remote hand
            else if (turnManager.Turn > 0 && !turnManager.IsCompletedByAll)
            {
                // alpha of the remote hand is used as indicator if the remote player "is active" and "made a turn"
                PhotonPlayer remote = PhotonNetwork.player.GetNext();
                float alpha = 0.5f;
                if (turnManager.GetPlayerFinishedTurn(remote))
                {
                    alpha = 1;
                }
                if (remote != null && remote.IsInactive)
                {
                    alpha = 0.1f;
                }

                remoteSelectionImage.color = new Color(1, 1, 1, alpha);
                remoteSelectionImage.sprite = SelectionToSprite(randomHand);
            }
        }

    }

    #region TurnManager Callbacks

    /// <summary>Called when a turn begins (Master Client set a new Turn number).</summary>
    public void OnTurnBegins(int turn)
    {
        Debug.Log("OnTurnBegins() turn: "+ turn);
        localSelection = Hand.None;
        remoteSelection = Hand.None;

        WinOrLossImage.gameObject.SetActive(false);

        localSelectionImage.gameObject.SetActive(false);
        remoteSelectionImage.gameObject.SetActive(true);

		IsShowingResults = false;
		ButtonCanvasGroup.interactable = true;
    }


    public void OnTurnCompleted(int obj)
    {
        Debug.Log("OnTurnCompleted: " + obj);

        CalculateWinAndLoss();
        UpdateScores();
        OnEndTurn();
    }


    // when a player moved (but did not finish the turn)
    public void OnPlayerMove(PhotonPlayer photonPlayer, int turn, object move)
    {
        Debug.Log("OnPlayerMove: " + photonPlayer + " turn: " + turn + " action: " + move);
        throw new NotImplementedException();
    }


    // when a player made the last/final move in a turn
    public void OnPlayerFinished(PhotonPlayer photonPlayer, int turn, object move)
    {
        Debug.Log("OnTurnFinished: " + photonPlayer + " turn: " + turn + " action: " + move);

        if (photonPlayer.IsLocal)
        {
            localSelection = (Hand)(byte)move;
        }
        else
        {
            remoteSelection = (Hand)(byte)move;
        }
    }

    #endregion

    public void OnTurnTimeEnds(int obj)
    {
		if (!IsShowingResults)
		{
			Debug.Log("OnTurnTimeEnds: Calling OnTurnCompleted");
			OnTurnCompleted(-1);
		}
	}

    private void UpdateScores()
    {
        if (result == ResultType.LocalWin)
        {
            PhotonNetwork.player.AddScore(1);   // this is an extension method for PhotonPlayer. you can see it's implementation
        }
    }

 

    #region Core Gameplay Methods
    
    /// <summary>Call to start the turn (only the Master Client will send this).</summary>
    public void StartTurn()
    {
        if (PhotonNetwork.isMasterClient)
        {
            turnManager.BeginTurn();
        }
    }
	
    public void MakeTurn(Hand selection)
    {
        turnManager.SendMove((byte)selection, true);
    }
	
    public void OnEndTurn()
    {
        StartCoroutine("ShowResultsBeginNextTurnCoroutine");
    }

    public IEnumerator ShowResultsBeginNextTurnCoroutine()
    {
		ButtonCanvasGroup.interactable = false;
		IsShowingResults = true;
       // yield return new WaitForSeconds(1.5f);

        if (result == ResultType.Draw)
        {
            WinOrLossImage.sprite = SpriteDraw;
        }
        else
        {
            WinOrLossImage.sprite = result == ResultType.LocalWin ? SpriteWin : SpriteLose;
        }
        WinOrLossImage.gameObject.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        StartTurn();
    }

    public void EndGame()
    {
		Debug.Log("EndGame");
    }

    private void CalculateWinAndLoss()
    {
        result = ResultType.Draw;
        if (localSelection == remoteSelection)
        {
            return;
        }

		if (localSelection == Hand.None)
		{
			result = ResultType.LocalLoss;
			return;
		}

		if (remoteSelection == Hand.None)
		{
			result = ResultType.LocalWin;
		}
        
        if (localSelection == Hand.Rock)
        {
            result = (remoteSelection == Hand.Scissors) ? ResultType.LocalWin : ResultType.LocalLoss;
        }
        if (localSelection == Hand.Paper)
        {
            result = (remoteSelection == Hand.Rock) ? ResultType.LocalWin : ResultType.LocalLoss;
        }

        if (localSelection == Hand.Scissors)
        {
            result = (remoteSelection == Hand.Paper) ? ResultType.LocalWin : ResultType.LocalLoss;
        }
    }

    private Sprite SelectionToSprite(Hand hand)
    {
        switch (hand)
        {
            case Hand.None:
                break;
            case Hand.Rock:
                return SelectedRock;
            case Hand.Paper:
                return SelectedPaper;
            case Hand.Scissors:
                return SelectedScissors;
        }

        return null;
    }

    private void UpdatePlayerTexts()
    {
        PhotonPlayer remote = PhotonNetwork.player.GetNext();
        PhotonPlayer local = PhotonNetwork.player;

        if (remote != null)
        {
            // should be this format: "name        00"
            RemotePlayerText.text = remote.NickName + "        " + remote.GetScore().ToString("D2");
        }
        else
        {

			TimerFillImage.anchorMax = new Vector2(0f,1f);
			TimeText.text = "";
            RemotePlayerText.text = "waiting for another player        00";
        }
        
        if (local != null)
        {
            // should be this format: "YOU   00"
            LocalPlayerText.text = "YOU   " + local.GetScore().ToString("D2");
        }
    }

    public IEnumerator CycleRemoteHandCoroutine()
    {
        while (true)
        {
            // cycle through available images
            randomHand = (Hand)Random.Range(1, 4);
            yield return new WaitForSeconds(0.5f);
        }
    }

    #endregion


    #region Handling Of Buttons

    public void OnClickRock()
    {
        MakeTurn(Hand.Rock);
    }

    public void OnClickPaper()
    {
       MakeTurn(Hand.Paper);
    }

    public void OnClickScissors()
    {
        MakeTurn(Hand.Scissors);
    }

    public void OnClickConnect()
    {
        PhotonNetwork.ConnectUsingSettings(null);
        PhotonHandler.StopFallbackSendAckThread();  // this is used in the demo to timeout in background!
    }
    
    public void OnClickReConnectAndRejoin()
    {
        PhotonNetwork.ReconnectAndRejoin();
        PhotonHandler.StopFallbackSendAckThread();  // this is used in the demo to timeout in background!
    }

    #endregion

	void RefreshUIViews() ////// Turn of Connect panel, enables game panel and  enables buttons if players are more than 1. 
	{
		TimerFillImage.anchorMax = new Vector2(0f,1f);

		ConnectUiView.gameObject.SetActive(!PhotonNetwork.inRoom);
		GameUiView.gameObject.SetActive(PhotonNetwork.inRoom);

		ButtonCanvasGroup.interactable = PhotonNetwork.room!=null?PhotonNetwork.room.PlayerCount > 1:false;
	}


    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom()");



		RefreshUIViews();
    } //Does Fucking nothing 

    public override void OnJoinedRoom()
    {
		RefreshUIViews();

        if (PhotonNetwork.room.PlayerCount == 2)
        {
            if (turnManager.Turn == 0)
            {
                // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
                StartTurn();
            }
        }
        else
        {
            Debug.Log("Waiting for another player");
        }
    }   //Start turn if players are exactly equal to 1

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
		Debug.Log("Other player arrived");

        if (PhotonNetwork.room.PlayerCount == 2)
        {
            if (turnManager.Turn == 0)
            {
                // when the room has two players, start the first turn (later on, joining players won't trigger a turn)
                StartTurn();
            }
        }
    }


    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
		Debug.Log("Other player disconnected! "+otherPlayer.ToStringFull());
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        DisconnectedPanel.gameObject.SetActive(true);
    }

}
