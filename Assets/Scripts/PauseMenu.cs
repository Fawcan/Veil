using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic; 
using System.Linq; 
using UnityEngine.Audio; // NEW: Required for AudioMixer

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI References")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;

    [Header("Audio")]
    public AudioMixer masterMixer; // NEW: Reference to the Audio Mixer asset

    [Header("Settings Controls - Gameplay")]
    public Slider volumeSlider; 
    public Slider musicVolumeSlider; // NEW: Music Volume Slider
    public Slider lookSensitivitySlider; 
    public Slider fovSlider; 
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown displayModeDropdown;
    public Toggle invertYToggle; 

    [Header("Settings Controls - Graphics")]
    public Toggle vSyncToggle; 
    public TMP_Dropdown framerateDropdown; 
    public TMP_Dropdown qualityDropdown; 
    public TMP_Dropdown antiAliasingDropdown; 

    [Header("Settings Displays")]
    public TextMeshProUGUI volumeText; 
    public TextMeshProUGUI sensitivityText; 
    public TextMeshProUGUI fovText; 

    private FirstPersonController controller;
    private Resolution[] resolutions; 
    
    // Arrays to map Dropdown indices to actual Unity values
    private readonly int[] targetFrameRates = { -1, 30, 60, 120, 144 }; // -1 = Unlimited
    private readonly int[] antiAliasingValues = { 0, 2, 4, 8 }; // 0 = Off

    void Start()
    {
        // Find the controller instance in the scene
        controller = FindFirstObjectByType<FirstPersonController>();
        if (controller == null)
        {
            Debug.LogError("FirstPersonController not found! Sensitivity/FOV won't work.");
        }

        // Ensure menus are hidden at the start
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (settingsMenuUI != null) settingsMenuUI.SetActive(false);
        
        GameIsPaused = false;
        Time.timeScale = 1f;

        // --- Initialize all UI and Settings ---
        InitializeResolutionDropdown();
        InitializeDisplayModeDropdown();
        InitializeVSyncToggle();
        InitializeFrameRateDropdown();
        InitializeQualityDropdown();
        InitializeAntiAliasingDropdown();

        // Initialize Gameplay/Audio Settings (using default values from the last prompt)
        InitializeGameplaySettings();
        InitializeAudioSettings();
    }

    // --- INITIALIZATION HELPERS ---

    void InitializeGameplaySettings()
    {
        // 1. Master Volume (Default 75%)
        float defaultVolume = 0.75f;
        AudioListener.volume = defaultVolume;
        if (volumeSlider != null) volumeSlider.value = defaultVolume;
        SetVolume(defaultVolume); 

        // 2. Field of View (Default 80)
        float defaultFOV = 80f;
        if (fovSlider != null) fovSlider.value = defaultFOV; 
        SetFOV(defaultFOV); 

        // 3. Look Sensitivity (Default 0.1f)
        float defaultSensitivity = 0.1f;
        if (lookSensitivitySlider != null) lookSensitivitySlider.value = defaultSensitivity;
        SetLookSensitivity(defaultSensitivity); 

        // 4. Invert Y (Default False)
        bool defaultInvertY = false;
        if (invertYToggle != null) invertYToggle.isOn = defaultInvertY;
        SetInvertY(defaultInvertY);
    }
    
    // NEW: Initialization for Music Volume
    void InitializeAudioSettings()
    {
        float defaultMusicVolume = 0.75f; // Default 75%

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = defaultMusicVolume;
        }

        // Must call the setter to apply the default value to the mixer
        SetMusicVolume(defaultMusicVolume);
    }

    void InitializeResolutionDropdown()
    {
        resolutions = Screen.resolutions.Select(r => new Resolution { width = r.width, height = r.height }).Distinct().ToArray();
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }
    }

    void InitializeDisplayModeDropdown()
    {
        if (displayModeDropdown != null)
        {
            displayModeDropdown.ClearOptions();
            List<string> displayOptions = new List<string> { "Fullscreen", "Windowed Fullscreen", "Windowed" };
            displayModeDropdown.AddOptions(displayOptions);

            FullScreenMode currentMode = Screen.fullScreenMode;
            int currentModeIndex = 0; 

            if (currentMode == FullScreenMode.ExclusiveFullScreen) currentModeIndex = 0;
            else if (currentMode == FullScreenMode.FullScreenWindow) currentModeIndex = 1;
            else if (currentMode == FullScreenMode.Windowed) currentModeIndex = 2;

            displayModeDropdown.value = currentModeIndex;
            displayModeDropdown.RefreshShownValue();
        }
    }

    void InitializeVSyncToggle()
    {
        if (vSyncToggle != null)
        {
            // VSync is enabled if vSyncCount > 0
            vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
        }
    }

    void InitializeFrameRateDropdown()
    {
        if (framerateDropdown != null)
        {
            framerateDropdown.ClearOptions();
            
            List<string> options = new List<string>();
            int currentRate = Application.targetFrameRate;
            int currentIndex = 0;

            for (int i = 0; i < targetFrameRates.Length; i++)
            {
                if (targetFrameRates[i] == -1)
                {
                    options.Add("Unlimited");
                }
                else
                {
                    options.Add(targetFrameRates[i].ToString() + " FPS");
                }

                if (targetFrameRates[i] == currentRate)
                {
                    currentIndex = i;
                }
            }

            framerateDropdown.AddOptions(options);
            framerateDropdown.value = currentIndex;
            framerateDropdown.RefreshShownValue();
        }
    }

    void InitializeQualityDropdown()
    {
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            // Get the names of the quality levels defined in Project Settings
            qualityDropdown.AddOptions(QualitySettings.names.ToList());
            
            // Set the dropdown value to the current quality level index
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }
    }

    void InitializeAntiAliasingDropdown()
    {
        if (antiAliasingDropdown != null)
        {
            antiAliasingDropdown.ClearOptions();
            List<string> options = new List<string> { "Off", "2x MSAA", "4x MSAA", "8x MSAA" };
            antiAliasingDropdown.AddOptions(options);

            int currentAA = QualitySettings.antiAliasing;
            int currentIndex = 0;

            if (currentAA == 2) currentIndex = 1;
            else if (currentAA == 4) currentIndex = 2;
            else if (currentAA == 8) currentIndex = 3;

            antiAliasingDropdown.value = currentIndex;
            antiAliasingDropdown.RefreshShownValue();
        }
    }


    // --- PAUSE MENU CONTROL ---

    public void TogglePause()
    {
        if (GameIsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false);
        
        Time.timeScale = 1f; 
        GameIsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        
        Time.timeScale = 0f; 
        GameIsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }


    // --- SETTINGS FUNCTIONS (Audio & Gameplay) ---
    
    // Master Volume
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        if (volumeText != null)
        {
            volumeText.text = Mathf.RoundToInt(volume * 100f).ToString() + "%";
        }
    }

    // NEW: Music Volume
    public void SetMusicVolume(float volume)
    {
        if (masterMixer != null)
        {
            // Converts linear slider value (0.0 to 1.0) to logarithmic decibels (-80.0 to 0.0)
            float dbVolume = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
            masterMixer.SetFloat("MusicVolume", dbVolume);
        }
    }

    public void SetLookSensitivity(float sensitivity)
    {
        if (controller != null)
        {
            controller.SetLookSensitivity(sensitivity);
        }
        if (sensitivityText != null)
        {
            sensitivityText.text = sensitivity.ToString("F1");
        }
    }

    public void SetFOV(float fov)
    {
        if (Camera.main != null)
        {
            Camera.main.fieldOfView = fov;
        }
        if (fovText != null)
        {
            fovText.text = Mathf.RoundToInt(fov).ToString();
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        // Get the Resolution struct from the array based on the dropdown index
        Resolution resolution = resolutions[resolutionIndex];
        
        // Apply the new resolution, keeping the current fullscreen mode
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
    }

    public void SetDisplayMode(int modeIndex)
    {
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;

        if (modeIndex == 0) // Fullscreen
            mode = FullScreenMode.ExclusiveFullScreen;
        else if (modeIndex == 1) // Windowed Fullscreen (Borderless)
            mode = FullScreenMode.FullScreenWindow;
        else if (modeIndex == 2) // Windowed
            mode = FullScreenMode.Windowed;

        // Apply the new display mode
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);
    }

    public void SetInvertY(bool inverted)
    {
        if (controller != null)
        {
            controller.SetInvertY(inverted);
        }
    }

    // --- SETTINGS FUNCTIONS (Graphics) ---

    public void SetVSync(bool isVSyncOn)
    {
        // 1 = VSync On (1 frame interval), 0 = VSync Off
        QualitySettings.vSyncCount = isVSyncOn ? 1 : 0;
    }

    public void SetTargetFrameRate(int index)
    {
        // Use the index to look up the rate from the array
        int frameRate = targetFrameRates[index];
        Application.targetFrameRate = frameRate;
    }

    public void SetQuality(int index)
    {
        // Use the index directly. The second argument (true) ensures settings are applied immediately.
        QualitySettings.SetQualityLevel(index, true);
    }

    public void SetAntiAliasing(int index)
    {
        // Use the index to look up the corresponding AA value (0, 2, 4, 8)
        int aaValue = antiAliasingValues[index];
        QualitySettings.antiAliasing = aaValue;
    }
}