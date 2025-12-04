using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Burmuruk.RPGStarterTemplate.UI.Samples
{
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] GameObject warninigWindow;
        [SerializeField] Button btnEnemiesHealth;
        [SerializeField] Button btnLanguage;
        [SerializeField] Button btnWindowMode;
        [SerializeField] Button btnResolution;
        [SerializeField] Button btnVsync;
        [SerializeField] Button btnFPSLimit;
        [SerializeField] Slider sldVolume;
        [SerializeField] GameObject[] extraOptions;
        [SerializeField] GameObject EOCancelPanel;

        [SerializeField] Button btnApply;
        [SerializeField] Button btnCancel;

        FullScreenMode fullScreenMode;
        int frameRate = 0;
        int vSync = 0;
        int[] resolution = new int[2];
        int language;
        float volume = 0;

        Dictionary<string, Action> changes = new();

        enum GameLanguage
        {
            None,
            Spanish,
            English
        }

        private void Start()
        {
            StartValues();
        }

        public void ShowMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            
            if (gameObject.activeSelf)
                StartValues();
        }

        public void ShowExtraOptions(int idx)
        {
            extraOptions[idx].SetActive(true);
        }

        public void DisableExtraOptions()
        {
            foreach (var option in extraOptions)
            {
                option.SetActive(false);
            }

            EOCancelPanel.SetActive(false);
        }

        public void Setlanguage(int idx)
        {
            GameLanguage language = idx switch
            {
                2 => GameLanguage.Spanish,
                _ => GameLanguage.English,
            };

            btnLanguage.GetComponentInChildren<TextMeshProUGUI>().text = language.ToString();
            CheckChanges();
        }

        public void SetWindowMode(int idx)
        {
            FullScreenMode mode = idx switch
            {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                _ => FullScreenMode.MaximizedWindow,
            };

            if (mode == fullScreenMode)
            {
                if (changes.ContainsKey("FullscreenMode"))
                    changes.Remove("FullscreenMode");
            }
            else
            {
                changes["FullscreenMode"] = () => Screen.fullScreenMode = mode;
            }

            btnWindowMode.GetComponentInChildren<TextMeshProUGUI>().text = mode.ToString();
            CheckChanges();
        }

        public void LimitFPS(int newFrameRate)
         {
             if (newFrameRate == frameRate)
            {
                if (changes.ContainsKey("FrameRate"))
                    changes.Remove("FrameRate");
            }
            else
            {
                changes["FrameRate"] = () => Application.targetFrameRate = newFrameRate;
            }

            btnFPSLimit.GetComponentInChildren<TextMeshProUGUI>().text = newFrameRate <= 0 ? "-" : newFrameRate.ToString();
            CheckChanges();
        }

        public void EnableVSync()
        {
            int newVsync = 0;

            if (btnVsync.GetComponentInChildren<TextMeshProUGUI>().text == "Of")
            {
                btnFPSLimit.enabled = true;
                newVsync = 60;
                btnVsync.GetComponentInChildren<TextMeshProUGUI>().text = "On";
            }
            else
            {
                newVsync = -1;
                btnFPSLimit.enabled = false;
                btnVsync.GetComponentInChildren<TextMeshProUGUI>().text = "Of";
            }

            if (vSync == newVsync)
            {
                if (changes.ContainsKey("VSync"))
                    changes.Remove("VSync");
            }
            else
            {
                changes["VSync"] = () => QualitySettings.vSyncCount = newVsync;
            }

            CheckChanges();
        }

        public void SetResolution(Vector2Int resolution)
        {
            if (resolution.x == this.resolution[0] && resolution.y == this.resolution[1])
            {
                if (changes.ContainsKey("Resolution"))
                    changes.Remove("Resolution");
            }
            else
            {
                changes["Resolution"] = () => Screen.SetResolution(resolution.x, resolution.y, fullScreenMode);
            }

            btnResolution.GetComponentInChildren<TextMeshProUGUI>().text = resolution.x + " x " + resolution.y;
            CheckChanges();
        }

        public void ApplyChanges()
        {
            if (changes == null) return;

            foreach (var change in changes)
            {
                change.Value?.Invoke();
            }

            SaveSettings();
            StartValues();
        }

        public void Cancel()
        {
            foreach (var option in extraOptions)
            {
                if (option.activeSelf)
                {
                    DisableExtraOptions();
                    return;
                }
            }

            changes.Clear();
        }

        private void StartValues()
        {
            LoadSettings();
            UpdateCurrentValues();
            CheckChanges();
            DisableExtraOptions();
        }

        private void CheckChanges()
        {
            if (changes == null || changes.Count <= 0)
            {
                btnApply.enabled = false;
                btnCancel.enabled = false;
            }
            else
            {
                btnApply.enabled = true;
                btnCancel.enabled = true;
            }
        }

        private void UpdateCurrentValues()
        {
            fullScreenMode = Screen.fullScreenMode;
            frameRate = Application.targetFrameRate;
            vSync = QualitySettings.vSyncCount;
            resolution[0] = Screen.currentResolution.width;
            resolution[1] = Screen.currentResolution.width;

            btnLanguage.GetComponentInChildren<TextMeshProUGUI>().text = ((GameLanguage)language).ToString();
            btnWindowMode.GetComponentInChildren<TextMeshProUGUI>().text = fullScreenMode.ToString();
            btnFPSLimit.GetComponentInChildren<TextMeshProUGUI>().text = frameRate <= 0 ? "-" : frameRate.ToString();
            btnVsync.GetComponentInChildren<TextMeshProUGUI>().text = vSync > 0 ? "On" : "Of";
            btnResolution.GetComponentInChildren<TextMeshProUGUI>().text = resolution[0] + " x " + resolution[1];
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("PlayersHealth", 0);
            PlayerPrefs.SetInt("Language", language);
            PlayerPrefs.SetInt("WindowMode", (int)fullScreenMode);
            PlayerPrefs.SetInt("VSync", vSync);
            PlayerPrefs.SetInt("LimitFPS", frameRate);
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.SetString("Resolution", resolution[0].ToString() + "x" + resolution[1].ToString());
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey("WindowMode"))
                Screen.fullScreenMode = (FullScreenMode)PlayerPrefs.GetInt("WindowMode");

            if (PlayerPrefs.HasKey("LimitFPS"))
                Application.targetFrameRate = PlayerPrefs.GetInt("LimitFPS");

            if (PlayerPrefs.HasKey("VSync"))
                QualitySettings.vSyncCount = PlayerPrefs.GetInt("VSync");

            if (PlayerPrefs.HasKey("Resolution"))
            {
                string[] resolution = PlayerPrefs.GetString("Resolution").Split('x');
                Screen.SetResolution(int.Parse(resolution[0]), int.Parse(resolution[1]), Screen.fullScreen);
            }
        }
    }
}
