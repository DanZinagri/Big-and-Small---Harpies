using System.Reflection;
using HarmonyLib;
using Verse;

namespace BigSmallHarpies
{
    // Entry point: applies this assembly's Harmony patches once at mod construction.
    public class HarpiesMod : Mod
    {
        public HarpiesMod(ModContentPack content) : base(content)
        {
            // The assembly is passed explicitly: on .NET 10 (1.7) the parameterless PatchAll() reads the calling
            // assembly off the stack, which breaks when the JIT inlines the call (Harmony issue #395).
            new Harmony("DZ.BSHarpies").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
