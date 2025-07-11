using System.Collections;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    [SerializeField]
    private SelectableInfo info;
    public SelectableInfo Info => info;

    // Cached references
    public ISelectableController selectableController;
    private Outline _selectOutline;
    public Outline SelectOutline {
        get {
            if(_selectOutline == null)
                _selectOutline = CreateOutline();
            return _selectOutline;
        }
        private set => _selectOutline = value;
    }

    // Variables
    public SelectableRenderInfo? SelectableRenderInfoV { get; private set; }

    private void Start()
    {
        UpdateSelectable(false);
    }

    public void UpdateSelectable(bool selected) 
    {
        if(selectableController != null)
            SelectableRenderInfoV = selectableController.GetSelectableInfo();
        else if(info != null)
            SelectableRenderInfoV = SelectableRenderInfo.DefaultRenderArgs(info);

        if(!SelectableRenderInfoV.HasValue)
            return;

        SelectableRenderInfo sri = SelectableRenderInfoV.Value;
        UpdateOutline(sri.color, selected ? 8 : 4);
    }

    public void ClearRenderInfo() => SelectableRenderInfoV = null;

    private Outline CreateOutline() 
    {
        // Find a new Outline
        GameObject outlineObject = gameObject; 
        if(selectableController != null && selectableController.GetOutlineObject() != null)
            outlineObject = selectableController.GetOutlineObject();

        if(outlineObject.TryGetComponent(out Outline selectOutlineExisting)) {
            _selectOutline = selectOutlineExisting;
            return selectOutlineExisting;
        } else {        
            return outlineObject.AddComponent<Outline>();
        }
    }

    public void UpdateOutline(Color color, float width) 
    {
        SelectOutline.OutlineColor = color;
        SelectOutline.OutlineWidth = width;
        SelectOutline.OutlineMode = Outline.Mode.OutlineVisible;
    }
    
}

public interface ISelectableController 
{
    public SelectableRenderInfo GetSelectableInfo();
    public GameObject GetOutlineObject();
}

/// <summary>
/// This information is either automatically generated from a SelectableInfo object, or created
///   by an ISelectableController instance.
/// </summary>
public struct SelectableRenderInfo 
{
    public string name;
    public string[] descriptionLines;
    public Color color;
    public Color selectColor;

    public SelectableRenderInfo(string name, string[] descriptionLines, Color color, Color selectColor) 
    {
        this.name = name;
        this.descriptionLines = descriptionLines;
        this.color = color;
        this.selectColor = selectColor;
    }

    public static SelectableRenderInfo DefaultRenderArgs(SelectableInfo input) { return new(input.DisplayName, input.DescriptionLines, input.color, input.selectedColor); }
    public static SelectableRenderInfo CreateEmpty() { return new("", new string[0], Color.white, Color.white); }
}