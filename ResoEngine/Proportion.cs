using ResoEngine.Support;

namespace ResoEngine;
public sealed class Proportion : IProportion, IValue
{
    private long _top;
    private long _bot;
    private Chirality _chirality;

    public EndLock TopLock { get; }
    public EndLock BotLock { get; }
    public Proportion(long top, long bot, Chirality chirality = Chirality.Pro)
    {
        _top = top;
        _bot = bot;
        _chirality = chirality;
        TopLock = EndLock.None;
        BotLock = EndLock.None;
    }

    public Proportion(long top, long bot, Chirality chirality, EndLock topLock, EndLock botLock)
    {
        _top = top;
        _bot = bot;
        _chirality = chirality;
        TopLock = topLock;
        BotLock = botLock;
    }
    public long RawTop => _top;
    public long RawBot => _bot;

    public void ForceChirality(Chirality newChirality)
    {
        if (_chirality != newChirality)
        {
            _top = -_top;
            _bot = -_bot;
            _chirality = newChirality;
        }
    }

    public long GetValueByPerspective(Chirality perspective) => perspective.IsPositive() ? GetNumerator() : GetDenominator();

    public long GetNumerator() => GetTick(_chirality);
    public long GetDenominator() => GetTick(_chirality.Invert());
    public long GetTick(Chirality chirality) => chirality == Chirality.Pro ? TopLock.Value(_top) : BotLock.Value(_bot);
    public double[] GetValues() => [_top / (double)_bot];


    public Proportion WithTop(long newTop) => new Proportion(newTop, _bot, _chirality, TopLock, BotLock);
    public Proportion WithBot(long newBot) => new Proportion(_top, newBot, _chirality, TopLock, BotLock);
    public Proportion InvertChirality() => new Proportion(-_top, -_bot, _chirality.Invert(), TopLock, BotLock);
    public Proportion Unlock() => new Proportion(_top, _bot, _chirality);


    public static Proportion Rational(long ticksPerUnit, long value) =>
        new Proportion(value, ticksPerUnit, Chirality.Pro, EndLock.Fixed, EndLock.None);
    public static Proportion Imaginary(long ticksPerUnit, long value) =>
        new Proportion(value, ticksPerUnit, Chirality.Con, EndLock.None, EndLock.Fixed);
    public static Proportion FixedRational(long ticksPerUnit, long value) =>
        new Proportion(value, ticksPerUnit, Chirality.Pro, EndLock.Fixed, EndLock.Fixed);
    public static Proportion FixedImaginary(long ticksPerUnit, long value) =>
        new Proportion(value, ticksPerUnit, Chirality.Con, EndLock.Fixed, EndLock.Fixed);



    public override string ToString()
    {
        return $"({GetTick(Chirality.Pro)},{GetTick(Chirality.Con)}){((int)_chirality > 0 ? '↑': '↓')}";
    }
}
