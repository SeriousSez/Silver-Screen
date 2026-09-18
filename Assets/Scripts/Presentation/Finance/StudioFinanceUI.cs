using System;
using System.Text;
using SilverScreen.Domain.Finance;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SilverScreen.Presentation.Finance
{
    public sealed class StudioFinanceUI : MonoBehaviour
    {
        private StudioEconomyDriver _driver;
        private Button _cashButton;
        private TextMeshProUGUI _cashText;
        private GameObject _panel;
        private TextMeshProUGUI _summaryText;
        private TextMeshProUGUI _transactionsText;
        private bool _uiBuilt;

        public bool IsPanelVisible => _panel != null && _panel.activeSelf;
        public event Action<bool> OnPanelVisibilityChanged;

        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.075f, 0.98f);
        private static readonly Color Gold = new Color(0.95f, 0.70f, 0.18f, 1f);

        private void Start()
        {
            _driver = GetComponent<StudioEconomyDriver>();
            if (_driver == null || _driver.FinanceService == null) return;

            EnsureUIBuilt();
            _driver.FinanceService.OnFinancesChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_driver?.FinanceService != null)
            {
                _driver.FinanceService.OnFinancesChanged -= Refresh;
            }
        }

        private void BuildUI()
        {
            if (_uiBuilt) return;
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            _uiBuilt = true;

            var buttonRect = CreateRect(
                "StudioCashButton",
                canvas.transform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-28f, -28f),
                new Vector2(250f, 52f));
            buttonRect.pivot = Vector2.one;
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.075f, 0.09f, 0.105f, 0.98f);
            _cashButton = buttonRect.gameObject.AddComponent<Button>();
            _cashButton.targetGraphic = buttonImage;
            _cashButton.onClick.AddListener(TogglePanel);
            _cashText = CreateText(buttonRect, "Cash: $250,000", Vector2.zero, new Vector2(250f, 52f), 20f, TextAlignmentOptions.Center, Gold);

            var backdrop = CreateRect(
                "StudioFinanceBackdrop",
                canvas.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.018f, 0.72f);
            _panel = backdrop.gameObject;

            var panelRect = CreateRect(
                "StudioFinancePanel",
                backdrop,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 680f));
            panelRect.gameObject.AddComponent<Image>().color = PanelColor;
            var outline = panelRect.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateText(panelRect, "STUDIO FINANCES", new Vector2(0f, 292f), new Vector2(650f, 48f), 29f, TextAlignmentOptions.Center, Gold);
            _summaryText = CreateText(panelRect, string.Empty, new Vector2(0f, 150f), new Vector2(620f, 210f), 20f, TextAlignmentOptions.TopLeft, Color.white);
            CreateText(panelRect, "RECENT TRANSACTIONS", new Vector2(0f, 33f), new Vector2(620f, 32f), 18f, TextAlignmentOptions.MidlineLeft, Gold);
            _transactionsText = CreateText(panelRect, string.Empty, new Vector2(0f, -143f), new Vector2(620f, 300f), 17f, TextAlignmentOptions.TopLeft, new Color(0.86f, 0.87f, 0.88f));

            var closeRect = CreateRect(
                "CloseFinanceButton",
                panelRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -294f),
                new Vector2(180f, 44f));
            var closeImage = closeRect.gameObject.AddComponent<Image>();
            closeImage.color = Gold;
            var closeButton = closeRect.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => SetPanelVisible(false));
            CreateText(closeRect, "CLOSE", Vector2.zero, new Vector2(180f, 44f), 17f, TextAlignmentOptions.Center, new Color(0.08f, 0.07f, 0.05f));

            _panel.SetActive(false);
        }

        private void TogglePanel()
        {
            SetPanelVisible(!IsPanelVisible);
        }

        public void SetPanelVisible(bool visible)
        {
            EnsureUIBuilt();
            if (_panel == null || _panel.activeSelf == visible) return;
            _panel.SetActive(visible);
            if (visible) Refresh();
            OnPanelVisibilityChanged?.Invoke(visible);
        }

        public void SetStandaloneCashButtonVisible(bool visible)
        {
            EnsureUIBuilt();
            if (_cashButton != null) _cashButton.gameObject.SetActive(visible);
        }

        private void EnsureUIBuilt()
        {
            if (!_uiBuilt) BuildUI();
        }

        private void Refresh()
        {
            if (_driver == null || _cashText == null) return;

            var finances = _driver.FinanceService;
            var accounting = _driver.AccountingService;
            _cashText.text = $"Cash: {FormatMoney(finances.CurrentCash)}";

            if (_summaryText != null)
            {
                _summaryText.text =
                    $"Current Cash\t<b>{FormatMoney(finances.CurrentCash)}</b>\n" +
                    $"Total Income\t<color=#78D69A>{FormatMoney(finances.TotalIncome)}</color>\n" +
                    $"Total Expenses\t<color=#E08A77>{FormatMoney(-finances.TotalExpenses)}</color>\n\n" +
                    $"Estimated Monthly Payroll\t{FormatMoney(accounting.EstimatedMonthlyPayroll)}\n" +
                    $"Estimated Building Upkeep\t{FormatMoney(accounting.EstimatedMonthlyBuildingUpkeep)}\n" +
                    $"Estimated Operating Costs\t<b>{FormatMoney(accounting.EstimatedMonthlyOperatingCosts)}</b>";
            }

            if (_transactionsText == null) return;

            var transactions = finances.Transactions;
            var builder = new StringBuilder();
            int first = Mathf.Max(0, transactions.Count - 8);
            for (int i = transactions.Count - 1; i >= first; i--)
            {
                var transaction = transactions[i];
                string color = transaction.IsIncome ? "#78D69A" : "#E08A77";
                builder.Append(transaction.Date.ToFormattedString());
                builder.Append("   ");
                builder.Append(transaction.Description);
                builder.Append("   <color=");
                builder.Append(color);
                builder.Append("><b>");
                builder.Append(FormatMoney(transaction.Amount, showPositiveSign: true));
                builder.AppendLine("</b></color>");
            }

            _transactionsText.text = builder.ToString();
        }

        public static string FormatMoney(Money value, bool showPositiveSign = false)
        {
            long absoluteCents = value.Cents < 0 ? -value.Cents : value.Cents;
            string prefix = value.Cents < 0 ? "-$" : value.Cents > 0 && showPositiveSign ? "+$" : "$";
            return $"{prefix}{absoluteCents / 100:N0}";
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string value,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            var rect = CreateRect(
                "Text",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                position,
                size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
    }
}
