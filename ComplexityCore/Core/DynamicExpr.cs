namespace Complexity.Core;

/// <summary>
/// A DynamicExpr resolves an expression iteratively over time.
///
/// "Multiplication can be thought of as continuous over time,
///  while boolean ops are instant/one-shot."
///
/// Each step evaluates the expression, applies resolver pressures (in ticks),
/// snaps to bounds, and records history. The process continues
/// until convergence or max steps.
///
/// This is the serial dimension: output feeds back into input.
/// Compound interest, sine-like oscillation, iterative refinement
/// all emerge from this pattern.
///
/// Convergence is measured in ticks — when tick changes fall below threshold.
/// </summary>
public class DynamicExpr
{
    public CExpr Expression { get; }
    public int Tick { get; private set; }
    public bool Converged { get; private set; }

    /// <summary>
    /// Called after each step with the current state.
    /// </summary>
    public event Action<DynamicExpr, CValue>? StepCompleted;

    public DynamicExpr(CExpr expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Advance one time step:
    /// 1. Evaluate the expression
    /// 2. Apply resolver pressures from all perspectives (in ticks)
    /// 3. Snap to space bounds
    /// 4. Record history
    /// 5. Update validity
    /// </summary>
    public CValue Step(double damping = 0.5)
    {
        // Evaluate expression at current state
        var current = Expression.Evaluate();
        var comps = (long[])current.Components.Clone();

        // Apply external resolver pressures (in ticks)
        foreach (var (name, perspective) in Expression.Resolvers)
        {
            var pressure = perspective.ComputePressure(comps);
            for (int i = 0; i < comps.Length; i++)
                comps[i] += (long)(pressure[i] * damping);
        }

        // Snap to bounds (already in ticks, no quantization needed)
        current = new CValue(Expression.Space, comps, current.Certainty).Snap();

        // Update expression state
        Expression.Value = current;
        Expression.UpdateValidity();
        Expression.History.Add(current.Clone());
        Tick++;

        StepCompleted?.Invoke(this, current);
        return current;
    }

    /// <summary>
    /// Run until convergence or max steps.
    /// Returns the final converged value.
    /// Convergence threshold is in ticks.
    /// </summary>
    /// <param name="maxSteps">Maximum number of steps before giving up.</param>
    /// <param name="threshold">Max tick change between steps to consider converged.</param>
    /// <param name="damping">Damping factor for resolver pressures (0..1).</param>
    public CValue Resolve(int maxSteps = 100, long threshold = 1, double damping = 0.5)
    {
        Converged = false;

        for (int s = 0; s < maxSteps; s++)
        {
            var prev = Expression.Value.Clone();
            var current = Step(damping);

            // Check if anything meaningful changed (in ticks)
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

        return Expression.Value;
    }

    /// <summary>
    /// Reset the dynamic expression to initial state.
    /// </summary>
    public void Reset()
    {
        Tick = 0;
        Converged = false;
        Expression.ClearHistory();
        Expression.Value = Expression.Space.Zero();
        Expression.UpdateValidity();
    }

    public override string ToString() =>
        $"DynamicExpr(tick={Tick}, converged={Converged}, validity={Expression.Validity:0.##})";
}
