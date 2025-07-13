using EMullen.Core;
using UnityEngine;

public interface IMarketEvaluator 
{
    public float EvaluatePurchasePrice(TradeMarket market);
    public float EvaluateSalePrice(TradeMarket market);

    public static IMarketEvaluator FindEvaluator(GameObject obj)
    {
        foreach (var component in obj.GetComponents<MonoBehaviour>()) {
            if(component is IMarketEvaluator eval)
                return eval;
        }
        return null;
    }
}