using System;
using System.Collections.Generic;
using System.Text;

namespace ResoEngine.Support;

public interface IValue
{
    long GetValueByPerspective(Chirality perspective);
    double[] GetValues();
}
