using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider motorVolumeSlider;


    void Start()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
            LoadVolume();
        else
        {
            SetMasterVolume();
            SetMotorVolume();
            SetSFXVolume();
            SetMusicVolume();
        }
    }
    public void SetMasterVolume()
    {
        float volume = masterVolumeSlider.value;
        audioMixer.SetFloat("MasterSounds", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMotorVolume()
    {
        float volume = motorVolumeSlider.value;
        audioMixer.SetFloat("MotorSounds", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MotorVolume", volume);
    }

    public void SetSFXVolume()
    {
        float volume = sfxVolumeSlider.value;
        audioMixer.SetFloat("SFXSounds", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void SetMusicVolume()
    {
        float volume = musicVolumeSlider.value;
        audioMixer.SetFloat("MusicSounds", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    private void LoadVolume()
    {
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        motorVolumeSlider.value = PlayerPrefs.GetFloat("MotorVolume");
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume");

        SetMasterVolume();
        SetMotorVolume();
        SetSFXVolume();
        SetMusicVolume();
    }




}
