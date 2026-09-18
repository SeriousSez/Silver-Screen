using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.SimulationTime;

namespace SilverScreen.Presentation.UI
{
    public class SimulationClockUI : MonoBehaviour
    {
        [Header("Service Reference")]
        [SerializeField] private SimulationTimeDriver _timeDriver;

        [Header("Display Elements")]
        [SerializeField] private TextMeshProUGUI _dateTimeText;

        [Header("Speed Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speed1XButton;
        [SerializeField] private Button _speed2XButton;
        [SerializeField] private Button _speed3XButton;

        [Header("Visual Feedback Colors")]
        [SerializeField] private Color _activeButtonBg = new Color(0.92f, 0.72f, 0.20f, 1f); // Gold
        [SerializeField] private Color _activeButtonText = new Color(0.08f, 0.10f, 0.12f, 1f); // Dark Slate
        [SerializeField] private Color _inactiveButtonBg = new Color(0.16f, 0.18f, 0.22f, 1f); // Dark Slate
        [SerializeField] private Color _inactiveButtonText = new Color(0.75f, 0.80f, 0.85f, 1f); // Light Slate

        private ISimulationTimeService _timeService;

        private void Start()
        {
            if (_timeDriver == null)
            {
                _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
            }

            if (_timeDriver != null)
            {
                BindTimeService(_timeDriver.TimeService);
            }

            SetupButtonCallbacks();
        }

        private void OnDestroy()
        {
            if (_timeService != null)
            {
                _timeService.OnMinutePassed -= HandleMinutePassed;
                _timeService.OnSpeedChanged -= HandleSpeedChanged;
            }
        }

        public void BindTimeService(ISimulationTimeService timeService)
        {
            if (_timeService != null)
            {
                _timeService.OnMinutePassed -= HandleMinutePassed;
                _timeService.OnSpeedChanged -= HandleSpeedChanged;
            }

            _timeService = timeService;

            if (_timeService != null)
            {
                _timeService.OnMinutePassed += HandleMinutePassed;
                _timeService.OnSpeedChanged += HandleSpeedChanged;

                UpdateClockDisplay(_timeService.CurrentTime);
                UpdateSpeedButtonVisuals(_timeService.CurrentSpeed);
            }
        }

        private void SetupButtonCallbacks()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(() =>
                {
                    if (_timeService != null) _timeService.SetSpeed(SimulationSpeed.Paused);
                });
            }

            if (_speed1XButton != null)
            {
                _speed1XButton.onClick.AddListener(() =>
                {
                    if (_timeService != null) _timeService.SetSpeed(SimulationSpeed.Normal);
                });
            }

            if (_speed2XButton != null)
            {
                _speed2XButton.onClick.AddListener(() =>
                {
                    if (_timeService != null) _speed2XButton.onClick.RemoveAllListeners();
                });
            }

            // Clean rebinding
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(() => _timeService?.SetSpeed(SimulationSpeed.Paused));
            }
            if (_speed1XButton != null)
            {
                _speed1XButton.onClick.RemoveAllListeners();
                _speed1XButton.onClick.AddListener(() => _timeService?.SetSpeed(SimulationSpeed.Normal));
            }
            if (_speed2XButton != null)
            {
                _speed2XButton.onClick.RemoveAllListeners();
                _speed2XButton.onClick.AddListener(() => _timeService?.SetSpeed(SimulationSpeed.Fast));
            }
            if (_speed3XButton != null)
            {
                _speed3XButton.onClick.RemoveAllListeners();
                _speed3XButton.onClick.AddListener(() => _timeService?.SetSpeed(SimulationSpeed.VeryFast));
            }
        }

        private void HandleMinutePassed(SimulationDateTime dateTime)
        {
            UpdateClockDisplay(dateTime);
        }

        private void HandleSpeedChanged(SimulationSpeed speed)
        {
            UpdateSpeedButtonVisuals(speed);
        }

        private void UpdateClockDisplay(SimulationDateTime dateTime)
        {
            if (_dateTimeText != null)
            {
                _dateTimeText.text = dateTime.ToFormattedString();
            }
        }

        private void UpdateSpeedButtonVisuals(SimulationSpeed activeSpeed)
        {
            SetButtonVisual(_pauseButton, activeSpeed == SimulationSpeed.Paused);
            SetButtonVisual(_speed1XButton, activeSpeed == SimulationSpeed.Normal);
            SetButtonVisual(_speed2XButton, activeSpeed == SimulationSpeed.Fast);
            SetButtonVisual(_speed3XButton, activeSpeed == SimulationSpeed.VeryFast);
        }

        private void SetButtonVisual(Button button, bool isActive)
        {
            if (button == null) return;

            var img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? _activeButtonBg : _inactiveButtonBg;
            }

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = isActive ? _activeButtonText : _inactiveButtonText;
                tmp.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        public void SetupReferences(
            SimulationTimeDriver timeDriver,
            TextMeshProUGUI dateTimeText,
            Button pauseBtn,
            Button speed1Btn,
            Button speed2Btn,
            Button speed3Btn)
        {
            _timeDriver = timeDriver;
            _dateTimeText = dateTimeText;
            _pauseButton = pauseBtn;
            _speed1XButton = speed1Btn;
            _speed2XButton = speed2Btn;
            _speed3XButton = speed3Btn;

            SetupButtonCallbacks();

            if (_timeDriver != null)
            {
                BindTimeService(_timeDriver.TimeService);
            }
        }

        public void SetDisplayVisible(bool visible)
        {
            if (_dateTimeText != null) _dateTimeText.gameObject.SetActive(visible);
            if (_pauseButton != null) _pauseButton.gameObject.SetActive(visible);
            if (_speed1XButton != null) _speed1XButton.gameObject.SetActive(visible);
            if (_speed2XButton != null) _speed2XButton.gameObject.SetActive(visible);
            if (_speed3XButton != null) _speed3XButton.gameObject.SetActive(visible);
        }
    }
}
