using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

namespace Sheduler
{
    public class TasksSheduler : MonoBehaviour
    {
        public int maxParallelTasks = 1;

        public static TasksSheduler Create (GameObject go)
        {
            return go.AddComponent<TasksSheduler> () as TasksSheduler;
        }

        public void Shedule (Task task)
        {
            m_queue.Enqueue (task);
            CheckForStart ();
        }

        private Queue<Task> m_queue;
        private int activeTasks = 0;

        void Awake ()
        {
            m_queue = new Queue<Task> ();
        }


        void CheckForStart ()
        {
            if ( (m_queue.Count > 0) && (activeTasks < maxParallelTasks) ) {
                StartCoroutine (RunTask (m_queue.Dequeue ()));
            }
        }

        IEnumerator RunTask (Task task)
        {
            activeTasks++;
            yield return task.Run();
            activeTasks--;
            CheckForStart ();
        }
    }
}
