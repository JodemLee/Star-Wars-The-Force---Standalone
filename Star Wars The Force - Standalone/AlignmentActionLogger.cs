using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using TheForce_Standalone.Alignment;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    public class AlignmentActionLogger
    {
        private static AlignmentActionLogger _instance;
        public static AlignmentActionLogger Instance => _instance ??= new AlignmentActionLogger();

        public List<AlignmentActionLogEntry> LogEntries { get; private set; } = new List<AlignmentActionLogEntry>();
        private const int MAX_LOG_ENTRIES = 100;

        public void LogAction(Pawn pawn, string actionName, AlignmentType alignmentType, float amount, string source = null)
        {
            var entry = new AlignmentActionLogEntry
            {
                Tick = Find.TickManager.TicksGame,
                PawnName = pawn?.LabelShort ?? "Unknown",
                ActionName = actionName,
                AlignmentType = alignmentType,
                Amount = amount,
                Source = source ?? "Unknown"
            };

            LogEntries.Insert(0, entry); // Add to beginning for chronological order (newest first)
            if (LogEntries.Count > MAX_LOG_ENTRIES)
            {
                LogEntries.RemoveRange(MAX_LOG_ENTRIES, LogEntries.Count - MAX_LOG_ENTRIES);
            }
        }

        public void ClearLog()
        {
            LogEntries.Clear();
        }
    }

    public class AlignmentActionLogEntry
    {
        public int Tick { get; set; }
        public string PawnName { get; set; }
        public string ActionName { get; set; }
        public AlignmentType AlignmentType { get; set; }
        public float Amount { get; set; }
        public string Source { get; set; }

        public string GetFormattedTime()
        {
            return (Tick / 60000f).ToString("F1") + "d";
        }

        public string GetFormattedAlignment()
        {
            return AlignmentType == AlignmentType.Lightside ?
                "Light Side" : "Dark Side";
        }

        public Color GetAlignmentColor()
        {
            return AlignmentType == AlignmentType.Lightside ?
                new Color(0.2f, 0.5f, 1f) : new Color(0.8f, 0.1f, 0.1f);
        }
    }
}