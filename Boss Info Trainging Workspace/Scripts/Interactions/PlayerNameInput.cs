using UnityEngine;
using TMPro;
using System.Collections;
using System;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using MUXR.Interactables;
using System.Collections.Generic;
using Fusion;
using MUXR.Networking;

public class PlayerNameInput : NetworkBehaviour
{
    #region ----Fields----
    #region Event Buttons
    public EventButton CycleUp;
    public EventButton CycleDown;
    public EventButton confirmLetterButton;
    public EventButton backspaceButton;
    public EventButton startButton;
    public EventButton resetButton;
    public EventButton chessMenuButton;
    public EventButton ResetChessButton;
    public EventButton puzzleButton;
    public EventButton chessButton;
    public EventButton returnButton;
    #endregion Event Buttons

    #region Puzzle
    public List<GameObject> puzzleParentObjects;
    private GameObject currentPuzzleParent;
    private int currentPuzzleIndex = -1;

    [Networked(OnChangedTargets = OnChangedTargets.All)] public NetworkObject CurrentPuzzle { get; set; }
    [Networked(OnChangedTargets = OnChangedTargets.All)] private int CurrentIndex { get; set; }
    [Networked(OnChangedTargets = OnChangedTargets.All)] public bool PuzzleActive { get; set; }
    #endregion Puzzle

    #region Chess
    public GameObject chessObject;
    private bool isButtonHeld = false;
    [Networked()] public NetworkBool ChessState { get; set; }
    public UnityEvent StartChess;
    public GameObject chessPiecesParent; // Reference to the parent object that holds all the chess pieces
    private List<Transform> chessPieces; // List to store references to all the chess pieces
    private List<Vector3> startingPositions; // List to store the starting positions of all the chess pieces
    private List<Quaternion> startingRotations; // List to store the starting rotations of all the chess pieces
    public GameObject[] chessButtonObjects;
    public GameObject[] puzzleButtonObjects;
    public GameObject puzzleButtonObject;
    public GameObject chessButtonObject;

    #endregion Chess

    #region Name 
    [Networked()] public string CurrentName { get; set; }
    private string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    #endregion Name 

    #region UI
    public TextMeshProUGUI displayText;
    #endregion UI

    #region Coroutines
    private Coroutine autoAdvanceCoroutine;
    private Coroutine autoReverseAdvanceCoroutine;
    #endregion Coroutines

    #region Timer
    [Networked()] public float Timer { get; set; }
    private bool timerRunning = false;
    #endregion Timer

    #region Completion
    public UnityEvent CompletedEvent;
    public UnityEvent LocalEffects;
    public UnityEvent ResetEvent;
    public HighscoreController highscoreController;
    #endregion Completion
    #endregion ----Fields----

    #region ----Methods----
    #region Init
    private void Start() //set listeners for all push buttons on arcade style controller
    {
        CycleUp.onPressed.AddListener(RPC_NextLetter);
        CycleUp.onPressed.AddListener(RPC_StartAutoAdvance);
        CycleUp.onRealeased.AddListener(RPC_StopAutoAdvance);
        CycleDown.onPressed.AddListener(RPC_PreviousLetter);
        CycleDown.onPressed.AddListener(RPC_StartAutoReverseAdvance);
        CycleDown.onRealeased.AddListener(RPC_StopAutoReverseAdvance);
        confirmLetterButton.onPressed.AddListener(RPC_ConfirmLetter);
        backspaceButton.onPressed.AddListener(RPC_BackspaceLetter);
        chessButton.onPressed.AddListener(RPC_ActivateChessObject);
        //chessButton.onRealeased.AddListener(RPC_DeactivateChessObject);
        startButton.onPressed.AddListener(StartPuzzle);
        resetButton.onPressed.AddListener(RPC_ResetPuzzle);
        ResetChessButton.onPressed.AddListener(RPC_ResetChessObjects);
        puzzleButton.onPressed.AddListener(RPC_ShowPuzzleButtons);
        chessMenuButton.onPressed.AddListener(RPC_ShowChessButtons);
        returnButton.onPressed.AddListener(RPC_DeactivateAllButtons);
        returnButton.onPressed.AddListener(RPC_DeactivateChessObject);
        returnButton.onPressed.AddListener(RPC_ResetPuzzle); //this could cause trolling.

        // Get references to all the chess pieces
        //chessPieces = new List<Transform>(chessPiecesParent.GetComponentsInChildren<Transform>());
        //chessPieces.Remove(chessPiecesParent.transform);

        // Store the starting positions of all the chess pieces
        startingPositions = new List<Vector3>();
        startingRotations = new List<Quaternion>();
        //foreach (Transform chessPiece in chessPieces)
        //{
        //    startingPositions.Add(chessPiece.position);
        //    startingRotations.Add(chessPiece.rotation);
        //}

        //if (CurrentPuzzle != null)
        //    CurrentPuzzle.gameObject.SetActive(PuzzleActive);

        displayText.text = new string('-', 3);
        currentPuzzleIndex = -1;
    }

    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    private void Init(GameObject originalGameObject)
    {
        PlayerNameInput originalPlayerNameInput = originalGameObject.GetComponent<PlayerNameInput>();
        this.highscoreController = originalPlayerNameInput.highscoreController;
        InitCurrentPuzzle(originalPlayerNameInput);


        if (Object.HasStateAuthority)
        {
            PuzzleActive = false;
            ChessState = false;
        }
        else
        {
            if (PuzzleActive)
            {
                //Sync state with new client
                ResetPuzzle(false);
                currentPuzzleParent = CurrentPuzzle.transform.parent.gameObject;
                CurrentPuzzle.GetComponent<CubeController>().shouldShowInit = false;
                CurrentPuzzle.GetComponent<Animator>().enabled = false;
                currentPuzzleParent.SetActive(true);

                timerRunning = true;
                LocalEffects?.Invoke();
            }
        }

        //chessObject.SetActive(ChessState);
        displayText.text = CurrentName + alphabet[CurrentIndex].ToString() + new string('-', 3 - CurrentName.Length);
    }

