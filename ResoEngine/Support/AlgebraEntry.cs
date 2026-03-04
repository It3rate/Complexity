using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;


public readonly record struct AlgebraEntry(int LeftIndex, int RightIndex, int ResultIndex, int Sign) // probably dont need sign as that is chirality.
{
    public override string ToString() =>
        $"c[{LeftIndex}] × c[{RightIndex}] → {(Sign >= 0 ? "+" : "−")}c[{ResultIndex}]";
}