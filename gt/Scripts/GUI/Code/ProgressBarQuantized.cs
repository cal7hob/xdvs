using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class ProgressBarQuantized : MonoBehaviour
{
	public int Position
	{
		set
		{
			position = value;
			Repaint();
		}
		get { return position; }
	}

	public int Size
	{
		set
		{
			size = value;
			Repaint();
		}
		get { return size; }
	}
	
	[SerializeField]
	private int position = 0;
	[SerializeField]
	private int size = 1;

	private int oldSize;
	private int oldPosition;
	private tk2dTiledSprite filledPart;
	private tk2dTiledSprite emptyPart;
	private Vector3 filledSpriteSize;
	private Vector3 emptySpriteSize;
	private Vector3 startPos;//scifi

	public Transform center;

	public tk2dTiledSprite EmptyPart
	{
		set { emptyPart = value; }
		get { return emptyPart; }
	}

	void Awake()
	{
		Transform trans = transform.Find("Filled");
		filledPart = trans ? trans.GetComponent<tk2dTiledSprite>() : null;
		trans = transform.Find("Empty");
		emptyPart = trans ? trans.GetComponent<tk2dTiledSprite>() : null;

		if (filledPart == null || emptyPart == null)
		{
			gameObject.SetActive(false);
			return;
		}

		emptySpriteSize = emptyPart.CurrentSprite.GetUntrimmedBounds().size;
		filledSpriteSize = filledPart.CurrentSprite.GetUntrimmedBounds().size;

		startPos = transform.localPosition;

		Repaint();
	}
	
	void Update()
	{
		if (!Application.isPlaying && (oldPosition != position || oldSize != size))
		{
			if (filledSpriteSize == Vector3.zero)
			{
				emptySpriteSize = emptyPart.CurrentSprite.GetUntrimmedBounds().size;
				filledSpriteSize = filledPart.CurrentSprite.GetUntrimmedBounds().size;
			}
			
			Repaint();
		}
	}
	
	private void Repaint()
	{
		if (size < 0)
			size = 0;
		
		position = Mathf.Clamp(position, 0, size);
		Vector2 temp;
		temp = filledSpriteSize;
		temp.x *= position;
		filledPart.dimensions = temp;
		emptyPart.transform.localPosition =
			filledPart.transform.localPosition + Vector3.right * temp.x;

		temp = emptySpriteSize;
		temp.x *= (size - position);
		emptyPart.dimensions = temp;
		oldSize = size;
		oldPosition = position;
		center.position = new Vector3(filledPart.transform.position.x + (filledPart.dimensions.x + emptyPart.dimensions.x) / 2, center.position.y, center.position.z);
	}

	private void Moving()
	{
		int deltaSize = 10 - size;//Интерфейс выровнен в рассчете на 10 ячеек топлива
		transform.localPosition = new Vector3 (startPos.x + deltaSize * filledSpriteSize.x / 2,startPos.y,startPos.z);
	}
}