    private void InitCurrentPuzzle(PlayerNameInput originalPlayerNameInput)
    {
        puzzleParentObjects = new List<GameObject>();
        foreach (var puzzleObject in originalPlayerNameInput.puzzleParentObjects)
            puzzleParentObjects.Add(puzzleObject);
    }
    #endregion Init

    #region ButtonLogic
    public void ShowInitialButtons()
    {
        StartCoroutine(ShowInitialButtonsCoroutine());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ShowChessButtons()
    {
        StartCoroutine(ShowChessButtonsCoroutine());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ShowPuzzleButtons()
    {
        StartCoroutine(ShowPuzzleButtonsCoroutine());
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_DeactivateAllButtons()
    {
        StartCoroutine(DeactivateAllButtonsCoroutine());
    }

    private IEnumerator ShowInitialButtonsCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SetGameObjectsActive(chessButtonObjects, false);
        SetGameObjectsActive(puzzleButtonObjects, false);
        returnButton.gameObject.SetActive(false);
        puzzleButtonObject.SetActive(true);
        chessButtonObject.SetActive(true);
    }

    private IEnumerator ShowChessButtonsCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SetGameObjectsActive(puzzleButtonObjects, false);
        SetGameObjectsActive(chessButtonObjects, true);
        returnButton.gameObject.SetActive(true);
        puzzleButtonObject.SetActive(false);
        chessButtonObject.SetActive(false);
    }

    private IEnumerator ShowPuzzleButtonsCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SetGameObjectsActive(chessButtonObjects, false);
        SetGameObjectsActive(puzzleButtonObjects, true);
        returnButton.gameObject.SetActive(true);
        puzzleButtonObject.SetActive(false);
        chessButtonObject.SetActive(false);
    }

