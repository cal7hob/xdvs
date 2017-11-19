using UnityEngine;

public class EnemyPointer : MonoBehaviour
{
    public EnemyPointers.EnemyInfo info;

    private const float MAIN_ENEMY_SCALE_MULTIPLIER = 1.25f;

    private static readonly Color MAIN_ENEMY_COLOR = new Color(r: 0.70f, g: 0.70f, b: 0.70f, a: 1.0f);

    private bool isMain;
    private Vector3 regularScale;
    private Color regularColor;
    private tk2dSprite sprite;

    public bool IsMain
    {
        get
        {
            return isMain;
        }
        set
        {
            isMain = value;

            if (isMain)
            {
                sprite.color = MAIN_ENEMY_COLOR;
                sprite.scale *= MAIN_ENEMY_SCALE_MULTIPLIER;
            }
            else
            {
                sprite.scale = regularScale;
                sprite.color = regularColor;
            }
        }
    }

    public float Alpha
    {
        set
        {
            if (isMain)
                return;

            Color color = sprite.color;

            color.a = value;

            sprite.color = color;
        }
    }

    public VehicleController Vehicle
    {
        get; set;
    }

    void Awake()
    {
        sprite = GetComponentInChildren<tk2dSprite>();
        regularScale = sprite.scale;
        regularColor = sprite.color;
    }

    void Update()
    {
        if (BattleController.MyVehicle == null)
            return;

        Vector3 coord = (BattleController.MyVehicle.transform.InverseTransformPoint(Vehicle.transform.position));

        coord.z = 0;

        coord.Normalize();

        coord.x *= EnemyPointers.FieldSize.x;
        coord.y *= EnemyPointers.FieldSize.y;

        transform.localPosition = coord;
        transform.up = coord.normalized;
    }
}
