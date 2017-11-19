using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemetriTools.Optimizations
{
    public class Timer
    {
        private float interval;
        private float stopTime;
        private bool paused;
        private float remainOnPause;

        public Timer(float interval)
        {
            Start(interval);
        }

        public bool TimeElapsed
        {
            get { return !paused && Time.time >= stopTime; }
        }

        public void Start(float newInterval = -1f)
        {
            if (newInterval > 0)
            {
                interval = newInterval;
            }

            stopTime = Time.time + interval;
        }

        public void Pause()
        {
            paused = true;
            remainOnPause = stopTime - Time.time;
        }

        public void Unpause()
        {
            paused = false;
            stopTime = Time.time + remainOnPause;
        }
    }
}
