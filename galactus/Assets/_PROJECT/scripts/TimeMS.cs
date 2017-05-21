using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class TimeMS : MonoBehaviour
{
    static private TimeMS instance;
    static private Stopwatch stopwatch;

    /// <summary>the list of tasks to execute, pair with a time to execute them</summary>
    private List<Task> timedEvents = new List<Task>();
    /// <summary>tasks to add to the actual list of tasks to execute. A separation is made so that the actual list is not modified while being executed.</summary>
    private List<Task> toQueue = new List<Task>();

    [Tooltip("set to how many milliseconds since the timer started")]
    public long now;

    /// <summary>Initializes the timer. Potentially called at various states of initailization.</summary>
    static public void StaticInit()
    {
        if (instance == null)
        {
            instance = FindObjectOfType(typeof(TimeMS)) as TimeMS;
            if (instance == null)
            {
                GameObject timeMs = new GameObject("TimeMS");
                instance = timeMs.AddComponent<TimeMS>();
            }
        }
        instance.Init();
    }
    public void Init()
    {
        instance = this;
        if (stopwatch != null)
        {
            stopwatch.Stop();
        }
        stopwatch = new Stopwatch();
        stopwatch.Start();
    }

    void OnDestroy()
    {
        instance = null;
        timedEvents.Clear();
        if (stopwatch != null)
        {
            stopwatch.Stop();
            stopwatch = null;
        }
    }

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            throw new System.Exception("There can only be one TimeMS");
        }
        Init();
    }

    /// <summary>How much time has passes (in milliseconds) since TimeMS was used first</summary>
    static public long time
    {
        get { if (stopwatch == null) { StaticInit(); } return stopwatch.ElapsedMilliseconds; }
    }

    [System.Serializable]
    public class Task
    {
        public System.Threading.TimerCallback what;
        public object how;
        public long when;
        public Task(System.Threading.TimerCallback what, object how, long when)
        {
            this.what = what; this.how = how; this.when = when;
        }
        public void Invoke() { what(how); }
    }

    /// <summary><para>Timer callback that takes an object parameter
    /// </para>example:<para>
    /// TimerMS.TimerCallback(1000, "Hello!", (param)=>{Debug.Log(param);}); // prints hello in 1 second
    /// </para>
    /// </summary>
    /// <param name="inHowManyMilliseconds"></param>
    /// <param name="how"></param>
    /// <param name="what"></param>
    static public void TimerCallback(long inHowManyMilliseconds, object how, System.Threading.TimerCallback what)
    {
        Task tn = new Task(what, how, TimeMS.time + inHowManyMilliseconds);
        instance.toQueue.Add(tn);
    }

    public delegate void Lambda();

    /// <summary><para>For (void)=>{} timer callbacks without parameters
    /// </para>example:<para>
    /// TimerMS.TimerCallback(1000, ()=>{Debug.Log("1 second has passed");});
    /// </para>
    /// </summary>
    /// <param name="inHowManyMilliseconds"></param>
    /// <param name="whatToExecute"></param>
    static public void TimerCallback(long inHowManyMilliseconds, Lambda whatToExecute)
    {
        TimerCallback(inHowManyMilliseconds, null, (param) => { whatToExecute(); });
    }

    void Update() { ServiceTimerQueue(); }
    void FixedUpdate() { ServiceTimerQueue(); }

    protected void ServiceTimerQueue()
    {
        now = time;
        Task tn;
        int index;
        // before executing timer tasks, add the tasks that need to be added into the list
        while (toQueue.Count > 0)
        {
            // pull each task out of the queue
            tn = toQueue[toQueue.Count - 1];
            toQueue.RemoveAt(toQueue.Count - 1);
            // insert in order, with the event furthest in the future located earliest in the list
            for (index = timedEvents.Count; index > 0 && timedEvents[index - 1].when < tn.when; --index) ;
            timedEvents.Insert(index, tn);
        }
        // execute all of the tasks that should have been executed by now
        for (index = timedEvents.Count - 1; index >= 0 && ((tn = timedEvents[index]).when <= now); --index)
        {
            tn.Invoke();
            //timedEvents.Remove(tn);
        }
        // remove all of the executed tasks
        if (index < timedEvents.Count - 1)
        {
            // truncate after index
            timedEvents.RemoveRange(index + 1, timedEvents.Count - (index + 1));
        }
    }

    public delegate void LambdaProgress(float progress);

    static public void CallbackWithDuration(long msDuration, LambdaProgress lambdaProgress)
    {
        long started = TimeMS.time;
        System.Threading.TimerCallback progressMethod = (self) => {
            float t = (float)(TimeMS.time - started) / msDuration;
            if (t > 1) t = 1;
            lambdaProgress(t);
            if (t < 1)
            {
                TimeMS.TimerCallback(0, self, self as System.Threading.TimerCallback);
            }
        };
        TimeMS.TimerCallback(0, progressMethod, progressMethod);
    }
}
