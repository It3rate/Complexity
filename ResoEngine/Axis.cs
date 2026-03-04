using System;
using System.Collections.Generic;
using System.Text;
using ResoEngine.Support;

namespace ResoEngine
{
    public class Axis
    {
        public Chirality Chirality { get; }
        public Proportion Left { get; } // will always be chirality.Con, imaginary value
        public Proportion Right { get; }// will always be chirality.Pro, rational value

        public long Min => Left.GetNumerator();
        public long Max => Right.GetNumerator();
        public long Unot => Left.GetDenominator();
        public long Unit => Right.GetDenominator();
        public Axis(Proportion left, Proportion right, Chirality chirality)
        {
            left.ForceChirality(Chirality.Con);
            right.ForceChirality(Chirality.Pro);
            Left = left;
            Right = right;
            Chirality = chirality;
        }
    }
}
