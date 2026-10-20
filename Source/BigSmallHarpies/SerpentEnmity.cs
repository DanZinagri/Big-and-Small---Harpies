using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BigSmallHarpies
{
    // One-sided social dislike: a pawn with one of requiredGenes thinks badly of any pawn whose body
    // has a hated body part (B&S serpent lower body) or who carries a hated gene. Same shape as
    // Seraphim Xenotype Reborn's angelic/demonic enmity thoughts.
    //
    // Performance:
    //  - Vanilla only evaluates social thoughts when an opinion is needed, and caches the result per
    //    pawn pair (SituationalThoughtHandler: recalculated at most every 100 ticks while queried,
    //    dropped 300 ticks after the last query).
    //  - The cheapest test runs first: pawns without the enmity gene return immediately.
    //  - "Is this body a serpent?" is cached per BodyDef, so the part scan happens once per race,
    //    not once per pawn pair. B&S swaps the race ThingDef, so a lamia's BodyDef carries the part.
    public class SerpentEnmityExtension : DefModExtension
    {
        public List<GeneDef> requiredGenes;

        public List<BodyPartDef> hatedBodyParts;

        public List<GeneDef> hatedGenes;

        private readonly Dictionary<BodyDef, bool> serpentBodyCache = new Dictionary<BodyDef, bool>();

        public bool HasSerpentBody(BodyDef body)
        {
            if (body == null || hatedBodyParts.NullOrEmpty())
            {
                return false;
            }
            if (!serpentBodyCache.TryGetValue(body, out bool result))
            {
                result = false;
                List<BodyPartRecord> parts = body.AllParts;
                for (int i = 0; i < parts.Count && !result; i++)
                {
                    result = hatedBodyParts.Contains(parts[i].def);
                }
                serpentBodyCache[body] = result;
            }
            return result;
        }
    }

    public class ThoughtWorker_SerpentEnmity : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn otherPawn)
        {
            SerpentEnmityExtension ext = def.GetModExtension<SerpentEnmityExtension>();
            if (ext == null || otherPawn == null || !HasAnyActiveGene(p, ext.requiredGenes))
            {
                return false;
            }
            if (!ext.HasSerpentBody(otherPawn.RaceProps?.body) && !HasAnyActiveGene(otherPawn, ext.hatedGenes))
            {
                return false;
            }
            if (!RelationsUtility.PawnsKnowEachOther(p, otherPawn))
            {
                return false;
            }
            return ThoughtState.ActiveAtStage(0);
        }

        private static bool HasAnyActiveGene(Pawn pawn, List<GeneDef> genes)
        {
            if (pawn?.genes == null || genes.NullOrEmpty())
            {
                return false;
            }
            for (int i = 0; i < genes.Count; i++)
            {
                if (pawn.genes.HasActiveGene(genes[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
