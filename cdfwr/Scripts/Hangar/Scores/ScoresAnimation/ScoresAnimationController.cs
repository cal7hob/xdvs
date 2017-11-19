using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

namespace XDevs.Scores.Animation
{
    public class ScoresAnimationController : MonoBehaviour
    {
        [SerializeField] private tk2dUIScrollableArea scrollableArea;
        [SerializeField] private LabelLocalizationAgent lblPlaceIncrease;
        [SerializeField] private ScoresPage scoresPage;
        [SerializeField] private PlaceChange changingPlaceAnimation;
        [SerializeField] private NumbersTransition transitionNumbersAnimation;
        [SerializeField] private tk2dTextMesh leaderboardArea;
        [SerializeField] private tk2dTextMesh regionLabel;
        [SerializeField] private tk2dCameraAnchor[] anchors;
        [SerializeField] private tk2dUILayout layoutToBeResized;

        [SerializeField] private ScoresMenuBehaviourPlayer playerMenuBehaviour;
        [SerializeField] private ScoresMenuBehaviourClan clanMenuBehaviour;

        [SerializeField] private ActivatedUpDownButton btnOk;
        [SerializeField] private ActivatedUpDownButton btnShare;

        [SerializeField] private ScoresItem scoresItemToMove;

        private Vector3 defaultLayoutMinBounds, defaultLayoutMaxBounds, defaultWindowLocalposition;
        private const float HEADER_ROW_HEIGHT = 85;
        private LeaderboardDelta delta;

        private ScoresAnimation scoresAnimation;

        public bool IsAnimating
        {
            get
            {
                return (changingPlaceAnimation != null && changingPlaceAnimation.isAnimating)
                       || (transitionNumbersAnimation != null && transitionNumbersAnimation.isAnimating);
            }
        }

        private void Awake()
        {
            if (layoutToBeResized == null)
                return;

            defaultLayoutMinBounds = layoutToBeResized.GetMinBounds();
            defaultLayoutMaxBounds = layoutToBeResized.GetMaxBounds();
            defaultWindowLocalposition = layoutToBeResized.transform.localPosition;
        }

        private void Start()
        {
            if (anchors != null && HangarController.Instance.GuiCamera != null)
            {
                for (int i = 0; i < anchors.Length; i++)
                {
                    if (anchors[i] != null)
                        anchors[i].AnchorCamera = HangarController.Instance.GuiCamera;
                }
            }
        }

