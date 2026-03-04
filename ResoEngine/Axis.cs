using System;
using System.Collections.Generic;
using System.Text;
using ResoEngine.Support;

namespace ResoEngine
{
    public class Axis : IValue, ISubSpace
    {
        public Chirality Chirality { get; }
        public Proportion Left { get; } // will always be chirality.Con, imaginary value
        public Proportion Right { get; }// will always be chirality.Pro, rational value



        public long Min => Left.GetNumerator();
        public long Max => Right.GetNumerator();
        public long Unot => Left.GetDenominator();
        public long Unit => Right.GetDenominator();

        public ISpace[] FrameElements => throw new NotImplementedException();

        public AlgebraEntry[] Algebra => throw new NotImplementedException();

        public IValue[] ChildValues => throw new NotImplementedException();

        public Axis(Proportion left, Proportion right, Chirality chirality)
        {
            left.ForceChirality(Chirality.Con);
            right.ForceChirality(Chirality.Pro);
            Left = left;
            Right = right;
            Chirality = chirality;
        }

        // convert to binary and each 0|1 to left right and walk the structure as a binary tree.
        // merge when you stop, so a Proportion can be merged to N/D (or any other stop point, like an area)
        public long GetValueByIndex(int index) 
        {
            throw new NotImplementedException();
        }
        public double[] GetValues()
        {
            throw new NotImplementedException();
        }
        public long GetTicksByPerspective(Chirality perspective)
        {
            throw new NotImplementedException();
        }

    }
}
