using MandateOfInk.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace MandateOfInk.Presentation
{
    // 여지도(輿地圖) 화면 — M 키로 펼치고 접는다 [가정]. 대동여지도풍 목판 지도 한 장을
    // 디제틱 소품처럼 보여주는 전체 화면. 미니맵 금지 정책과 별개(플레이어 위치 표시 없음 —
    // 소울라이크식으로 지도는 정보가 아니라 소품이다).
    // UI는 코드로 조립(서책 UI와 같은 결) — 지도 아트는 Recraft 산출물.
    public sealed class MapUIController : MonoBehaviour
    {
        [SerializeField] private Sprite _mapSprite;
        [SerializeField] private Font _font;
        [SerializeField] private BoolEventChannelSO _mapOpened; // 커서/시점 글루가 구독
        [Header("[가정]")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.M;

        private GameObject _root;
        private bool _open;

        private void Start()
        {
            BuildUI();
            SetOpen(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) SetOpen(!_open);
        }

        private void SetOpen(bool open)
        {
            _open = open;
            if (_root != null) _root.SetActive(open);
            if (_mapOpened != null) _mapOpened.Raise(open);
        }

        private void BuildUI()
        {
            _root = new GameObject("MapCanvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 42; // 서책(40) 위 — 동시에 열리면 지도가 앞
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _root.AddComponent<GraphicRaycaster>();

            // 어둑한 배경막
            var dim = new GameObject("Dim");
            dim.transform.SetParent(_root.transform, false);
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.045f, 0.6f);

            // 지도 한 장 — 세로로 긴 목판 지도, 화면 높이에 맞춤
            var map = new GameObject("Map");
            map.transform.SetParent(_root.transform, false);
            var mapRt = map.AddComponent<RectTransform>();
            mapRt.anchorMin = new Vector2(0.5f, 0.5f);
            mapRt.anchorMax = new Vector2(0.5f, 0.5f);
            mapRt.sizeDelta = new Vector2(580f, 1020f);
            var mapImg = map.AddComponent<Image>();
            if (_mapSprite != null) mapImg.sprite = _mapSprite;
            else mapImg.color = new Color(0.93f, 0.89f, 0.8f, 1f);
            mapImg.preserveAspect = true;

            var hint = new GameObject("Hint");
            hint.transform.SetParent(_root.transform, false);
            var hintRt = hint.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.sizeDelta = new Vector2(400f, 30f);
            hintRt.anchoredPosition = new Vector2(0f, 24f);
            var hintText = hint.AddComponent<Text>();
            hintText.font = _font;
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.85f, 0.8f, 0.7f, 0.75f);
            hintText.text = $"{_toggleKey}: 접기";
        }
    }
}
