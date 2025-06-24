using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The TradeMarket is a place for all pricing values to be set. There can be multiple TradeMarkets
///   for different situations such as a standard market and a sale market. The IMarketEvaluator
///   observes this class to determine its buy/sell value.
/// </summary>
public class TradeMarket : MonoBehaviour 
{
    public float firewood_pricePerFt = 0.95f;
    public float firewood_radiusMultiplier = 1.25f;
}