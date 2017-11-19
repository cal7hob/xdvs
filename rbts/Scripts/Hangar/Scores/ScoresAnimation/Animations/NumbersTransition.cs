using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XDevs.Scores.Animation
{
    public class NumbersTransition : ScoresAnimation
    {
        public float scrollDuration = 2f;
        public float transitionNumbersAnimationDuration = 5f;

        public override bool Setup(tk2dUIScrollableArea scrollableArea, ScoresPage scoresPage,
            LeaderboardDelta delta, ScoresItem scoresItemToMove)
        {
            base.Setup(scrollableArea, scoresPage, delta, scoresItemToMove);

            // Increase SortingOrder by 10 to overlap other items
            scoresPage.HighlightedItem.transform.AddToSortingOrderRecursively(10, true, true);
            
            scrollableArea.Value = 0f; // Scrolling will start from the top
            scoresPage.HighlightedItem.Place = delta.oldLeaderboardItem.place;

            if (ScoresAnimationManager.Dbg)
                Debug.LogError("NumbersTransition.Setup(): scoresPage.HighlightedItem.name: " +
                               scoresPage.HighlightedItem.name);

            return true;
        }

        public override IEnumerator Show()
        {
            isAnimating = true;
            yield return StartCoroutine(MoveScroll(scrollDuration));

            CoroutineJoin coroutineJoin = new CoroutineJoin(this);

            coroutineJoin.StartSubtask(ScaleUp(transitionNumbersAnimationDuration));
            coroutineJoin.StartSubtask(ScoreTransitioning(transitionNumbersAnimationDuration));
            coroutineJoin.StartSubtask(PlaceTransitioning(transitionNumbersAnimationDuration));

            yield return coroutineJoin.WaitForAll();

            // Scaling down
            yield return StartCoroutine(
                coTweenTransformTo(
                    scoresPage.HighlightedItem.transform,
                    0.3f,
                    scoresPage.HighlightedItem.transform.localPosition,
                    new Vector3(
                        scoresPage.HighlightedItem.transform.localScale.x / scaleMultiplier,
                        scoresPage.HighlightedItem.transform.localScale.y / scaleMultiplier,
                        scoresPage.HighlightedItem.transform.localScale.z),
                    scoresPage.HighlightedItem.transform.localRotation.z));

            isAnimating = false;
        }

        public class CoroutineJoin
        {
            List<bool> _subTasks = new List<bool>();

            private readonly MonoBehaviour _owningComponent;

            public CoroutineJoin(MonoBehaviour owningComponent)
            {
                _owningComponent = owningComponent;
            }

            public void StartSubtask(IEnumerator routine)
            {
                _subTasks.Add(false);
                _owningComponent.StartCoroutine(StartJoinableCoroutine(_subTasks.Count - 1, routine));
            }

            public Coroutine WaitForAll()
            {
                return _owningComponent.StartCoroutine(WaitForAllSubtasks());
            }

            private IEnumerator WaitForAllSubtasks()
            {
                while (true)
                {
                    bool completedCheck = true;
                    for (int i = 0; i < _subTasks.Count; i++)
                    {
                        if (!_subTasks[i])
                        {
                            completedCheck = false;
                            break;
                        }
                    }

                    if (completedCheck)
                    {
                        break;
                    }

                    yield return null;
                }
            }

            private IEnumerator StartJoinableCoroutine(int index, IEnumerator coroutine)
            {
                yield return _owningComponent.StartCoroutine(coroutine);
                _subTasks[index] = true;
            }
        }

        private IEnumerator MoveScroll(float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                // Easing normalized chunk of duration
                float nt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));

                // Scrolling started from the top
                scrollableArea.Value = Mathf.Lerp(0f, scoresPage.HighlightedItem.ScrollPosition, nt);

                yield return null;
            }
        }

        private IEnumerator PlaceTransitioning(float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                // Easing normalized chunk of duration
                float nt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));

                scoresPage.HighlightedItem.Place =
                    (int)Mathf.Lerp(delta.oldLeaderboardItem.place, delta.newLeaderboardItem.place, nt);

                yield return null;
            }

            scoresPage.HighlightedItem.Place = delta.newLeaderboardItem.place;
        }

        private IEnumerator ScoreTransitioning(float duration)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                // Easing normalized chunk of duration
                float nt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));

                ((ICanHazScore)scoresPage.HighlightedItem).Score = (int)Mathf.Lerp(delta.oldLeaderboardItem.score,
                    delta.newLeaderboardItem.score, nt);

                yield return null;
            }

            ((ICanHazScore)scoresPage.HighlightedItem).Score = delta.newLeaderboardItem.score;
        }
    }
}
