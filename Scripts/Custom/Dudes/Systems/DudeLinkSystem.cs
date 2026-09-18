using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Runtime link state for players using a LinkingDevice.
    /// Persistent serial/backup live on the LinkingDevice item.
    /// </summary>
    public static class DudeLinkSystem
    {
        public const int LinkFollowerSlots = 3;

        private static readonly Dictionary<Mobile, LinkRuntime> m_ByPlayer =
            new Dictionary<Mobile, LinkRuntime>();

        private static Timer m_PassiveTimer;

        private class LinkRuntime
        {
            public LinkingDevice Device;
            public DudeBall Ball;
            public Dictionary<string, DateTime> NextAbilityById;
            public DateTime NextBurnPulse;
            public DateTime NextSpringPulse;
            public DateTime NextFaultlinePulse;
            public DateTime NextSkillGainPulse;
            public bool FollowersApplied;
        }

        public static void Initialize()
        {
            EventSink.Login += OnLogin;
            EventSink.Logout += OnLogout;
            EventSink.PlayerDeath += OnPlayerDeath;
            EventSink.SkillGain += OnSkillGain;

            CommandSystem.Register("abi1", AccessLevel.Player, new CommandEventHandler(OnAbi1));
            CommandSystem.Register("abi2", AccessLevel.Player, new CommandEventHandler(OnAbi2));

            if (m_PassiveTimer != null)
                m_PassiveTimer.Stop();

            m_PassiveTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), PulsePassives);
        }

        public static bool IsLinked(Mobile m)
        {
            if (m == null)
                return false;

            LinkRuntime rt;
            if (!m_ByPlayer.TryGetValue(m, out rt) || rt == null)
                return false;

            return rt.Device != null && !rt.Device.Deleted && rt.Device.IsLinked;
        }

        public static LinkingDevice GetDevice(Mobile m)
        {
            LinkRuntime rt;
            if (m == null || !m_ByPlayer.TryGetValue(m, out rt) || rt == null)
                return null;
            return rt.Device;
        }

        public static DudeBall GetLinkedBall(Mobile m)
        {
            LinkRuntime rt;
            if (m == null || !m_ByPlayer.TryGetValue(m, out rt) || rt == null)
                return null;

            if (rt.Ball != null && !rt.Ball.Deleted)
                return rt.Ball;

            if (rt.Device != null)
                return rt.Device.ResolveLinkedBall();

            return null;
        }

        public static bool TryRegister(Mobile player, LinkingDevice device, DudeBall ball)
        {
            if (player == null || device == null || ball == null)
                return false;

            if (IsLinked(player))
                return false;

            LinkRuntime rt = new LinkRuntime();
            rt.Device = device;
            rt.Ball = ball;
            rt.NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            rt.FollowersApplied = true;
            m_ByPlayer[player] = rt;
            return true;
        }

        public static void Unregister(Mobile player)
        {
            if (player == null)
                return;

            m_ByPlayer.Remove(player);
        }

        /// <summary>
        /// Called from PlayerMobile.OnBeforeDeath — revert human form before corpse.
        /// </summary>
        public static void HandleBeforeDeath(Mobile m)
        {
            if (m == null || !IsLinked(m))
                return;

            LinkingDevice device = GetDevice(m);
            if (device != null)
                device.Revert(m, true, true);
        }

        private static void OnLogin(LoginEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            Mobile m = e.Mobile;
            // Safest: any mid-link flag on a device in pack → revert to human.
            LinkingDevice device = FindLinkedDeviceInPack(m);
            if (device != null && device.IsLinked)
                device.Revert(m, false, false);
        }

        private static void OnLogout(LogoutEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            Mobile m = e.Mobile;
            if (!IsLinked(m))
            {
                LinkingDevice device = FindLinkedDeviceInPack(m);
                if (device != null && device.IsLinked)
                    device.Revert(m, false, false);
                return;
            }

            LinkingDevice linked = GetDevice(m);
            if (linked != null)
                linked.Revert(m, false, false);
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            // Primary revert is HandleBeforeDeath; this is a safety net.
            if (e == null || e.Mobile == null)
                return;

            if (IsLinked(e.Mobile))
            {
                LinkingDevice device = GetDevice(e.Mobile);
                if (device != null)
                    device.Revert(e.Mobile, true, true);
            }
        }

        private static void OnSkillGain(SkillGainEventArgs e)
        {
            if (e == null || e.From == null || e.Skill == null)
                return;

            if (!IsLinked(e.From))
                return;

            if (!DudeCombatSkills.IsTracked(e.Skill.SkillName))
                return;

            DudeBall ball = GetLinkedBall(e.From);
            if (ball == null || ball.StoredDude == null)
                return;

            DudeCombatSkills.SyncGainToBall(e.From, e.Skill, ball.StoredDude, ball);
        }

        private static LinkingDevice FindLinkedDeviceInPack(Mobile m)
        {
            if (m == null || m.Backpack == null)
                return null;

            List<LinkingDevice> list = m.Backpack.FindItemsByType<LinkingDevice>(true);
            if (list == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                LinkingDevice d = list[i];
                if (d != null && !d.Deleted && d.IsLinked)
                    return d;
            }

            return null;
        }

        private static void OnAbi1(CommandEventArgs e)
        {
            TryFireAbility(e.Mobile, 1);
        }

        private static void OnAbi2(CommandEventArgs e)
        {
            TryFireAbility(e.Mobile, 2);
        }

        private static void TryFireAbility(Mobile from, int stage)
        {
            if (from == null)
                return;

            if (!IsLinked(from))
            {
                from.SendMessage(0x22, "Only a Dude can do that!");
                return;
            }

            DudeBall ball = GetLinkedBall(from);
            if (ball == null || ball.Deleted || ball.StoredDude == null)
            {
                from.SendMessage(0x22, "Your linked Dude Ball is missing.");
                return;
            }

            DudeData data = ball.StoredDude;
            DudeExperience.EnsureEvolutionAbilities(data);

            DudeAbility ability = FindActiveAbility(data, stage);
            if (ability == null)
            {
                from.SendMessage(0x22, "No stage-{0} combat ability is unlocked.", stage);
                return;
            }

            LinkRuntime rt;
            if (!m_ByPlayer.TryGetValue(from, out rt) || rt == null)
                return;

            if (rt.NextAbilityById == null)
                rt.NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            DateTime readyAt;
            if (rt.NextAbilityById.TryGetValue(ability.Id, out readyAt) && DateTime.UtcNow < readyAt)
            {
                double left = (readyAt - DateTime.UtcNow).TotalSeconds;
                from.SendMessage("Ability not ready ({0:0.0}s).", left);
                return;
            }

            Mobile target = ResolveAbilityTarget(from, ability);
            if (target == null)
            {
                from.SendMessage(0x22, "You need a valid combat target.");
                return;
            }

            if (!ability.TryExecuteLinked(from, data, ball, target))
            {
                from.SendMessage(0x22, "You cannot use {0} right now.", ability.Name);
                return;
            }

            TimeSpan cd = ability.Cooldown;
            if (HasUnlocked(data, "slipstream"))
            {
                DudeAbilityConfig.EnsureLoaded();
                DudeAbilityTune slip = DudeAbilityConfig.Get("slipstream");
                double reduce = slip != null && slip.ReduceSeconds > 0.0 ? slip.ReduceSeconds : 2.0;
                double floor = slip != null && slip.FloorSeconds > 0.0 ? slip.FloorSeconds : 7.0;
                cd = TimeSpan.FromSeconds(Math.Max(floor, cd.TotalSeconds - reduce));
            }

            rt.NextAbilityById[ability.Id] = DateTime.UtcNow + cd;
        }

        private static Mobile ResolveAbilityTarget(Mobile from, DudeAbility ability)
        {
            if (ability == null)
                return null;

            string id = ability.Id;
            if (string.Equals(id, "tide_mend", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind_self", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tide_chorus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "ring_of_fire", StringComparison.OrdinalIgnoreCase))
            {
                Mobile combatant = from.Combatant as Mobile;
                if (combatant != null && !combatant.Deleted && combatant.Alive)
                    return combatant;
                return from;
            }

            Mobile t = from.Combatant as Mobile;
            if (t != null && !t.Deleted && t.Alive)
                return t;

            return null;
        }

        private static DudeAbility FindActiveAbility(DudeData data, int stage)
        {
            if (data == null)
                return null;

            List<string> ids = data.GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                DudeAbility ability = DudeAbilityRegistry.Get(ids[i]);
                if (ability == null)
                    continue;
                if (ability.Stage != stage)
                    continue;
                if (DudeCreature.IsPassiveAbilityId(ability.Id))
                    continue;
                return ability;
            }

            return null;
        }

        private static bool HasUnlocked(DudeData data, string abilityId)
        {
            if (data == null || string.IsNullOrEmpty(abilityId))
                return false;

            List<string> ids = data.GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], abilityId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void PulsePassives()
        {
            if (m_ByPlayer.Count == 0)
                return;

            List<Mobile> players = new List<Mobile>(m_ByPlayer.Keys);
            for (int i = 0; i < players.Count; i++)
            {
                Mobile m = players[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;

                LinkRuntime rt;
                if (!m_ByPlayer.TryGetValue(m, out rt) || rt == null)
                    continue;

                DudeBall ball = rt.Ball;
                if (ball == null || ball.Deleted)
                    ball = rt.Device != null ? rt.Device.ResolveLinkedBall() : null;

                if (ball == null || ball.StoredDude == null)
                    continue;

                TryStage3PassivesLinked(m, ball.StoredDude, rt);

                Mobile combatant = m.Combatant as Mobile;
                if (combatant != null && !combatant.Deleted && combatant.Alive)
                {
                    if (DateTime.UtcNow >= rt.NextSkillGainPulse)
                    {
                        rt.NextSkillGainPulse = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
                        DudeCombatSkills.TryGainOnHit(ball.StoredDude, ball, m);
                    }
                }
            }
        }

        private static void TryStage3PassivesLinked(Mobile caster, DudeData data, LinkRuntime rt)
        {
            if (caster == null || data == null || rt == null)
                return;

            if (!caster.Alive || caster.Map == null || caster.Map == Map.Internal)
                return;

            Mobile combatant = caster.Combatant as Mobile;
            bool inCombat = combatant != null && !combatant.Deleted && combatant.Alive;

            if (HasUnlocked(data, "burn"))
                TryBurnPassive(caster, data, rt, inCombat);

            if (HasUnlocked(data, "spring"))
                TrySpringPassive(caster, data, rt, inCombat);

            if (HasUnlocked(data, "faultline"))
                TryFaultlinePassive(caster, data, rt, inCombat);
        }

        private static void TryBurnPassive(Mobile caster, DudeData data, LinkRuntime rt, bool inCombat)
        {
            if (!inCombat)
                return;

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            if (def == null || def.Type != DudeType.Fire)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("burn");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 1.0;
            double hitChance = tune != null && tune.HitChance > 0.0 ? tune.HitChance : 0.5;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.3;

            DateTime now = DateTime.UtcNow;
            if (now < rt.NextBurnPulse)
                return;

            rt.NextBurnPulse = now + TimeSpan.FromSeconds(tick);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(caster, caster, candidates);
            if (candidates.Count == 0)
                return;

            int damage = Math.Max(1, (int)(DudeExperience.GetBlastDamage(data.Level) * vs));
            bool anyHit = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Utility.RandomDouble() >= hitChance)
                    continue;

                Mobile target = candidates[i];
                AOS.Damage(target, caster, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(target, false);
                anyHit = true;
            }

            if (anyHit)
            {
                caster.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Burn*");
                caster.PlaySound(0x208);
            }
        }

        private static void TrySpringPassive(Mobile caster, DudeData data, LinkRuntime rt, bool inCombat)
        {
            if (!inCombat)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("spring");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 2.0;
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.05;

            DateTime now = DateTime.UtcNow;
            if (now < rt.NextSpringPulse)
                return;

            rt.NextSpringPulse = now + TimeSpan.FromSeconds(tick);

            int blast = DudeExperience.GetBlastDamage(data.Level);
            int selfHeal = Math.Max(1, (int)(blast * 0.15));
            int pctHeal = Math.Max(1, (int)(caster.HitsMax * healFrac));
            int heal = Math.Min(selfHeal, pctHeal);
            if (heal < 1)
                heal = 1;

            caster.Hits = Math.Min(caster.HitsMax, caster.Hits + heal);
            DudeAbilityVfx.PlayWaterHeal(caster);

            PlayerMobile pm = caster as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null)
                return;

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature ally = followers[i] as DudeCreature;
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;
                if (ally.Map != caster.Map)
                    continue;
                if (!caster.InRange(ally, 2))
                    continue;

                int allyPct = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int allyHeal = Math.Min(Math.Max(1, (int)(blast * 0.15)), allyPct);
                ally.Hits = Math.Min(ally.HitsMax, ally.Hits + allyHeal);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
        }

        private static void TryFaultlinePassive(Mobile caster, DudeData data, LinkRuntime rt, bool inCombat)
        {
            if (!inCombat)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("faultline");
            double gap = tune != null && tune.GapSeconds > 0.0 ? tune.GapSeconds : 10.0;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.33;
            double stunMin = tune != null && tune.StunMin > 0.0 ? tune.StunMin : 0.5;
            double stunMax = tune != null && tune.StunMax > 0.0 ? tune.StunMax : 1.0;
            if (stunMax < stunMin)
                stunMax = stunMin;

            DateTime now = DateTime.UtcNow;
            if (now < rt.NextFaultlinePulse)
                return;

            rt.NextFaultlinePulse = now + TimeSpan.FromSeconds(gap);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(caster, caster, candidates);

            List<Mobile> valid = new List<Mobile>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Mobile m = candidates[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (m is PlayerMobile)
                    continue;
                if (m is DudeCreature)
                    continue;
                valid.Add(m);
            }

            if (valid.Count == 0)
                return;

            Mobile target = valid[Utility.Random(valid.Count)];
            int damage = Math.Max(1, (int)(DudeExperience.GetBlastDamage(data.Level) * vs));

            caster.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Faultline*");
            AOS.Damage(target, caster, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);

            double stun = stunMin + (Utility.RandomDouble() * (stunMax - stunMin));
            target.Paralyze(TimeSpan.FromSeconds(stun));
        }

        /// <summary>
        /// Award kill EXP to a linked player's ball (called from DudeKillHandler).
        /// </summary>
        public static void TryAwardLinkedKill(Mobile master, Mobile victim)
        {
            if (master == null || victim == null || !IsLinked(master))
                return;

            DudeBall ball = GetLinkedBall(master);
            if (ball == null || ball.Deleted || ball.StoredDude == null)
                return;

            // Sync current hits onto ball before EXP/level-up ApplyData side-effects.
            ball.StoredDude.Hits = Math.Max(0, Math.Min(master.Hits, ball.StoredDude.HitsMax));

            int amount = DudeExperience.CalculateKillExp(ball.StoredDude, victim);
            if (amount <= 0)
                return;

            DudeExperience.AwardExperience(ball, amount, master);

            // Refresh linked form after possible level-up (stats / skills stay on Dude).
            if (ball.StoredDude != null)
            {
                DudeDefinition def = DudeRegistry.Get(ball.StoredDude.DefinitionId);
                if (def != null)
                {
                    master.BodyMod = def.Body;
                    master.HueMod = def.Hue;
                }

                master.RawStr = Math.Max(1, ball.StoredDude.Str);
                master.RawDex = Math.Max(1, ball.StoredDude.Dex);
                master.RawInt = Math.Max(1, ball.StoredDude.Int);

                int hits = ball.StoredDude.Hits;
                if (hits < 1)
                    hits = 1;
                if (hits > master.HitsMax)
                    hits = master.HitsMax;
                master.Hits = hits;

                DudeCombatSkills.ApplyToMobile(master, ball.StoredDude);
                master.SendSpeedControl(SpeedControlType.MountSpeed);
            }
        }
    }
}
