namespace CodeForge.Data
{
    public enum CodeTokenType
    {
        Targeting = 0,  // Resolves target entity
        Condition = 1,  // Evaluates boolean combat state
        Action = 2,     // Executes a combat ability (Offense, Defense, Utility)
        Float = 3,      // Numeric float constant / multiplier
        Int = 4         // Numeric integer constant / stat
    }
}

