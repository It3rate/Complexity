using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;


public readonly record struct AlgebraEntry(int LeftIdx, int RightIdx, int ResultIdx, int Sign)
{
    public override string ToString() =>
        $"c[{LeftIdx}] × c[{RightIdx}] → {(Sign >= 0 ? "+" : "−")}c[{ResultIdx}]";
}