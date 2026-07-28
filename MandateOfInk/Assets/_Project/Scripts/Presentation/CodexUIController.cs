using System.Collections.Generic;
using MandateOfInk.Combat;
using MandateOfInk.Core.Events;
using MandateOfInk.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MandateOfInk.Presentation
{
    // 고서 장서(인벤토리) UI — I 키로 열고 닫는다 [가정]. 펼친 고서(한지) 위에
    // 왼쪽 끝=수집 목록(미수집은 「??」), 가운데=낱장 3D 뷰어(드래그 회전·휠 확대),
    // 오른쪽=선택한 장의 출전·한글 풀이·세계관 주석. 한문 원문은 3D 낱장 위에 조판된다.
    // UI는 코드로 조립(재현 가능·씬 오염 최소) — 스프라이트는 Recraft 산출물.
    public sealed class CodexUIController : MonoBehaviour
    {
        [SerializeField] private Sprite _panelSprite;      // 펼친 고서 배경
        [SerializeField] private Sprite _pageIconSprite;   // 목록 슬롯 낱장 아이콘
        [SerializeField] private Font _font;
        [SerializeField] private HaeryePageSO[] _allPages; // SortOrder 순
        [SerializeField] private BoolEventChannelSO _codexOpened; // 커서/시점 글루가 구독
        [Header("[가정]")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.I;

        private CodexInventory _inventory;
        private CodexPageViewer _viewer;
        private GameObject _root;
        private Text _titleText;
        private Text _sourceText;
        private Text _bodyText;
        private Text _countText;
        private readonly List<Text> _slotLabels = new List<Text>();
        private int _selected;
        private bool _open;

        private static readonly Color InkDark = new Color(0.17f, 0.15f, 0.13f, 1f);
        private static readonly Color InkFaint = new Color(0.35f, 0.32f, 0.28f, 0.55f);

        private void Start()
        {
            var cc = FindFirstObjectByType<CharacterController>();
            if (cc != null) _inventory = cc.GetComponent<CodexInventory>();
            if (_inventory != null) _inventory.OnChanged += Refresh;
            _viewer = gameObject.AddComponent<CodexPageViewer>();
            BuildUI();
            SetOpen(false);
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.OnChanged -= Refresh;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) SetOpen(!_open);
        }

        private void SetOpen(bool open)
        {
            _open = open;
            if (_root != null) _root.SetActive(open);
            if (_viewer != null) _viewer.SetActiveStage(open);
            if (open) Refresh();
            if (_codexOpened != null) _codexOpened.Raise(open);
        }

        private void BuildUI()
        {
            // 슬롯 버튼·드래그가 필요 — EventSystem이 없으면 만든다
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            _root = new GameObject("CodexCanvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            _root.AddComponent<GraphicRaycaster>();

            // 어둑한 배경막
            var dim = MakeRect("Dim", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.06f, 0.05f, 0.045f, 0.55f);

            // 펼친 고서 패널
            var panel = MakeRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1460f, 820f), Vector2.zero);
            var panelImg = panel.gameObject.AddComponent<Image>();
            if (_panelSprite != null) panelImg.sprite = _panelSprite;
            else panelImg.color = new Color(0.93f, 0.89f, 0.8f, 1f);

            // 제목(장서)과 수집 수
            _countText = MakeText("Count", panel, new Vector2(0.5f, 1f), new Vector2(0f, -44f),
                new Vector2(600f, 40f), 26, TextAnchor.MiddleCenter, InkDark);
            _countText.text = "藏書";

            // 왼쪽 끝 — 수집 목록
            for (int i = 0; i < (_allPages != null ? _allPages.Length : 0); i++)
            {
                int index = i;
                var slot = MakeRect($"Slot_{i}", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(290f, 58f), new Vector2(42f, -104f - i * 62f));
                slot.pivot = new Vector2(0f, 1f);

                var btnImg = slot.gameObject.AddComponent<Image>();
                btnImg.color = new Color(0f, 0f, 0f, 0.02f); // 클릭 영역
                var btn = slot.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => { _selected = index; Refresh(); });

                var icon = MakeRect("Icon", slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(34f, 44f), new Vector2(24f, 0f));
                var iconImg = icon.gameObject.AddComponent<Image>();
                if (_pageIconSprite != null) iconImg.sprite = _pageIconSprite;
                iconImg.preserveAspect = true;

                var label = MakeText("Label", slot, new Vector2(0f, 0.5f), new Vector2(160f, 0f),
                    new Vector2(230f, 52f), 19, TextAnchor.MiddleLeft, InkDark);
                _slotLabels.Add(label);
            }

            // 가운데 — 낱장 3D 뷰어(드래그 회전·휠 확대)
            var viewerRect = MakeRect("PageViewer", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(490f, 620f), new Vector2(-135f, -14f));
            var raw = viewerRect.gameObject.AddComponent<RawImage>();
            raw.texture = _viewer.Texture;
            var relay = viewerRect.gameObject.AddComponent<CodexPageDragRelay>();
            relay.Viewer = _viewer;

            var viewerHint = MakeText("ViewerHint", panel, new Vector2(0.5f, 0.5f), new Vector2(-135f, -350f),
                new Vector2(490f, 26f), 17, TextAnchor.MiddleCenter, InkFaint);
            viewerHint.text = "드래그: 낱장 돌려보기 · 휠: 당겨보기";

            // 오른쪽 — 출전·한글 풀이
            _titleText = MakeText("Title", panel, new Vector2(1f, 1f), new Vector2(-320f, -104f),
                new Vector2(560f, 44f), 28, TextAnchor.MiddleCenter, InkDark);
            _sourceText = MakeText("Source", panel, new Vector2(1f, 1f), new Vector2(-320f, -148f),
                new Vector2(560f, 30f), 20, TextAnchor.MiddleCenter, new Color(0.3f, 0.15f, 0.1f, 0.9f));
            _bodyText = MakeText("Body", panel, new Vector2(1f, 1f), new Vector2(-320f, -450f),
                new Vector2(580f, 540f), 22, TextAnchor.UpperLeft, InkDark);

            var hint = MakeText("Hint", panel, new Vector2(0.5f, 0f), new Vector2(0f, 30f),
                new Vector2(700f, 30f), 18, TextAnchor.MiddleCenter, InkFaint);
            hint.text = $"{_toggleKey}: 덮기";
        }

        private void Refresh()
        {
            if (_allPages == null || _slotLabels.Count == 0) return;
            int collected = 0;
            for (int i = 0; i < _allPages.Length && i < _slotLabels.Count; i++)
            {
                bool has = _inventory != null && _inventory.Has(_allPages[i]);
                if (has) collected++;
                _slotLabels[i].text = has ? _allPages[i].Title : "?? — 아직 찾지 못한 낱장";
                _slotLabels[i].color = has ? InkDark : InkFaint;
                _slotLabels[i].fontStyle = i == _selected ? FontStyle.Bold : FontStyle.Normal;
            }
            _countText.text = $"藏書   {collected} / {_allPages.Length}";

            var page = _selected >= 0 && _selected < _allPages.Length ? _allPages[_selected] : null;
            bool hasSelected = page != null && _inventory != null && _inventory.Has(page);
            _titleText.text = hasSelected ? page.Title : "미수록";
            _sourceText.text = hasSelected && !string.IsNullOrEmpty(page.SourceBook)
                ? $"出典 — {page.SourceBook}" : "";
            _bodyText.text = hasSelected ? page.KoreanText + "\n\n" + page.Lore
                : "낱장을 찾아 서책에 되엮으면 그 이치가 드러난다.";
            if (_viewer != null) _viewer.ShowPage(hasSelected ? page : null);
        }

        private RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            if (size != Vector2.zero) rt.sizeDelta = size;
            else { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            rt.anchoredPosition = anchoredPos;
            return rt;
        }

        private Text MakeText(string name, RectTransform parent, Vector2 anchor, Vector2 anchoredPos,
            Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var rt = MakeRect(name, parent, anchor, anchor, size, anchoredPos);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
