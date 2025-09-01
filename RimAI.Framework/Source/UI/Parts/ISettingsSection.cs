using System;
using Verse;

namespace RimAI.Framework.UI.Parts
{
    // 简单的 UI 部件接口，负责在 Listing_Standard 上绘制并处理交互
    internal interface ISettingsSection
    {
        void Draw(Listing_Standard listing);
    }
}
