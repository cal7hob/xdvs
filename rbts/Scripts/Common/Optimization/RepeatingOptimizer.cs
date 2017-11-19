using UnityEngine;

namespace DemetriTools.Optimizations
{
	public class RepeatingOptimizer
	{
        public bool BoolValue { get; set; }
        public object ObjectValue { get; set; }

        private float timeInterval;
		private int frameInterval;

		private int lastUpdateFrameCount;
		private float lastUpdateTime;

		public RepeatingOptimizer(float timeInterval, int frameInterval = 0)
		{
			SetIntervals(timeInterval, frameInterval);
		    lastUpdateTime = - timeInterval;
            lastUpdateFrameCount = - frameInterval;
		}

		public bool AskPermission()
		{
			if (Time.time - lastUpdateTime < timeInterval || Time.frameCount - lastUpdateFrameCount < frameInterval)
				return false;

			lastUpdateTime = Time.time;
			lastUpdateFrameCount = Time.frameCount;

			return true;
		}

	    public void Reset(float firstTimeInterval = -0.001f, int firstFrameInterval = 0)
	    {
            lastUpdateTime = Time.time + firstTimeInterval - timeInterval;
	        lastUpdateFrameCount = Time.frameCount + firstFrameInterval - frameInterval;
	    }

	    public void SetIntervals(float newTimeInterval, int newFrameInterval)
	    {
	        timeInterval = newTimeInterval;
	        frameInterval = newFrameInterval;
	    }
	}
}