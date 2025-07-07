using System;
using TMPro;
using UnityEngine;

public class ProgressionMetricRenderer : MonoBehaviour 
{

    [SerializeField]
    private TMP_Text metricNameText;
    [SerializeField]
    private TMP_Text metricValueText;

    public void Render(string dispName, ProgressionMetric targetMetric, ProgressionMetric playerMetric = null) 
    {
        metricNameText.text = dispName;

        if(playerMetric != null && playerMetric.GetName() != targetMetric.GetName())
            throw new InvalidOperationException($"Can't render, target metric and player metric types do not match.");

        switch(targetMetric.GetMetricType()) {
            case ProgressionMetric.MetricType.Boolean:
                if(playerMetric != null)
                    metricValueText.text = playerMetric.GetBoolValue() ? "Completed" : "Not completed";
                else
                    metricValueText.text = targetMetric.GetBoolValue().ToString();
                break;

            case ProgressionMetric.MetricType.Integer:
                string playerPortion = "???";
                if(playerMetric != null)
                    playerPortion = playerMetric.GetIntValue().ToString();
                string targetPortion = targetMetric.GetIntValue().ToString();
                metricValueText.text = $"{playerPortion}/{targetPortion}";
                break;

            case ProgressionMetric.MetricType.Float:
                playerPortion = "???";
                if(playerMetric != null)
                    playerPortion = playerMetric.GetFloatValue().ToString();
                targetPortion = targetMetric.GetFloatValue().ToString();
                metricValueText.text = $"{playerPortion}/{targetPortion}";
                break;                
        }
    }

}