using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BigSmallHarpies
{
    // Vermillion rebirth: Anomaly's death refusal, but the revival erupts in a fire burst and leaves
    // no scars or death refusal sickness (so no -8 "death refusal" mood thought either).
    // Subclassing keeps every vanilla integration (corpse gizmo, AI auto-revive, corpse ticking),
    // since vanilla always looks the hediff up by type. Only the private Resurrect step is replaced,
    // by the Harmony prefix below.
    public class Hediff_VermillionRebirth : Hediff_DeathRefusal
    {
        // Same radius as vanilla's fire burst ability and our phoenix resurgence.
        private const float BurstRadius = 5.9f;

        private static readonly AccessTools.FieldRef<Hediff_DeathRefusal, Sustainer> SustainerRef =
            AccessTools.FieldRefAccess<Hediff_DeathRefusal, Sustainer>("resurrectSustainer");

        private static readonly AccessTools.FieldRef<Hediff_DeathRefusal, bool> ResurrectingRef =
            AccessTools.FieldRefAccess<Hediff_DeathRefusal, bool>("resurrecting");

        // One stored revival at a time; the gene grants the next one after a recharge.
        public override int MaxUses => 1;

        // Mirrors Hediff_DeathRefusal.Resurrect with three changes: no scar chance, no death refusal
        // sickness, and a fire burst centred on the revived harpy (who is immune to fire).
        internal void ResurrectInFlames()
        {
            SustainerRef(this)?.End();
            ResurrectingRef(this) = false;
            pawn.Drawer.renderer.SetAnimation(null);
            ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams
            {
                gettingScarsChance = 0f,
                canKidnap = false,
                canTimeoutOrFlee = false,
                useAvoidGridSmart = true,
                canSteal = false,
                invisibleStun = true
            });

            if (pawn.Spawned)
            {
                GenExplosion.DoExplosion(pawn.Position, pawn.Map, BurstRadius, DamageDefOf.Flame, pawn,
                    postExplosionSpawnThingDef: ThingDefOf.Filth_Fuel,
                    postExplosionSpawnChance: 1f,
                    postExplosionSpawnThingCount: 1);
            }

            if (pawn.Spawned && pawn.Faction != Faction.OfPlayer && !pawn.Downed)
            {
                Thing weapon = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Weapon), PathEndMode.OnCell, TraverseParms.For(pawn), 5f);
                if (weapon != null)
                {
                    Job job = JobGiver_PickupDroppedWeapon.PickupWeaponJob(pawn, weapon, ignoreForbidden: true);
                    if (job != null)
                    {
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
            }

            // Last, as vanilla does: severity 0 removes the spent hediff.
            if (UsesLeft == 0)
            {
                Severity = 0f;
            }
        }
    }

    // Runs only when a death refusal revival actually happens. Vanilla hediffs fall through untouched.
    [HarmonyPatch(typeof(Hediff_DeathRefusal), "Resurrect")]
    public static class Hediff_DeathRefusal_Resurrect_Patch
    {
        public static bool Prefix(Hediff_DeathRefusal __instance)
        {
            if (__instance is Hediff_VermillionRebirth rebirth)
            {
                rebirth.ResurrectInFlames();
                return false;
            }
            return true;
        }
    }

    public class VermillionRebirthExtension : DefModExtension
    {
        public HediffDef hediff;

        // Ticks between recharge checks. 10000-120000 (about 4 hours to 2 days) matches the
        // framework's GreaterDeathless gene this replaces.
        public IntRange rechargeTicks = new IntRange(10000, 120000);
    }

    // Recharging gene: whenever the countdown runs out and the pawn has no rebirth stored, grant one.
    // Same timer shape as B&S's GreaterDeathless, which can only hand out vanilla DeathRefusal.
    // Cost per gene tick is one integer subtraction.
    public class Gene_VermillionRebirth : Gene
    {
        private int ticksUntilRecharge;

        private VermillionRebirthExtension Settings => def.GetModExtension<VermillionRebirthExtension>();

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            ticksUntilRecharge -= delta;
            if (ticksUntilRecharge > 0)
            {
                return;
            }
            VermillionRebirthExtension settings = Settings;
            if (settings?.hediff == null)
            {
                ticksUntilRecharge = 60000;
                return;
            }
            ticksUntilRecharge = settings.rechargeTicks.RandomInRange;
            if (!ModsConfig.AnomalyActive || pawn.Dead || pawn.health.hediffSet.HasHediff(settings.hediff))
            {
                return;
            }
            pawn.health.AddHediff(settings.hediff);
        }

        public override void PostRemove()
        {
            base.PostRemove();
            HediffDef hediffDef = Settings?.hediff;
            if (hediffDef == null)
            {
                return;
            }
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilRecharge, "ticksUntilRecharge", 0);
        }
    }
}
