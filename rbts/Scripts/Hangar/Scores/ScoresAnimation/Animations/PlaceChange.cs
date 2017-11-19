using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace XDevs.Scores.Animation
{
    /// <summary>
    /// Анимированное перемещение элемента игрока в таблице очков.
    /// </summary>
    public class PlaceChange : ScoresAnimation
    {
        public float placeChangeAnimationDuration = 3f;
        public float scaleUpDuration = 1f;
        public float scaleDownDuration = 0.3f;

        private List<ScoresItem> pageItemsList = new List<ScoresItem>();

        // Место ScoresItem игрока в полученной с сервера таблице очков до перемещения
        private int scoresItemToMovePosition;

        public override bool Setup(tk2dUIScrollableArea scrollableArea, ScoresPage scoresPage,
            LeaderboardDelta delta, ScoresItem scoresItemToMove)
        {
            base.Setup(scrollableArea, scoresPage, delta, scoresItemToMove);

            if (scoresPage.ScoresItemPrefab.CollisionHandler == null)
            {
                Debug.LogError("PlaceChange.Setup(), scoresPage.ScoresItemPrefab.collisionHandler == null");
                return false;
            }

            if (pageItemsList.Count > 0)
                pageItemsList.Clear();

            pageItemsList = scoresPage.PageItems.Values.ToList();

            foreach (var scoresItem in scoresPage.PageItems.Values)
            {
                // Включаем CollisionHandler, чтобы наши ScoresItems смещались вниз
                scoresItem.CollisionHandler.gameObject.SetActive(true);

                var initialTransform = new InitTransform
                {
                    pos = scoresItem.transform.localPosition,
                    scale = scoresItem.transform.localScale,
                    angle = scoresItem.transform.eulerAngles.z,
                    scrollPosition = scoresItem.ScrollPosition,
                };

                initTransforms.Add(initialTransform);
            }

            scoresItemToMovePosition =
                pageItemsList.IndexOf(
                    pageItemsList.Find(scoresItem => scoresItem.Place == scoresItemToMove.Place));

            var scoresItemToMoveOldPosition = scoresItemToMovePosition + delta.PlaceDelta;

            // Передвигаем наш ScoresItem между его местом и последним
            if (scoresItemToMoveOldPosition < pageItemsList.Count)
            {
                MoveScoreItem(scoresItemToMovePosition, scoresItemToMoveOldPosition);
            }
            else // Передвигаем наш ScoresItem в самый конец
            {
                if (scoresItemToMovePosition != pageItemsList.Count - 1)
                    MoveScoreItem(scoresItemToMovePosition, pageItemsList.Count - 1);
            }

            scoresItemToMove.Place = delta.oldLeaderboardItem.place;
            ((ICanHazScore)scoresItemToMove).Score = delta.oldLeaderboardItem.score;

            // Increase SortingOrder by 1 to overlap other items
            scoresItemToMove.transform.AddToSortingOrderRecursively(10, true, true);

            scrollableArea.Value = scoresItemToMove.ScrollPosition;

            if (ScoresAnimationManager.Dbg)
                Debug.LogError("PlaceChange.Setup(), scoresItemToMove.name: " + scoresItemToMove.name);

            return true;
        }

        private void MoveScoreItem(int oldPlace, int newPlace)
        {
            //Debug.LogErrorFormat("Moving {0} from {1} to {2} ({3})", places[oldPlace], oldPlace, newPlace, places[newPlace]);

            //for (var index = 0; index < places.Count; index++)
            //{
            //    Debug.LogErrorFormat("Before, Place: {0}, Item: {1}", index, places[index]);
            //}

            var tempItem = pageItemsList[oldPlace];
            pageItemsList.RemoveAt(oldPlace);

            pageItemsList.Insert(newPlace, tempItem);

            // Меняем ScrollPosition
            pageItemsList[newPlace].ScrollPosition = pageItemsList[newPlace - 1].ScrollPosition;

            for (var index = Mathf.Min(oldPlace, newPlace); index <= Mathf.Max(oldPlace, newPlace); index++)
            {
                pageItemsList[index].Place--;
                pageItemsList[index].transform.localPosition = initTransforms[index].pos;
            }

            //for (var index = 0; index < places.Count; index++)
            //{
            //    Debug.LogErrorFormat("After, Place: {0}, Item: {1}", index, places[index]);
            //}
        }

        public override IEnumerator Show()
        {
            isAnimating = true;

            if (ScoresAnimationManager.Dbg)
                Debug.LogError("PlaceChange.Show()");

            yield return StartCoroutine(ScaleUp(scaleUpDuration));

            scoresItemToMove.CollisionHandler.OnTriggerEnterEvent
                += delegate (Collider other)
                {
                    //Debug.LogWarning("OnTriggerEnter from Show: " + other.transform.parent.name);

                    var previousItemPlace = pageItemsList.IndexOf(other.transform.parent.GetComponent<ScoresItem>()) + 1;

                    StartCoroutine(coTweenTransformTo(other.transform.parent, 0.3f,
                        initTransforms[previousItemPlace].pos,
                        initTransforms[previousItemPlace].scale,
                        initTransforms[previousItemPlace].angle));

                    scoresItemToMove.Place =
                        other.transform.parent.GetComponent<ScoresItem>().Place;

                    other.transform.parent.GetComponent<ScoresItem>().Place++;
                };

            yield return StartCoroutine(ChangePlace(placeChangeAnimationDuration));

            // Scaling down
            yield return StartCoroutine(
                coTweenTransformTo(
                    scoresItemToMove.transform,
                    scaleDownDuration,
                    scoresItemToMove.transform.localPosition,
                    new Vector3(
                        scoresItemToMove.transform.localScale.x / scaleMultiplier,
                        scoresItemToMove.transform.localScale.y / scaleMultiplier,
                        scoresItemToMove.transform.localScale.z),
                    scoresItemToMove.transform.localRotation.z));

            scoresItemToMove.Place = delta.newLeaderboardItem.place;

            scoresItemToMove.CollisionHandler.gameObject.SetActive(false);

            isAnimating = false;

            yield break;
        }

        // Scaling ScoresItem up
        private new IEnumerator ScaleUp(float duration)
        {
            //Debug.LogWarning("Scale before Coroutine: " + scoresItemToMove.collisionHandler.transform.localScale);

            // Коллайдер collisionHandler'а также увеличится
            yield return StartCoroutine(base.ScaleUp(duration));

            // Resetting our collisionHandler's collider's scale so it won't trigger on prev. item etc.
            scoresItemToMove.CollisionHandler.transform.localScale /= scaleMultiplier;
        }

        private IEnumerator ChangePlace(float duration)
        {
            var startPosition = scoresItemToMove.transform.localPosition;
            var startScrollValue = scoresItemToMove.ScrollPosition;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                // Easing normalized chunk of duration
                float nt = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));

                scoresItemToMove.transform.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        initTransforms[scoresItemToMovePosition].pos,
                        nt);

                scrollableArea.Value = Mathf.Lerp(startScrollValue,
                    initTransforms[scoresItemToMovePosition].scrollPosition, nt);

                ((ICanHazScore)scoresItemToMove).Score =
                    (int)Mathf.Lerp(delta.oldLeaderboardItem.score, delta.newLeaderboardItem.score, nt);

                yield return null;
            }

            ((ICanHazScore)scoresItemToMove).Score = delta.newLeaderboardItem.score;
        }
    }
}
