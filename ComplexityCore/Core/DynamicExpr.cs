namespace Complexity.Core;

/// <summary>
/// The simulation layer: runs a CExpr iteratively over time, with optional
/// resolver pressures, tracking history and convergence.
///
/// CExpr is a pure, stateless transform — it maps values to values.
/// DynamicExpr is the engine that drives that transform over time and owns
/// all mutable state: CurrentValue, History, Resolvers, Tick, Converged.
///
/// "Multiplication can be thought of as continuous over time,
///  while boolean ops are instant/one-shot."
///
/// Each step:
///   1. Evaluates the expression (pure CExpr call)
///   2. Applies resolver pressures (all in ticks)
///   3. Snaps to space bounds
///   4. Records into History
///   5. Fires StepCompleted event
///
/// Convergence is measured in ticks: when the change between steps falls
/// below a threshold, the system is considered stable.
/// </summary>
public class DynamicExpr
{
    public CExpr Expression { get; }

    /// <summary>The evolving output value at the current tick.</summary>
    public CValue CurrentValue { get; private set; }

    /// <summary>State captured after each step.</summary>
    public List<CValue> History { get; } = new();

    /// <summary>
    /// External forces acting on the result at each step.
    /// Each perspective pushes CurrentValue toward its preferred region (in ticks).
    /// </summary>
    public Dictionary<string, Perspective> Resolvers { get; } = new();

    public int Tick { get; private set; }
    public bool Converged { get; private set; }

    /// <summary>Called after each step with the current result.</summary>
    public event Action<DynamicExpr, CValue>? StepCompleted;

    public DynamicExpr(CExpr expression)
    {
        Expression = expression;
        CurrentValue = expression.Space.Zero();
    }

    /// <summary>Add a named resolver that will pressure the result each step.</summary>
    public void AddResolver(string name, Perspective perspective)
        => Resolvers[name] = perspective;

    /// <summary>Remove a named resolver.</summary>
    public bool RemoveResolver(string name) => Resolvers.Remove(name);

    /// <summary>Clear the step history.</summary>
    public void ClearHistory() => History.Clear();

    /// <summary>
    /// Advance one time step:
    ///   1. Evaluate the expression (pure — uses Left/Right from CExpr)
    ///   2. Apply resolver pressures in ticks
    ///   3. Snap to space bounds
    ///   4. Record into History, update CurrentValue, fire event
    /// </summary>
    public CValue Step(double damping = 0.5)
    {
        // Evaluate the pure expression
        var current = Expression.Evaluate();
        var comps = (long[])current.Components.Clone();

        // Apply external resolver pressures (all in ticks)
        foreach (var (_, perspective) in Resolvers)
        {
            var pressure = perspective.ComputePressure(comps);
            for (int i = 0; i < comps.Length; i++)
                comps[i] += (long)(pressure[i] * damping);
        }

        // Snap to bounds (already in ticks, no quantization needed)
        current = new CValue(Expression.Space, comps, current.Certainty).Snap();

        CurrentValue = current;
        History.Add(current.Clone());
        Tick++;

        StepCompleted?.Invoke(this, current);
        return current;
    }

    /// <summary>
    /// Run until convergence or max steps.
    /// Convergence: tick-level change between steps falls below threshold.
    /// Returns the final CurrentValue.
    /// </summary>
    public CValue Resolve(int maxSteps = 100, long threshold = 1, double damping = 0.5)
    {
        Converged = false;

        for (int s = 0; s < maxSteps; s++)
        {
            var prev = CurrentValue.Clone();
            var current = Step(damping);

            long maxDelta = 0;
            for (int i = 0; i < Expression.Space.Dims; i++)
            {
                long delta = Math.Abs(current.Components[i] - prev.Components[i]);
                maxDelta = Math.Max(maxDelta, delta);
            }

            if (maxDelta < threshold)
            {
                Converged = true;
                break;
            }
        }

        return CurrentValue;
    }

    /// <summary>Reset to initial state: zero tick, no history, zero current value.</summary>
    public void Reset()
    {
        Tick = 0;
        Converged = false;
        History.Clear();
        CurrentValue = Expression.Space.Zero();
    }

    public override string ToString() =>
        $"DynamicExpr(tick={Tick}, converged={Converged})";
}
