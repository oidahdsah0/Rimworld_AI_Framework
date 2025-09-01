using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public partial class RimAIFrameworkMod
    {
        private void DrawCacheSection(Listing_Standard listing)
        {
            listing.Label("RimAI.CacheSettings".Translate());
            listing.Gap(4f);
            var row = listing.GetRect(24f);
            Widgets.CheckboxLabeled(row, "RimAI.CacheEnabled".Translate(), ref _cacheEnabledBuffer);
            listing.Gap(6f);
            listing.Label("RimAI.CacheTtl".Translate(_cacheTtlBuffer.ToString()));
            _cacheTtlBuffer = (int)listing.Slider(_cacheTtlBuffer, 5, 3600);
        }

        private void DrawHttpSection(Listing_Standard listing)
        {
            listing.Label("RimAI.HttpSettings".Translate());
            listing.Gap(4f);
            listing.Label("RimAI.HttpTimeout".Translate(_httpTimeoutBuffer.ToString()));
            _httpTimeoutBuffer = (int)listing.Slider(_httpTimeoutBuffer, 5, 3600);
        }
    }
}
