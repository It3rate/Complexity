using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;

public record PVRef : IProportion
{
    public Proportion Target { get; }
    public Chirality Chirality { get; }

    public PVRef(Proportion target, Chirality chirality)
    {
        Target = target;
        Chirality = chirality;
    }
    public long GetNumerator() => Target.GetTick(Chirality.Pro);
    public long GetDenominator() => Target.GetTick(Chirality.Con);
    public long GetTick(Chirality chirality) => Target.GetTick(Chirality);
    public double[] GetValues() => [GetNumerator()];
}
