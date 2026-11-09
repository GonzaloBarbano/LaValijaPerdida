using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GridManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private TMP_Text wordListText;

    [Header("Colores")]
    [SerializeField] private Color normalColor = new Color(0.96f, 0.93f, 0.85f);
    [SerializeField] private Color selectingColor = new Color(0.88f, 0.65f, 0.25f);
    [SerializeField] private Color foundColor = new Color(0.65f, 0.28f, 0.12f);

    // Sopa del prólogo (6x6), tomada del boceto del parcial 1
    private readonly string[] rows =
    {
        "VALIJA",
        "XMAPAO",
        "TRENUS",
        "BOQRIE",
        "LUZCAD",
        "ESTOPH"
    };

    private readonly List<string> words = new List<string> { "VALIJA", "MAPA", "TREN" };
    private readonly HashSet<string> foundWords = new HashSet<string>();

    private Cell[,] cells;
    private Cell startCell;
    private List<Cell> currentLine = new List<Cell>();
    private bool selecting;

    private void Start()
    {
        BuildGrid();
        RefreshWordList();
    }

    private void BuildGrid()
    {
        int size = rows.Length;
        cells = new Cell[size, size];

        var layout = gridContainer.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = size;

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                Cell cell = Instantiate(cellPrefab, gridContainer);
                cell.Setup(this, r, c, rows[r][c]);
                cell.SetColor(normalColor);
                cells[r, c] = cell;
            }
        }
    }

    // --- Selección por arrastre ---

    public void BeginSelection(Cell cell)
    {
        selecting = true;
        startCell = cell;
        UpdateSelection(cell);
    }

    public void UpdateSelection(Cell cell)
    {
        if (!selecting) return;
        ResetHighlight();
        currentLine = GetLine(startCell, cell);
        foreach (Cell c in currentLine)
            if (!c.Found) c.SetColor(selectingColor);
    }

    public void EndSelection()
    {
        if (!selecting) return;
        selecting = false;

        string selected = BuildString(currentLine);
        string reversed = Reverse(selected);

        foreach (string word in words)
        {
            if (foundWords.Contains(word)) continue;
            if (selected == word || reversed == word)
            {
                foundWords.Add(word);
                foreach (Cell c in currentLine)
                {
                    c.Found = true;
                    c.SetColor(foundColor);
                }
                RefreshWordList();
                break;
            }
        }

        currentLine.Clear();
        ResetHighlight();

        if (foundWords.Count == words.Count)
            Debug.Log("¡Sopa completada!");
    }

    // Devuelve las celdas en línea recta (horizontal, vertical o diagonal)
    private List<Cell> GetLine(Cell a, Cell b)
    {
        var line = new List<Cell>();
        int dr = b.Row - a.Row;
        int dc = b.Col - a.Col;

        bool straight = dr == 0 || dc == 0 || Mathf.Abs(dr) == Mathf.Abs(dc);
        if (!straight)
        {
            line.Add(a);
            return line;
        }

        int steps = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc));
        int stepR = System.Math.Sign(dr);   // devuelve 0 cuando dr es 0
        int stepC = System.Math.Sign(dc);

        for (int i = 0; i <= steps; i++)
            line.Add(cells[a.Row + stepR * i, a.Col + stepC * i]);

        return line;
    }

    private void ResetHighlight()
    {
        foreach (Cell c in cells)
            c.SetColor(c.Found ? foundColor : normalColor);
    }

    private string BuildString(List<Cell> line)
    {
        var sb = new StringBuilder();
        foreach (Cell c in line) sb.Append(c.Letter);
        return sb.ToString();
    }

    private string Reverse(string s)
    {
        char[] arr = s.ToCharArray();
        System.Array.Reverse(arr);
        return new string(arr);
    }

    private void RefreshWordList()
    {
        var sb = new StringBuilder();
        foreach (string word in words)
            sb.Append(foundWords.Contains(word) ? $"<s>{word}</s>   " : $"{word}   ");
        wordListText.text = sb.ToString();
    }
}