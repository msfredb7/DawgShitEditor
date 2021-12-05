using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CCC.UPaintGUI;
using System.IO;
using System;
using UnityEngine.UI;

public class Manager : UPaintGUIManager
{
    [SerializeField] private Vector2 _pageResolution;
    [SerializeField] private PageManager _pageManager;

    protected override void Awake()
    {
        base.Awake();

        _pageManager.ActivePageChanged += OnActivePageChanged;
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
            for (int i = 0; i < _pageManager.PageCount; i++)
            {
                var path = GetExportPath(i);
                var dir = Path.GetDirectoryName(path);

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(path, _pageManager.GetPageUPaint(i).ExportToImage(UPaintGUI.ExportEncoding.JPG));
            }

            _refs.Animator.SetTrigger("export successful");
        }
        catch (Exception e)
        {
            _refs.FailedExportText.text = "Failed Export: " + e.Message;
            _refs.Animator.SetTrigger("export failed");
        }
    }

    protected virtual string GetExportPath(int pageIndex)
    {
        string fullPath = _refs.SaveLocationInputField.text;

        fullPath = fullPath.Replace('/', '\\');

        if (!fullPath.EndsWith("\\"))
            fullPath += "\\";

        fullPath += $"{_refs.FileNameInputField.text}\\{pageIndex}.jpg";

        return fullPath;
    }
}
