using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace XDevs
{
    [Serializable]
    public class BgTypePair
    {
        public BgType bgType;
        public GameObject[] bgComponents;
    }

    public class BgChanger: MonoBehaviour
    {
        [SerializeField] private List<BgTypePair> bgTypeList;

        private bool isInited = false;
        private Dictionary<BgType, BgTypePair> bgTypeDic;

        private void Init()
        {
            if (isInited)
                return;

            bgTypeDic = new Dictionary<BgType, BgTypePair>();
            if (bgTypeList != null)
                for (int i = 0; i < bgTypeList.Count; i++)
                    if (bgTypeList[i] != null && bgTypeList[i].bgComponents != null && bgTypeList[i].bgComponents.Length > 0)
                        bgTypeDic[bgTypeList[i].bgType] = bgTypeList[i];
        }

        public void SetBg(BgType bgType)
        {
            if (!isInited)
                Init();

            if (bgType != BgType.None)
            {
                foreach (var pair in bgTypeDic)//Сначала выключаем все объекты
                    MiscTools.SetObjectsActivity(pair.Value.bgComponents, false);

                BgTypePair bgTypePair = null;
                if (!bgTypeDic.TryGetValue(bgType, out bgTypePair))
                    Debug.LogErrorFormat("Cant turn on bg of type {0} on item {1}", bgType, MiscTools.GetFullTransformName(transform));
                else
                    MiscTools.SetObjectsActivity(bgTypePair.bgComponents, true);//Теперь включаем нужный
                //if (bgTypePair == null)
                //{
                //    KeyValuePair<BgType, BgTypePair> pair = bgTypeDic.FirstOrDefault();
                //    if (pair.Value.bgComponents != null && pair.Value.bgComponents.Length > 0)
                //        bgTypePair = pair.Value;
                //}
            }
        }
    }
}
