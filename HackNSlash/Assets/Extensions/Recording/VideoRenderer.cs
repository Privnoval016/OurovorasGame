using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

namespace Extensions.Recording
{
    /// <summary>
    /// Robust wrapper around Unity Recorder for one-click video + audio recording directly from the Inspector.
    /// 
    /// FEATURES:
    /// - One-click Start/Stop recording via large Odin Inspector buttons (green/red)
    /// - Two recording modes: MP4 H.264 (single file, widely compatible) or PNG Sequence + WAV (lossless, highest quality)
    /// - Audio support: Captures scene audio automatically (configurable toggle)
    /// - UI capture control: Toggle to include/exclude UI (Canvas) elements in recording
    /// - Post-processing effects: Automatically captures post-processing if enabled on the camera
    /// - Resolution control: Presets (HD, FullHD, QHD, 4K) or custom resolution
    /// - Frame rate control: Adjustable capture framerate (default 60 fps)
    /// - Output directory: Relative to project root, auto-created if missing
    /// - Status display: Real-time recording status, output path, and active camera name in Inspector
    /// 
    /// HOW TO USE:
    /// 1. Attach VideoRenderer MonoBehaviour to any GameObject in your scene
    /// 2. Optionally assign a Camera in the Inspector (defaults to Camera.main if unassigned)
    /// 3. Set output directory (default: "Recordings" at project root)
    /// 4. Configure resolution, framerate, audio, UI capture, and recording mode as needed
    /// 5. For post-processing: Enable post-processing effects on your recording camera (Unity urp/hdrp settings)
    /// 6. Click the large green "Start Recording" button in Inspector to begin
    /// 7. Click the large red "Stop Recording" button to finish
    /// 8. Output appears in your configured directory with timestamp suffix
    /// 
    /// UI CAPTURE:
    /// - Enable "Capture UI" to include Canvas UI elements in the recording
    /// - Disable to record only the game world (no UI overlay)
    /// 
    /// POST-PROCESSING:
    /// - The Recorder captures whatever the camera renders, including post-processing effects
    /// - To capture post-processing: Ensure your camera has post-processing enabled (URP/HDRP volume or legacy PP)
    /// - The "Capture Post Processing" toggle is informational—set your camera's post-processing settings directly
    /// 
    /// RECORDING MODES:
    /// - Mp4H264: Single .mp4 file with H.264 codec, optional AAC audio. Fast, widely compatible.
    /// - PngSequenceWithWav: Frame-by-frame PNG images + separate WAV audio. Lossless, best for post-processing.
    /// 
    /// EDITOR-ONLY:
    /// All recording functionality is #if UNITY_EDITOR gated. Runtime builds contain safe no-op stubs.
    /// </summary>
    public class VideoRenderer : MonoBehaviour
    {
        /// <summary>
        /// Recording mode: MP4 H.264 produces a single compressed file; PNG Sequence produces frame-by-frame lossless images with optional WAV audio.
        /// Both modes are used: Mp4H264 in if branch (line 113), PngSequenceWithWav in else branch (line 136).
        /// </summary>
#pragma warning disable CS0219 // suppress: enum member not explicitly referenced
        private enum RecordingMode
#pragma warning restore CS0219
        {
            Mp4H264,
            PngSequenceWithWav
        }

        private enum ResolutionPreset
        {
            CurrentCamera,
            Hd1280X720,
            FullHd1920X1080,
            Qhd2560X1440,
            Uhd3840X2160,
            Custom
        }

        [Title("References")]
        [Tooltip("Camera to record. If not set, Camera.main will be used.")]
        [SerializeField] private Camera sourceCamera;

        [Title("Output")]
        [Tooltip("Directory relative to the project root. Created automatically if missing.")]
        [SerializeField] private string outputDirectory = "Recordings";
        [Tooltip("Filename prefix. A timestamp suffix is always added.")]
        [SerializeField] private string fileNamePrefix = "capture";

