using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BigSmallHarpies
{
    public class HarpiesSettings : ModSettings
    {
        // Read by PatchOperationIfUnarmedOption while XML patches apply, so a change needs a restart.
        public bool unarmedOnly;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref unarmedOnly, "unarmedOnly", false);
        }
    }

    // Entry point: loads settings and applies this assembly's Harmony patches once at mod construction.
    // Settings load here because mod classes are created before XML patches run.
    public class HarpiesMod : Mod
    {
        public const string GenesAndMorePackageId = "RedMattis.BigSmall.Core";

        public static HarpiesSettings Settings;

        public HarpiesMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HarpiesSettings>();

            // The assembly is passed explicitly: on .NET 10 (1.7) the parameterless PatchAll() reads the calling
            // assembly off the stack, which breaks when the JIT inlines the call (Harmony issue #395).
            new Harmony("DZ.BSHarpies").PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory()
        {
            return "DZ_Harpies_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Both genes belong to Genes & More, and its LoadFolders entry holds the patch, so without it the
            // option does nothing. It shows greyed and unticked then; a saved tick is left alone for when it returns.
            bool available = ModsConfig.IsActive(GenesAndMorePackageId);
            bool ticked = available && Settings.unarmedOnly;

            Rect row = listing.GetRect(Text.LineHeight);
            if (available && Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }
            TooltipHandler.TipRegion(row, "DZ_Harpies_UnarmedOnlyDesc".Translate());

            // Widgets only dims the box when disabled, so the label is tinted here.
            GUI.color = available ? Color.white : Color.gray;
            Widgets.CheckboxLabeled(row, "DZ_Harpies_UnarmedOnly".Translate(), ref ticked, disabled: !available);
            GUI.color = Color.white;

            if (available)
            {
                Settings.unarmedOnly = ticked;
            }

            listing.Gap(listing.verticalSpacing);
            listing.Label((available ? "DZ_Harpies_RestartRequired" : "DZ_Harpies_UnarmedNeedsGenesAndMore").Translate());

            listing.End();
        }
    }
}
