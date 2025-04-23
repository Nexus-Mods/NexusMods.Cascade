using System;

namespace NexusMods.Cascade;

/// <summary>
/// When placed on a static method, overrides for this method will be created that uave all the valid permutations
/// of `out LVar` exepcted for the given input lvars.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class GenerateLVarOverridesAttribute : Attribute
{

}
