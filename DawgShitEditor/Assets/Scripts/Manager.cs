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
            public List<byte[]> Layers = new List<byte[]>();
        }
        public List<Page> Pages = new List<Page>();
    }

    [SerializeField] private Vector2 _pageResolution;
    [SerializeField] private PageManager _pageManager;
    [SerializeField] private Button _newTextButton;
    [SerializeField] private RectTransform _textPrefab;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;

    private Transform GetTextContainer()
    {
        return _refs.DrawCanvas.transform.Find("TextContainer");
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

        for (int p = 0; p < _pageManager.PageCount; p++)
        {
            var pageUPaint = _pageManager.GetPageUPaint(p);
            var pageState = loadState.Pages[p];

            // reset layers
            while (pageUPaint.LayerCount > 0)
                pageUPaint.RemoveLayer(0);
            while (pageUPaint.LayerCount < pageState.Layers.Count)
                pageUPaint.AddLayer();

            for (int l = 0; l < pageUPaint.LayerCount; l++)
            {
                var layerTexture = pageUPaint.GetLayerTexture(l);

                // restore texture state
                layerTexture.LoadRawTextureData(pageState.Layers[l]);
                layerTexture.Apply();
            }
        }

        ScreenMessagePositive("Load successful");
    }

    private void OnSaveClick()
    {
        SaveState saveState = new SaveState();
        for (int p = 0; p < _pageManager.PageCount; p++)
        {
            var pageUPaint = _pageManager.GetPageUPaint(p);
            var pageState = new SaveState.Page();

            for (int l = 0; l < pageUPaint.LayerCount; l++)
            {
                var layerTexture = pageUPaint.GetLayerTexture(l);
                pageState.Layers.Add(layerTexture.GetRawTextureData());
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
