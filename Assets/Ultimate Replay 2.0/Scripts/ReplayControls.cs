using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Default replay controls used for demonstration and testing.
    /// Uses legacy GUI for ui rendering.
    /// </summary>
    public class ReplayControls : MonoBehaviour
    {
        // Private
        private const float playPauseWidth = 24;
        private const float playPauseHeight = 20;
        private const float stateButtonWidth = 48;
        private const float stateButtonHeight = 18;
        private const float lookMultiplier = 300;
        private static readonly Color normal = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        private static readonly Color highlight = Color.white;

        private ReplayStorageTarget storageTarget = new ReplayMemoryTarget();
        private ReplayHandle recordHandle = ReplayHandle.invalid;
        private ReplayHandle playbackHandle = ReplayHandle.invalid;
        private Camera freeCam = null;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private Vector2 camRotation = Vector2.zero;

        private bool showSettings = false;
        private bool reversePlay = false;
        private Texture2D playTexture = null;
        private Texture2D pauseTexture = null;
        private Texture2D settingsTexture = null;

        private Texture2D whitePixel = null;
        private Texture2D recordTexture = null;
        private Texture2D playbackTexture = null;

        // Public
        /// <summary>
        /// Should recording begin when the game starts.
        /// </summary>
        public bool recordOnStart = true;
        /// <summary>
        /// Should the free cam mode be enabled during playback.
        /// </summary>
        public bool allowPlaybackFreeCam = true;
        /// <summary>
        /// How fast the free cam can move around the scene.
        /// </summary>
        public float flySpeed = 8;
        /// <summary>
        /// How fast the free cam can look around the scene.
        /// </summary>
        public float lookSpeed = 0.6f;

        public KeyCode liveModeShortcut = KeyCode.L;
        public KeyCode recordModeShortcut = KeyCode.R;
        public KeyCode playModeShortcut = KeyCode.P;

        // Properties
        public ReplayHandle RecordHandle
        {
            get { return recordHandle; }
        }

        public ReplayHandle PlaybackHandle
        {
            get { return playbackHandle; }
        }

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            // Find or create a replay manager
            ReplayManager.ForceAwake();

            // Create default texture
            whitePixel = new Texture2D(1, 1);
            whitePixel.SetPixel(0, 0, Color.white);
            whitePixel.Apply();

            if (allowPlaybackFreeCam == true)
            {
                // Find free cam
                freeCam = GetComponent<Camera>();

                // Add the camera if one was not found
                if (freeCam == null)
                    freeCam = gameObject.AddComponent<Camera>();

                // Disable by default
                freeCam.enabled = false;
            }
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Start()
        {
            playTexture = Resources.Load<Texture2D>("PlayIcon");
            pauseTexture = Resources.Load<Texture2D>("PauseIcon");
            settingsTexture = Resources.Load<Texture2D>("SettingsIcon");
            recordTexture = Resources.Load<Texture2D>("RecordIcon");
            playbackTexture = Resources.Load<Texture2D>("PlaybackIcon");

            // Start recording
            if (recordOnStart == true)
                recordHandle = ReplayManager.BeginRecording(storageTarget, null, false, true);
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Update()
        {
            // Check for camera update
            if (allowPlaybackFreeCam == false || freeCam == null)
                return;

            // Only move the camera during playback
            if (ReplayManager.IsReplaying(playbackHandle) == true)
            {
                // Get xy input
                float v = Input.GetKey(KeyCode.W) == true ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
                float h = Input.GetKey(KeyCode.A) == true ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;

                // Move camera
                transform.Translate(Vector3.forward * flySpeed * v * Time.deltaTime);
                transform.Translate(Vector3.right * flySpeed * h * Time.deltaTime);

                // Check for mouse down
                if (Input.GetMouseButtonDown(1) == true)
                {
                    // Prevent camera snapping when starting dragging - offset based upon initial rotation
                    camRotation.y = transform.localRotation.eulerAngles.y;
                    camRotation.x = transform.localRotation.eulerAngles.x;

                    // Wrap angles
                    if (camRotation.y > 360)
                        camRotation.y = 0;
                }

                // Check for mouse down
                if (Input.GetMouseButton(1) == true)
                {
                    // Get mouse input
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = -Input.GetAxis("Mouse Y");

                    // Apply mouse with speed
                    camRotation.y += mouseX * lookSpeed * lookMultiplier * Time.deltaTime;
                    camRotation.x += mouseY * lookSpeed * lookMultiplier * Time.deltaTime;

                    // Apply the rotation
                    transform.rotation = Quaternion.Euler(camRotation.x, camRotation.y, 0);
                }
            }
        }

        /// <summary>
        /// Called by unity.
        /// </summary>
        public void OnGUI()
        {
            // Default label style
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Bold;

            // Create the gui screen area
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            {
                GUILayout.BeginHorizontal();
                {
                    // Create a button style
                    GUIStyle button = new GUIStyle(GUI.skin.button);

                    button.padding = new RectOffset(3, 3, 3, 3);
                    button.margin = new RectOffset(-1, -1, 0, 0);

                    GUI.color = (ReplayManager.IsRecording(recordHandle) == false && ReplayManager.IsReplaying(playbackHandle) == false) ? highlight : normal;

                    if (GUILayout.Button(new GUIContent("Live", "Live mode"), button, GUILayout.Width(stateButtonWidth), GUILayout.Height(stateButtonHeight)) == true ||
                        IsReplayKeyPressed(liveModeShortcut) == true)
                    {
                        // Exit free cam
                        ExitPlaybackFreeCam();

                        // Live mode is just game mode (No replay element)
                        ReplayGoLive();
                    }

                    GUI.color = (ReplayManager.IsRecording(recordHandle) == true) ? highlight : normal;

                    if (GUILayout.Button(new GUIContent("Rec", recordTexture, "Begin recording"), button, GUILayout.Width(stateButtonWidth), GUILayout.Height(stateButtonHeight)) == true ||
                        IsReplayKeyPressed(recordModeShortcut) == true)
                    {
                        // Exit free cam
                        ExitPlaybackFreeCam();

                        // Check for playback
                        if (ReplayManager.IsReplaying(playbackHandle) == true)
                            ReplayManager.StopPlayback(ref playbackHandle);

                        // Start a fresh recording
                        if (ReplayManager.IsRecording(RecordHandle) == false)
                        {
                            // Clear old data
                            if (storageTarget.MemorySize > 0)
                                storageTarget.PrepareTarget(ReplayTargetTask.Discard);

                            recordHandle = ReplayManager.BeginRecording(storageTarget, null, false, true);
                        }
                    }

                    GUI.color = (ReplayManager.IsReplaying(playbackHandle) == true) ? highlight : normal;

                    if (GUILayout.Button(new GUIContent("Play", playbackTexture, "Begin playback"), button, GUILayout.Width(stateButtonWidth), GUILayout.Height(stateButtonHeight)) == true ||
                        IsReplayKeyPressed(playModeShortcut) == true)
                    {
                        // Enable the free cam
                        if (allowPlaybackFreeCam == true)
                            EnterPlaybackFreeCam();

                        // Stop recording
                        if (ReplayManager.IsRecording(recordHandle) == true)
                        {
                            ReplayManager.StopRecording(ref recordHandle);
                        }

                        // Start playback
                        if(ReplayManager.IsReplaying(playbackHandle) == false)
                            playbackHandle = ReplayManager.BeginPlayback(storageTarget, null, true);
                    }

                    GUI.color = highlight;

                    // Push to right
                    GUILayout.FlexibleSpace();

                    // Recording status
                    if (ReplayManager.IsRecording(recordHandle) == true)
                    {
                        // Get the storage target
                        ReplayStorageTarget target = ReplayManager.GetReplayStorageTarget(recordHandle);

                        string recordTime = ReplayTime.GetCorrectedTimeValueString((target != null) ? target.Duration : 0);

                        GUILayout.Label(string.Format("Recording: {0}", recordTime), labelStyle);

                        // Draw recoring lines
                        // Top left
                        DrawGUILine(new Vector2(50, 50), new Vector2(80, 50));
                        DrawGUILine(new Vector2(50, 50), new Vector2(50, 80));

                        // Top right
                        DrawGUILine(new Vector2(Screen.width - 80, 50), new Vector2(Screen.width - 50, 50));
                        DrawGUILine(new Vector2(Screen.width - 50, 50), new Vector2(Screen.width - 50, 80));

                        // Bottom left
                        DrawGUILine(new Vector2(50, Screen.height - 50), new Vector2(80, Screen.height - 50));
                        DrawGUILine(new Vector2(50, Screen.height - 50), new Vector2(50, Screen.height - 80));

                        // Bottom right
                        DrawGUILine(new Vector2(Screen.width - 80, Screen.height - 50), new Vector2(Screen.width - 50, Screen.height - 50));
                        DrawGUILine(new Vector2(Screen.width - 50, Screen.height - 50), new Vector2(Screen.width - 50, Screen.height - 80));
                    }

                    if (ReplayManager.IsReplaying(playbackHandle) == true)
                    {
                        if (allowPlaybackFreeCam == true && freeCam != null)
                        {
                            GUILayout.BeginVertical();
                            {
                                // Draw the free cam label
                                GUILayout.Label("Free Cam Enabled", labelStyle);

                                GUI.color = new Color(0.3f, 0.3f, 0.3f);

                                GUIStyle subLabelStyle = new GUIStyle(GUI.skin.label);
                                subLabelStyle.padding = new RectOffset(0, 0, -2, -2);
                                subLabelStyle.fontSize = 10;
                                subLabelStyle.alignment = TextAnchor.MiddleRight;

                                // Draw hint label
                                GUILayout.Label("Free Move: WASD", subLabelStyle);
                                GUILayout.Label("Free Look: RMB", subLabelStyle);

                                GUI.color = Color.white;
                            }
                            GUILayout.EndVertical();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                // Push to bottom
                GUILayout.FlexibleSpace();

                // Make sure we are not recording for the playback controls
                if (ReplayManager.IsReplaying(playbackHandle) == true)
                {
                    // Get the playback source
                    ReplayStorageTarget target = ReplayManager.GetReplayStorageTarget(playbackHandle);

                    // Get the playback time
                    ReplayTime playbackTime = ReplayManager.GetPlaybackTime(playbackHandle);

                    GUILayout.BeginHorizontal();
                    {
                        // Check for paused
                        if (ReplayManager.IsPlaybackPaused(playbackHandle) == true)
                        {
                            // Draw a play button
                            if (GUILayout.Button(playTexture, GUILayout.Width(playPauseWidth), GUILayout.Height(playPauseHeight)) == true)
                            {
                                // Resume the replay
                                ReplayManager.ResumePlayback(playbackHandle);
                            }
                        }
                        else
                        {
                            // Draw a pause button
                            if (GUILayout.Button(pauseTexture, GUILayout.Width(playPauseWidth), GUILayout.Height(playPauseHeight)) == true)
                            {
                                // Pause playback
                                ReplayManager.PausePlayback(playbackHandle);
                            }
                        }

                        

                        // Slider space
                        GUILayout.BeginVertical();
                        {
                            // Push down slightly
                            GUILayout.Space(10);

                            float input = playbackTime.NormalizedTime;

                            // Draw the seek slider
                            float output = GUILayout.HorizontalSlider(input, 0, 1, GUILayout.Height(playPauseHeight));

                            // Check for change
                            if (input != output)
                            {
                                // Seek to recording location
                                ReplayManager.SetPlaybackTimeNormalized(playbackHandle, output, PlaybackOrigin.Start);
                            }

                        }
                        GUILayout.EndVertical();

                        // Settings button
                        if (GUILayout.Button(new GUIContent(settingsTexture, "Open playback settings"), GUILayout.Width(playPauseWidth), GUILayout.Height(playPauseHeight)) == true)
                        {
                            // Toggle settings
                            showSettings = !showSettings;
                        }

                        // Check for settings window
                        if (showSettings == true)
                        {
                            Rect area = new Rect(Screen.width - 160, Screen.height - 100, 140, 50);

                            DrawGUISettings(area);
                        }

                        

                        string currentTime = ReplayTime.GetCorrectedTimeValueString(playbackTime.Time);
                        string totalTime = ReplayTime.GetCorrectedTimeValueString((target != null) ? target.Duration : 0);

                        GUILayout.Label(string.Format("{0} / {1}", currentTime, totalTime), GUI.skin.button, GUILayout.Width(75));
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
        }

        private void DrawGUISettings(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            {
                ReplayTime playbackTime = ReplayManager.GetPlaybackTime(playbackHandle);

                // Playback speed
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Speed:", GUILayout.Width(55));
                    float timeScale = GUILayout.HorizontalSlider(playbackTime.TimeScale, 0, 2);

                    // Update the time scale
                    if (timeScale != playbackTime.TimeScale)
                        ReplayManager.SetPlaybackTimeScale(playbackHandle, timeScale);
                }
                GUILayout.EndHorizontal();

                // Playback direction
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Reverse:", GUILayout.Width(55));
                    bool result = GUILayout.Toggle(reversePlay, string.Empty);

                    // Chcck for change
                    if (result != reversePlay)
                    {
                        reversePlay = result;

                        // Check if we are currently replaying
                        if (ReplayManager.IsReplaying(playbackHandle) == true)
                        {
                            // Set direction for playback
                            ReplayManager.SetPlaybackDirection(playbackHandle, (reversePlay == true) 
                                ? ReplayManager.PlaybackDirection.Backward 
                                : ReplayManager.PlaybackDirection.Forward);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
        }

        private void ReplayGoLive()
        {
            // Stop all recording
            if (ReplayManager.IsRecording(recordHandle) == true)
                ReplayManager.StopRecording(ref recordHandle);

            // Stop all playback
            if (ReplayManager.IsReplaying(playbackHandle) == true)
                ReplayManager.StopPlayback(ref playbackHandle);
        }

        private void EnterPlaybackFreeCam()
        {
            // Require free cam
            if (freeCam == null)
                return;

            Camera main = null;

            // Only one camera should be enabled
            Camera[] all = Component.FindObjectsOfType<Camera>();

            // Use the first camera
            for (int i = 0; i < all.Length; i++)
            {
                // Disregard any disabled cameras
                if (all[i].enabled == true)
                {
                    // Find first enabled
                    main = all[i];
                    break;
                }
            }

            // Check for a main camera found
            if (main != null)
            {
                // Get the positions
                transform.position = main.transform.position;
                transform.rotation = main.transform.rotation;
            }

            // Enable the free cam
            freeCam.enabled = true;
        }

        private void ExitPlaybackFreeCam()
        {
            // Require free cam
            if (freeCam == null)
                return;

            freeCam.enabled = false;

            // Reset position
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        private bool IsReplayKeyPressed(KeyCode key)
        {
            // Check for modifier key
            //bool modifier = Input.GetKeyDown(KeyCode.LeftShift) == true ||
            //    Input.GetKeyDown(KeyCode.RightShift) == true;

            //// Modifier must be held
            //if (modifier == false)
            //    return false;

            // Check for key press
            return Input.GetKey(key);
        }

        private void DrawGUILine(Vector2 start, Vector2 end)
        {
            float width = 2;

            // Find length
            Vector2 delta = end - start;

            // Find angle
            float a = Mathf.Rad2Deg * Mathf.Atan(delta.y / delta.x);

            // Check for backwards angle
            if (delta.x < 0)
                a += 180;

            // Find second width
            int width2 = (int)Mathf.Ceil(width / 2);

            // Transform and draw
            GUIUtility.RotateAroundPivot(a, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width2, delta.magnitude, width), whitePixel);
            GUIUtility.RotateAroundPivot(-a, start);
        }
    }
}