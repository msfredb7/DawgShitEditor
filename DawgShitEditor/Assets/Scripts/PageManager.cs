using CCC.UPaintGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineX;

public class PageManager : MonoBehaviour
{
    private class Page
    {
        public GameObject RootGameObject;
        public GameObject BGDark;
        public GameObject BGLight;
        public UPaintGUI Upaint;
        public Button Button;
    }

    [SerializeField] private Button _addButton = null;
    [SerializeField] private Button _pagePrefab = null;
    [SerializeField] private Transform _layerButtonContainer = null;
    [SerializeField] private UPaintGUIReferences _refs;

    private List<Page> _pages = new List<Page>();
    private Page _activePage = null;

    public int PageCount => _pages.Count;

    public delegate void PageChangeDelegate(UPaintGUI previousPage, UPaintGUI newPage);
    public event PageChangeDelegate ActivePageChanged;

    void Start()
    {
        _addButton.onClick.AddListener(AddPage);

        AddPage(_refs.DrawCanvas.gameObject);
    }

    private void AddPage()
    {
        // duplicate the last page
        GameObject newPageCanvas = Instantiate(_pages[_pages.Count - 1].RootGameObject, _pages[_pages.Count - 1].RootGameObject.transform.parent, worldPositionStays: true);
        AddPage(newPageCanvas);
    }

    public UPaintGUI GetPageUPaint(int i) => _pages[i].Upaint;

    private void AddPage(GameObject canvas)
    {
        var page = new Page();

        // create new button for the page
        var newButton = Instantiate(_pagePrefab, _layerButtonContainer);
        newButton.onClick.AddListener(() => SelectPage(page));
        newButton.transform.Find("X Button").GetComponent<Button>().onClick.AddListener(() => RemovePage(page));
        newButton.transform.Find("Up Button").GetComponent<Button>().onClick.AddListener(() => MovePageUp(page));
        newButton.transform.Find("Down Button").GetComponent<Button>().onClick.AddListener(() => MovePageDown(page));

        // assign refs
        page.Upaint = canvas.GetComponentInChildren<UPaintGUI>(includeInactive: true);
        page.Button = newButton;
        page.RootGameObject = canvas;
        page.BGDark = canvas.transform.GetChild(0).Find("BG Dark").gameObject;
        page.BGLight = canvas.transform.GetChild(0).Find("BG Light").gameObject;
        _pages.Add(page);

        // add 1 layer
        if (page.Upaint.LayerCount == 0)
            page.Upaint.AddLayer();

        // set active
        SelectPage(page);
    }

    private void SelectPage(Page page)
    {
        if (page == _activePage)
            return;

        var previousPage = _activePage;

        // deactivate old
        if (_activePage != null)
        {
            _activePage.RootGameObject.SetActive(false);
        }

        // reassign refs
        _refs.DrawCanvas = page?.RootGameObject.GetComponent<Canvas>();
        _refs.Upaint = page?.Upaint;
        _refs.UpaintBackgroundDark = page?.BGDark;
        _refs.UpaintBackgroundLight = page?.BGLight;
        _activePage = page;

        // activate new
        _activePage.RootGameObject?.SetActive(true);
        ActivePageChanged?.Invoke(previousPage?.Upaint, _activePage?.Upaint);
    }

    private void RemovePage(Page page)
    {
        // force minimum of 1 page
        if (_pages.Count == 1)
            return;

        if (!_pages.Remove(page))
            return;

        if (_activePage == page)
            SelectPage(_pages[0]);

        Destroy(page.RootGameObject);
        Destroy(page.Button.gameObject);
    }

    private void MovePageUp(Page page)
    {
        _pages.Swap(_pages.IndexOf(page), _pages.IndexOf(page) - 1);
    }

    private void MovePageDown(Page page)
    {
        _pages.Swap(_pages.IndexOf(page), _pages.IndexOf(page) + 1);
    }

    private void Update()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            _pages[i].Button.transform.SetSiblingIndex(i);
            _pages[i].Button.transform.Find("X Button").GetComponent<Button>().interactable = _pages.Count > 1;
            _pages[i].Button.transform.Find("Up Button").GetComponent<Button>().interactable = i > 0;
            _pages[i].Button.transform.Find("Down Button").GetComponent<Button>().interactable = i < _pages.Count - 1;
            _pages[i].Button.transform.Find("Selected Frame").gameObject.SetActive(_activePage == _pages[i]);
            _pages[i].Button.GetComponent<RawImage>().texture = _pages[i].Upaint.GetCombinedTexture();
        }
    }
}
