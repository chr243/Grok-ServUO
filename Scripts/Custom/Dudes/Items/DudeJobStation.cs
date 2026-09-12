using System;
using Server.Custom.Dudes;
using Server.Custom.Dudes.Jobs;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// Generic Job Station: accepts one DudeBall, resolves a job from the Dude,
    /// runs travel → work → return, deposits rewards into this container.
    /// </summary>
    public class DudeJobStation : BaseContainer
    {
        private DudeBall m_ActiveBall;
        private DudeJobWorker m_Worker;
        private bool m_JobActive;
        private string m_JobId;
        private DudeJobStage m_Stage;
        private Point3D m_Destination;
        private int m_Distance;
        private DateTime m_JobStartUtc;
        private DateTime m_StageStartUtc;
        private TimeSpan m_OutboundDuration;
        private TimeSpan m_WorkDuration;
        private TimeSpan m_ReturnDuration;
        private Timer m_JobTimer;
        private DateTime m_NextWorkAnim;
        private Item m_PendingReward;

        [Constructable]
        public DudeJobStation()
            : base(0xE43) // wooden chest (UOR-classic)
        {
            Name = "Dude Job Station";
            Hue = 2413;
            Weight = 10.0;
            Movable = true;
            GumpID = 0x44;
            DropSound = 0x42;
        }

        public DudeJobStation(Serial serial)
            : base(serial)
        {
        }

        public DudeBall ActiveBall
        {
            get
            {
                if (m_ActiveBall != null && m_ActiveBall.Deleted)
                    m_ActiveBall = null;
                return m_ActiveBall;
            }
        }

        public DudeJobWorker Worker
        {
            get
            {
                if (m_Worker != null && m_Worker.Deleted)
                    m_Worker = null;
                return m_Worker;
            }
        }

        public bool JobActive
        {
            get { return m_JobActive; }
        }

        public DudeJobStage Stage
        {
            get { return m_Stage; }
        }

        public Point3D Destination
        {
            get { return m_Destination; }
        }

        public int Distance
        {
            get { return m_Distance; }
        }

        public override int DefaultMaxWeight
        {
            // Enough headroom for MaxStoredOre stacks of Iron Ore.
            get { return 100000; }
        }

        public override int DefaultMaxItems
        {
            get { return 125; }
        }

        public void ClearWorkerLink()
        {
            m_Worker = null;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            DudeBall ball = ActiveBall;
            if (ball == null || !ball.HasDude)
            {
                list.Add("No Dude assigned");
                list.Add("Drop a filled Dude Ball to start a job");
                list.Add("Cap: {0} per resource type", DudeJobConfig.MaxStoredResource);
            }
            else
            {
                DudeData data = ball.StoredDude;
                list.Add("Dude: {0} (Lv {1}, {2})", data.DisplayName, data.Level, data.Type);

                DudeJob job = DudeJobRegistry.Get(m_JobId);
                if (job == null && !m_JobActive)
                    job = DudeJobRegistry.GetJobForDude(data);

                if (job != null)
                    list.Add("Job: {0}", job.Name);
                else
                    list.Add("Job: none available");

                list.Add("Job skill: {0}", DudeJobHarvest.FormatSkillLabel(data));

                if (m_JobActive)
                {
                    list.Add("Status: {0}", FormatStage(m_Stage));
                    if (m_Distance > 0)
                        list.Add("Destination: {0} tiles away", m_Distance);
                }
                else
                {
                    list.Add("Status: Idle — double-click to open or start");
                }
            }

            DudeJob propJob = null;
            if (ball != null && ball.HasDude)
                propJob = ResolveCurrentJob(ball.StoredDude);
            list.Add(FormatStorageLine(propJob));
        }

        public override void OnSingleClick(Mobile from)
        {
            if (ActiveBall != null && ActiveBall.HasDude)
                LabelTo(from, "Dude Job Station [{0}]", ActiveBall.StoredDude.DisplayName);
            else
                LabelTo(from, "Dude Job Station");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            from.CloseGump(typeof(DudeJobStationGump));
            from.SendGump(new DudeJobStationGump(from, this));
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            DudeBall ball = dropped as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            // Non-ball items: normal container deposit (player storing / retrieving flow).
            return base.OnDragDrop(from, dropped);
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            DudeBall ball = dropped as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            return base.TryDropItem(from, dropped, sendFullMessage);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (item == m_ActiveBall && m_JobActive)
            {
                from.SendMessage("You cannot remove the Dude Ball while a job is in progress.");
                reject = LRReason.CannotLift;
                return false;
            }

            return base.CheckLift(from, item, ref reject);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            DudeBall ball = item as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            return base.OnDragDropInto(from, item, p);
        }

        public bool TryAssignBall(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted)
                return false;

            if (m_JobActive)
            {
                from.SendMessage("A job is already in progress.");
                return false;
            }

            if (ActiveBall != null)
            {
                from.SendMessage("This station already has a Dude Ball. Retrieve it first.");
                return false;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                from.SendMessage("That Dude Ball is empty.");
                return false;
            }

            if (ball.IsSummoned)
            {
                from.SendMessage("Recall the Dude before assigning it to a Job Station.");
                return false;
            }

            if (ball.StoredDude.IsFainted)
            {
                from.SendMessage("{0} is fainted and cannot work. Revive it first.", ball.StoredDude.DisplayName);
                return false;
            }

            DudeJob job = DudeJobRegistry.GetJobForDude(ball.StoredDude);
            if (job == null)
            {
                from.SendMessage("{0} has no job available at this station.", ball.StoredDude.DisplayName);
                return false;
            }

            // Accept into station inventory and track as active.
            if (ball.Parent != this)
            {
                if (!base.TryDropItem(from, ball, true))
                {
                    from.SendMessage("Could not place the Dude Ball in the station.");
                    return false;
                }
            }

            if (ball.Parent != this)
            {
                from.SendMessage("Could not place the Dude Ball in the station.");
                return false;
            }

            m_ActiveBall = ball;
            ball.AssignedStation = this;
            InvalidateProperties();

            from.SendMessage(0x59, "{0} assigned. Job available: {1}. Double-click the station to Start Job.", ball.StoredDude.DisplayName, job.Name);
            return true;
        }


        public int GetStoredOreCount()
        {
            return GetStoredRewardCount(typeof(IronOre));
        }

        public int GetStoredRewardCount(Type rewardType)
        {
            if (rewardType == null)
                return 0;

            int total = 0;

            foreach (Item item in Items)
            {
                if (item == null || item.Deleted || item == m_ActiveBall)
                    continue;

                if (rewardType.IsInstanceOfType(item))
                    total += item.Amount;
            }

            return total;
        }

        public int GetStoredRewardCount(DudeJob job)
        {
            if (job == null)
                return 0;

            int total = 0;
            foreach (Item item in Items)
            {
                if (item == null || item.Deleted || item == m_ActiveBall)
                    continue;
                if (job.CountsTowardStorage(item))
                    total += item.Amount;
            }
            return total;
        }

        public bool IsRewardStorageFull(DudeJob job)
        {
            if (job == null)
                return false;

            return GetStoredRewardCount(job) >= DudeJobConfig.MaxStoredResource;
        }

        public bool IsOreStorageFull()
        {
            return GetStoredRewardCount(typeof(IronOre)) >= DudeJobConfig.MaxStoredResource;
        }

        private DudeJob ResolveCurrentJob(DudeData data)
        {
            DudeJob job = DudeJobRegistry.Get(m_JobId);
            if (job == null && data != null)
                job = DudeJobRegistry.GetJobForDude(data);
            return job;
        }

        private string FormatStorageLine(DudeJob job)
        {
            if (job == null)
                return string.Format("Stored: {0}/{1}", GetStoredOreCount(), DudeJobConfig.MaxStoredResource);

            return string.Format("{0}: {1}/{2}", job.GetResourceLabel(), GetStoredRewardCount(job), DudeJobConfig.MaxStoredResource);
        }

        public bool TryStopJob(Mobile from)
        {
            if (!m_JobActive)
            {
                if (from != null)
                    from.SendMessage("No job is running.");
                return false;
            }

            AbortJob(from, "Job stopped.");
            PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Job stopped.");
            return true;
        }

        public bool TryStartJob(Mobile from)
        {
            if (from != null && !from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return false;
            }

            if (m_JobActive)
            {
                if (from != null)
                    from.SendMessage("A job is already running.");
                return false;
            }

            DudeBall ball = ActiveBall;
            if (ball == null || !ball.HasDude)
            {
                if (from != null)
                    from.SendMessage("Assign a filled Dude Ball first.");
                return false;
            }

            DudeJob startJobCheck = DudeJobRegistry.GetJobForDude(ball.StoredDude);
            if (IsRewardStorageFull(startJobCheck))
            {
                if (from != null)
                    from.SendMessage("Station is full ({0} {1}). Empty it before starting.", DudeJobConfig.MaxStoredResource, startJobCheck.GetResourceLabel());
                return false;
            }

            if (ball.IsSummoned)
            {
                if (from != null)
                    from.SendMessage("Recall the Dude before starting a job.");
                return false;
            }

            if (ball.StoredDude != null && ball.StoredDude.IsFainted)
            {
                if (from != null)
                    from.SendMessage("{0} is fainted and cannot work. Revive it first.", ball.StoredDude.DisplayName);
                return false;
            }

            DudeData data = ball.StoredDude;
            DudeJob job = DudeJobRegistry.GetJobForDude(data);
            if (job == null)
            {
                if (from != null)
                    from.SendMessage("This Dude cannot perform any job here.");
                return false;
            }

            Point3D dest;
            int dist;
            if (!job.TryFindDestination(this, data, out dest, out dist))
            {
                if (from != null)
                    from.SendMessage("No suitable {0} found near this station.", job.GetResourceLabel());
                return false;
            }

            m_JobId = job.Id;
            m_Destination = dest;
            m_Distance = dist;
            m_OutboundDuration = job.EstimateTravelTime(dist);
            m_WorkDuration = job.GetWorkDuration(data);
            m_ReturnDuration = m_OutboundDuration;
            m_JobStartUtc = DateTime.UtcNow;
            m_StageStartUtc = DateTime.UtcNow;
            m_Stage = DudeJobStage.TravelingOut;
            m_JobActive = true;
            m_NextWorkAnim = DateTime.UtcNow;
            m_PendingReward = null;

            if (!SpawnWorker(data))
            {
                AbortJob(from, "Could not spawn worker.");
                return false;
            }

            m_Worker.SetGoal(m_Destination);
            StartJobTimer();
            InvalidateProperties();

            if (from != null)
            {
                from.SendMessage(0x59, "{0} begins {1} ({2} tiles away).", data.DisplayName, job.Name, dist);
            }

            return true;
        }

        public bool TryRetrieveBall(Mobile from)
        {
            if (from == null)
                return false;

            if (m_JobActive)
            {
                from.SendMessage("Wait for the job to finish before retrieving the Dude Ball.");
                return false;
            }

            DudeBall ball = ActiveBall;
            if (ball == null)
            {
                from.SendMessage("No Dude Ball is assigned.");
                return false;
            }

            ball.AssignedStation = null;
            m_ActiveBall = null;

            if (from.Backpack != null && from.Backpack.TryDropItem(from, ball, false))
            {
                from.SendMessage(0x59, "You retrieve the Dude Ball.");
            }
            else
            {
                ball.MoveToWorld(from.Location, from.Map);
                from.SendMessage("You retrieve the Dude Ball (dropped at your feet).");
            }

            InvalidateProperties();
            return true;
        }

        private bool SpawnWorker(DudeData data)
        {
            DespawnWorker();

            if (Map == null || Map == Map.Internal)
                return false;

            DudeJobWorker worker = new DudeJobWorker();
            worker.Station = this;
            worker.BoundBall = m_ActiveBall;
            worker.ApplyFromDude(data);

            Point3D spawn = GetSpawnLocation();
            worker.MoveToWorld(spawn, Map);
            m_Worker = worker;
            return true;
        }

        private Point3D GetSpawnLocation()
        {
            Map map = Map;
            Point3D loc = Location;

            if (map == null)
                return loc;

            for (int i = 0; i < 8; i++)
            {
                int x = loc.X + Utility.RandomMinMax(-1, 1);
                int y = loc.Y + Utility.RandomMinMax(-1, 1);
                int z = map.GetAverageZ(x, y);
                Point3D p = new Point3D(x, y, z);
                if (map.CanFit(p, 16, false, false))
                    return p;
            }

            return loc;
        }

        private void DespawnWorker()
        {
            if (m_Worker != null && !m_Worker.Deleted)
            {
                m_Worker.Station = null;
                m_Worker.Delete();
            }

            m_Worker = null;
        }

        public void OnWorkerLost(DudeJobWorker worker)
        {
            if (worker != m_Worker)
                return;

            m_Worker = null;

            if (!m_JobActive)
                return;

            // Respawn and continue — never permanently block or lose the Dude.
            DudeBall ball = ActiveBall;
            if (ball == null || !ball.HasDude)
            {
                AbortJob(null, null);
                return;
            }

            if (!SpawnWorker(ball.StoredDude))
            {
                AbortJob(null, null);
                return;
            }

            if (m_Stage == DudeJobStage.TravelingOut)
                m_Worker.SetGoal(m_Destination);
            else if (m_Stage == DudeJobStage.TravelingBack || m_Stage == DudeJobStage.Completing)
                m_Worker.SetGoal(Location);
            else if (m_Stage == DudeJobStage.Working)
                m_Worker.TeleportTo(m_Destination, Map);
        }

        private void StartJobTimer()
        {
            StopJobTimer();
            m_JobTimer = Timer.DelayCall(DudeJobConfig.JobTickInterval, DudeJobConfig.JobTickInterval, new TimerCallback(JobTick));
        }

        private void StopJobTimer()
        {
            if (m_JobTimer != null)
            {
                m_JobTimer.Stop();
                m_JobTimer = null;
            }
        }

        private void JobTick()
        {
            if (Deleted)
            {
                StopJobTimer();
                return;
            }

            if (!m_JobActive)
            {
                StopJobTimer();
                return;
            }

            DudeBall ball = ActiveBall;
            if (ball == null || ball.Deleted || !ball.HasDude)
            {
                AbortJob(null, null);
                return;
            }

            DudeJob job = DudeJobRegistry.Get(m_JobId);
            if (job == null)
            {
                AbortJob(null, null);
                return;
            }

            if (Worker == null)
                OnWorkerLost(null);

            if (Worker == null)
                return;

            switch (m_Stage)
            {
                case DudeJobStage.TravelingOut:
                    ProcessTravel(true, job, ball.StoredDude);
                    break;
                case DudeJobStage.Working:
                    ProcessWork(job, ball.StoredDude);
                    break;
                case DudeJobStage.TravelingBack:
                    ProcessTravel(false, job, ball.StoredDude);
                    break;
                case DudeJobStage.Completing:
                    CompleteJob(job, ball.StoredDude);
                    break;
            }

            InvalidateProperties();
        }

        private void ProcessTravel(bool outbound, DudeJob job, DudeData data)
        {
            DudeJobWorker worker = Worker;
            if (worker == null)
                return;

            Point3D goal = outbound ? m_Destination : Location;
            if (!worker.HasGoal || worker.Goal != goal)
                worker.SetGoal(goal);

            bool arrived = worker.FollowGoal(1);

            TimeSpan stageElapsed = DateTime.UtcNow - m_StageStartUtc;
            TimeSpan expected = outbound ? m_OutboundDuration : m_ReturnDuration;

            if (worker.IsStuck(DudeJobConfig.StuckTimeout) || stageElapsed >= DudeJobConfig.MaxTravelDuration)
            {
                worker.TeleportToGoal();
                arrived = true;
            }
            else if (!arrived && stageElapsed >= expected + TimeSpan.FromSeconds(15.0))
            {
                // Soft fallback if pathfinding is slow.
                worker.TeleportToGoal();
                arrived = true;
            }

            if (arrived)
            {
                if (outbound)
                {
                    m_Stage = DudeJobStage.Working;
                    m_StageStartUtc = DateTime.UtcNow;
                    m_NextWorkAnim = DateTime.UtcNow;
                    worker.ClearGoal();
                }
                else
                {
                    m_Stage = DudeJobStage.Completing;
                    m_StageStartUtc = DateTime.UtcNow;
                }
            }
        }

        private void ProcessWork(DudeJob job, DudeData data)
        {
            DudeJobWorker worker = Worker;
            if (worker == null)
                return;

            if (!worker.InRange(m_Destination, 2))
                worker.TeleportTo(m_Destination, Map);

            if (DateTime.UtcNow >= m_NextWorkAnim)
            {
                worker.PlayWorkAnimation(job, data);
                m_NextWorkAnim = DateTime.UtcNow + DudeJobConfig.WorkAnimInterval;
            }

            if (DateTime.UtcNow - m_StageStartUtc >= m_WorkDuration)
            {
                // Create reward now; deposit on return/complete so full-container is handled at station.
                if (m_PendingReward == null || m_PendingReward.Deleted)
                    m_PendingReward = job.CreateReward(data);

                m_Stage = DudeJobStage.TravelingBack;
                m_StageStartUtc = DateTime.UtcNow;
                worker.SetGoal(Location);
            }
        }

        private void CompleteJob(DudeJob job, DudeData data)
        {
            DepositReward(job);
            TryDepositBonusDust();

            DespawnWorker();
            m_JobId = job != null ? job.Id : m_JobId;

            DudeBall ball = ActiveBall;
            if (ball != null && ball.HasDude && data != null)
            {
                int beforeLevel = data.Level;
                DudeExperience.AwardExperience(ball, DudeJobConfig.JobCycleExp, null);
                PublicOverheadMessage(MessageType.Regular, 0x59, false,
                    string.Format("{0} +{1} EXP", data.DisplayName, DudeJobConfig.JobCycleExp));
                if (data.Level > beforeLevel)
                {
                    PublicOverheadMessage(MessageType.Regular, 0x44, false,
                        string.Format("{0} reached level {1}!", data.DisplayName, data.Level));
                }
            }

            // Auto-loop until stopped or resource cap reached.
            if (IsRewardStorageFull(job))
            {
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                StopJobTimer();
                InvalidateProperties();
                PublicOverheadMessage(MessageType.Regular, 0x22, false,
                    string.Format("Station full ({0}). Job stopped.", FormatStorageLine(job)));
                return;
            }

            if (!BeginNextJobLoop(job, data))
            {
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                StopJobTimer();
                InvalidateProperties();
                PublicOverheadMessage(MessageType.Regular, 0x22, false, "Could not continue job.");
                return;
            }

            InvalidateProperties();
            PublicOverheadMessage(MessageType.Regular, 0x3B2, false,
                string.Format("Cycle done ({0}). Continuing...", FormatStorageLine(job)));
        }

        private bool BeginNextJobLoop(DudeJob job, DudeData data)
        {
            if (job == null || data == null)
                return false;

            DudeBall ball = ActiveBall;
            if (ball == null || !ball.HasDude || ball.IsSummoned)
                return false;

            Point3D dest;
            int dist;
            if (!job.TryFindDestination(this, data, out dest, out dist))
                return false;

            m_JobId = job.Id;
            m_Destination = dest;
            m_Distance = dist;
            m_OutboundDuration = job.EstimateTravelTime(dist);
            m_WorkDuration = job.GetWorkDuration(data);
            m_ReturnDuration = m_OutboundDuration;
            m_JobStartUtc = DateTime.UtcNow;
            m_StageStartUtc = DateTime.UtcNow;
            m_Stage = DudeJobStage.TravelingOut;
            m_JobActive = true;
            m_NextWorkAnim = DateTime.UtcNow;
            m_PendingReward = null;

            if (!SpawnWorker(data))
                return false;

            m_Worker.SetGoal(m_Destination);
            StartJobTimer();
            return true;
        }

        /// <summary>
        /// 1% (configurable) chance each cycle to also deposit Dude Dust into the station.
        /// </summary>
        private void TryDepositBonusDust()
        {
            if (DudeJobConfig.JobDustChance <= 0.0 || DudeJobConfig.JobDustAmount <= 0)
                return;

            if (Utility.RandomDouble() >= DudeJobConfig.JobDustChance)
                return;

            DudeDust dust = new DudeDust(DudeJobConfig.JobDustAmount);

            // Stack onto existing dust in the station when possible.
            foreach (Item item in Items)
            {
                if (item == null || item.Deleted || item == m_ActiveBall)
                    continue;

                DudeDust existing = item as DudeDust;
                if (existing != null && existing.Amount + dust.Amount <= 60000)
                {
                    existing.Amount += dust.Amount;
                    dust.Delete();
                    PublicOverheadMessage(MessageType.Regular, 0x59, false,
                        string.Format("+{0} Dude Dust!", DudeJobConfig.JobDustAmount));
                    return;
                }
            }

            if (!TryDropReward(dust))
                dust.MoveToWorld(GetSpawnLocation(), Map);

            PublicOverheadMessage(MessageType.Regular, 0x59, false,
                string.Format("+{0} Dude Dust!", DudeJobConfig.JobDustAmount));
        }

        private void DepositReward(DudeJob job)
        {
            Item reward = m_PendingReward;
            m_PendingReward = null;

            if (reward == null || reward.Deleted)
            {
                if (job != null && ActiveBall != null && ActiveBall.HasDude)
                    reward = job.CreateReward(ActiveBall.StoredDude);
            }

            if (reward == null || reward.Deleted)
                return;

            // Never push past the configured resource cap for this job.
            if (job != null && job.CountsTowardStorage(reward))
            {
                int room = DudeJobConfig.MaxStoredResource - GetStoredRewardCount(job);
                if (room <= 0)
                {
                    reward.Delete();
                    return;
                }

                if (reward.Amount > room)
                    reward.Amount = room;
            }

            // Try stack into existing piles first.
            if (reward.Stackable)
            {
                foreach (Item item in Items)
                {
                    if (item == null || item.Deleted || item == m_ActiveBall)
                        continue;

                    if (item.GetType() == reward.GetType() && item.Amount + reward.Amount <= 60000)
                    {
                        item.Amount += reward.Amount;
                        reward.Delete();
                        return;
                    }
                }
            }

            if (TryDropReward(reward))
                return;

            // Container full — drop beside station, never silent-lose.
            reward.MoveToWorld(GetSpawnLocation(), Map);
            PublicOverheadMessage(MessageType.Regular, 0x22, false, "Station full — resources left beside it.");
        }

        private bool TryDropReward(Item reward)
        {
            if (reward == null || reward.Deleted)
                return true;

            int count = Items.Count;
            int maxItems = MaxItems;
            if (maxItems > 0 && count >= maxItems)
                return false;

            int totalWeight = TotalWeight + reward.PileWeight + reward.TotalWeight;
            if (MaxWeight > 0 && totalWeight > MaxWeight)
                return false;

            AddItem(reward);
            return reward.Parent == this;
        }

        private void AbortJob(Mobile from, string message)
        {
            // Keep the ball; despawn worker; drop any pending reward beside station.
            if (m_PendingReward != null && !m_PendingReward.Deleted)
            {
                m_PendingReward.MoveToWorld(GetSpawnLocation(), Map);
                m_PendingReward = null;
            }

            DespawnWorker();
            m_JobActive = false;
            m_Stage = DudeJobStage.Idle;
            StopJobTimer();
            InvalidateProperties();

            if (from != null && !string.IsNullOrEmpty(message))
                from.SendMessage(message);
        }

        public string GetStatusMessage()
        {
            DudeBall ball = ActiveBall;
            string dude = (ball != null && ball.HasDude) ? ball.StoredDude.DisplayName : "none";
            DudeJob job = DudeJobRegistry.Get(m_JobId);
            string jobName = job != null ? job.Name : "none";

            DudeData data = (ball != null && ball.HasDude) ? ball.StoredDude : null;
            DudeJob statusJob = job != null ? job : ResolveCurrentJob(data);
            string storage = FormatStorageLine(statusJob);

            if (!m_JobActive)
                return string.Format("Station idle. Dude: {0}. Job: {1}. {2}.", dude, jobName, storage);

            return string.Format(
                "Dude: {0}. Job: {1}. Status: {2}. Dest: {3} tiles. {4}.",
                dude, jobName, FormatStage(m_Stage), m_Distance, storage);
        }

        private static string FormatStage(DudeJobStage stage)
        {
            switch (stage)
            {
                case DudeJobStage.TravelingOut:
                    return "Traveling to resource";
                case DudeJobStage.Working:
                    return "Working";
                case DudeJobStage.TravelingBack:
                    return "Returning";
                case DudeJobStage.Completing:
                    return "Depositing";
                default:
                    return "Idle";
            }
        }

        private int CountOutputItems()
        {
            int count = 0;
            foreach (Item item in Items)
            {
                if (item == null || item.Deleted)
                    continue;
                if (item == m_ActiveBall)
                    continue;
                count++;
            }
            return count;
        }

        public override void OnDelete()
        {
            StopJobTimer();

            // Safety: never lose the Dude — eject ball to world.
            if (m_ActiveBall != null && !m_ActiveBall.Deleted)
            {
                DudeBall ball = m_ActiveBall;
                ball.AssignedStation = null;
                m_ActiveBall = null;

                if (ball.Parent == this)
                {
                    // Remove from us then place in world
                }

                Point3D loc = GetWorldLocation();
                Map map = Map;
                if (map != null && map != Map.Internal)
                    ball.MoveToWorld(loc, map);
            }

            if (m_PendingReward != null && !m_PendingReward.Deleted)
            {
                Point3D loc = GetWorldLocation();
                Map map = Map;
                if (map != null && map != Map.Internal)
                    m_PendingReward.MoveToWorld(loc, map);
                m_PendingReward = null;
            }

            DespawnWorker();
            base.OnDelete();
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (item == m_ActiveBall)
            {
                if (m_JobActive)
                {
                    // Should have been blocked by CheckLift; if forced, abort safely.
                    AbortJob(null, null);
                }

                if (m_ActiveBall != null)
                    m_ActiveBall.AssignedStation = null;

                m_ActiveBall = null;
                InvalidateProperties();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_ActiveBall);
            writer.Write(m_Worker);
            writer.Write(m_JobActive);
            writer.Write(m_JobId);
            writer.Write((int)m_Stage);
            writer.Write(m_Destination);
            writer.Write(m_Distance);
            writer.Write(m_JobStartUtc);
            writer.Write(m_StageStartUtc);
            writer.Write(m_OutboundDuration);
            writer.Write(m_WorkDuration);
            writer.Write(m_ReturnDuration);
            writer.Write(m_PendingReward);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            DudeJobRegistry.EnsureInitialized();

            m_ActiveBall = reader.ReadItem() as DudeBall;
            m_Worker = reader.ReadMobile() as DudeJobWorker;
            m_JobActive = reader.ReadBool();
            m_JobId = reader.ReadString();
            m_Stage = (DudeJobStage)reader.ReadInt();
            m_Destination = reader.ReadPoint3D();
            m_Distance = reader.ReadInt();
            m_JobStartUtc = reader.ReadDateTime();
            m_StageStartUtc = reader.ReadDateTime();
            m_OutboundDuration = reader.ReadTimeSpan();
            m_WorkDuration = reader.ReadTimeSpan();
            m_ReturnDuration = reader.ReadTimeSpan();
            m_PendingReward = reader.ReadItem();

            if (m_ActiveBall != null)
                m_ActiveBall.AssignedStation = this;

            if (m_Worker != null)
            {
                m_Worker.Station = this;
                m_Worker.BoundBall = m_ActiveBall;
            }

            // Delay resume until the world is fully loaded (ball/map refs ready).
            if (m_JobActive)
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerCallback(ResumeJobAfterWorldLoad));
        }

        private void ResumeJobAfterWorldLoad()
        {
            if (Deleted || !m_JobActive)
                return;

            if (m_ActiveBall != null && !m_ActiveBall.Deleted)
                m_ActiveBall.AssignedStation = this;

            RecoverJobAfterLoad();

            if (m_JobActive)
                StartJobTimer();
        }

        /// <summary>
        /// After restart: compute which stage the job should be in based on elapsed time,
        /// respawn worker if needed, and teleport to the appropriate place.
        /// </summary>
        private void RecoverJobAfterLoad()
        {
            DudeBall ball = ActiveBall;
            if (ball == null || !ball.HasDude)
            {
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                DespawnWorker();
                return;
            }

            if (ball.StoredDude != null && ball.StoredDude.IsFainted)
            {
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                DespawnWorker();
                return;
            }

            DudeJob job = DudeJobRegistry.Get(m_JobId);
            if (job == null)
                job = DudeJobRegistry.GetJobForDude(ball.StoredDude);

            if (job == null)
            {
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                DespawnWorker();
                return;
            }

            m_JobId = job.Id;

            TimeSpan elapsed = DateTime.UtcNow - m_JobStartUtc;
            TimeSpan t1 = m_OutboundDuration;
            TimeSpan t2 = t1 + m_WorkDuration;
            TimeSpan t3 = t2 + m_ReturnDuration;

            if (elapsed >= t3)
            {
                // Finished while offline — same completion path as a live cycle (EXP, dust, loop).
                if (m_PendingReward == null || m_PendingReward.Deleted)
                    m_PendingReward = job.CreateReward(ball.StoredDude);

                CompleteJob(job, ball.StoredDude);
                return;
            }

            if (elapsed < t1)
            {
                m_Stage = DudeJobStage.TravelingOut;
                m_StageStartUtc = m_JobStartUtc;
            }
            else if (elapsed < t2)
            {
                m_Stage = DudeJobStage.Working;
                m_StageStartUtc = m_JobStartUtc + t1;
                if (m_PendingReward == null || m_PendingReward.Deleted)
                    m_PendingReward = job.CreateReward(ball.StoredDude);
            }
            else
            {
                m_Stage = DudeJobStage.TravelingBack;
                m_StageStartUtc = m_JobStartUtc + t2;
                if (m_PendingReward == null || m_PendingReward.Deleted)
                    m_PendingReward = job.CreateReward(ball.StoredDude);
            }

            if (Worker == null)
                SpawnWorker(ball.StoredDude);

            if (Worker != null)
            {
                if (m_Stage == DudeJobStage.TravelingOut)
                {
                    // Place somewhere along the route (teleport to dest if mostly done).
                    double frac = 0.0;
                    if (m_OutboundDuration.TotalSeconds > 0)
                        frac = (DateTime.UtcNow - m_JobStartUtc).TotalSeconds / m_OutboundDuration.TotalSeconds;
                    if (frac > 0.8)
                        Worker.TeleportTo(m_Destination, Map);
                    else
                        Worker.TeleportTo(GetSpawnLocation(), Map);
                    Worker.SetGoal(m_Destination);
                }
                else if (m_Stage == DudeJobStage.Working)
                {
                    Worker.TeleportTo(m_Destination, Map);
                    Worker.ClearGoal();
                }
                else
                {
                    Worker.TeleportTo(m_Destination, Map);
                    Worker.SetGoal(Location);
                }
            }
        }



    }
}
