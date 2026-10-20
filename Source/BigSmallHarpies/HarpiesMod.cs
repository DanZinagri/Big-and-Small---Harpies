using HarmonyLib;
using Verse;

namespace BigSmallHarpies
{
    // Entry point: applies this assembly's Harmony patches once at mod construction.
    public class HarpiesMod : Mod
    {
        public HarpiesMod(ModContentPack content) : base(content)
        {
            new Harmony("DZ.BSHarpies").PatchAll();
        }
    }
}
