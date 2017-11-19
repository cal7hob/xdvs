using System.Collections;
using UnityEngine;

public abstract class BattleTutorial_PC_Part : MonoBehaviour
{
    [SerializeField] private StretchImageByText imageStretcher;
    [SerializeField] private float sprXPadding = 20;
    [SerializeField] protected BattleTutorial battleTutorial;

    private float defaultAddToTextLength;

    [Header("PC control sprites holder")]
    [SerializeField] protected tk2dBaseSprite spr;
    [SerializeField] protected tk2dBaseSprite sprAlt;

    public virtual string KeyFire { get { return "tutorialMessageKey_9"; } }

    protected virtual void Start()
    {
        if(imageStretcher != null)
        {
            defaultAddToTextLength = imageStretcher.addToTextLength;
        }
    }

    protected void SetBtnsSprite(string spriteName)
    {
        spr.gameObject.SetActive(true);
        spr.SetSprite(spriteName);
        Align(battleTutorial.LblTutorMessage.text);
    }

    protected void SetBtnsSprite(tk2dBaseSprite sprite, string spriteName)
    {
        sprite.gameObject.SetActive(true);
        sprite.SetSprite(spriteName);

        Align(battleTutorial.LblTutorMessage.text);
    }

    public abstract IEnumerator Lessons();

    public virtual IEnumerator MoveLesson()
    {
        battleTutorial.CurrentBattleLesson = BattleTutorial.BattleLessons.move;

        yield return StartCoroutine(battleTutorial.ShowTutorMessage(battleTutorial.KeyMove, (int)VoiceEventKey.TankMoveLessonButtons));

        SetBtnsSprite(spr, "arrowBtns");

        JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left].IsOn = true;

        yield return StartCoroutine(battleTutorial.CheckIfMoveLessonIsDone());
        yield return StartCoroutine(HideTutorMessage());
    }

    public virtual IEnumerator HideTutorMessage()
    {
        battleTutorial.TutorMessageVisible = false;
        battleTutorial.SetAnimationDirection(BattleTutorial.AnimDirections.reversed);

        battleTutorial.Emersion.Play();

        yield return new WaitForSeconds(battleTutorial.Emersion.clip.length);

        spr.gameObject.SetActive(false);
        sprAlt.gameObject.SetActive(false);

        if (imageStretcher != null)
            imageStretcher.addToTextLength = defaultAddToTextLength;
    }

    protected void Align(string tutorMessage)
    {
        if (imageStretcher == null)
            return;

        spr.transform.localPosition = Vector3.zero;

        var sprRenderer = spr.GetComponent<Renderer>();

        var stringSize = battleTutorial.LblTutorMessage.GetEstimatedMeshBoundsForString(tutorMessage);

        spr.transform.localPosition += Vector3.right * (stringSize.size.x + sprRenderer.bounds.extents.x + sprXPadding);

        imageStretcher.addToTextLength += sprRenderer.bounds.extents.x + sprXPadding * 0.5f;

        imageStretcher.StretchImage(0, null);
    }
}
