using System;
using Server.Custom.Dudes;
using Server.Custom.Dudes.Jobs;
using Server.Mobiles;
using Server.Multis;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// Generic Job Station: accepts one DudeBall, resolves a job from the Dude,
    /// runs travel → work → return, deposits rewards into this container.
    /// </summary>
    public class DudeJobStation : BaseContainer, ISecurable
    {
        private DudeBall m_ActiveBall;
        private DudeJobWorker m_Worker;
        private Mobile m_Placer;
        private SecureLevel m_SecureLevel = SecureLevel.Owner;
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

        /// <summary>House secure access level — always Owner for this station.</summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return SecureLevel.Owner; }
            set { m_SecureLevel = SecureLevel.Owner; }
        }

        /// <summary>Player who placed the station in a house; only they may open/use it.</summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Placer
        {
            get { return m_Placer; }
            set { m_Placer = value; InvalidateProperties(); }
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
                if (DudeJobConfig.MaxStoredResource > 0)
                    list.Add("Cap: {0} per resource type", DudeJobConfig.MaxStoredResource);
                else
                    list.Add("Cap: unlimited");
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
                    list.Add("Status: Idle — drop a Dude Ball to start");
                }
            }

            DudeJob propJob = null;
            if (ball != null && ball.HasDude)
                propJob = ResolveCurrentJob(ball.StoredDude);
            list.Add(FormatStorageLine(propJob));

            if (m_Placer != null && !m_Placer.Deleted)
                list.Add("Owner: {0}", m_Placer.Name);

            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house == null || !house.IsInside(this))
                list.Add("Requires: house placement");
            else if (!IsSecure)
                list.Add("Requires: secure (house)");
            else
                list.Add("Secured in house");
        }

        public override void OnSingleClick(Mobile from)
        {
            if (ActiveBall != null && ActiveBall.HasDude)
                LabelTo(from, "Dude Job Station [{0}]", ActiveBall.StoredDude.DisplayName);
            else
                LabelTo(from, "Dude Job Station");
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            if (!base.OnDroppedToWorld(from, p))
                return false;

            if (from == null || from.Map == null)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(p, from.Map, ItemData.Height);
            if (house == null || !house.IsInside(p, ItemData.Height))
            {
                from.SendMessage("A Dude Job Station can only be placed inside a house.");
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && !house.IsCoOwner(from))
            {
                from.SendMessage("You must be a house owner or co-owner to place a Job Station.");
                return false;
            }

            m_Placer = from;
            InvalidateProperties();
            from.SendMessage("Job Station placed. Secure it in the house before using it.");
            return true;
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (m != null && m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (!ValidateHouseSecureUse(m, false))
                return false;

            return m != null && m_Placer != null && m == m_Placer;
        }

        /// <summary>
        /// Must sit inside a house, be house-secured, and only the placer may use it.
        /// </summary>
        public bool ValidateHouseSecureUse(Mobile from, bool message)
        {
            if (Deleted)
                return false;

            if (from != null && from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house == null || !house.IsInside(this))
            {
                if (message && from != null)
                    from.SendMessage("The Job Station must be placed inside a house.");
                return false;
            }

            if (!IsSecure)
            {
                if (message && from != null)
                    from.SendMessage("The Job Station must be secured in the house first. Use the house secure command.");
                return false;
            }

            if (from != null && (m_Placer == null || from != m_Placer))
            {
                if (message)
                    from.SendMessage("Only the person who placed this Job Station can use it.");
                return false;
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            if (!ValidateHouseSecureUse(from, true))
                return;

            from.CloseGump(typeof(DudeJobStationGump));
            from.SendGump(new DudeJobStationGump(from, this));
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!ValidateHouseSecureUse(from, true))
                return false;

            DudeBall ball = dropped as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            // Non-ball items: normal container deposit (player storing / retrieving flow).
            return base.OnDragDrop(from, dropped);
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (!ValidateHouseSecureUse(from, true))
                return false;

            DudeBall ball = dropped as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            return base.TryDropItem(from, dropped, sendFullMessage);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            // Removing the active Dude Ball stops the job (see OnItemRemoved).
            return base.CheckLift(from, item, ref reject);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!ValidateHouseSecureUse(from, true))
                return false;

            DudeBall ball = item as DudeBall;
            if (ball != null)
                return TryAssignBall(from, ball);

            return base.OnDragDropInto(from, item, p);
        }

        public bool TryAssignBall(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted)
                return false;

            if (!ValidateHouseSecureUse(from, true))
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
            ball.Visible = false; // hidden in station — retrieve via gump only
            InvalidateProperties();

            from.SendMessage(0x59, "{0} assigned. Starting {1}...", ball.StoredDude.DisplayName, job.Name);

            if (!TryStartJob(from))
            {
                // Assign kept; player can fix (full storage / no resource) and re-drop or wait.
                from.SendMessage("Job did not start. Fix the issue, retrieve the ball, or try again.");
            }

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
            if (job == null || DudeJobConfig.MaxStoredResource <= 0)
                return false;

            return GetStoredRewardCount(job) >= DudeJobConfig.MaxStoredResource;
        }

        public bool IsOreStorageFull()
        {
            if (DudeJobConfig.MaxStoredResource <= 0)
                return false;

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
            string cap = DudeJobConfig.MaxStoredResource > 0
                ? DudeJobConfig.MaxStoredResource.ToString()
                : "∞";

            if (job == null)
                return string.Format("Stored: {0}/{1}", GetStoredOreCount(), cap);

            return string.Format("{0}: {1}/{2}", job.GetResourceLabel(), GetStoredRewardCount(job), cap);
        }

        /// <summary>
        /// Overhead status text for the station's owner only; these went to every client in range
        /// on every cycle. Skipped when the owner is offline or out of range, as before.
        /// </summary>
        private void OwnerMessage(int hue, string text)
        {
            if (m_Placer != null && !m_Placer.Deleted)
                PrivateOverheadMessage(MessageType.Regular, hue, false, text, m_Placer.NetState);
        }

        public bool TryStopJob(Mobile from)
        {
            if (!ValidateHouseSecureUse(from, true))
                return false;

            if (!m_JobActive)
            {
                if (from != null)
                    from.SendMessage("No job is running.");
                return false;
            }

            AbortJob(from, "Job stopped.");
            OwnerMessage(0x3B2, "Job stopped.");
            return true;
        }

        public bool TryStartJob(Mobile from)
        {
            if (from != null && !from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return false;
            }

            if (!ValidateHouseSecureUse(from, true))
                return false;

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

            if (!ValidateHouseSecureUse(from, true))
                return false;

            DudeBall ball = ActiveBall;
            if (ball == null)
            {
                from.SendMessage("No Dude Ball is assigned.");
                return false;
            }

            if (m_JobActive)
                AbortJob(from, "Job stopped — Dude Ball retrieved.");

            ball.AssignedStation = null;
            ball.Visible = true;
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

        
        private Mobile GetHarvester()
        {
            DudeJobWorker worker = Worker;
            if (worker != null && !worker.Deleted)
                return worker;
            if (m_Placer != null && !m_Placer.Deleted)
                return m_Placer;
            return null;
        }

        private Item CreateJobReward(DudeJob job, DudeData data)
        {
            if (job == null)
                return null;

            Mobile harvester = GetHarvester();
            if (harvester != null && Map != null && Map != Map.Internal)
                return job.CreateReward(data, Map, m_Destination, harvester);

            return job.CreateReward(data);
        }

private Point3D GetSpawnLocation()
        {
            // Use the station's own location/Z so house interiors work.
            // Pathfinding into houses often fails; return teleports land here.
            return Location;
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

            // Abort if moved out of house or unsecured while running.
            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house == null || !house.IsInside(this) || !IsSecure)
            {
                AbortJob(null, "Job Station must remain secured in a house.");
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

            DudeJobStage stageBefore = m_Stage;

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

            // The tooltip only changes with the stage (CompleteJob refreshes after deposits/EXP).
            // Rebuilding it every tick re-ran GetProperties (house lookup, storage scan, several
            // string.Formats) once a second for every running station.
            if (m_Stage != stageBefore && !Deleted)
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

            TimeSpan stageElapsed = DateTime.UtcNow - m_StageStartUtc;
            TimeSpan expected = outbound ? m_OutboundDuration : m_ReturnDuration;
            TimeSpan overdueGrace = outbound ? TimeSpan.FromSeconds(30.0) : TimeSpan.FromSeconds(15.0);
            bool overdue = stageElapsed >= expected + overdueGrace;
            bool maxed = stageElapsed >= DudeJobConfig.MaxTravelDuration;

            int dist = (int)worker.GetDistanceToSqrt(goal);
            bool arrived = worker.InRange(goal, 1);

            // Far travel or already abandoned path: teleport once, never pathfind-spam.
            // Short trips may walk; on first stuck, teleport and abandon path until SetGoal.
            if (!arrived && (worker.PathAbandoned || dist > 8 || overdue || maxed || worker.IsStuck(DudeJobConfig.StuckTimeout)))
            {
                worker.AbandonPath();
                if (outbound)
                    worker.TeleportToGoal();
                else
                    worker.TeleportTo(GetSpawnLocation(), Map); // house Z for return

                arrived = true;
            }
            else if (!arrived)
            {
                arrived = worker.FollowGoal(1);

                // FollowGoal may abandon on stuck mid-tick — teleport same tick, no retry loop.
                if (!arrived && worker.PathAbandoned)
                {
                    if (outbound)
                        worker.TeleportToGoal();
                    else
                        worker.TeleportTo(GetSpawnLocation(), Map);

                    arrived = true;
                }
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
                    m_PendingReward = CreateJobReward(job, data);

                m_Stage = DudeJobStage.TravelingBack;
                m_StageStartUtc = DateTime.UtcNow;
                worker.SetGoal(Location);
            }
        }

        private void CompleteJob(DudeJob job, DudeData data)
        {
            DepositReward(job);
            TryDepositBonusDust();

            // The worker is kept for the next loop (BeginNextJobLoop reuses it); it is only
            // despawned below when the job stops.
            m_JobId = job != null ? job.Id : m_JobId;

            DudeBall ball = ActiveBall;
            if (ball != null && ball.HasDude && data != null)
            {
                int beforeLevel = data.Level;
                DudeExperience.AwardExperience(ball, DudeJobConfig.JobCycleExp, null);
                OwnerMessage(0x59,
                    string.Format("{0} +{1} EXP", data.DisplayName, DudeJobConfig.JobCycleExp));
                if (data.Level > beforeLevel)
                {
                    OwnerMessage(0x44,
                        string.Format("{0} reached level {1}!", data.DisplayName, data.Level));
                }
            }

            // Auto-loop until stopped or resource cap reached.
            if (IsRewardStorageFull(job))
            {
                DespawnWorker();
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                StopJobTimer();
                InvalidateProperties();
                OwnerMessage(0x22,
                    string.Format("Station full ({0}). Job stopped.", FormatStorageLine(job)));
                return;
            }

            if (!BeginNextJobLoop(job, data))
            {
                DespawnWorker();
                m_JobActive = false;
                m_Stage = DudeJobStage.Idle;
                StopJobTimer();
                InvalidateProperties();
                OwnerMessage(0x22, "Could not continue job.");
                return;
            }

            InvalidateProperties();
            OwnerMessage(0x3B2,
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

            // Reuse the worker that just walked back to the station. Deleting it and spawning a
            // fresh mobile (plus shorts and tool) every cycle sent remove/create packets to every
            // client in range. ApplyFromDude picks up level-ups and is a no-op otherwise.
            DudeJobWorker worker = Worker;
            if (worker != null)
                worker.ApplyFromDude(data);
            else if (!SpawnWorker(data))
                return false;

            m_Worker.SetGoal(m_Destination);

            // Called from JobTick, so the repeating timer is normally still running.
            if (m_JobTimer == null)
                StartJobTimer();

            return true;
        }

        /// <summary>
        /// 5% (configurable) chance each cycle to also deposit Dude Dust into the station.
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
                    OwnerMessage(0x59,
                        string.Format("+{0} Dude Dust!", DudeJobConfig.JobDustAmount));
                    return;
                }
            }

            if (!TryDropReward(dust))
                dust.MoveToWorld(GetSpawnLocation(), Map);

            OwnerMessage(0x59,
                string.Format("+{0} Dude Dust!", DudeJobConfig.JobDustAmount));
        }

        private void DepositReward(DudeJob job)
        {
            Item reward = m_PendingReward;
            m_PendingReward = null;

            if (reward == null || reward.Deleted)
            {
                if (job != null && ActiveBall != null && ActiveBall.HasDude)
                    reward = CreateJobReward(job, ActiveBall.StoredDude);
            }

            if (reward == null || reward.Deleted)
                return;

            // Cap deposits only when MaxStoredResource is enabled (> 0).
            if (DudeJobConfig.MaxStoredResource > 0 && job != null && job.CountsTowardStorage(reward))
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
            OwnerMessage(0x22, "Station full — resources left beside it.");
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

        public string[] GetStatusLines()
        {
            DudeBall ball = ActiveBall;
            string dude = (ball != null && ball.HasDude) ? ball.StoredDude.DisplayName : "none";
            DudeJob job = DudeJobRegistry.Get(m_JobId);
            DudeData data = (ball != null && ball.HasDude) ? ball.StoredDude : null;
            DudeJob statusJob = job != null ? job : ResolveCurrentJob(data);
            string jobName = statusJob != null ? statusJob.Name : (job != null ? job.Name : "none");
            string storage = FormatStorageLine(statusJob);

            if (!m_JobActive)
            {
                return new string[]
                {
                    string.Format("Dude: {0}", dude),
                    string.Format("Job: {0}", jobName),
                    "Status: Idle",
                    "Dest: -",
                    storage
                };
            }

            return new string[]
            {
                string.Format("Dude: {0}", dude),
                string.Format("Job: {0}", jobName),
                string.Format("Status: {0}", FormatStage(m_Stage)),
                string.Format("Dest: {0} tiles", m_Distance),
                storage
            };
        }

        public string GetStatusMessage()
        {
            return string.Join(" ", GetStatusLines());
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
                ball.Visible = true;
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
                    AbortJob(null, "Job stopped — Dude Ball removed.");

                if (m_ActiveBall != null)
                {
                    m_ActiveBall.AssignedStation = null;
                    m_ActiveBall.Visible = true;
                }

                m_ActiveBall = null;
                InvalidateProperties();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

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
            writer.Write(m_Placer);
            writer.Write((int)m_SecureLevel);
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

            if (version >= 1)
            {
                m_Placer = reader.ReadMobile();
                m_SecureLevel = (SecureLevel)reader.ReadInt();
            }

            m_SecureLevel = SecureLevel.Owner;

            if (m_ActiveBall != null)
            {
                m_ActiveBall.AssignedStation = this;
                m_ActiveBall.Visible = false;
            }

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
            {
                m_ActiveBall.AssignedStation = this;
                m_ActiveBall.Visible = false;
            }

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
                    m_PendingReward = CreateJobReward(job, ball.StoredDude);

                // The saved worker may be anywhere; start the next loop with a fresh one at the
                // station (CompleteJob now keeps a live worker for reuse).
                DespawnWorker();
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
                    m_PendingReward = CreateJobReward(job, ball.StoredDude);
            }
            else
            {
                m_Stage = DudeJobStage.TravelingBack;
                m_StageStartUtc = m_JobStartUtc + t2;
                if (m_PendingReward == null || m_PendingReward.Deleted)
                    m_PendingReward = CreateJobReward(job, ball.StoredDude);
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