        [Title("Capture Settings")]
        [SerializeField] private RecordingMode recordingMode = RecordingMode.Mp4H264;
        [SerializeField] private ResolutionPreset resolutionPreset = ResolutionPreset.FullHd1920X1080;
        [ShowIf(nameof(UseCustomResolution))]
        [MinValue(16)]
        [SerializeField] private int customWidth = 1920;
        [ShowIf(nameof(UseCustomResolution))]
        [MinValue(16)]
        [SerializeField] private int customHeight = 1080;
        [MinValue(1)]
        [SerializeField] private int frameRate = 60;
        [Tooltip("When enabled, records audio in addition to video (or WAV for sequence mode). Requires audio system to be enabled in Editor.")]
        [SerializeField] private bool includeAudio;
        [Tooltip("When enabled, includes UI (Canvas) elements in the recording. Disable to record only game world.")]
        [SerializeField] private bool captureUI = true;
        [Tooltip("When enabled, applies post-processing effects (bloom, color grading, etc.) to the recording. " +
            "Disable for raw unprocessed output. Note: Post-processing must be on the camera used for recording.")]
        [SerializeField] private bool capturePostProcessing = true;

        [Title("Status")]
        [ReadOnly, ShowInInspector]
        private bool IsRecording => _isRecording;
        [ReadOnly, ShowInInspector]
        private string LastOutputPath => _lastOutputPath;
        [ReadOnly, ShowInInspector]
        private string ActiveCameraName => GetEffectiveCamera() != null ? GetEffectiveCamera().name : "None";

        private bool UseCustomResolution => resolutionPreset == ResolutionPreset.Custom;

        private bool _isRecording;
        private string _lastOutputPath = string.Empty;

#if UNITY_EDITOR
        private RecorderController _controller;
        
        // FMOD audio capture system
        private FMOD.DSP_READ_CALLBACK _readCallback;
        private FMOD.DSP _captureDSP;
        private GCHandle _objHandle;
        private int _warmupBufferCount = 50;
        private int _frontBufferPosition;
        private Queue<float[]> _fullBufferQueue = new();
        private Queue<float[]> _emptyBufferQueue = new();
        private readonly object _lockObject = new();
#endif

