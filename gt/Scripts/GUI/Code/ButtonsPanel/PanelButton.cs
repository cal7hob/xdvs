using UnityEngine;

namespace XDevs.ButtonsPanel
{
    public class PanelButton : StateEventSender
    {
        public float height = 0;
        public int priority;

        public void CalculateHeight ()
        {
            var rs = gameObject.GetComponentsInChildren<Renderer>();
            height = 0;
            if (rs.Length > 0) {
                float maxY = rs[0].bounds.max.y;
                float minY = rs[0].bounds.min.y;

                foreach (var r in rs)
                {
                    maxY = Mathf.Max(r.bounds.max.y, maxY);
                    minY = Mathf.Min(r.bounds.min.y, minY);
                }
                height = maxY - minY;
            }
        }
    }
}