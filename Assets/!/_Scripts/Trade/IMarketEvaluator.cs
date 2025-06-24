using EMullen.Core;
using UnityEngine;

public interface IMarketEvaluator 
{
    public float EvaluatePurchasePrice(TradeMarket market);
    public float EvaluateSalePrice(TradeMarket market);

    public static IMarketEvaluator FindEvaluator(GameObject obj)
    {
        BLog.Highlight($" ssss Looking at: {obj.name}");
        foreach (var component in obj.GetComponents<MonoBehaviour>()) {
            BLog.Highlight($"Looking at: {component.name}");
            if(component is IMarketEvaluator eval)
                return eval;
        }
        return null;
    }
}