        public bool Init(JSONObject scoresData, List<object> friendsScoresData, LeaderboardDelta delta)
        {
            this.delta = delta;
            var pageName = delta.time + "_" + delta.area;

            if (ScoresAnimationManager.Dbg)
                Debug.LogError("ScoresAnimationController.Init() pageName: " + pageName);

            List<object> leaderboard;
            string areaName = string.Empty;

            if (delta.area == "friends")
            {
                leaderboard = friendsScoresData;
            }
            else
            {
                var areaDict = new JsonPrefs(scoresData[delta.time]).ValueObjectDict(delta.area);
                var areaPrefs = new JsonPrefs(areaDict);
                leaderboard = areaPrefs.ValueObjectList("leaderBoard");
                areaName = areaPrefs.ValueString("name", "Unknown");
            }

            if (scrollableArea.contentContainer != null)
                Destroy(scrollableArea.contentContainer);

            switch (delta.area)
            {
                case "clans":
                    scoresPage = ScoresPage.Create<ScoresPageClans>(pageName, scrollableArea,
                        ScoresController.Instance.clanItemPrefab, clanMenuBehaviour);
                    break;
                default: // TODO: delta.area could contain garbage. Maybe we need to check for it.
                    scoresPage = ScoresPage.Create<ScoresPagePlayers>(pageName, scrollableArea,
                        ScoresController.Instance.playerItemPrefab, playerMenuBehaviour);
                    break;
            }

            foreach (JSONObject item in leaderboard)
            {
                scoresPage.AddItem(item);
            }

            scoresPage.Reposition();

            if (scoresPage.HighlightedItem != null)
            {
                scoresItemToMove = scoresPage.HighlightedItem;
            }
            else
            {
                Debug.LogError("ScoresAnimationController.Init: scoresPage.HighlightedItem == null");

                if (ScoresAnimationManager.Dbg)
                {
                    // Для тестов анимируем не "наш" ScoresItem, а тот, позиция/очки которого изменяются в delta
                    scoresItemToMove = scoresPage.PageItems.Values.FirstOrDefault(item => item.Place == delta.newLeaderboardItem.place);
                    // Zero-based array

                    Debug.LogError(
                        "Для тестов анимируем не \"наш\" ScoresItem, а тот, позиция/очки которого изменяются в delta");

                    if (scoresItemToMove == null)
                    {
                        Debug.LogError("…Но он тоже не найден. :(");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            scoresPage.scrollToHighlightedItem = false; // We don't need it
            scoresPage.gameObject.SetActive(true);

            scrollableArea.contentContainer = scoresPage.gameObject;
            scrollableArea.ContentLength = scoresPage.ContentLength;

            if (delta.area == "friends")
            {
                leaderboardArea.text = Localizer.GetText("lblStatsFriends");
            }
            else
            {
                var selectedAreaParameter = Localizer.GetText(
                    string.Format("WeeklyAwardArea{0}{1}", char.ToUpper(delta.area[0]), delta.area.Substring(1)));

                leaderboardArea.text = Localizer.GetText("lblWeeklyAwardArea", selectedAreaParameter);
            }

            // Если задан собственный текст меш для региона - присваиваем регион туда, иначе добавляем в leaderboardArea
            if (regionLabel != null)
            {
                if (delta.area == "world" || delta.area == "clans" || delta.area == "friends")
                    regionLabel.text = string.Empty;
                else
                    regionLabel.text = areaName;
            }
            else
            {
                if (delta.area != "world" && delta.area != "clans" && delta.area != "friends")
                    leaderboardArea.text += "\n" + areaName;
            }

            lblPlaceIncrease.Parameter = delta.PlaceDelta.ToString();

            // Изменение высоты и позиционирование окна
            if (layoutToBeResized != null)
            {
                float bodyYpositionDelta = 0;

                //TODO: отрефакторить вместе с WeeklyAwardsArea
                if (delta.area == "world" || delta.area == "clans") 
                    bodyYpositionDelta = HEADER_ROW_HEIGHT;

                layoutToBeResized.SetBounds(defaultLayoutMinBounds,
                    new Vector3(defaultLayoutMaxBounds.x,
                        defaultLayoutMaxBounds.y - bodyYpositionDelta,
                        defaultLayoutMaxBounds.z));

                layoutToBeResized.transform.localPosition =
                    new Vector2(defaultWindowLocalposition.x,
                        defaultWindowLocalposition.y - bodyYpositionDelta / 2);
            }

            if (btnOk != null && btnShare != null)
            {
                btnOk.Activated = false;
                btnShare.Activated = false;
            }

            switch (delta.animation)
            {
                case Animation.ChangePlace:
                    scoresAnimation = changingPlaceAnimation;
                    break;

                case Animation.TransitionNumbers:
                    scoresAnimation = transitionNumbersAnimation;
                    break;
            }

            if (scoresAnimation == null)
                return false;

            if (!scoresAnimation.Setup(scrollableArea, scoresPage, delta, scoresItemToMove))
                return false;

            return true;
        }

        public void Show()
        {
            if (ScoresAnimationManager.Dbg)
                Debug.LogErrorFormat("ScoresAnimationController.Show({0});", delta);

            playerMenuBehaviour.contextMenu.transform.AddToSortingOrderRecursively(20, true, true);
            clanMenuBehaviour.contextMenu.transform.AddToSortingOrderRecursively(20, true, true);

            StartCoroutine(scoresAnimation.Show());

            // По просьбе Дениса, чтобы игрок досматривал анимацию до конца
            StartCoroutine(ActivateButtonsOnAnimationEnd());

            StartCoroutine(EnablePressEventsOnAnimationEnd());
        }

        public void SharePlaceIncrease()
        {
            StartCoroutine(Share());
        }

        private IEnumerator ActivateButtonsOnAnimationEnd()
        {
            if (btnOk == null && btnShare == null)
                yield break;

            while (IsAnimating)
            {
                yield return null;
            }

            btnOk.Activated = true;
            btnShare.Activated = true;
        }

        private IEnumerator EnablePressEventsOnAnimationEnd()
        {
            Action<bool> registerPressEvents = enabled =>
            {
                foreach (var scoresItem in scoresPage.PageItems.Values)
                {
                    scoresItem.UiItem.enabled = enabled;
                }

                scrollableArea.backgroundUIItem.enabled = enabled;
            };

            registerPressEvents(false);
            
            while (IsAnimating)
            {
                yield return null;
            }

            registerPressEvents(true);
        }

        private IEnumerator Share()
        {
            yield return new WaitForEndOfFrame();

            SocialSettings.GetSocialService()
                .Post(Localizer.GetText("textSharePlaceIncrease", delta.PlaceDelta), MiscTools.GetScreenshot());
        }

        private void OnOKButtonPress()
        {
            GUIPager.SetActivePage("MainMenu");
        }
    }
}
