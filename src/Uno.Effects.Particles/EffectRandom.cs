namespace Uno.Effects.Particles;

/// <summary>
/// A small, fast, fully deterministic pseudo-random source (xorshift32).
/// </summary>
/// <remarks>
/// Determinism is the foundation the recommendation builds on: the same
/// <see cref="ParticleEffect.Seed"/> must produce the same frames on every
/// platform, which rules out <see cref="System.Random"/> (its algorithm is not
/// contractually stable across runtimes). This is a struct so effects can hold
/// one by value and never allocate while stepping.
/// </remarks>
public struct EffectRandom
{
    private uint _state;

    public EffectRandom(int seed)
    {
        // Any odd, non-zero state works; xorshift can never reach zero from one.
        unchecked
        {
            _state = ((uint)seed * 2654435761u) | 1u;
        }
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public float NextSingle()
    {
        unchecked
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state >> 8) * (1f / 16777216f);
        }
    }

    /// <summary>Uniform float in [min, max).</summary>
    public float Range(float min, float max) => min + (max - min) * NextSingle();

    /// <summary>Uniform float in [0, max).</summary>
    public float Next(float max) => max * NextSingle();

    /// <summary>Returns 1 or -1 with equal probability.</summary>
    public float Sign() => NextSingle() < 0.5f ? 1f : -1f;

    /// <summary>Uniform integer in [0, exclusiveMax).</summary>
    public int NextInt(int exclusiveMax) => (int)(NextSingle() * exclusiveMax) % Math.Max(1, exclusiveMax);
}
