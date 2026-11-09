using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Cell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public char Letter { get; private set; }
    public bool Found { get; set; }

    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;

    private GridManager manager;

    public void Setup(GridManager gridManager, int row, int col, char letter)
    {
        manager = gridManager;
        Row = row;
        Col = col;
        Letter = letter;
        label.text = letter.ToString();
    }

    public void SetColor(Color color)
    {
        background.color = color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (manager != null) manager.BeginSelection(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null) manager.UpdateSelection(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (manager != null) manager.EndSelection();
    }
}