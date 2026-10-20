using HarmonyLib;
using RimWorld;
using Verse;

namespace BigSmallHarpies
{
    [DefOf]
    public static class HarpyDefOf
    {
        public static StatDef DZ_HarpyMeleeIgniteChance;

        static HarpyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HarpyDefOf));
        }
    }

    // Copy of Alpha Skills' AS_FireStarter mechanic, on our own stat so it needs no Alpha Skills.
    // After any landed melee hit (weapon or natural attack) the attacker rolls DZ_HarpyMeleeIgniteChance;
    // on success the target takes 10 Flame damage, and vanilla DamageWorker_Flame then rolls to set it
    // alight (FireUtility.ChanceToAttachFireFromEvent), exactly as Alpha Skills does.
    // Runs only when a melee hit lands, so the cost is one stat read per hit; the stat hides at 0,
    // so pawns without the gene return immediately.
    [HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
    public static class Verb_MeleeAttackDamage_ApplyMeleeDamageToTarget_Patch
    {
        private const float FlameDamage = 10f;

        public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
        {
            if (!(__instance.caster is Pawn attacker))
            {
                return;
            }
            Thing victim = target.Thing;
            if (victim == null || victim.Destroyed)
            {
                return;
            }
            float chance = attacker.GetStatValue(HarpyDefOf.DZ_HarpyMeleeIgniteChance);
            if (chance <= 0f || !Rand.Chance(chance))
            {
                return;
            }
            victim.TakeDamage(new DamageInfo(DamageDefOf.Flame, FlameDamage, 0f, -1f, attacker));
        }
    }
}
