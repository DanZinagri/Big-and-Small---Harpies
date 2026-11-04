using System.Collections.Generic;
using System.Xml;
using Verse;

namespace BigSmallHarpies
{
    // Runs its child operations only when the "unarmed harpies" mod setting is on.
    // With the setting off the patch never applies, so there is no runtime cost, and it reports
    // success so the game does not log it as a failed patch. Toggling needs a restart.
    public class PatchOperationIfUnarmedOption : PatchOperation
    {
        public List<PatchOperation> operations;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (HarpiesMod.Settings == null || !HarpiesMod.Settings.unarmedOnly || operations == null)
            {
                return true;
            }
            foreach (PatchOperation operation in operations)
            {
                if (!operation.Apply(xml))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
