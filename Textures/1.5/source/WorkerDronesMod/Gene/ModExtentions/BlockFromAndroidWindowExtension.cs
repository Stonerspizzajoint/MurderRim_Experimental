using Verse;

namespace WorkerDronesMod
{
    public class BlockFromAndroidWindowExtension : DefModExtension
    {
        // When true, the gene is blocked from being added.
        public bool blockFromAndroidWindow = false;

        // When true, the gene cannot be removed from the selected gene list.
        public bool cannotBeRemoved = false;

        // When true, the gene is marked as needing at least one of these marked genes to continue.
        public bool mustHaveAtLeastOne = false;

        // A type tag for grouping required genes.
        // If two or more genes share the same geneTypeTag (and mustHaveAtLeastOne is true),
        // then at least one gene from that group must be selected in order for the window to accept.
        public string geneTypeTag;
    }
}

