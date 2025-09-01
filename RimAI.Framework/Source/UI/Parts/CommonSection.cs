using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public partial class RimAIFrameworkMod
    {
        private void DrawSection(
            Listing_Standard listing,
            string title,
            string activeProviderId,
            ref string lastProviderId,
            System.Action<string> setActiveProviderId,
            IEnumerable<string> providerIds,
            System.Action<string> loadAction,
            System.Action<Listing_Standard> drawFieldsAction)
        {
            listing.Label(title);
            listing.Gap(4f);

            var providerIdList = providerIds?.ToList() ?? new List<string>();
            if (!providerIdList.Any())
            {
                listing.Label("RimAI.NoProviders".Translate());
                return;
            }

            string currentLabel = string.IsNullOrEmpty(activeProviderId)
                ? "RimAI.SelectProvider".Translate()
                : activeProviderId.CapitalizeFirst();

            if (Widgets.ButtonText(listing.GetRect(30f), currentLabel))
            {
                var options = new List<FloatMenuOption>();
                foreach (var id in providerIdList)
                {
                    string captured = id;
                    options.Add(new FloatMenuOption(captured.CapitalizeFirst(), () => setActiveProviderId(captured)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            if (activeProviderId != lastProviderId)
            {
                loadAction(activeProviderId);
                lastProviderId = activeProviderId;
            }

            if (string.IsNullOrEmpty(activeProviderId))
            {
                listing.Label("RimAI.PlsSelectProvider".Translate());
                return;
            }

            listing.Gap(8f);
            drawFieldsAction(listing);
        }
    }
}
