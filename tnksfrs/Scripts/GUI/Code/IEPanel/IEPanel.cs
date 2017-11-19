using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IEPanel : MonoBehaviour	// Items & Effects Panel
{
    public static IEPanel Instance{get; private set;}
	public IECell ieCellPrefab;
	public float cellWidth = 100;
    [SerializeField] private Transform cellsWrapper;

    private List<IECell> commonCells = new List<IECell>(4);
    private Dictionary<string, IECell> commonCellsDict = new Dictionary<string, IECell>(4);
    
	
	void Awake()
	{
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

	public IECell AddCell(string key, IECell.IEIcon icon, bool isEffect)
	{
        IECell cell = (IECell)Instantiate(ieCellPrefab);
        commonCells.Add(cell);
        cell.transform.parent = cellsWrapper;
        cell.transform.localPosition = Vector3.left * (commonCells.Count - 1) * cellWidth;// для расположения влево бонусы/вправо расходки cell.transform.localPosition = Vector3.left * effectCells.Count * cellWidth;
        cell.ItemType = icon;
        cell.IsEffect = isEffect;
        commonCellsDict.Add(key, cell);
        UpdateWrapperPos();
        return cell;
    }

	public void RemoveCell(string key)
	{
        IECell cell;
        commonCellsDict.TryGetValue(key, out cell);
        if (cell == null)
            return;

        Destroy(cell.gameObject);
        //cell.gameObject.SetActive(false);
        commonCellsDict.Remove(key);
        commonCells.Remove(cell);
        UpdateWrapperPos();
    }

    public void RemoveAllItemsCells()
    {
        List<string> list = new List<string>();
        foreach (KeyValuePair<string, IECell> cellPair in commonCellsDict)
        {
            if (!cellPair.Value.IsEffect)//Только для ракет
            {
                //DT3.LogError("Destroy {0}", cellPair.Key);
                list.Add(cellPair.Key);
                if (cellPair.Value.gameObject != null)
                    Destroy(cellPair.Value.gameObject);
                commonCells.Remove(cellPair.Value);
            }
        }
        //Чтобы не удалять элементы словаря в том же цикле в котором мы перебираем эти элементы
        for(int i = 0; i < list.Count; i++)
             commonCellsDict.Remove(list[i]);
    }

	public IECell GetCell(string key)
	{
        IECell cell;
		commonCellsDict.TryGetValue(key, out cell);
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