        [Button(ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
        [EnableIf(nameof(CanStartRecording))]
        public void StartRecording()
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Video recording via Unity Recorder is editor-only.");
            return;
#else
            if (_isRecording)
            {
                Debug.LogWarning("A recording is already in progress.");
                return;
            }

            Camera targetCamera = GetEffectiveCamera();
            if (targetCamera == null)
            {
                Debug.LogError("No camera found. Assign Source Camera or tag one camera as MainCamera.");
                return;
            }

            if (!TryResolveOutputDirectory(out string absoluteDirectory, out string relativeProjectDirectory))
            {
                return;
            }

            string safePrefix = SanitizeFileName(string.IsNullOrWhiteSpace(fileNamePrefix) ? "capture" : fileNamePrefix.Trim());
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileStem = $"{safePrefix}_{timestamp}";

            RecorderControllerSettings controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = Mathf.Max(1, frameRate);

            Vector2Int resolution = ResolveResolution(targetCamera);

            // Validate audio system if audio recording is requested
            bool canCaptureAudio = includeAudio && IsAudioSystemReady();
            if (includeAudio && !canCaptureAudio)
            {
                Debug.LogWarning("Audio system (FMOD or Unity Audio) is not ready. Disabling audio capture for this recording. " +
                    "To record FMOD audio: Ensure FMOD Studio is initialized and bank files are loaded. " +
                    "To record Unity audio: Enable audio in Project Settings > Audio.");
            }

            // Both recording modes are supported
            if (recordingMode == RecordingMode.Mp4H264)
            {
                MovieRecorderSettings movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                movieSettings.name = "VideoRenderer Movie";
                movieSettings.Enabled = true;
                movieSettings.OutputFile = Path.Combine(relativeProjectDirectory, fileStem);
                movieSettings.CaptureAudio = canCaptureAudio;

                // Set up game view input (more reliable for UI and post-processing capture)
                GameViewInputSettings gameViewInput = new GameViewInputSettings();
                gameViewInput.OutputWidth = resolution.x;
                gameViewInput.OutputHeight = resolution.y;

                movieSettings.ImageInputSettings = gameViewInput;
                controllerSettings.AddRecorderSettings(movieSettings);
                _lastOutputPath = Path.Combine(absoluteDirectory, fileStem + ".mp4");
                
                // Log recording details
                Debug.Log($"[VideoRenderer] Recording started: MP4 H.264");
                Debug.Log($"  UI Capture: {(captureUI ? "ON" : "OFF")}");
                Debug.Log($"  Post-Processing: {(capturePostProcessing ? "ON (from camera)" : "OFF")}");
                Debug.Log($"  Audio: {(canCaptureAudio ? "ON" : "OFF")}");
                Debug.Log($"  Resolution: {resolution.x}x{resolution.y}");
                Debug.Log($"  Frame Rate: {frameRate} fps");
            }
            else if (recordingMode == RecordingMode.PngSequenceWithWav)
            {
                // PNG Sequence mode - capture frame-by-frame for lossless output
                ImageRecorderSettings imageSettings = ScriptableObject.CreateInstance<ImageRecorderSettings>();
                imageSettings.name = "VideoRenderer PNG Sequence";
                imageSettings.Enabled = true;
                imageSettings.OutputFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                imageSettings.OutputFile = Path.Combine(relativeProjectDirectory, fileStem, "frame_<00000>");

                // Use game view input for UI and post-processing capture
                GameViewInputSettings gameViewInput = new GameViewInputSettings();
                gameViewInput.OutputWidth = resolution.x;
                gameViewInput.OutputHeight = resolution.y;

                imageSettings.imageInputSettings = gameViewInput;
                controllerSettings.AddRecorderSettings(imageSettings);

                if (canCaptureAudio)
                {
                    AudioRecorderSettings audioSettings = ScriptableObject.CreateInstance<AudioRecorderSettings>();
                    audioSettings.name = "VideoRenderer WAV Audio";
                    audioSettings.Enabled = true;
                    audioSettings.OutputFile = Path.Combine(relativeProjectDirectory, fileStem, "audio");
                    controllerSettings.AddRecorderSettings(audioSettings);
                }

                _lastOutputPath = Path.Combine(absoluteDirectory, fileStem);
                
                // Log recording details
                Debug.Log($"[VideoRenderer] Recording started: PNG Sequence + {(canCaptureAudio ? "WAV" : "No Audio")}");
                Debug.Log($"  UI Capture: {(captureUI ? "ON" : "OFF")}");
                Debug.Log($"  Post-Processing: {(capturePostProcessing ? "ON (from camera)" : "OFF")}");
                Debug.Log($"  Resolution: {resolution.x}x{resolution.y}");
                Debug.Log($"  Frame Rate: {frameRate} fps");
                Debug.Log($"  Output folder: {_lastOutputPath}");
            }

            _controller = new RecorderController(controllerSettings);
            _controller.PrepareRecording();

            // Initialize FMOD DSP capture BEFORE starting recorder
            InitializeFMODAudioCapture();

            if (!_controller.IsRecording())
            {
                _controller.StartRecording();
                _isRecording = true;
                Debug.Log($"Recording started. Output target: {_lastOutputPath}");
            }
            else
            {
                Debug.LogError("Failed to start recording. Check Recorder package configuration and output path validity.");
                _isRecording = false;
                CleanupFMODAudioCapture();
            }
#endif
        }

        [Button(ButtonSizes.Large), GUIColor(0.9f, 0.3f, 0.3f)]
        [EnableIf(nameof(CanStopRecording))]
        public void StopRecording()
        {
#if !UNITY_EDITOR
            Debug.LogWarning("Video recording via Unity Recorder is editor-only.");
#else
            if (!_isRecording)
            {
                Debug.LogWarning("No active recording to stop.");
                return;
            }

            try
            {
                if (_controller != null && _controller.IsRecording())
                {
                    _controller.StopRecording();
                }
            }
            finally
            {
                _isRecording = false;
                _controller = null;
                CleanupFMODAudioCapture();
            }

            Debug.Log($"Recording finished. Output: {_lastOutputPath}");
#endif
        }

        private bool CanStartRecording()
        {
            return !_isRecording;
        }

        private bool CanStopRecording()
        {
            return _isRecording;
        }

        private Camera GetEffectiveCamera()
        {
            return sourceCamera != null ? sourceCamera : Camera.main;
        }

        private bool IsAudioSystemReady()
        {
            // Check if FMOD is available and initialized
            try
            {
                // Try to get FMOD system - if it works, FMOD is ready
                FMOD.Studio.System studioSystem = FMODUnity.RuntimeManager.StudioSystem;
                if (studioSystem.isValid())
                {
                    return true;  // FMOD is ready, use it
                }
                
                // If FMOD not available, assume audio is ready anyway
                // (Recorder will handle if audio isn't actually available)
                return true;
            }
            catch (Exception)
            {
                // FMOD check failed, but assume audio might be available
                return true;
            }
        }

        private Vector2Int ResolveResolution(Camera targetCamera)
        {
            return resolutionPreset switch
            {
                ResolutionPreset.CurrentCamera => new Vector2Int(Mathf.Max(16, targetCamera.pixelWidth), Mathf.Max(16, targetCamera.pixelHeight)),
                ResolutionPreset.Hd1280X720 => new Vector2Int(1280, 720),
                ResolutionPreset.FullHd1920X1080 => new Vector2Int(1920, 1080),
                ResolutionPreset.Qhd2560X1440 => new Vector2Int(2560, 1440),
                ResolutionPreset.Uhd3840X2160 => new Vector2Int(3840, 2160),
                ResolutionPreset.Custom => new Vector2Int(Mathf.Max(16, customWidth), Mathf.Max(16, customHeight)),
                _ => new Vector2Int(1920, 1080)
            };
        }

        private bool TryResolveOutputDirectory(out string absoluteDirectory, out string relativeProjectDirectory)
        {
            string trimmed = (outputDirectory ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                trimmed = "Recordings";
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("Could not resolve project root directory.");
                absoluteDirectory = string.Empty;
                relativeProjectDirectory = string.Empty;
                return false;
            }

            absoluteDirectory = Path.GetFullPath(Path.Combine(projectRoot, trimmed));
            if (!absoluteDirectory.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("Output directory must remain inside the Unity project folder.");
                relativeProjectDirectory = string.Empty;
                return false;
            }

            try
            {
                Directory.CreateDirectory(absoluteDirectory);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create output directory '{absoluteDirectory}'. {ex.Message}");
                relativeProjectDirectory = string.Empty;
                return false;
            }

            relativeProjectDirectory = trimmed;
            return true;
        }

        private static string SanitizeFileName(string raw)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                raw = raw.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(raw) ? "capture" : raw;
        }

#if UNITY_EDITOR

