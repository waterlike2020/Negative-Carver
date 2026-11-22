using NegativeCarver.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NegativeCarver.Unity
{
    public sealed class PilotHud : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private CutterController cutter;

        private Text scoreText;
        private Text stageText;
        private Text dropText;
        private Text chainText;
        private Text statusText;
        private GameObject resultPanel;
        private Text titleText;
        private Button undoButton;
        private Button redoButton;

        private void Awake()
        {
            EnsureEventSystem();
            var canvasObject = new GameObject("PilotHudCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PilotRules.MobileWidth, PilotRules.MobileHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            BuildCommandButtons(canvasObject.transform);
            BuildHistoryButtons(canvasObject.transform);
            scoreText = CreateLine(canvasObject.transform, -16f);
            stageText = CreateLine(canvasObject.transform, -56f);
            dropText = CreateLine(canvasObject.transform, -96f);
            chainText = CreateLine(canvasObject.transform, -136f);
            statusText = CreateLine(canvasObject.transform, -176f);
            BuildResultPanel(canvasObject.transform);
        }

        private void Update()
        {
            if (grid == null || cutter == null || scoreText == null || stageText == null || dropText == null || chainText == null || statusText == null || resultPanel == null || titleText == null || undoButton == null || redoButton == null)
            {
                return;
            }

            undoButton.interactable = cutter.CanUndo;
            redoButton.interactable = cutter.CanRedo;
            scoreText.text = "Score " + grid.Match.ShownScore(grid.CheckCoreGemFreed());
            stageText.text = "Stage " + grid.Match.Stage;
            dropText.text = "Drops " + grid.Match.DropCount;
            chainText.text = "Chain " + grid.Match.ChainCount;
            statusText.text = MatchStatus.Describe(grid.CheckCoreGemFreed(), grid.Match.Defeated);
            resultPanel.SetActive(statusText.text != MatchStatus.Playing);
            titleText.text = statusText.text;
        }

        private void BuildCommandButtons(Transform parent)
        {
            CreateCommandButton(parent, "Left", -300f, CutterCommand.Left);
            CreateCommandButton(parent, "Right", -100f, CutterCommand.Right);
            CreateCommandButton(parent, "Rotate", 100f, CutterCommand.Rotate);
            CreateCommandButton(parent, "Drop", 300f, CutterCommand.HardDrop);
        }

        private void BuildHistoryButtons(Transform parent)
        {
            undoButton = CreateHistoryButton(parent, "Undo", -300f, UndoTurn);
            redoButton = CreateHistoryButton(parent, "Redo", -100f, RedoTurn);
        }

        private Button CreateHistoryButton(Transform parent, string label, float x, UnityAction listen)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = Color.white;
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(listen);
            var text = CreateLine(buttonObject.transform, -8f);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.sizeDelta = new Vector2(160f, 48f);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, 96f);
            rect.sizeDelta = new Vector2(160f, 56f);
            return button;
        }

        private void UndoTurn()
        {
            if (cutter == null)
            {
                return;
            }

            cutter.Undo();
        }

        private void RedoTurn()
        {
            if (cutter == null)
            {
                return;
            }

            cutter.Redo();
        }

        private void CreateCommandButton(Transform parent, string label, float x, CutterCommand command)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = Color.white;
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => Enqueue(command));
            var text = CreateLine(buttonObject.transform, -8f);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.sizeDelta = new Vector2(160f, 48f);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, 24f);
            rect.sizeDelta = new Vector2(160f, 56f);
        }

        private void Enqueue(CutterCommand command)
        {
            if (cutter == null)
            {
                return;
            }

            cutter.Enqueue(command);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildResultPanel(Transform parent)
        {
            var panel = new GameObject("ResultPanel");
            panel.transform.SetParent(parent, false);
            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            var panelRect = image.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(480f, 240f);

            titleText = CreateLine(panel.transform, -28f);
            titleText.alignment = TextAnchor.UpperCenter;
            titleText.color = Color.white;
            titleText.rectTransform.sizeDelta = new Vector2(440f, 48f);

            var buttonObject = new GameObject("Restart");
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.white;
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(RestartMatch);
            var label = CreateLine(buttonObject.transform, -8f);
            label.text = "Next";
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.sizeDelta = new Vector2(200f, 40f);
            var buttonRect = buttonImage.rectTransform;
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, -48f);
            buttonRect.sizeDelta = new Vector2(200f, 48f);

            resultPanel = panel;
            resultPanel.SetActive(false);
        }

        private void RestartMatch()
        {
            if (grid == null)
            {
                return;
            }

            grid.AdvanceStage();
        }

        private static Text CreateLine(Transform parent, float y)
        {
            var line = new GameObject("HudLine");
            line.transform.SetParent(parent, false);
            var text = line.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = Color.black;
            text.alignment = TextAnchor.UpperLeft;
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, y);
            rect.sizeDelta = new Vector2(640f, 40f);
            return text;
        }
    }
}
