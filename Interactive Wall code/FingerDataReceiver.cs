using UnityEngine;
using UnityEngine.Video;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;

public class FingerDataReceiver : MonoBehaviour
{
    // =========================================================
    // ======================= VIDEOS ==========================
    // =========================================================
    public VideoPlayer videoPlayer;
    public VideoClip sleepVideo, wakeVideo, talkVideo, walkToBigVideo, bigRobotVideo, finalVideo;

    // Body reaction videos
    public VideoClip headReactionVideo;
    public VideoClip chestReactionVideo;
    public VideoClip rightArmReactionVideo;
    public VideoClip leftArmReactionVideo;
    public VideoClip baseReactionVideo;

    // =========================================================
    // ======================== AUDIO ==========================
    // =========================================================

    [Header("Interact Video Audio")]
    public AudioSource audioSource;
    public AudioClip interactAudio1;
    public AudioClip interactAudio2;
    public AudioClip interactAudio3;
    public AudioClip interactAudio4;

    [Header("Interact Audio Timings")]
    public float audioTime1 = 1.5f;
    public float audioTime2 = 4f;
    public float audioTime3 = 7f;
    public float audioTime4 = 10f;

    bool a1Played, a2Played, a3Played, a4Played;

    [Header("Big + Small Robot Audio")]
    public AudioClip bigRobotAudio1;
    public AudioClip smallRobotAudio1;
    public AudioClip bigRobotAudio2;
    public AudioClip smallRobotAudio2;
    public AudioClip bigRobotAudio3;
    public AudioClip smallRobotAudio3;
    public AudioClip bigRobotAudio4;
    public AudioClip bigRobotAudio5;
    public AudioClip bigRobotAudio6;
    public AudioClip bigRobotAudio7;

    // =========================================================
    // ======================== TEXT ===========================
    // =========================================================
    public TextMeshProUGUI promptTMP, wakeTMP, talkTMP, interactTMP;
    public TextMeshProUGUI smallRobotTMP, bigRobotTMP, BigRobotText, finalTMP;

    [TextArea] public string talkText1;
    [TextArea] public string talkText2;

    [TextArea] public string walkText1, walkText2, walkText3, walkText4;

    [TextArea] public string headText, chestText, rightArmText, leftArmText, baseText;

    // =========================================================
    // ===================== TOUCH ZONES =======================
    // =========================================================
    public RectTransform wakeTouchZone, talkTouchZone, interactTouchZone;
    public RectTransform HeadTouchZone, ChestTouchZone, RightArmTouchZone, LeftArmTouchZone;
    public RectTransform BaseTextZone, FinalTouchZone;
    public Canvas canvas;

    public GameObject touchZoneHighlightPrefab;

    // =========================================================
    // =================== BODY PART AUDIO =====================
    // =========================================================
    public AudioClip headAudio;
    public AudioClip chestAudio;
    public AudioClip rightArmAudio;
    public AudioClip leftArmAudio;
    public AudioClip baseAudio;

    // =========================================================
    // ======================== UDP ============================
    // =========================================================
    UdpClient udpClient;
    Thread receiveThread;

    float fingerX, fingerY, fingerZ;

    // ✅ EXACT WALL TOUCH RANGE (FROM YOUR DATA)


    bool isTouchingWall;
    bool wasTouchingWall;

    // =========================================================
    // ====================== STATE ============================
    // =========================================================
    enum RobotState { Sleep, Wake, Talk, Walk, BigRobot, Final }
    RobotState currentState = RobotState.Sleep;

    bool touchLocked;
    bool headDone, chestDone, rightDone, leftDone, baseShown;
    bool videoCompletedOnce;
    bool finalMessageStarted;

    // Big robot internal control
    double bigRobotInteractionTime;
    bool playingBodyVideo;
    VideoClip currentBodyVideo;

    // =========================================================
    // ======================= START ===========================
    // =========================================================
    void Start()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
        InitializeSleepState();

