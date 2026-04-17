using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConfigPopup : BasePopup
{
    private const string RowsLabelFormat = "Rows: {0}";
    private const string ColumnsLabelFormat = "Columns: {0}";

    [SerializeField] private Slider rowsSlider;
    [SerializeField] private Slider columnsSlider;
    [SerializeField] private TMP_Text rowsText;
    [SerializeField] private TMP_Text columnsText;

    private void OnEnable()
    {
        int rows = Mathf.RoundToInt(rowsSlider.value);
        int columns = Mathf.RoundToInt(columnsSlider.value);
        
        UpdateRowsLabel(rows);
        UpdateColumnsLabel(columns);
    }

    public void OnRowsSliderValueChanged(float value)
    {
        int rows = Mathf.RoundToInt(value);
        UpdateRowsLabel(rows);
    }

    public void OnColumnsSliderValueChanged(float value)
    {
        int columns = Mathf.RoundToInt(value);
        UpdateColumnsLabel(columns);
    }
    private void UpdateRowsLabel(int value)
    {
        string label = string.Format(RowsLabelFormat, value);
        rowsText.text = label;
    }

    private void UpdateColumnsLabel(int value)
    {
        string label = string.Format(ColumnsLabelFormat, value);
        columnsText.text = label;
    }
}