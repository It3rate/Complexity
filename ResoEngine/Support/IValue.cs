using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;

public interface IValue
{
    long GetTicksByPerspective(Chirality perspective);
    double[] GetValues();
}
