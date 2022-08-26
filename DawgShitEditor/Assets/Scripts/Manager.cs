using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CCC.UPaintGUI;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngineX;
using CCC.UPaintFileBrowser;

public class Manager : UPaintGUIManager
{
    [System.Serializable]
    private class SaveState
    {
        [System.Serializable]
        public class Page
        {
            [System.Serializable]
            public class TextField
            {
                public int FontStyle;
                public string Text;
                public SerializableVector2 Position;
                public float FontSize;
                public float SizeDeltaX;
            }

            [System.Serializable]
            public class Layer
            {
                public byte[] TextureData;
                public bool Visible;
            }

            public List<Layer> Layers = new List<Layer>();
            public List<TextField> Texts = new List<TextField>();
        }

        [System.Serializable]
        public struct SerializableColor
        {
            public float R;
            public float G;
            public float B;
            public float A;

            public SerializableColor(float r, float g, float b, float a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public static implicit operator Color(SerializableColor x) => new Color(x.R, x.G, x.B, x.A);
            public static implicit operator SerializableColor(Color x) => new SerializableColor(x.r, x.g, x.b, x.a);
        }

        [System.Serializable]
        public struct SerializableVector2
        {
            public float X;
            public float Y;

            public SerializableVector2(float x, float y)
            {
                X = x;
                Y = y;
            }

            public static implicit operator Vector2(SerializableVector2 x) => new Vector2(x.X, x.Y);
            public static implicit operator SerializableVector2(Vector2 x) => new SerializableVector2(x.x, x.y);
        }

        public List<Page> Pages = new List<Page>();
        public List<SerializableColor> ColorSwatches = new List<SerializableColor>();
    }

    [SerializeField] private Vector2 _pageResolution;
    [SerializeField] private PageManager _pageManager;
    [SerializeField] private Button _newTextButton;
    [SerializeField] private RectTransform _textPrefab;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Image _pasteImage;

    private Transform GetTextContainer()
    {
        return _refs.DrawCanvas.transform.Find("TextContainer");
    }
    private Transform GetTextContainer(UPaintGUI uPaintGUI)
    {
        return uPaintGUI.GetComponentInParent<Canvas>(includeInactive: true).transform.Find("TextContainer");
    }

    protected override void Awake()
    {
        base.Awake();

        _newTextButton.onClick.AddListener(OnNewTextButtonClicked);
        _saveButton.onClick.AddListener(OnSaveClick);
        _loadButton.onClick.AddListener(OnLoadClick);

        _pageManager.ActivePageChanged += OnActivePageChanged;
    }

    private void OnLoadClick()
    {
        SaveState loadState = null;

        string[] loadLocations = StandaloneFileBrowser.OpenFilePanel("Select File to Load", GetProjectPath(), "upaint", multiselect: false);
        string loadLocation = loadLocations.Length == 0 ? null : loadLocations[0];

        if (string.IsNullOrEmpty(loadLocation))
            return;

        try
        {
            BinaryFormatter bf = new BinaryFormatter(); // to fix
            using (var stream = File.Open(loadLocation, FileMode.Open))
            {
                loadState = (SaveState)bf.Deserialize(stream);
            }
        }
        catch (Exception e)
        {
            ScreenMessageNegative("Failed to load: " + e.Message);
        }

        if (loadState == null)
            return;

        // adjust page count
        while (_pageManager.PageCount < loadState.Pages.Count)
        {
            _pageManager.AddPage();
            ApplyResolution();
        }

        while (_pageManager.PageCount > loadState.Pages.Count)
            _pageManager.RemovePage(_pageManager.PageCount - 1);

        // Update colors
        while (_refs.ColorSwatchBar.ColorCount > 0)
        {
            _refs.ColorSwatchBar.RemoveColor(_refs.ColorSwatchBar.ColorCount - 1);
        }
        foreach (var color in loadState.ColorSwatches)
        {
            _refs.ColorSwatchBar.AddColor(color);
        }

        for (int p = 0; p < _pageManager.PageCount; p++)
        {
            var pageUPaint = _pageManager.GetPageUPaint(p);
            var pageState = loadState.Pages[p];

            // destroy existing texts
            var textContainer = GetTextContainer(pageUPaint);
            foreach (var controllableText in textContainer.GetComponentsInChildren<ControllableText>(includeInactive: true))
            {
                Destroy(controllableText.gameObject);
            }
            // restore texts
            foreach (var textSave in pageState.Texts)
            {
                var controllableText = Instantiate(_textPrefab, textContainer, worldPositionStays: true).GetComponent<ControllableText>();
                controllableText.Text = textSave.Text;
                controllableText.FontSize = textSave.FontSize;
                controllableText.SizeDeltaX = textSave.SizeDeltaX;
                controllableText.FontIndex = textSave.FontStyle;
                controllableText.GetComponent<RectTransform>().anchoredPosition = textSave.Position;
            }

            // reset layers
            while (pageUPaint.LayerCount > 0)
                pageUPaint.RemoveLayer(0);
            while (pageUPaint.LayerCount < pageState.Layers.Count)
                pageUPaint.AddLayer();

            for (int l = 0; l < pageUPaint.LayerCount; l++)
            {
                var layerTexture = pageUPaint.GetLayerTexture(l);

                // restore texture state
                layerTexture.LoadRawTextureData(pageState.Layers[l].TextureData);
                layerTexture.Apply();
                pageUPaint.SetLayerVisible(l, pageState.Layers[l].Visible);
            }
        }

        ScreenMessagePositive("Load successful");
    }

