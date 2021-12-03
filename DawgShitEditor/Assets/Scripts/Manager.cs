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
    [SerializeField] private Button _newPageButton;

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
            File.WriteAllBytes(GetExportPath(), _refs.Upaint.ExportToPNG());

            _refs.Animator.SetTrigger("export successful");
        }
        catch (Exception e)
        {
            _refs.FailedExportText.text = "Failed Export: " + e.Message;
            _refs.Animator.SetTrigger("export failed");
        }
    }
}
