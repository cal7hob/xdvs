using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollResizer : MonoBehaviour
{
    //Весь этот скрипт сделан для того,чтобы корректно растягивать скроллбар, скролларею и её содержимое при разных разрешениях и изменении разрешения. Вообще в норме это должен делать сам тулкит, но 
    //В данном случае он отрабатывает как то не так как надо. Когда найду способ исправить это - удалю этот скрипт вообще. Если кто будет его использовать и менять - отпишитесь тут, кабы чего не вышло. 
    public tk2dUIScrollbar scrollbar;
    public GameObject upArrow;
    public GameObject downArrow;
    public GameObject down;
    public GameObject up;
    public GameObject up2;
    public GameObject up3;
    public GameObject containsLayout;
    private tk2dUILayout mainLayout;
    public GameObject[] alignedObjects;
    public GameObject objectForAlign;
    public GameObject forScrollableArea;
    public GameObject master;
    private float oldMasterPosition;
    private tk2dUIScrollableArea ScrollableArea;
    private float oldMinLayoutY;
    private bool awakeIsCalled;
    
    //Константа, которая означает значение mainLayout.bMin.y на момент когда разрешение 1920 на 1080.  Меняем размер всего, исходя из неё. Способ корявый, но работает, а автоматически скроллбар не ресайзит вместе с лэйаутом
    //в начале. В процессе игры при изменении разрешения всё ресайзится корректно, это костыль исключительно для корректного старта на разрешениях, отличающихся от 1920х1080. В дальнейшем, возможно, найду более оптимальный способ. 
    public float MinLayoutYFullHd = -552f;
    //Расстояние между верхним спрайтом и объектом, прикреплённым к этому спрайту. 
    public float DistanceBetweenMasterAndObject = 75f;
    //Остальные константы для 1920х1080. От них ресайзим под другие разрешения. 
    public float ScrollAreaContentLenghtFullHd = 600f;
    public float ScrollbarLenghtFullHd = 370f;
    public float ScrollableVisibleAreaFullHd = 230f;
    
    void Awake()
    {
        awakeIsCalled = true;
        Messenger.Subscribe(EventId.ResolutionChanged, ResizeScreen);
        ScrollableArea = forScrollableArea.GetComponent<tk2dUIScrollableArea>();
        mainLayout = containsLayout.GetComponent<tk2dUILayout>();
        ResizeScreen(EventId.ResolutionChanged, null);

    }

    void OnEnable()
    {
        if (awakeIsCalled) return;
        Awake();
    }

    void OnDisable()
    {
        Messenger.Unsubscribe(EventId.ResolutionChanged, ResizeScreen);
        awakeIsCalled = false;
        scrollbar.scrollBarLength = ScrollbarLenghtFullHd;
        ScrollableArea.ContentLength = ScrollAreaContentLenghtFullHd;
        ScrollableArea.VisibleAreaLength = ScrollableVisibleAreaFullHd;
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ResolutionChanged, ResizeScreen);
    }

    void ResizeScreen(EventId eid, EventInfo ei)
    {
        ReshapeThis();
        scrollbar.scrollBarLength = ScrollbarLenghtFullHd;
        AfterReshape();
    }

    private void AfterReshape()
    {
        scrollbar.scrollBarLength = scrollbar.scrollBarLength +
                                Mathf.Abs(mainLayout.bMin.y - MinLayoutYFullHd);
        ScrollableArea.Value = 0;
        oldMasterPosition = master.transform.position.y;
        MoveMasterAndSlaves();
        PruningLayout();
    }

    private void ReshapeThis()
    {
        //Решейп работает с разницей от текущих значений, поэтому берём размеры всех панелей сверху и снизу, прибавляем их к краям экрана и получаем место для нашего лэйаута
        oldMinLayoutY = mainLayout.bMin.y;
        var bottomSpriteRenderer = down.GetComponent<Renderer>();
        var topSpriteRenderer = up.GetComponent<Renderer>();
        var topSpriteRenderer1 = up2.GetComponent<Renderer>();
        var topSpriteRenderer2 = up3.GetComponent<Renderer>();
        var currentTk2DCamera = HangarController.Instance.Tk2dGuiCamera;

        float layoutBottomPosition = bottomSpriteRenderer == null ? 0 : bottomSpriteRenderer.bounds.size.y;
        float layoutTopPosition = topSpriteRenderer == null ? 0 : topSpriteRenderer.bounds.size.y;
        float layoutTopPosition2 = topSpriteRenderer1 == null ? 0 : topSpriteRenderer1.bounds.size.y;
        float layoutTopPosition3 = topSpriteRenderer2 == null ? 0 : topSpriteRenderer2.bounds.size.y;


        var deltaBottom = (layoutBottomPosition + currentTk2DCamera.ScreenExtents.yMin) - mainLayout.GetMinBounds().y;
        var deltaTop = (currentTk2DCamera.ScreenExtents.yMax - layoutTopPosition - layoutTopPosition2 - layoutTopPosition3) - mainLayout.GetMaxBounds().y;
        mainLayout.Reshape(new Vector3(0, deltaBottom, 0), new Vector3(0, deltaTop, 0), true);
    }

    private void PruningLayout()
    {
        ScrollableArea.ContentLength -= (oldMinLayoutY - mainLayout.bMin.y);
        if (ScrollableArea.ContentLength < ScrollableArea.VisibleAreaLength)
        {
            ScrollableArea.ContentLength = ScrollableArea.VisibleAreaLength + 1;
        }
        if (ScrollableArea.ContentLength > ScrollAreaContentLenghtFullHd)
        {
            ScrollableArea.ContentLength = ScrollAreaContentLenghtFullHd;
        }
    }

    private void MoveMasterAndSlaves()
    {
        master.transform.position = new Vector3(master.transform.position.x, mainLayout.GetMaxBounds().y - DistanceBetweenMasterAndObject, master.transform.position.z);
        foreach (var obj in alignedObjects)
        {
            if (obj.name.Equals(master.name)) continue;
            obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y + (master.transform.position.y - oldMasterPosition), obj.transform.position.z);
        }
    }

}
