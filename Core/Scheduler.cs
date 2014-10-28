using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BlazeSoft.Net.Web
{
    public static class Scheduler
    {
        static Scheduler()
        {
            SchedulerThread = new Thread(new ThreadStart(SchedulerThreadTick));
            SchedulerThread.Start();
        }

        private static Thread SchedulerThread;
        private static void SchedulerThreadTick()
        {
            while(true)
            {
                foreach (SchedulerItem schedulerItem in ScheduledItems.ToArray())
                    if (schedulerItem.LastRan <= DateTime.UtcNow - schedulerItem.Delay)
                    {
                        schedulerItem.DoRun();
                        if (!schedulerItem.Loop)
                            ScheduledItems.Remove(schedulerItem);
                    }

                Thread.Sleep(15000);
            }
        }

        internal static List<SchedulerItem> ScheduledItems = new List<SchedulerItem>();

        public static SchedulerItem AddScheduler(string id, TimeSpan delay, bool loop = true, bool runNow = false)
        {
            SchedulerItem schedulerItem = new SchedulerItem(id, delay, loop, runNow);
            ScheduledItems.Add(schedulerItem);
            return schedulerItem;
        }

        public static bool IsScheduled(string id)
        {
            return ScheduledItems.Where(s => s.ID == id).FirstOrDefault() != null;
        }

        public static SchedulerItem GetScheduler(string id)
        {
            return ScheduledItems.Where(s => s.ID == id).FirstOrDefault();
        }

        public static SchedulerItem[] GetSchedulers(string id)
        {
            return ScheduledItems.Where(s => s.ID == id).ToArray();
        }

        public static void RemoveScheduler(string id)
        {
            foreach (SchedulerItem schedulerItem in ScheduledItems.Where(s => s.ID == id).ToArray())
                ScheduledItems.Remove(schedulerItem);
        }

        public static void RunScheduler(string id)
        {
            foreach (SchedulerItem schedulerItem in ScheduledItems.Where(s => s.ID == id).ToArray())
                schedulerItem.DoRun();
        }
    }

    public class SchedulerItem
    {
        internal SchedulerItem(string id, TimeSpan delay, bool loop, bool runNow)
        {
            this.ID = id;
            this.Delay = delay;
            this.Loop = loop;

            if (runNow)
                this.LastRan = DateTime.UtcNow - this.Delay;
            else
                this.LastRan = DateTime.UtcNow;
        }

        public string ID { get; set; }
        public TimeSpan Delay { get; set; }
        public bool Loop { get; set; }
        public DateTime LastRan { get; set; }

        public event EventHandler Run;

        internal bool DoRun()
        {
            if (this.Run != null)
                this.Run.Invoke(this, new EventArgs());

            this.LastRan = DateTime.UtcNow;

            return true;
        }
    }
}