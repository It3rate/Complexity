namespace Complexity.Core;

/// <summary>
/// A Perspective is one view into a shared substrate.
/// It has its own CSpace (with bounds), a current value, confidence,
/// and an axis map that connects its local axes to substrate indices.
///
/// "Multiple perspectives can all coexist on the same elements."
/// "Communication between them is automatic. It requires no message-passing."
///
/// Each Perspective computes tension (pressure) independently.
/// When multiple perspectives operate on a shared substrate (in ticks),
/// their pressures sum and the state converges.
///
/// Substrate and pressure are all in ticks (longs).
/// Confidence (0..1) is the one double — it weights the pressure.
/// </summary>
public class Perspective
{
    public CSpace Space { get; }
    public CValue Value { get; set; }
    public double Confidence { get; set; }
    public int[] AxisMap { get; }

    /// <summary>
    /// Create a perspective.
    /// </summary>
    /// <param name="space">The complexity space this perspective uses.</param>
    /// <param name="value">Current preferred state in this space (tick-based).</param>
    /// <param name="confidence">How strongly this perspective pushes (0..1).</param>
    /// <param name="axisMap">Maps local axis indices to substrate indices.
    /// axisMap[localAxis] = substrateIndex.</param>
    public Perspective(CSpace space, CValue value, double confidence, int[] axisMap)
    {
        Space = space;
        Value = value;
        Confidence = confidence;
        AxisMap = (int[])axisMap.Clone();
    }

    /// <summary>
    /// Convenience constructor for single-space perspectives where axes map 1:1.
    /// </summary>
    public Perspective(CSpace space, CValue value, double confidence)
        : this(space, value, confidence, Enumerable.Range(0, space.Dims).ToArray())
    {
    }

    /// <summary>
    /// Read the substrate (tick values) through this perspective's lens.
    /// </summary>
    public CValue ReadFromSubstrate(long[] substrate)
    {
        var comps = new long[Space.Dims];
        for (int i = 0; i < Space.Dims; i++)
            comps[i] = substrate[AxisMap[i]];
        return new CValue(Space, comps);
    }

    /// <summary>
    /// Compute what pressure this perspective exerts on the shared substrate.
    /// Each perspective pushes toward its preferred region (within its bounds).
    /// Stronger confidence = stronger push.
    ///
    /// Pressure is in ticks. Confidence (double) scales the tick magnitude.
    /// </summary>
    public long[] ComputePressure(long[] substrate)
    {
        // Read the substrate through our lens
        var localVal = ReadFromSubstrate(substrate);

        // Compute tension: how far is the substrate from our valid range? (in ticks)
        var tension = Tension.Compute(localVal);

        // Convert tension to substrate-space pressure (ticks)
        var pressure = new long[substrate.Length];
        for (int i = 0; i < tension.PerAxis.Length; i++)
        {
            var t = tension.PerAxis[i];
            if (t.Direction != 0)
            {
                int subIdx = AxisMap[t.AxisIndex];
                // Magnitude is in ticks, Confidence is 0..1 double
                pressure[subIdx] += (long)(t.Direction * t.Magnitude * Confidence);
            }
        }
        return pressure;
    }

    /// <summary>
    /// Get the residual tension of this perspective given a substrate state (ticks).
    /// </summary>
    public Tension ResidualTension(long[] substrate)
    {
        var localVal = ReadFromSubstrate(substrate);
        return Tension.Compute(localVal);
    }

    public override string ToString() =>
        $"Perspective({Space.Name}, conf={Confidence:0.##}, {Space.Dims}D)";
}

/// <summary>
/// Static methods for negotiating across multiple perspectives.
///
/// "The negotiated truth is the intersection of all valid regions."
/// Each perspective applies its own constraints and pushes toward its own preferred region.
/// The outcome emerges from the tension.
///
/// All state and pressure values are in ticks (longs).
/// </summary>
public static class Negotiation
{
    /// <summary>
    /// Negotiate across multiple perspectives to find a shared state.
    /// All perspectives compute pressure in parallel; pressures sum; state moves.
    /// No perspective inspects another.
    ///
    /// State, pressure, and threshold are all in ticks.
    /// </summary>
    /// <param name="perspectives">The perspectives to negotiate between.</param>
    /// <param name="initial">Initial substrate state in ticks (one value per substrate axis).</param>
    /// <param name="maxIterations">Maximum iteration count.</param>
    /// <param name="threshold">Convergence threshold in ticks (max change per iteration).</param>
    /// <param name="damping">Damping factor to prevent oscillation (0..1, default 0.5).</param>
    /// <returns>The converged substrate state (ticks) and residual tensions per perspective.</returns>
    public static (long[] State, Tension[] Residuals) Negotiate(
        Perspective[] perspectives,
        long[] initial,
        int maxIterations = 100,
        long threshold = 1,
        double damping = 0.5)
    {
        var state = (long[])initial.Clone();

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Collect pressure from ALL perspectives (parallel, in ticks)
            var totalPressure = new long[state.Length];
            foreach (var p in perspectives)
            {
                var pressure = p.ComputePressure(state);
                for (int i = 0; i < state.Length; i++)
                    totalPressure[i] += pressure[i];
            }

            // Apply combined pressure with damping
            long maxDelta = 0;
            for (int i = 0; i < state.Length; i++)
            {
                long delta = (long)(totalPressure[i] * damping);
                state[i] += delta;
                maxDelta = Math.Max(maxDelta, Math.Abs(delta));
            }

            // Convergence check (in ticks)
            if (maxDelta < threshold)
                break;
        }

        // Compute residual tensions
        var residuals = new Tension[perspectives.Length];
        for (int i = 0; i < perspectives.Length; i++)
            residuals[i] = perspectives[i].ResidualTension(state);

        return (state, residuals);
    }
}