        udpClient = new UdpClient(5053);
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // =========================================================
    // ===================== VIDEO PLAY ========================
    // =========================================================
    void PlayVideo(VideoClip clip, bool loop = false)
    {
        ResetInteractAudio();
        videoCompletedOnce = false;

        videoPlayer.Stop();

        // Embedded audio only for base videos
        if (clip == sleepVideo || clip == wakeVideo || clip == talkVideo)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetDirectAudioVolume(0, 1f);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        videoPlayer.isLooping = loop;
        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    // =========================================================
    // ======================= AUDIO ===========================
    // =========================================================
    void ResetInteractAudio()
    {
        a1Played = a2Played = a3Played = a4Played = false;
        if (audioSource != null) audioSource.Stop();
    }

    void HandleInteractAudio()
    {
        if (currentState != RobotState.Walk || !videoPlayer.isPlaying) return;

        double t = videoPlayer.time;
        if (!a1Played && t >= audioTime1) { PlayAudio(interactAudio1); a1Played = true; }
        if (!a2Played && t >= audioTime2) { PlayAudio(interactAudio2); a2Played = true; }
        if (!a3Played && t >= audioTime3) { PlayAudio(interactAudio3); a3Played = true; }
        if (!a4Played && t >= audioTime4) { PlayAudio(interactAudio4); a4Played = true; }
    }

    void PlayAudio(AudioClip clip)
    {
        if (!clip || !audioSource) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    // =========================================================
    // ======================= UPDATE ==========================
    // =========================================================
    void Update()
    {

        HandleInteractAudio();
        if (touchLocked) return;

        Vector2 screenPos = new Vector2(
            (1f - fingerX) * Screen.width,
            (1f - fingerY) * Screen.height
        );

        switch (currentState)
        {
            case RobotState.Sleep:
                if (IsWallTouchInside(wakeTouchZone, screenPos))
                    StartWake();
                break;

            case RobotState.Wake:
                if (IsWallTouchInside(talkTouchZone, screenPos))
                    StartTalk();
                break;

            case RobotState.Talk:
                if (IsWallTouchInside(interactTouchZone, screenPos) && !wasTouchingWall)
                    StartWalk();
                break;

            case RobotState.Walk:
                if (IsWallTouchInside(interactTouchZone, screenPos) && !wasTouchingWall)
                    StartBigRobot();
                break;

            case RobotState.BigRobot:
                HandleBigRobotTouches(screenPos);
                break;
        }

        // ✅ EDGE TRACKING
        wasTouchingWall = isTouchingWall;
    }

    // =========================================================
    // =================== STATE METHODS =======================
    // =========================================================
    void StartWake()
    {
        wasTouchingWall = false;
        touchLocked = true;

        DisableAllTexts();
        DisableAllTouchZones();

        wakeTMP.gameObject.SetActive(true);
        wakeTMP.text = "System boot sequence initiated…";

        currentState = RobotState.Wake;
        PlayVideo(wakeVideo);
        StartCoroutine(WakeTextSequence());
    }

    void StartTalk()
    {
        wasTouchingWall = false;
        touchLocked = true;

        DisableAllTexts();
        DisableAllTouchZones();

        talkTMP.gameObject.SetActive(true);
        currentState = RobotState.Talk;

        PlayVideo(talkVideo);
        StartCoroutine(TalkTextSequence());
    }

    void StartWalk()
    {
        wasTouchingWall = false;
        touchLocked = true;

        DisableAllTexts();
        DisableAllTouchZones();

        interactTMP.gameObject.SetActive(true);
        currentState = RobotState.Walk;

        PlayVideo(walkToBigVideo);
        StartCoroutine(WalkTextSequence());
    }

    void StartBigRobot()
    {
        wasTouchingWall = false;
        touchLocked = true;

        DisableAllTexts();
        DisableAllTouchZones();

        currentState = RobotState.BigRobot;
        PlayVideo(bigRobotVideo);
        StartCoroutine(BigRobotStory());
    }
    // =========================================================
    // ===================== COROUTINES ========================
    // =========================================================

    IEnumerator TalkTextSequence()
    {
        talkTMP.text = talkText1;
        yield return new WaitForSeconds(4f);
        talkTMP.text = talkText2;
    }

    IEnumerator WakeTextSequence()
    {
        float waitTime = Mathf.Max(0f, (float)wakeVideo.length - 6f);
        yield return new WaitForSeconds(waitTime);
        wakeTMP.text = "hello , human!          <<SAY HI>>";
    }

    IEnumerator WalkTextSequence()
    {
        interactTMP.text = walkText1;
        yield return new WaitForSeconds(2.5f);

        interactTMP.text = walkText2;
        yield return new WaitForSeconds(4f);

        interactTMP.text = walkText3;
        yield return new WaitForSeconds(5f);

        interactTMP.text = walkText4;
        yield return new WaitForSeconds(3f);
    }

    IEnumerator BigRobotStory()
    {
        smallRobotTMP.gameObject.SetActive(false);
        bigRobotTMP.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = "Signal detected... Who's there?";
        PlayAudio(bigRobotAudio1);
        yield return new WaitForSeconds(bigRobotAudio1.length);
        bigRobotTMP.gameObject.SetActive(false);

        smallRobotTMP.gameObject.SetActive(true);
        smallRobotTMP.text = "Hey! You’ve finally activated.";
        PlayAudio(smallRobotAudio1);
        yield return new WaitForSeconds(smallRobotAudio1.length);
        smallRobotTMP.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = "Glad to see another unit online. Been a long silence.";
        PlayAudio(bigRobotAudio2);
        yield return new WaitForSeconds(bigRobotAudio2.length);
        bigRobotTMP.gameObject.SetActive(false);

        smallRobotTMP.gameObject.SetActive(true);
        smallRobotTMP.text = "Yeah! Let's tell the humans how we work - motion vision and touch synced together";
        PlayAudio(smallRobotAudio2);
        yield return new WaitForSeconds(smallRobotAudio2.length);
        smallRobotTMP.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = "Sure! now i'll take it from here";
        PlayAudio(bigRobotAudio3);
        yield return new WaitForSeconds(bigRobotAudio3.length);
        bigRobotTMP.gameObject.SetActive(false);

        smallRobotTMP.gameObject.SetActive(true);
        smallRobotTMP.text = "Bye, humans! My role ends here. See you after this session";
        PlayAudio(smallRobotAudio3);
        yield return new WaitForSeconds(smallRobotAudio3.length);
        smallRobotTMP.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = " Hello again humans!";
        PlayAudio(bigRobotAudio4);
        yield return new WaitForSeconds(bigRobotAudio4.length);
        bigRobotTMP.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = " Now it is up to us together—to explore and understand how this system works.";
        PlayAudio(bigRobotAudio5);
        yield return new WaitForSeconds(bigRobotAudio5.length);
        bigRobotTMP.gameObject.SetActive(false);

        bigRobotInteractionTime = videoPlayer.time;

        HeadTouchZone.gameObject.SetActive(true); SpawnHighlight(HeadTouchZone);
        ChestTouchZone.gameObject.SetActive(true); SpawnHighlight(ChestTouchZone);
        RightArmTouchZone.gameObject.SetActive(true); SpawnHighlight(RightArmTouchZone);
        LeftArmTouchZone.gameObject.SetActive(true); SpawnHighlight(LeftArmTouchZone);
        BaseTextZone.gameObject.SetActive(true); SpawnHighlight(BaseTextZone);

        touchLocked = false;
    }

    IEnumerator BigRobotFinalMessage()
    {
        BigRobotText.gameObject.SetActive(false);

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = "Hope you enjoyed this journey and got to know something about me.";
        PlayAudio(bigRobotAudio6);
        yield return new WaitForSeconds(bigRobotAudio6.length);
        bigRobotTMP.gameObject.SetActive(false);

        FinalTouchZone.gameObject.SetActive(true);
        SpawnHighlight(FinalTouchZone);
        touchLocked = false;

        bigRobotTMP.gameObject.SetActive(true);
        bigRobotTMP.text = "This interaction was just the beginning. Until next time, humans.";
        PlayAudio(bigRobotAudio7);
        yield return new WaitForSeconds(bigRobotAudio7.length);
        bigRobotTMP.gameObject.SetActive(false);
    }

    // =========================================================
    // ==================== BODY TOUCH =========================
    // =========================================================

    void HandleBigRobotTouches(Vector2 pos)
    {
        if (!headDone && IsWallTouchInside(HeadTouchZone, pos))
            ShowBodyText(ref headDone, HeadTouchZone, headText);

        if (!chestDone && IsWallTouchInside(ChestTouchZone, pos))
            ShowBodyText(ref chestDone, ChestTouchZone, chestText);

        if (!rightDone && IsWallTouchInside(RightArmTouchZone, pos))
            ShowBodyText(ref rightDone, RightArmTouchZone, rightArmText);

        if (!leftDone && IsWallTouchInside(LeftArmTouchZone, pos))
            ShowBodyText(ref leftDone, LeftArmTouchZone, leftArmText);

        if (!baseShown && IsWallTouchInside(BaseTextZone, pos))
            ShowBodyText(ref baseShown, BaseTextZone, baseText);

        if (finalMessageStarted && IsWallTouchInside(FinalTouchZone, pos))
            StartFinal();
    }

    void ShowBodyText(ref bool flag, RectTransform zone, string text)
    {
        flag = true;
        zone.gameObject.SetActive(false);

        BigRobotText.gameObject.SetActive(true);
        BigRobotText.text = text;

        playingBodyVideo = true;
        DisableAllTouchZones();

        if (text == headText) currentBodyVideo = headReactionVideo;
        else if (text == chestText) currentBodyVideo = chestReactionVideo;
        else if (text == rightArmText) currentBodyVideo = rightArmReactionVideo;
        else if (text == leftArmText) currentBodyVideo = leftArmReactionVideo;
        else if (text == baseText) currentBodyVideo = baseReactionVideo;

        audioSource.Stop();
        if (text == headText) audioSource.clip = headAudio;
        else if (text == chestText) audioSource.clip = chestAudio;
        else if (text == rightArmText) audioSource.clip = rightArmAudio;
        else if (text == leftArmText) audioSource.clip = leftArmAudio;
        else if (text == baseText) audioSource.clip = baseAudio;

        audioSource.Play();

        videoPlayer.Stop();
        videoPlayer.isLooping = true;
        videoPlayer.clip = currentBodyVideo;
        videoPlayer.Play();

        StartCoroutine(ReturnToBigRobotAfterAudio());
    }

    IEnumerator ReturnToBigRobotAfterAudio()
    {
        yield return new WaitForSeconds(audioSource.clip.length);

        audioSource.Stop();
        videoPlayer.isLooping = false;

        BigRobotText.gameObject.SetActive(false);

        videoPlayer.clip = bigRobotVideo;
        videoPlayer.time = bigRobotInteractionTime;
        videoPlayer.Play();

        if (!headDone) { HeadTouchZone.gameObject.SetActive(true); SpawnHighlight(HeadTouchZone); }
        if (!chestDone) { ChestTouchZone.gameObject.SetActive(true); SpawnHighlight(ChestTouchZone); }
        if (!rightDone) { RightArmTouchZone.gameObject.SetActive(true); SpawnHighlight(RightArmTouchZone); }
        if (!leftDone) { LeftArmTouchZone.gameObject.SetActive(true); SpawnHighlight(LeftArmTouchZone); }
        if (!baseShown) { BaseTextZone.gameObject.SetActive(true); SpawnHighlight(BaseTextZone); }

        playingBodyVideo = false;

        if (headDone && chestDone && rightDone && leftDone && baseShown && !finalMessageStarted)
        {
            finalMessageStarted = true;
            StartCoroutine(BigRobotFinalMessage());
        }
    }

    // =========================================================
    // ====================== HELPERS ==========================
    // =========================================================



    bool IsWallTouchInside(RectTransform zone, Vector2 screenPos)
    {
        if (!zone || !zone.gameObject.activeSelf) return false;
        if (!isTouchingWall) return false;   // ✅ USE PYTHON FLAG

        return RectTransformUtility.RectangleContainsScreenPoint(
            zone,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera
        );
    }

    void DisableAllTexts()
    {
        promptTMP.gameObject.SetActive(false);
        wakeTMP.gameObject.SetActive(false);
        talkTMP.gameObject.SetActive(false);
        interactTMP.gameObject.SetActive(false);
        smallRobotTMP.gameObject.SetActive(false);
        bigRobotTMP.gameObject.SetActive(false);
        BigRobotText.gameObject.SetActive(false);
        finalTMP.gameObject.SetActive(false);
    }

    void DisableAllTouchZones()
    {
        wakeTouchZone.gameObject.SetActive(false);
        talkTouchZone.gameObject.SetActive(false);
        interactTouchZone.gameObject.SetActive(false);
        HeadTouchZone.gameObject.SetActive(false);
        ChestTouchZone.gameObject.SetActive(false);
        RightArmTouchZone.gameObject.SetActive(false);
        LeftArmTouchZone.gameObject.SetActive(false);
        BaseTextZone.gameObject.SetActive(false);
        FinalTouchZone.gameObject.SetActive(false);
    }

    void SpawnHighlight(RectTransform zone)
    {
        if (!touchZoneHighlightPrefab || !zone) return;
        Instantiate(touchZoneHighlightPrefab, zone).transform.localPosition = Vector3.zero;
    }

    void InitializeSleepState()
    {
        DisableAllTexts();
        DisableAllTouchZones();

        headDone = chestDone = rightDone = leftDone = baseShown = false;
        finalMessageStarted = false;
        touchLocked = false;

        promptTMP.gameObject.SetActive(true);
        promptTMP.text = "AWAITING HUMAN INTERACTION\nWAVE TO ACTIVATE";

        currentState = RobotState.Sleep;
        PlayVideo(sleepVideo, true);
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (playingBodyVideo) return;

        switch (currentState)
        {
            case RobotState.Sleep:
                wakeTouchZone.gameObject.SetActive(true);
                SpawnHighlight(wakeTouchZone);
                break;

            case RobotState.Wake:
                talkTouchZone.gameObject.SetActive(true);
                SpawnHighlight(talkTouchZone);
                touchLocked = false;
                break;

            case RobotState.Talk:
            case RobotState.Walk:
                interactTouchZone.gameObject.SetActive(true);
                SpawnHighlight(interactTouchZone);
                touchLocked = false;
                break;

            case RobotState.Final:
                InitializeSleepState();
                break;
        }
    }

    // =========================================================
    // ======================== UDP ============================
    // =========================================================

    void ReceiveData()
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                var data = udpClient.Receive(ref ip);
                var msg = Encoding.UTF8.GetString(data);

                if (msg.StartsWith("FINGER"))
                {
                    var p = msg.Split(' ');
                    fingerX = Mathf.Clamp01(float.Parse(p[1]));
                    fingerY = Mathf.Clamp01(float.Parse(p[2]));
                    fingerZ = float.Parse(p[3]);
                    isTouchingWall = p[4] == "1";
                }

            }
            catch { }
        }
    }

    void OnApplicationQuit()
    {
        udpClient?.Close();
        receiveThread?.Abort();
    }
    // ================= FINAL STATE =================
    void StartFinal()
    {
        touchLocked = true;
        DisableAllTexts();
        DisableAllTouchZones();

        finalTMP.gameObject.SetActive(true);
        finalTMP.text = "Interaction ended.\nHope you had a good experience.";

        currentState = RobotState.Final;
        PlayVideo(finalVideo);
    }

}
