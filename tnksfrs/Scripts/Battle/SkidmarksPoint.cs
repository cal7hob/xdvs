using UnityEngine;
using XD;

public interface ISkidmarksPoint
{
    WheelCollider WheelCollider
    {
        get;
    }

    void SetGround(bool value);
    void Draw();
    void Chop();
}

public class SkidmarksPoint : MonoBehaviour, ISkidmarksPoint
{
    [SerializeField]
    private WheelCollider       wheelCollider = null;

    private bool                onGround = false;
    private Skidmarks           skidmarks = null;
    private int                 lastSkidmarkId = Skidmarks.DEFAULT_SKIDMARK_ID;

    public WheelCollider WheelCollider
    {
        get
        {
            return wheelCollider;
        }
    }

    private void Awake()
    {
        skidmarks = StaticType.BattleController.Instance<IBattleController>().Skidmarks;

        if (wheelCollider == null)
        {
            onGround = true;
        }

    }
    
    public void Chop()
    {
        lastSkidmarkId = Skidmarks.DEFAULT_SKIDMARK_ID;
    }

    public void SetGround(bool onGround)
    {
        this.onGround = onGround;
    }

    public void Draw()
    {
        if (!onGround)
        {
            return;
        }

        if (skidmarks == null)
        {
            return;
        }

        lastSkidmarkId = skidmarks.AddSkidMark(transform.position, transform.up, 1, lastSkidmarkId);
    }
}
