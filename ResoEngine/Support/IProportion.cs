namespace ResoEngine.Support;

public interface IProportion
{
    long GetNumerator();
    long GetDenominator();
    long GetTick(Chirality chirality);
}
