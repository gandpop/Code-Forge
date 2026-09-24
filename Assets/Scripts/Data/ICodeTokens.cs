using System.Collections;

namespace CodeForge.Data
{
    public interface IConditionToken
    {
        bool Evaluate(CombatContext context);
    }

    public interface ICombatActionToken
    {
        IEnumerator ExecuteAction(CombatContext context);
    }
}
