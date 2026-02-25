namespace Complexity.Core;

/// <summary>
/// A CBox holds a space definition + value + resolvers + history + validity.
/// It is the container for a value that can be pushed by external forces (Perspectives)
/// and tracks its own history and validity over time.
///
/// All values are tick-based (longs). Resolvers push in ticks.
///
/// Corresponds to the Box interface from the outline:
///   Dims      → Space.Dims
///   Value     → Value (as CValue, with long[] Components)
///   Resolvers → Resolvers dictionary
///   History   → History list
///   Validity  → Validity (derived from Tension)
///
/// Generalizes Number: where Number has a Focal + Domain + Polarity,
/// CBox has a CValue + CSpace + Resolvers + History.
/// </summary>
public class CBox
{
    public CSpace Space { get; }
    public CValue Value { get; set; }

    /// <summary>
    /// External forces acting on this box. Each perspective pushes
    /// the value toward its own preferred region (in ticks).
    /// </summary>
    public Dictionary<string, Perspective> Resolvers { get; } = new();

    /// <summary>
    /// State at each tick. Tracks resolution over time.
    /// </summary>
    public List<CValue> History { get; } = new();

    /// <summary>
    /// How well the current value fits the space (0..1).
    /// 1.0 = fully valid, decreasing with tension.
    /// </summary>
    public double Validity { get; private set; } = 1.0;

    /// <summary>
    /// Current tension (error signal) of the value against its space bounds (in ticks).
    /// </summary>
    public Tension CurrentTension { get; private set; } = Tension.None;

    public CBox(CSpace space, CValue? initialValue = null)
    {
        Space = space;
        Value = initialValue ?? space.Zero();
        UpdateValidity();
    }

    public CBox(CSpace space, params double[] components)
    {
        Space = space;
        Value = new CValue(space, components);
        UpdateValidity();
    }

    /// <summary>
    /// Recompute validity from current tension.
    /// </summary>
    public void UpdateValidity()
    {
        CurrentTension = Tension.Compute(Value);
        Validity = CurrentTension.Validity;
    }

    /// <summary>
    /// Advance one time step: apply resolver pressures (in ticks), snap to bounds,
    /// record history, update validity.
    /// </summary>
    public void Step(double damping = 0.5)
    {
        var comps = (long[])Value.Components.Clone();

        // Apply resolver pressures (all in ticks)
        foreach (var (name, perspective) in Resolvers)
        {
            var pressure = perspective.ComputePressure(comps);
            for (int i = 0; i < comps.Length; i++)
                comps[i] += (long)(pressure[i] * damping);
        }

        Value = new CValue(Space, comps, Value.Certainty).Snap();
        UpdateValidity();
        History.Add(Value.Clone());
    }

    /// <summary>
    /// Add a named resolver (perspective) that will push this box's value.
    /// </summary>
    public void AddResolver(string name, Perspective perspective)
    {
        Resolvers[name] = perspective;
    }

    /// <summary>
    /// Remove a named resolver.
    /// </summary>
    public bool RemoveResolver(string name) => Resolvers.Remove(name);

    /// <summary>
    /// Clear history.
    /// </summary>
    public void ClearHistory() => History.Clear();

    public override string ToString() =>
        $"CBox({Space.Name}: {Value}, validity={Validity:0.##})";
}
