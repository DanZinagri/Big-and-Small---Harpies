using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BigSmallHarpies
{
    // Ability comp for the garudakin wind abilities. Vanilla has no ability comp that simply damages
    // a target, and its explosion comp hits everyone in range, including the caster and allies.
    //   radius <= 0 : damage the targeted pawn (windblast).
    //   radius >  0 : damage every pawn within radius of the caster except the caster (windstorm);
    //                 hostilesOnly limits it to enemies. Plays castFleck (scaled to the radius) at the
    //                 caster and ringFlecks in two rings, at half and full radius, so the burst is visible.
    // Knockback comes from the damage type: DZ_WindBlast uses VEF's DamageWorker_PushBackAttackMelee,
    // which shoves the victim away from the instigator (the caster). Runs only when the ability is cast.
    public class CompProperties_AbilityWindDamage : CompProperties_AbilityEffect
    {
        public DamageDef damageDef;

        public float damageAmount = 10f;

        public float armorPenetration;

        public float radius;

        // Stun after the shove so the victim cannot act instantly from its new cell.
        // 120 ticks = 2 seconds; set per ability in XML. 0 = none.
        public int stunTicks = 120;

        // Area mode only.
        public bool hostilesOnly = true;

        public FleckDef castFleck;

        public List<FleckDef> ringFlecks;

        public int ringFleckCount = 12;

        public float ringFleckScale = 1.5f;

        public CompProperties_AbilityWindDamage()
        {
            compClass = typeof(CompAbilityEffect_WindDamage);
        }
    }

    public class CompAbilityEffect_WindDamage : CompAbilityEffect
    {
        private new CompProperties_AbilityWindDamage Props => (CompProperties_AbilityWindDamage)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (Props.radius <= 0f)
            {
                if (target.Pawn != null && !target.Pawn.Dead)
                {
                    Hit(caster, target.Pawn);
                }
                return;
            }
            if (!caster.Spawned)
            {
                return;
            }
            PlayBurst(caster);
            // Collect first: the knockback moves pawns, which would disturb the cell scan.
            List<Pawn> victims = new List<Pawn>();
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, Props.radius, true))
            {
                if (thing is Pawn pawn && pawn != caster && !pawn.Dead && (!Props.hostilesOnly || pawn.HostileTo(caster)))
                {
                    victims.Add(pawn);
                }
            }
            for (int i = 0; i < victims.Count; i++)
            {
                Hit(caster, victims[i]);
            }
        }

        private void PlayBurst(Pawn caster)
        {
            Map map = caster.Map;
            if (Props.castFleck != null)
            {
                FleckMaker.Static(caster.Position, map, Props.castFleck, Props.radius);
            }
            if (Props.ringFlecks.NullOrEmpty() || Props.ringFleckCount <= 0)
            {
                return;
            }
            Vector3 center = caster.Position.ToVector3Shifted();
            for (int i = 0; i < Props.ringFleckCount; i++)
            {
                float angle = 360f / Props.ringFleckCount * i;
                Vector3 dir = Vector3Utility.FromAngleFlat(angle);
                for (int ring = 1; ring <= 2; ring++)
                {
                    Vector3 loc = center + dir * (Props.radius * 0.5f * ring);
                    if (!loc.ToIntVec3().InBounds(map))
                    {
                        continue;
                    }
                    FleckDef fleck = Props.ringFlecks[(i + ring) % Props.ringFlecks.Count];
                    FleckMaker.Static(loc, map, fleck, Props.ringFleckScale);
                }
            }
        }

        private void Hit(Pawn caster, Pawn victim)
        {
            victim.TakeDamage(new DamageInfo(Props.damageDef, Props.damageAmount, Props.armorPenetration, -1f, caster));
            if (Props.stunTicks > 0 && !victim.Dead && victim.Spawned && victim.stances != null)
            {
                victim.stances.stunner.StunFor(Props.stunTicks, caster);
            }
        }

        // AI: single target only on hostiles; the storm only when a hostile is inside its radius.
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            if (Props.radius <= 0f)
            {
                return target.Pawn != null && target.Pawn.HostileTo(caster);
            }
            if (!caster.Spawned)
            {
                return false;
            }
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, Props.radius, true))
            {
                if (thing is Pawn pawn && pawn != caster && !pawn.Dead && !pawn.Downed && pawn.HostileTo(caster))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
