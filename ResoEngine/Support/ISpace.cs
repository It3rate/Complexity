using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;

public interface ISpace
{
    AlgebraEntry[] Algebra { get; }
    IValue[] ChildValues { get; }
}
public interface ISubSpace : ISpace
{
    ISpace[] FrameElements { get; }
}
