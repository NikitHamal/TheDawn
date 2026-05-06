using TheDawn.Data;

namespace TheDawn.Systems;

public sealed class TimeSystem
{
    public int DayNumber { get; set; } = 1;
    public GamePhase Phase { get; set; } = GamePhase.Day;
    public double PhaseElapsed { get; set; }
    public bool PhaseChangedThisFrame { get; private set; }

    public double PhaseDuration => Phase switch
    {
        GamePhase.Day => GameConfig.DaySeconds,
        GamePhase.Dusk => GameConfig.DuskSeconds,
        GamePhase.Night => DayNumber >= 60 ? GameConfig.NightSeconds * 1.3 : GameConfig.NightSeconds,
        GamePhase.Dawn => GameConfig.DawnSeconds,
        _ => GameConfig.DaySeconds
    };

    public double Remaining => Math.Max(0, PhaseDuration - PhaseElapsed);
    public double PhaseProgress => PhaseDuration <= 0 ? 1 : Math.Clamp(PhaseElapsed / PhaseDuration, 0, 1);

    public void Update(double seconds)
    {
        PhaseChangedThisFrame = false;
        PhaseElapsed += seconds;
        while (PhaseElapsed >= PhaseDuration)
        {
            PhaseElapsed -= PhaseDuration;
            AdvancePhase();
            PhaseChangedThisFrame = true;
        }
    }

    private void AdvancePhase()
    {
        Phase = Phase switch
        {
            GamePhase.Day => GamePhase.Dusk,
            GamePhase.Dusk => GamePhase.Night,
            GamePhase.Night => GamePhase.Dawn,
            GamePhase.Dawn => GamePhase.Day,
            _ => GamePhase.Day
        };
        if (Phase == GamePhase.Day) DayNumber++;
    }

    public string ClockLabel()
    {
        var minutes = (int)Math.Floor(Remaining / 60.0);
        var seconds = (int)Math.Floor(Remaining % 60.0);
        var phaseName = Phase == GamePhase.Day ? "Light" : Phase.ToString();
        return $"DAY {DayNumber} {phaseName} {minutes:00}:{seconds:00}";
    }
}