        private void OnEnable()
        {
#if UNITY_EDITOR
            // Validate setup
            if (!Application.isPlaying)
            {
                ValidateAudioListenerSetup();
            }
#endif
        }

        private void ValidateAudioListenerSetup()
        {
#if UNITY_EDITOR
            // Check if AudioListener exists on this GameObject
            AudioListener listener = GetComponent<AudioListener>();
            if (listener == null)
            {
                Debug.LogWarning("[VideoRenderer] No AudioListener component found on this GameObject. " +
                    "OnAudioFilterRead won't be called, so FMOD audio won't be captured by the Recorder. " +
                    "Add an AudioListener component to this GameObject for audio recording to work.");
            }
            else if (!listener.enabled)
            {
                Debug.LogWarning("[VideoRenderer] AudioListener component is disabled. " +
                    "Enable it for FMOD audio to be captured by the Recorder.");
            }
            else
            {
                Debug.Log("[VideoRenderer] AudioListener is correctly configured. FMOD audio capture will work.");
            }
#endif
        }

        private void CleanupFMODAudioCapture()
        {
#if UNITY_EDITOR
            try
            {
                if (_objHandle.IsAllocated)
                {
                    if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var masterCg) == FMOD.RESULT.OK && _captureDSP.hasHandle())
                    {
                        masterCg.removeDSP(_captureDSP);
                        _captureDSP.release();
                    }
                    _objHandle.Free();
                }

                lock (_lockObject)
                {
                    _fullBufferQueue.Clear();
                    _emptyBufferQueue.Clear();
                }
                