    private IEnumerator DeactivateAllButtonsCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        SetGameObjectsActive(chessButtonObjects, false);
        SetGameObjectsActive(puzzleButtonObjects, false);
        returnButton.gameObject.SetActive(false);
        puzzleButtonObject.SetActive(true);
        chessButtonObject.SetActive(true);
    }


    private void SetGameObjectsActive(GameObject[] gameObjects, bool isActive)
    {
        for (int i = 0; i < gameObjects.Length; i++)
            gameObjects[i].SetActive(isActive);
    }
    #endregion ButtonLogic

    #region Timer
    private void Update()
    {
        if (timerRunning)
        {
            Timer += Time.deltaTime;
            displayText.text = CurrentName + " - " + Timer.ToString("0");
        }
    }

    public void StopTimer()
    {
        timerRunning = false;
        PuzzleActive = false;
    }
    #endregion Timer

    #region Name
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_NextLetter()
    {
        NextLetter();
    }

    private void NextLetter()
    {
        if (!PuzzleActive && CurrentName.Length < 3)
        {
            CurrentIndex = (CurrentIndex + 1) % alphabet.Length;
            RPC_ClientShowName(CurrentName, CurrentIndex);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PreviousLetter()
    {
        PreviousLetter();
    }

    private void PreviousLetter()
    {
        if (!PuzzleActive && CurrentName.Length < 3)
        {
            CurrentIndex = ((CurrentIndex - 1) + alphabet.Length) % alphabet.Length;
            RPC_ClientShowName(CurrentName, CurrentIndex);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConfirmLetter()
    {
        if (!PuzzleActive && CurrentName.Length < 3)
        {
            CurrentName += alphabet[CurrentIndex];
            CurrentIndex = 0;
            RPC_ClientShowName(CurrentName, CurrentIndex);

            if (CurrentName.Length == 3)
                Debug.Log("Player name: " + CurrentName);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_BackspaceLetter()
    {
        if (!PuzzleActive && CurrentName.Length > 0)
        {
            CurrentName = CurrentName.Substring(0, CurrentName.Length - 1);
            CurrentIndex = 0; // set to the index of 'A'
            RPC_ClientShowName(CurrentName, CurrentIndex);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClientShowName(string currentName, int currentIndex)
    {
        displayText.text = currentName + (currentName.Length < 3 ? alphabet[currentIndex].ToString() + new string('-', 2 - currentName.Length) : "");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartAutoAdvance()
    {
        if (!PuzzleActive)
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StopAutoAdvance()
    {
        if (!PuzzleActive && autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
    }

    private IEnumerator AutoAdvanceCoroutine()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            NextLetter();
            yield return new WaitForSeconds(0.3f);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartAutoReverseAdvance()
    {
        if (!PuzzleActive)
            autoReverseAdvanceCoroutine = StartCoroutine(AutoReverseAdvanceCoroutine());
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StopAutoReverseAdvance()
    {
        if (!PuzzleActive && autoReverseAdvanceCoroutine != null)
        {
            StopCoroutine(autoReverseAdvanceCoroutine);
            autoReverseAdvanceCoroutine = null;
        }
    }


    private IEnumerator AutoReverseAdvanceCoroutine()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            PreviousLetter();
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void ResetName()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentIndex = 0;
        CurrentName = "";
        RPC_ClientShowName(CurrentName, CurrentIndex);
    }
    #endregion Name

    #region Game Flow
    #region Start
    [ContextMenu("Start")]
    private void StartPuzzle()
    {
        if (FusionManager.Runner.GameMode != GameMode.Server && (!(CurrentName.Length == 3) || PuzzleActive))
        {
            Debug.Log("Either name not complete or a puzzle is active!");
            return;
        }

        RPC_ServerStartGame();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ServerStartGame()
    {
        ResetPuzzle(false);

        currentPuzzleIndex = currentPuzzleIndex + 1 < puzzleParentObjects.Count ? currentPuzzleIndex + 1 : 0;
        if (currentPuzzleIndex < 0) currentPuzzleIndex = 0;

        currentPuzzleParent = puzzleParentObjects[currentPuzzleIndex];//can we use this to trigger ActivatePuzzleGroup in list manager?
        currentPuzzleParent.SetActive(true);
        CurrentPuzzle = currentPuzzleParent.GetComponentInChildren<CubeController>().GetComponent<NetworkObject>();
        CurrentPuzzle.gameObject.SetActive(true);
        highscoreController.UpdateCurrentLevel(currentPuzzleIndex);

        PuzzleActive = true;
        RPC_ClientStartGame(currentPuzzleIndex);
        RPC_CheckChildGrabbables();
        RPC_PlayEffects();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CheckChildGrabbables()
    {
        List<SocketGrabbable> childGrababbles = new List<SocketGrabbable>();
        foreach (Transform child in currentPuzzleParent.GetComponentInChildren<CubeController>().transform)
        {
            SocketGrabbable grabbable;
            if (child.gameObject.TryGetComponent<SocketGrabbable>(out grabbable))
                childGrababbles.Add(grabbable);
        }

        foreach (SocketGrabbable childGrabbable in childGrababbles)
        {

            var colliders = childGrabbable.GetComponentsInChildren<Collider>();

            foreach (var collider in colliders)
                collider.gameObject.SetActive(false);

            StartCoroutine(WaitForSeconds(5, () =>
            {
                foreach (var collider in colliders)
                    collider.gameObject.SetActive(true);
            }));

            if (!Object.HasStateAuthority)
                return;
            childGrabbable.isCompleted = false;
            childGrabbable.OnCompleted = null;
            childGrabbable.OnCompleted += () =>
            {
                foreach (SocketGrabbable check in childGrababbles)
                    if (!check.isCompleted)
                        return;
                childGrabbable.OnCompleted = null;
                highscoreController.SetScore(CurrentName,
                                             Mathf.FloorToInt(Timer),
                                             currentLevel: (((int)highscoreController.currentLevel) + 1),
                                             onUpdated: () => RPC_UpdateHighscore());
                RPC_CompletedPuzzle();
            };
        }
    }
    public void CheckCompletion()
    {

    }

    IEnumerator WaitForSeconds(float seconds, Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();

    }

    #region ChessFunction
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ActivateChessObject()
    {

        ChessState = true;
        ActivateChessObject();
        //isButtonHeld = true;
        //StartCoroutine(CheckButtonHold());
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ResetChessObjects()
    {
        ResetChessPieces();
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_DeactivateChessObject()
    {
        ChessState = false;
        DeactivateChessObject();
        //isButtonHeld = false;
        //if (chessObject.activeSelf)

    }

    //private IEnumerator CheckButtonHold()
    //{
    //    yield return new WaitForSeconds(1f);  // wait for 1 second before checking

    //    if (isButtonHeld)
    //    {
    //        DeactivateChessObject();
    //    }
    //    else if (!chessObject.activeSelf)
    //    {
    //        ActivateChessObject();
    //    }
    //}

    private void ActivateChessObject()
    {
        if (chessObject != null)
        {
            StartChess.Invoke();
            chessObject.SetActive(true);
        }
    }

    private void DeactivateChessObject()
    {
        if (chessObject != null)
        {
            chessObject.SetActive(false);
        }
    }

    public void ResetChessPieces()
    {
        // Reset the positions of all the chess pieces to their starting positions
        for (int i = 0; i < chessPieces.Count; i++)
        {
            chessPieces[i].position = startingPositions[i];
            chessPieces[i].rotation = startingRotations[i];
        }
    }
    #endregion
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_UpdateHighscore()
    {
        //TODO: Improve this
        FindObjectOfType<HighscoreController>().UpdateAllScoreViews();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ClientStartGame(int selectedIndex)
    {
        if (!Object.HasStateAuthority)
            ResetPuzzle(false);
        currentPuzzleParent = CurrentPuzzle.transform.parent.gameObject;
        currentPuzzleParent.SetActive(true);
        CurrentPuzzle.gameObject.SetActive(true);
        CurrentPuzzle.GetComponent<Animator>().enabled = true;
        highscoreController.UpdateCurrentLevel(selectedIndex);

        for (int i = 0; i < puzzleParentObjects.Count; i++)
            if (i != selectedIndex)
                puzzleParentObjects[i].SetActive(false);

        Timer = -3f;
        timerRunning = true;
    }
    #endregion Start
    [ContextMenu("Completion")]
    public void TestCompletion()
    {
        CompletedEvent.Invoke();
    }
    #region Complete

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CompletedPuzzle()
    {
        PuzzleComplete();
        ToggleEmission();
        Debug.Log(GetCompletionMessage());
    }

    public void PuzzleComplete()
    {
        // Call this method from your other script when the puzzle is completed
        if (timerRunning)
        {
            StopTimer();

            // Display the final time
            Debug.Log(CurrentName + " completed the puzzle in " + Timer.ToString("0.00") + " seconds!");

            // Reset the timer
            Timer = 0f;

            CompletedEvent.Invoke();
        }
    }
    public string GetCompletionMessage()  //for daniel to grab the complete score and put to networked scoreboard. 
    {
        if (!(CurrentName.Length == 3) || Timer == 0f)
        {
            Debug.Log("Either name not complete or timer not started!");
            return "";
        }

        return CurrentName + " completed the puzzle in " + Timer.ToString("0.00") + " seconds!";
    }

    public void DeactivateCurrentPuzzleAfterComplete()
    {
        StartCoroutine(DeactivateCurrentPuzzleCoroutine());
    }

    private IEnumerator DeactivateCurrentPuzzleCoroutine()
    {
        yield return new WaitForSeconds(10f);  // wait for 10 seconds before deactivating to match particles and animations
        if (currentPuzzleParent != null)
            currentPuzzleParent.SetActive(false);
    }

    #endregion Complete

    #region Restart
    [ContextMenu("Restart")]

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ResetPuzzle()
    {
        ResetPuzzle();
    }

    private void ResetPuzzle(bool resetName = true)
    {
        if (currentPuzzleParent != null)
        {
            currentPuzzleParent.SetActive(false);
            currentPuzzleParent = null;
        }

        StopTimer();

        // Reset the timer
        Timer = 0f;

        if (resetName)
            ResetName();

        ResetEvent.Invoke();
    }
    #endregion Restart

    #region VFX

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayEffects()
    {
        LocalEffects?.Invoke();
        CurrentPuzzle.GetComponent<Animator>().SetTrigger("Puzzle Start");
    }
    public void ToggleEmission()
    {

        Renderer renderer = CurrentPuzzle.GetComponentInChildren<MeshRenderer>();
        Material material = renderer.material;


        material.EnableKeyword("_EMISSION");
        StartCoroutine(DisableEmissionAfterSeconds(30));
    }
    IEnumerator DisableEmissionAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        // Disable emission
        Renderer renderer = CurrentPuzzle.GetComponentInChildren<MeshRenderer>();
        Material material = renderer.material;
        material.DisableKeyword("_EMISSION");
    }
    #endregion VFX
    #endregion Game Flow
    #endregion ----Methods----
}
