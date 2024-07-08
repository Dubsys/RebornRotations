namespace DefaultRotations.Ranged;
[Rotation("Default", CombatType.PvE, GameVersion = "7.00", Description = "")]
[SourceCode(Path = "main/DefaultRotations/Ranged/DNC_Default.cs")]
[Api(2)]
public sealed class DNC_Default : DancerRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Holds Tech Step if no targets in range (Warning, will drift)")]
    public bool HoldTechForTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Holds Standard Step if no targets in range (Warning, will drift & Buff may fall off)")]
    public bool HoldStepForTargets { get; set; } = false;
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime <= 15)
        {
            if (StandardStepPvE.CanUse(out var act, skipAoeCheck: true) || ExecuteStepGCD(out act)) return act;
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.TechnicalFinish) && DevilmentPvE.CanUse(out act)) return true;

        if ((IsLastGCD(ActionID.QuadrupleTechnicalFinishPvE) && TechnicalStepPvE.EnoughLevel) ||
            (IsLastGCD(ActionID.DoubleStandardFinishPvE) && !TechnicalStepPvE.EnoughLevel))
        {
            if (DevilmentPvE.CanUse(out act)) return true;
        }

        if (IsDancing) return base.EmergencyAbility(nextGCD, out act);

        if (TechnicalStepPvE.Cooldown.ElapsedAfter(115) && UseBurstMedicine(out act)) return true;

        if (FanDanceIiiPvE.CanUse(out act, skipAoeCheck: true)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (IsDancing) return false;

        if ((Player.HasStatus(true, StatusID.Devilment) || Feathers > 3 || !TechnicalStepPvE.EnoughLevel) && !FanDanceIiiPvE.CanUse(out _, skipAoeCheck: true))
        {
            if (FanDancePvE.CanUse(out act, skipAoeCheck: true) || FanDanceIiPvE.CanUse(out act)) return true;
        }

        if (ShouldUseFlourish() && FlourishPvE.CanUse(out act)) return true;

        if (FanDanceIvPvE.CanUse(out act, skipAoeCheck: true) || UseClosedPosition(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }

    private bool ShouldUseFlourish() =>
        (Player.HasStatus(true, StatusID.Devilment) && Player.HasStatus(true, StatusID.TechnicalFinish)) ||
        (!Player.HasStatus(true, StatusID.Devilment) && !Player.HasStatus(true, StatusID.TechnicalFinish));
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (!InCombat && !Player.HasStatus(true, StatusID.ClosedPosition) && ClosedPositionPvE.CanUse(out act)) return true;

        if (FinishTheDance(out act) || ExecuteStepGCD(out act)) return true;

        if (ShouldUseTechnicalStep(out act)) return true;

        if (AttackGCD(out act, Player.HasStatus(true, StatusID.Devilment))) return true;

        return base.GeneralGCD(out act);
    }

    private bool ShouldUseTechnicalStep(out IAction? act)
    {
        if (HoldTechForTargets)
        {
            if (HasHostilesInMaxRange && IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        else
        {
            if (IsBurst && InCombat && TechnicalStepPvE.CanUse(out act, skipAoeCheck: true)) return true;
        }
        act = null;
        return false;
    }
    #endregion

    #region Extra Methods
    private bool AttackGCD(out IAction? act, bool burst)
    {
        act = null;
        if (IsDancing || Feathers > 3) return false;

        if ((burst || Esprit >= 85) && SaberDancePvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (!DevilmentPvE.CanUse(out _, skipComboCheck: true) && TillanaPvE.CanUse(out act, skipAoeCheck: true)) return true;

        if (StarfallDancePvE.CanUse(out act, skipAoeCheck: true) || LastDancePvE.CanUse(out act)) return true;

        if (ShouldUseStandardStep(out act)) return true;

        if (UseDanceMoves(out act)) return true;

        return false;
    }

    private bool ShouldUseStandardStep(out IAction? act)
    {
        if (HoldStepForTargets)
        {
            if (HasHostilesInMaxRange && UseStandardStep(out act)) return true;
        }
        else
        {
            if (UseStandardStep(out act)) return true;
        }
        act = null;
        return false;
    }

    private bool UseDanceMoves(out IAction? act)
    {
        if (BloodshowerPvE.CanUse(out act) || FountainfallPvE.CanUse(out act) || RisingWindmillPvE.CanUse(out act) ||
            ReverseCascadePvE.CanUse(out act) || BladeshowerPvE.CanUse(out act) || WindmillPvE.CanUse(out act) ||
            FountainPvE.CanUse(out act) || CascadePvE.CanUse(out act)) return true;

        act = null;
        return false;
    }

    private bool UseStandardStep(out IAction act)
    {
        if (!StandardStepPvE.CanUse(out act, skipAoeCheck: true)) return false;
        if (Player.WillStatusEndGCD(2, 0, true, StatusID.StandardFinish)) return true;

        if (!HasHostilesInRange) return false;
        if (Player.HasStatus(true, StatusID.TechnicalFinish) && Player.WillStatusEndGCD(2, 0, true, StatusID.TechnicalFinish) ||
            TechnicalStepPvE.Cooldown.IsCoolingDown && TechnicalStepPvE.Cooldown.WillHaveOneChargeGCD(2)) return false;

        return true;
    }

    private bool UseClosedPosition(out IAction act)
    {
        if (!ClosedPositionPvE.CanUse(out act)) return false;

        if (InCombat && Player.HasStatus(true, StatusID.ClosedPosition))
        {
            foreach (var friend in PartyMembers)
            {
                if (friend.HasStatus(true, StatusID.ClosedPosition_2026) && ClosedPositionPvE.Target.Target != friend)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool FinishTheDance(out IAction? act)
    {
        bool areDanceTargetsInRange = AllHostileTargets.Any(hostile => hostile.DistanceToPlayer() < 14);

        if (Player.HasStatus(true, StatusID.StandardStep) && CompletedSteps == 2 &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.StandardStep)) &&
            DoubleStandardFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (Player.HasStatus(true, StatusID.TechnicalStep) && CompletedSteps == 4 &&
            (areDanceTargetsInRange || Player.WillStatusEnd(1f, true, StatusID.TechnicalStep)) &&
            QuadrupleTechnicalFinishPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        act = null;
        return false;
    }
    #endregion
}
