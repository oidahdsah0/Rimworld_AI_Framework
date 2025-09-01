using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimAI.Framework.API;
using RimAI.Framework.Configuration;
using RimAI.Framework.Configuration.Models;
using RimAI.Framework.Core.Lifecycle;
using RimAI.Framework.Shared.Logging;
using RimAI.Framework.Contracts;
using RimAI.Framework.Execution;
using RimAI.Framework.Translation;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimAI.Framework.UI
{
    public partial class RimAIFrameworkMod : Mod
    {
        private readonly RimAIFrameworkSettings settings;

        // Chat
        private string _chatApiKeyBuffer = "";
        private string _chatModelBuffer = "";
        private string _chatEndpointBuffer = "";
        private float _chatTemperatureBuffer = 0.7f;
        private float _chatTopPBuffer = 1.0f;
        private float _chatTypicalPBuffer = 1.0f;
        private int _chatMaxTokensBuffer = 0;
        private int _chatConcurrencyLimitBuffer = 5;
        private string _chatCustomHeadersBuffer = "";
        private string _chatStaticParamsBuffer = "";
        private string _lastChatProviderId = null;
        private bool _isChatTesting = false;
        private string _chatTestStatusMessage = "RimAI.ChatTestHint";

        // Embedding
        private string _embeddingApiKeyBuffer = "";
        private string _embeddingModelBuffer = "";
        private string _embeddingEndpointBuffer = "";
        private string _embeddingCustomHeadersBuffer = "";
        private string _embeddingStaticParamsBuffer = "";
        private string _lastEmbeddingProviderId = null;
        private bool _isEmbeddingTesting = false;
        private string _embeddingTestStatusMessage = "RimAI.EmbedTestHint";
        private int _embeddingConcurrencyLimitBuffer = 4;

        private Vector2 _scrollPosition = Vector2.zero;
        private float _viewHeight = 1500f;
        // Cache UI buffers
        private bool _cacheEnabledBuffer = true;
    private int _cacheTtlBuffer = 5;
        // HTTP timeout UI buffer
    private int _httpTimeoutBuffer = 30;
        // One-time init guard for Network & Cache UI buffers
        private bool _netCacheUIInited = false;

        public RimAIFrameworkMod(ModContentPack content) : base(content)
        {
            FrameworkDI.Assemble();
            settings = GetSettings<RimAIFrameworkSettings>();
        }

        public override string SettingsCategory() => "RimAI.SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // 状态提示：保持键本身，渲染时再 Translate，以避免语言切换后仍显示旧语言

            var listing = new Listing_Standard();
            if (_chatTestStatusMessage == "RimAI.ChatTestHint") _chatTestStatusMessage = "RimAI.ChatTestHint";
            if (_embeddingTestStatusMessage == "RimAI.EmbedTestHint") _embeddingTestStatusMessage = "RimAI.EmbedTestHint";
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, _viewHeight);
            Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

            listing.Begin(viewRect);

            // 第一排：保存/重置/测试/Embedding开关（显示在“聊天服务”标题的上一行）
            Rect topBtnRect = listing.GetRect(30f);
            float topBtnW = topBtnRect.width / 4f - 6f;
            if (Widgets.ButtonText(new Rect(topBtnRect.x, topBtnRect.y, topBtnW, 30f), "RimAI.Save".Translate()))
                HandleCombinedSave();
            if (Widgets.ButtonText(new Rect(topBtnRect.x + topBtnW + 4f, topBtnRect.y, topBtnW, 30f), "RimAI.Reset".Translate()))
                HandleCombinedReset();
            if (Widgets.ButtonText(new Rect(topBtnRect.x + 2 * (topBtnW + 4f), topBtnRect.y, topBtnW, 30f), "RimAI.Test".Translate(), active: !_isChatTesting && !_isEmbeddingTesting))
                HandleCombinedTest();
            // Embedding 开关按钮：Embed:OFF(红)/Embed:ON(绿)
            var prevColor = GUI.color;
            bool embedOn = settings.EmbeddingEnabled;
            GUI.color = embedOn ? Color.green : Color.red;
            if (Widgets.ButtonText(new Rect(topBtnRect.x + 3 * (topBtnW + 4f), topBtnRect.y, topBtnW, 30f), embedOn ? "Embed:ON" : "Embed:OFF"))
            {
                settings.EmbeddingEnabled = !settings.EmbeddingEnabled;
                settings.Write();
                // 切换时清空一次测试提示，避免误导
                _embeddingTestStatusMessage = settings.EmbeddingEnabled ? "RimAI.EmbedTestHint" : "RimAI.EmbedDisabled";
            }
            GUI.color = prevColor;
            listing.Gap(6f);
            // 顶部按钮下方显示 Chat/Embedding 的返回信息（若是键则翻译）
            var chatMsg = _chatTestStatusMessage;
            if (!string.IsNullOrEmpty(chatMsg) && chatMsg.StartsWith("RimAI.")) chatMsg = chatMsg.Translate();
            var embedMsg = _embeddingTestStatusMessage;
            if (!string.IsNullOrEmpty(embedMsg) && embedMsg.StartsWith("RimAI.")) embedMsg = embedMsg.Translate();
            listing.Label(chatMsg);
            listing.Label(embedMsg);
            listing.Gap(8f);

            // ----- Chat 区域 -----
            DrawSection(listing, "RimAI.ChatService".Translate(), settings.ActiveChatProviderId, ref _lastChatProviderId,
                (newId) => {
                    settings.ActiveChatProviderId = newId;
                    // 自动同步 Embedding 供应商
                    settings.ActiveEmbeddingProviderId = newId;
                },
                FrameworkDI.SettingsManager.GetAllChatProviderIds(),
                LoadChatSettings, DrawChatFields);

            listing.GapLine(24f);

            // ----- Embedding 区域 -----
            DrawSection(listing, "RimAI.EmbeddingService".Translate(), settings.ActiveEmbeddingProviderId, ref _lastEmbeddingProviderId,
                (newId) => settings.ActiveEmbeddingProviderId = newId,
                FrameworkDI.SettingsManager.GetAllEmbeddingProviderIds(),
                LoadEmbeddingSettings, DrawEmbeddingFields);


            // 记录内容高度并限制滚动范围
            // ----- Network & Cache 区域与操作按钮 -----
            listing.GapLine(24f);
            // Initialize network/cache buffers once per window open
            if (!_netCacheUIInited)
            {
                _cacheEnabledBuffer = settings.CacheEnabled;
                _cacheTtlBuffer = settings.CacheTtlSeconds;
                _httpTimeoutBuffer = settings.HttpTimeoutSeconds;
                _netCacheUIInited = true;
            }
            DrawCacheSection(listing);
            listing.Gap(8f);
            DrawHttpSection(listing);
            listing.GapLine(24f);
            Rect btnRect = listing.GetRect(30f);
            float btnW = btnRect.width / 2f - 4f;
            if (Widgets.ButtonText(new Rect(btnRect.x, btnRect.y, btnW, 30f), "RimAI.NetworkCacheSaveBtn".Translate()))
                HandleNetworkCacheSave();
            if (Widgets.ButtonText(new Rect(btnRect.x + btnW + 4f, btnRect.y, btnW, 30f), "RimAI.NetworkCacheResetBtn".Translate()))
                HandleNetworkCacheReset();
            listing.Gap(8f);

            if (Event.current.type == EventType.Layout)
            {
                _viewHeight = Mathf.Max(_viewHeight, listing.CurHeight);
                float maxScroll = Mathf.Max(0f, _viewHeight - inRect.height);
                _scrollPosition.y = Mathf.Clamp(_scrollPosition.y, 0f, maxScroll);
            }

            listing.End();
            Widgets.EndScrollView();
        }

    // DrawSection 已移动到 Parts/CommonSection.cs

        // --- Chat Specific Logic ---
    // Chat 部分已移动到 Parts/ChatPart.cs
        
    // LoadChatSettings 已移动到 Parts/ChatPart.cs
        
    // HandleChatSave 已移动到 Parts/ChatPart.cs

        // --- 新增 Chat Reset ---
    // HandleChatReset 已移动到 Parts/ChatPart.cs

        // --- 【修复】恢复完整的 Chat 测试方法 ---
    // HandleChatTestLegacy 已移动到 Parts（如需保留）

        // --- Embedding Specific Logic ---
    // Embedding 字段绘制已移动到 Parts/EmbeddingPart.cs
        
    // LoadEmbeddingSettings 已移动到 Parts/EmbeddingPart.cs

        // --- Cache Section ---
    // DrawCacheSection 已移动到 Parts/NetworkHttpPart.cs

        // --- HTTP Settings Section ---
    // DrawHttpSection 已移动到 Parts/NetworkHttpPart.cs
        
    // HandleEmbeddingSave 已移动到 Parts/EmbeddingPart.cs

        // --- 新增 Embedding Reset ---
    // HandleEmbeddingReset 已移动到 Parts/EmbeddingPart.cs
        
    // HandleEmbeddingTestLegacy 已移动到 Parts（如需保留）

    // HandleChatTest 已移动到 Parts/ChatPart.cs

    // HandleEmbeddingTest 已移动到 Parts/EmbeddingPart.cs

        // --- Combined Chat + Embedding 操作 ---
        private void HandleCombinedSave()
        {
            // 保存 Chat 设置
            HandleChatSave();
            // 如 Embedding Key 为空，则同步 Chat Key 和 Provider
            if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
            {
                settings.ActiveEmbeddingProviderId = settings.ActiveChatProviderId;
                _embeddingApiKeyBuffer = _chatApiKeyBuffer;
                Messages.Message("RimAI.EmbeddingAutoSync".Translate(), MessageTypeDefOf.CautionInput);
            }
            // 保存 Embedding 设置
            HandleEmbeddingSave();
        }

        private void HandleNetworkCacheSave()
        {
            settings.CacheEnabled = _cacheEnabledBuffer;
            settings.CacheTtlSeconds = _cacheTtlBuffer;
            settings.HttpTimeoutSeconds = _httpTimeoutBuffer;
            settings.Write();
            HttpClientFactory.ApplyConfiguredTimeout();
            Messages.Message("RimAI.NetworkCacheSaved".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        private void HandleNetworkCacheReset()
        {
            _cacheEnabledBuffer = true;
            _cacheTtlBuffer = 120;
            _httpTimeoutBuffer = 100;
            settings.CacheEnabled = _cacheEnabledBuffer;
            settings.CacheTtlSeconds = _cacheTtlBuffer;
            settings.HttpTimeoutSeconds = _httpTimeoutBuffer;
            settings.Write();
            HttpClientFactory.ApplyConfiguredTimeout();
            Messages.Message("RimAI.NetworkCacheReset".Translate(), MessageTypeDefOf.PositiveEvent);
        }

        private void HandleCombinedReset()
        {
            HandleChatReset();
            HandleEmbeddingReset();
        }

        private void HandleCombinedTest()
        {
            HandleChatTest();
            if (settings.EmbeddingEnabled)
            {
                // 若 Embedding Key 为空，则在测试前自动从 Chat 复制 Provider 与 Key（不落盘，仅本次测试使用）
                if (string.IsNullOrWhiteSpace(_embeddingApiKeyBuffer))
                {
                    settings.ActiveEmbeddingProviderId = settings.ActiveChatProviderId;
                    _embeddingApiKeyBuffer = _chatApiKeyBuffer;
                    Messages.Message("RimAI.EmbeddingAutoSync".Translate(), MessageTypeDefOf.CautionInput);
                }
                HandleEmbeddingTest();
            }
            else
                _embeddingTestStatusMessage = "RimAI.EmbedDisabled";
        }
    }
}