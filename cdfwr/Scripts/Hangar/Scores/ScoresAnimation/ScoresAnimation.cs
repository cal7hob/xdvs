using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XDevs.Scores.Animation
{
    public enum Animation
    {
        ChangePlace,
        TransitionNumbers
    }

    public abstract class ScoresAnimation : MonoBehaviour
    {
        public bool isAnimating;

        protected ScoresPage scoresPage;
        protected tk2dUIScrollableArea scrollableArea;
        protected float scaleMultiplier = 1.2f;
        protected LeaderboardDelta delta;
        protected ScoresItem scoresItemToMove;

        protected class InitTransform
        {
            public Vector3 pos;
            public Vector3 scale;
            public float angle;
            public float scrollPosition;
        }

        protected List<InitTransform> initTransforms = new List<InitTransform>();

        public virtual bool Setup(tk2dUIScrollableArea scrollableArea, ScoresPage scoresPage, LeaderboardDelta delta,
            ScoresItem scoresItemToMove)
        {
            this.scrollableArea = scrollableArea;
            this.scoresPage = scoresPage;
            this.delta = delta;
            this.scoresItemToMove = scoresItemToMove;

            if (initTransforms.Count > 0)
                initTransforms.Clear();

            // Показываем стрелочку с дельтой поднятия места
            if (scoresItemToMove.DeltaParent != null)
                scoresItemToMove.DeltaParent.SetActive(true);

            if (scoresItemToMove.LblDelta != null)
                scoresItemToMove.LblDelta.text = string.Format("+{0}", delta.PlaceDelta);

            if (ScoresAnimationManager.Dbg)
                Debug.LogError("ScoresAnimation.Setup(), delta: " + delta);

            return true;
        }

        public abstract IEnumerator Show();

        //public static void AddToSortingOrderRecursively(Transform transform, int orderToAdd)
        //{
        //    foreach (Transform childTransform in transform)
        //    {
        //        var tk2dBaseSprite = childTransform.GetComponent<tk2dBaseSprite>();
        //        if (tk2dBaseSprite != null)
        //            tk2dBaseSprite.SortingOrder += orderToAdd;

        //        var tk2dTextMesh = childTransform.GetComponent<tk2dTextMesh>();
        //        if (tk2dTextMesh != null)
        //            tk2dTextMesh.SortingOrder += orderToAdd;

        //        AddToSortingOrderRecursively(childTransform, orderToAdd);
        //    }
        //}

        protected IEnumerator coTweenTransformTo(Transform transform, float time, Vector3 toPos, Vector3 toScale,
            float toRotation)
        {
            Vector3 fromPos = transform.localPosition;
            Vector3 fromScale = transform.localScale;
            Vector3 euler = transform.localEulerAngles;
            float fromRotation = euler.z;

            for (float t = 0; t < time; t += tk2dUITime.deltaTime)
            {
                float nt = Mathf.Clamp01(t / time);
                nt = Mathf.Sin(nt * Mathf.PI * 0.5f);

                transform.localPosition = Vector3.Lerp(fromPos, toPos, nt);
                transform.localScale = Vector3.Lerp(fromScale, toScale, nt);
                euler.z = Mathf.Lerp(fromRotation, toRotation, nt);
                transform.localEulerAngles = euler;
                yield return 0;
            }

            euler.z = toRotation;
            transform.localPosition = toPos;
            transform.localScale = toScale;
            transform.localEulerAngles = euler;
        }

        protected IEnumerator ScaleUp(float duration)
        {
            var start = scrollableArea.ContentLength;
            var stop = start + scoresPage.ItemHeight * (scaleMultiplier - 1);

            StartCoroutine(
                coTweenTransformTo(
                    scoresItemToMove.transform, 
                    duration,
                    scoresItemToMove.transform.localPosition,
                    new Vector3(
                        scoresItemToMove.transform.localScale.x * scaleMultiplier,
                        scoresItemToMove.transform.localScale.y * scaleMultiplier, 
                        scoresItemToMove.transform.localScale.z),
                    scoresItemToMove.transform.localRotation.z));

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                // Easing normalized chunk of duration
                float nt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));

                scrollableArea.ContentLength = (int)Mathf.Lerp(start, stop, nt);

                scrollableArea.Value += scoresItemToMove.ScrollPosition - scrollableArea.Value;

                yield return null;
            }
        }
    }
}