    private void OnSaveClick()
    {
        SaveState saveState = new SaveState();

        // save colors
        foreach (var color in _refs.ColorSwatchBar.GetColors())
        {
            saveState.ColorSwatches.Add(color);
        }

        for (int p = 0; p < _pageManager.PageCount; p++)
        {
            var pageUPaint = _pageManager.GetPageUPaint(p);
            var pageState = new SaveState.Page();

            // save text
            var textContainer = GetTextContainer(pageUPaint);
            foreach (var controllableText in textContainer.GetComponentsInChildren<ControllableText>(includeInactive: true))
            {
                var textSave = new SaveState.Page.TextField();
                textSave.Text = controllableText.Text;
                textSave.FontSize = controllableText.FontSize;
                textSave.SizeDeltaX = controllableText.SizeDeltaX;
                textSave.FontStyle = controllableText.FontIndex;
                textSave.Position = controllableText.GetComponent<RectTransform>().anchoredPosition;
                pageState.Texts.Add(textSave);
            }

            for (int l = 0; l < pageUPaint.LayerCount; l++)
            {
                var layerTexture = pageUPaint.GetLayerTexture(l);
                var savedLayer = new SaveState.Page.Layer()
                {
                    TextureData = layerTexture.GetRawTextureData(),
                    Visible = pageUPaint.IsLayerVisible(l)
                };
                pageState.Layers.Add(savedLayer);
            }

            saveState.Pages.Add(pageState);
        }

        string saveLocation = StandaloneFileBrowser.SaveFilePanel("Select Save Location", GetProjectPath(), defaultName: "Save.upaint", extension: "upaint");

        if (string.IsNullOrEmpty(saveLocation))
            return;

        try
        {
            BinaryFormatter bf = new BinaryFormatter(); // to fix
            using (var stream = File.Open(saveLocation, FileMode.OpenOrCreate))
            {
                bf.Serialize(stream, saveState);
            }
        }
        catch (Exception e)
        {
            ScreenMessageNegative("Failed to save: " + e.Message);
            return;
        }

        ScreenMessagePositive("Save successful");
    }

    private void OnNewTextButtonClicked()
    {
        var rectTransform = Instantiate(_textPrefab, GetTextContainer(), worldPositionStays: true).GetComponent<RectTransform>();
        var worldSpawnPos = (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        rectTransform.position = worldSpawnPos;
    }

    protected override void HandleColorPickingShortcut()
    {
        if (EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.GetComponent<UPaintGUIControllableObject>() != null)
            return;

        base.HandleColorPickingShortcut();
    }

    protected override void HandleWASDMovement()
    {
        // remove arrow keys
        Vector2 move = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector2.up;
        if (Input.GetKey(KeyCode.A))
            move += Vector2.left;
        if (Input.GetKey(KeyCode.S))
            move += Vector2.down;
        if (Input.GetKey(KeyCode.D))
            move += Vector2.right;

        if (move != Vector2.zero)
            move.Normalize();

        _refs.CameraManager.Move(move * Time.deltaTime * _refs.CameraManager.CurrentSize * 1);
    }

    private void OnActivePageChanged(UPaintGUI previousPage, UPaintGUI newPage)
    {
        if (previousPage != null)
            SetColor(previousPage.PaintColor);
        ApplyBrush();
    }

    protected override void LoadPlayerPrefs()
    {
        base.LoadPlayerPrefs();

        // force fixed resolution
        _refs.ResolutionXInputField.text = _pageResolution.x.ToString();
        _refs.ResolutionYInputField.text = _pageResolution.y.ToString();
    }

    protected override void Export()
    {
        try
        {
            _pageManager.ExportAllPages(GetExportPath());
            ScreenMessagePositive("Export successful");
        }
        catch (Exception e)
        {
            ScreenMessageNegative("Failed Export: " + e.Message);
        }
    }

    private string GetProjectPath()
    {
        string fullPath = _refs.SaveLocationInputField.text;

        fullPath = fullPath.Replace('/', '\\');

        if (!fullPath.EndsWith("\\"))
            fullPath += "\\";

        fullPath += $"{_refs.FileNameInputField.text}";

        return fullPath;
    }

    protected override string GetExportPath()
    {
        return $"{GetProjectPath()}\\{{0}}.jpg";
    }
}
