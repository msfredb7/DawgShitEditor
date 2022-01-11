using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CCC.UPaintGUI;
using System.IO;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Manager : UPaintGUIManager
{
    [SerializeField] private Vector2 _pageResolution;
    [SerializeField] private PageManager _pageManager;
    [SerializeField] private Button _newTextButton;
    [SerializeField] private RectTransform _textPrefab;

    private Transform GetTextContainer()
    {
        return _refs.DrawCanvas.transform.Find("TextContainer");
    }

    protected override void Awake()
    {
        base.Awake();

        _newTextButton.onClick.AddListener(OnNewTextButtonClicked);

        _pageManager.ActivePageChanged += OnActivePageChanged;
    }

    private void OnNewTextButtonClicked()
    {
        var rectTransform = Instantiate(_textPrefab, GetTextContainer(), worldPositionStays: true).GetComponent<RectTransform>();
        var worldSpawnPos = (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        rectTransform.position = worldSpawnPos;
    }

    protected override void HandleColorPickingShortcut()
    {
        if (EventSystem.current.currentSelectedGameObject?.GetComponent<UPaintGUIControllableObject>() != null)
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
            _refs.Animator.SetTrigger("export successful");
        }
        catch (Exception e)
        {
            _refs.FailedExportText.text = "Failed Export: " + e.Message;
            _refs.Animator.SetTrigger("export failed");
        }
    }

    protected override string GetExportPath()
    {
        string fullPath = _refs.SaveLocationInputField.text;

        fullPath = fullPath.Replace('/', '\\');

        if (!fullPath.EndsWith("\\"))
            fullPath += "\\";

        fullPath += $"{_refs.FileNameInputField.text}\\{{0}}.jpg";

        return fullPath;
    }
}
