using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;

public class EndLock
{
    public EndLockKind Kind { get; private set; }
    PVRef? _ref;
    EndLock(EndLockKind kind, Chirality chirality = Chirality.Pro, Proportion? target = null)
    {
        if(target != null)
        {
            _ref = new PVRef(target, chirality);
        }
        else
        {
            kind = EndLockKind.None;
        }
        Kind = kind;
    }
    public long Value(long defaultValue) =>  Kind switch
    {
        EndLockKind.EqualOther => _ref!.GetNumerator(),
        EndLockKind.NegateOther => -_ref!.GetNumerator(),
        _ => defaultValue,
    };
    public static EndLock None => new EndLock(EndLockKind.None);
    public static EndLock Fixed => new EndLock(EndLockKind.Fixed);
}

public enum EndLockKind
{
    None,
    Fixed,
    EqualOther,
    NegateOther,
}
