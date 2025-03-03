using System;
using UnityEngine;

public static class LumberEvaluator {

    public static float EvaluateTotalLength(this TreeLogGroup group) 
    {
        float ValueLog(TreeLog log) {
            if(log == null)
                return 0;

            float childValues = 0;
            foreach(TreeLog child in log.ChildBranches) {                
                childValues += ValueLog(child);
            }

            return log.Data.length + childValues;
        }
        return ValueLog(group.Root);
    }
    
    public static float EvaluateTotalWeight(this TreeLogGroup group) 
    {
        float CalculateVolume(TreeLog log) {
            if(log == null)
                return 0;

            TreeLogData data = log.Data;
            float volume = (float) (data.length*Math.PI*Math.Pow(data.radius, 2));

            float childValues = 0;
            foreach(TreeLog child in log.ChildBranches) {                
                childValues += CalculateVolume(child);
            }

            return volume + childValues;
        }
        return CalculateVolume(group.Root)*1f;
    }

    public static float EvaluateLumber(this TreeLogGroup group, float pricePerFt, float radiusMultiplier) 
    {
        float ValueLog(TreeLog log) {
            if(log == null)
                return 0;

            TreeLogData data = log.Data;
            float value = data.length*pricePerFt * data.radius*radiusMultiplier;

            float childValues = 0;
            foreach(TreeLog child in log.ChildBranches) {                
                childValues += ValueLog(child);
            }

            return value + childValues;
        }
        return ValueLog(group.Root);
    }

}