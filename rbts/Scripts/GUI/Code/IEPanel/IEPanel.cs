using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pool;

public class IEPanel : MonoBehaviour	// Items & Effects Panel
{
    private const string CELL_RESOURCE_PATH = "GuiPrefabs/IECellFTRI";

    public static IEPanel Instance{ get; private set;}
	public float cellWidth = 100;
    [SerializeField] private Transform cellsWrapper;

    private readonly List<IECell> cells = new List<IECell>(2);
    
	void Awake()
	{
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void CellReleased(IECell cell)
    {
        cells.Remove(cell);
    }

    public IECell AddCell(VehicleEffect effect)
	{
	    IECell cell = PoolManager.GetObject<IECell>(CELL_RESOURCE_PATH, Vector3.zero, Quaternion.identity);
        cell.Assign(this);
        cell.SetEffect(effect);
        cell.transform.SetParent(cellsWrapper);
        cells.Add(cell);
        cell.transform.localPosition = Vector3.left * (cells.Count - 1) * cellWidth;// для расположения влево бонусы/вправо расходки cell.transform.localPosition = Vector3.left * effectCells.Count * cellWidth;
        UpdateWrapperPos();

        return cell;
    }

    public void RemoveAllCells()
    {
        foreach (IECell cell in cells.ToArray())
        {
            cell.Release();
        }
    }

    /// <summary>
    /// Выравнивание итемов по центру
    /// </summary>
    private void UpdateWrapperPos()
    {
        //Изменяем позицию клеток
        for (int i = 0; i < cells.Count; i++)
            cells[i].transform.localPosition = Vector3.left * i * cellWidth;
        //Выравниваем wrapper
        int childCount = cells.Count;
        cellsWrapper.localPosition = new Vector3((childCount - 1) * cellWidth * 0.5f, cellsWrapper.localPosition.y, cellsWrapper.localPosition.z);
    }
}