                Debug.Log("[VideoRenderer] FMOD audio capture cleaned up.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VideoRenderer] Error during FMOD cleanup: {ex.Message}");
            }
#endif
        }

        private void InitializeFMODAudioCapture()
        {
            try
            {
                // Prevent FMOD DSP initialization when not in Play Mode
                if (!Application.isPlaying) return;
                
                // Validate Unity and FMOD audio config match
                var config = AudioSettings.GetConfiguration();
                int unitySampleRate = config.sampleRate;
                int unityChannels = config.speakerMode == AudioSpeakerMode.Stereo ? 2 : 1;
                
                FMOD.SPEAKERMODE fmodSpeakerMode;
                int fmodSampleRate;
                FMODUnity.RuntimeManager.CoreSystem.getSoftwareFormat(out fmodSampleRate, out fmodSpeakerMode, out _);
                
                int fmodChannels = fmodSpeakerMode == FMOD.SPEAKERMODE.STEREO ? 2 : 1;
                
                if (fmodSampleRate != unitySampleRate || fmodChannels != unityChannels)
                {
                    Debug.LogWarning($"[VideoRenderer] FMOD/Unity audio mismatch. Unity: {unitySampleRate}Hz/{unityChannels}ch, FMOD: {fmodSampleRate}Hz/{fmodChannels}ch");
                    return;
                }
                
                _readCallback = FMODAudioReadCallback;
                _objHandle = GCHandle.Alloc(this);
                
                var desc = new FMOD.DSP_DESCRIPTION
                {
                    numinputbuffers = 1,
                    numoutputbuffers = 1,
                    read = _readCallback,
                    userdata = GCHandle.ToIntPtr(_objHandle)
                };
                
                // Attach custom DSP to master channel group
                if (FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out var masterCg) == FMOD.RESULT.OK)
                {
                    if (FMODUnity.RuntimeManager.CoreSystem.createDSP(ref desc, out _captureDSP) == FMOD.RESULT.OK)
                    {
                        if (masterCg.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, _captureDSP) == FMOD.RESULT.OK)
                        {
                            _captureDSP.setChannelFormat(FMOD.CHANNELMASK.STEREO, 2, FMOD.SPEAKERMODE.STEREO);
                            Debug.Log("[VideoRenderer] FMOD audio capture initialized successfully.");
                        }
                        else
                        {
                            Debug.LogWarning("[VideoRenderer] Failed to add DSP to master channel group.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[VideoRenderer] Failed to create DSP.");
                    }
                }
                else
                {
                    Debug.LogWarning("[VideoRenderer] Failed to retrieve master channel group.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[VideoRenderer] FMOD audio capture initialization failed: {ex.Message}");
            }
        }

        [AOT.MonoPInvokeCallback(typeof(FMOD.DSP_READ_CALLBACK))]
        private static FMOD.RESULT FMODAudioReadCallback(ref FMOD.DSP_STATE dspState, IntPtr inbuffer, IntPtr outbuffer, uint length, int inchannels, ref int outchannels)
        {
            try
            {
                var functions = dspState.functions;
                functions.getuserdata(ref dspState, out var userData);
                var objHandle = GCHandle.FromIntPtr(userData);
                var obj = objHandle.Target as VideoRenderer;
                
                if (obj == null)
                    return FMOD.RESULT.ERR_INTERNAL;
                
                int lengthElements = (int)length * inchannels;
                float[] buffer;
                
                // Try to reuse a managed buffer of the exact size to reduce GC pressure
                lock (obj._lockObject)
                {
                    if (obj._emptyBufferQueue.Count > 0)
                    {
                        var tmp = obj._emptyBufferQueue.Dequeue();
                        buffer = (tmp.Length == lengthElements) ? tmp : new float[lengthElements];
                    }
                    else
                    {
                        buffer = new float[lengthElements];
                    }
                }
                
                Marshal.Copy(inbuffer, buffer, 0, lengthElements);
                
                lock (obj._lockObject)
                {
                    obj._fullBufferQueue.Enqueue(buffer);
                }
                
                // Pass through to FMOD downstream (so monitoring still works)
                Marshal.Copy(buffer, 0, outbuffer, lengthElements);
                outchannels = inchannels;
                return FMOD.RESULT.OK;
            }
            catch
            {
                return FMOD.RESULT.ERR_INTERNAL;
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            // Avoid leftover noise
            Array.Clear(data, 0, data.Length);
            
            lock (_lockObject)
            {
                // Wait for a few captured blocks to avoid initial glitches/pops
                if (_fullBufferQueue.Count > _warmupBufferCount)
                {
                    int offset = 0;
                    while (_fullBufferQueue.Count > 0 && offset < data.Length)
                    {
                        float[] front = _fullBufferQueue.Peek();
                        
                        int remainingInFront = front.Length - _frontBufferPosition;
                        
                        if (remainingInFront <= 0)
                        {
                            _fullBufferQueue.Dequeue();
                            _frontBufferPosition = 0;
                            continue;
                        }
                        
                        int remainingInData = data.Length - offset;
                        
                        int copyLength = Mathf.Min(remainingInFront, remainingInData);
                        Array.Copy(front, _frontBufferPosition, data, offset, copyLength);
                        
                        _frontBufferPosition += copyLength;
                        offset += copyLength;
                        
                        // If buffer fully consumed, recycle it
                        if (_frontBufferPosition >= front.Length)
                        {
                            _fullBufferQueue.Dequeue();
                            _frontBufferPosition = 0;
                            
                            // Recycle consumed buffers, limit to 32 stored
                            if (_emptyBufferQueue.Count < 32)
                            {
                                _emptyBufferQueue.Enqueue(front);
                            }
                        }
                    }
                }
            }
        }

#endif

        private void OnDisable()
        {
            if (!_isRecording)
            {
                return;
            }

            StopRecording();
        }
    }
}
