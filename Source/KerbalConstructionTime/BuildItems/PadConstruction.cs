﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalConstructionTime
{
    public class PadConstruction : IKCTBuildItem
    {
        /// <summary>
        /// Index of the pad in KSCItem.LaunchPads.
        /// </summary>
        public int LaunchpadIndex = 0;
        public double Progress = 0, BP = 0, Cost = 0;
        public string Name;
        public bool UpgradeProcessed = false;

        private KSCItem _ksc = null;

        public KSCItem KSC
        {
            get
            {
                if (_ksc == null)
                {
                    _ksc = KCTGameStates.KSCs.Find(ksc => ksc.PadConstructions.Contains(this));
                }
                return _ksc;
            }
        }

        public PadConstruction()
        {
        }

        public PadConstruction(string name)
        {
            Name = name;
        }

        public double GetBuildRate()
        {
            double rateTotal = 0;
            if (KSC != null)
            {
                rateTotal = Utilities.GetBothBuildRateSum(KSC);
            }
            return rateTotal;
        }

        public string GetItemName() => Name;

        public BuildListVessel.ListType GetListType() => BuildListVessel.ListType.KSC;

        public double GetFractionComplete() => Progress / BP;

        public double GetTimeLeft() => (BP - Progress) / GetBuildRate();

        public void IncrementProgress(double UTDiff)
        {
            if (!IsComplete()) AddProgress(GetBuildRate() * UTDiff);
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && (IsComplete() || !PresetManager.Instance.ActivePreset.GeneralSettings.KSCUpgradeTimes))
            {
                if (ScenarioUpgradeableFacilities.Instance != null && !KCTGameStates.ErroredDuringOnLoad)
                {
                    KCT_LaunchPad lp = KSC.LaunchPads[LaunchpadIndex];
                    lp.isOperational = true;
                    lp.DestructionNode = new ConfigNode("DestructionState");
                    UpgradeProcessed = true;

                    try
                    {
                        KCTEvents.OnPadConstructionComplete?.Fire(this, lp);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        public bool IsComplete() => Progress >= BP;

        public void SetBP(double cost)
        {
            BP = CalculateBP(cost);
        }

        public static double CalculateBP(double cost)
        {
            var variables = new Dictionary<string, string>()
            {
                { "C", cost.ToString() },
                { "O", PresetManager.Instance.ActivePreset.TimeSettings.OverallMultiplier.ToString() },
                { "Adm", "0" },
                { "AC", "0" },
                { "LP", "1" },
                { "MC", "0" },
                { "RD", "0" },
                { "RW", "0" },
                { "TS", "0" },
                { "SPH", "0" },
                { "VAB", "0" },
                { "Other", "0" }
            };

            double bp = MathParser.GetStandardFormulaValue("KSCUpgrade", variables);
            if (bp <= 0) { bp = 1; }

            return bp;
        }

        private void AddProgress(double amt)
        {
            Progress += amt;
            if (Progress > BP) Progress = BP;
        }
    }
}
