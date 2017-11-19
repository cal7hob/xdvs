using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IEPanel : MonoBehaviour	// Items & Effects Panel
{
    public static IEPanel Instance { get; private set; }
    public IECell ieCellPrefab;
    public float cellWidth = 100;
    [SerializeField]
    private Transform cellsWrapper;

    private List<IECell> commonCells = new List<IECell>(4);
    private Dictionary<int, IECell> commonCellsDict = new Dictionary<int, IECell>(4);

    void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public IECell AddCell(int uiId, string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        IECell cell = Instantiate(ieCellPrefab);
        commonCells.Add(cell);
        cell.transform.parent = cellsWrapper;
        cell.transform.localPosition = Vector3.left * (commonCells.Count - 1) * cellWidth;// для расположения влево бонусы/вправо расходки cell.transform.localPosition = Vector3.left * effectCells.Count * cellWidth;
        cell.IconName = iconName;
        cell.IsEffect = true;
        commonCellsDict.Add(uiId, cell);
        UpdateWrapperPos();

        return cell;
    }

    public void RemoveCell(int uiId)
    {
        IECell cell;
        if (!commonCellsDict.TryGetValue(uiId, out cell))
            return;

        commonCells.Remove(cell);
        commonCellsDict.Remove(uiId);
        Destroy(cell.gameObject);
        //cell.gameObject.SetActive(false);
        UpdateWrapperPos();
    }

    public void RemoveAllItemsCells()
    {
        List<int> list = new List<int>();
        foreach (KeyValuePair<int, IECell> cellPair in commonCellsDict)
        {
            if (!cellPair.Value.IsEffect)//Только для ракет
            {
                list.Add(cellPair.Key);
                if (cellPair.Value.gameObject != null)
                    Destroy(cellPair.Value.gameObject);
                commonCells.Remove(cellPair.Value);
            }
        }
        //Чтобы не удалять элементы словаря в том же цикле в котором мы перебираем эти элементы
        for (int i = 0; i < list.Count; i++)
            commonCellsDict.Remove(list[i]);
    }

    public IECell GetCell(int uiId)
    {
        IECell cell;
        commonCellsDict.TryGetValue(uiId, out cell);
        return cell;
    }

    /// <summary>
    /// Выравнивание итемов по центру
    /// </summary>
    private void UpdateWrapperPos()
    {
        //Изменяем позицию клеток
        for (int i = 0; i < commonCells.Count; i++)
            commonCells[i].transform.localPosition = Vector3.left * i * cellWidth;
        //Выравниваем wrapper
        int childCount = commonCellsDict.Count;
        cellsWrapper.localPosition = new Vector3((childCount - 1) * cellWidth * 0.5f, cellsWrapper.localPosition.y, cellsWrapper.localPosition.z);
    }
}
