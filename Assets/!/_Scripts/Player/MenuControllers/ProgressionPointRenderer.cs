using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ProgressionPointRenderer : MonoBehaviour 
{
    [Header("References")]
    [SerializeField]
    private Image backgroundImage;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text descriptionText;
    [SerializeField]
    private TMP_Text priceText;

    [SerializeField]
    private GameObject progressionMetricPrefab;
    [SerializeField]
    private Transform metricsHolder;

    [Header("Settings")]
    [SerializeField]
    private ProgressionPoint point;
    public ProgressionPoint Point => point;
    public DisplayMode displayMode;
    [SerializeField]
    private List<DisplayModeSettings> displayModeSettings;
    [SerializeField]
    private float heightNoDesc;
    [SerializeField]
    private float heightWithDesc;
    [SerializeField]
    private float descLineHeight;

    private float lastTimeRendered;
    public ProgressionData shownProgressionData;

    private void Awake()
    {

    }

    private void Start()
    {

    }

    private void Update() 
    {
        Render(false);
    }

    public void Render(bool bypassTimeLimit = true) 
    {
        if(bypassTimeLimit) {
            if(Time.time - lastTimeRendered < 1f)
                return;
        }

        lastTimeRendered = Time.time;

        UpdateDisplayMode(FindDisplayModeSettings(displayMode));

        if(point == null)
            return;

        string nameText = point.progressionDisplayName;
        List<string> descriptionLines = point.progressionDescription.Split("\n").ToList();
        string priceText = $"${point.price}";  

        if(displayMode == DisplayMode.INVISIBLE) {
            nameText = "???";
            descriptionLines = new() { "???" };
            priceText = "$???";
        }

        UpdateTextElements(nameText, descriptionLines, priceText);
        UpdateSize(descriptionLines.Count);
        UpdateMetrics();
    }

    private void UpdateDisplayMode(DisplayModeSettings settings) 
    {
        backgroundImage.color = settings.background;
        nameText.color = settings.titleText;
        descriptionText.color = settings.descriptionText;
    }

    private DisplayModeSettings FindDisplayModeSettings(DisplayMode mode) 
    {
        foreach(DisplayModeSettings testSettings in displayModeSettings) {
            if(testSettings.displayMode == mode)
                return testSettings;
        }

        throw new InvalidOperationException($"Failed to find display mode settings for \"{mode}\"");
    }

    private void UpdateTextElements(string nameText, List<string> descriptionLines, string priceText) 
    {
        this.nameText.text = nameText;
        this.descriptionText.text = string.Join("\n", descriptionLines);
        this.priceText.text = priceText;
    }

    private void UpdateSize(int descLineCnt) 
    {
        RectTransform t = GetComponent<RectTransform>();
        Vector2 sizeDelta = t.sizeDelta;
        if(descLineCnt == 0)
            sizeDelta.y = heightNoDesc;
        else {
            sizeDelta.y = heightWithDesc + (descLineHeight * (descLineCnt-1));
        }
        t.sizeDelta = sizeDelta;            
    }

    private void UpdateMetrics() 
    {
        BLog.Highlight($"Point {point.name} has metrics: {point.targetMetrics}");
        int targetMetricCnt = 0;
        if(point.targetMetrics != null)
            targetMetricCnt = point.targetMetrics.Count;

        BLog.Highlight(targetMetricCnt.ToString());

        if(metricsHolder.childCount != targetMetricCnt) {
            if(metricsHolder.childCount > targetMetricCnt) {
                for(int i = metricsHolder.childCount; i > point.targetMetrics.Count; i--) {
                    Destroy(metricsHolder.GetChild(i));
                }
            } else {
                for(int i = 0; i < targetMetricCnt - metricsHolder.childCount; i++) {
                    Instantiate(progressionMetricPrefab, metricsHolder);
                }
            }
        }

        for(int i = 0; i < targetMetricCnt; i++) {
            ProgressionPoint.ProgressionMetricData pmd = point.targetMetrics[i];
            string dispName = pmd.displayName;
            ProgressionMetric metric = pmd.metric;

            GameObject progMetricObj = metricsHolder.GetChild(i).gameObject;
            ProgressionMetricRenderer renderer = progMetricObj.GetComponent<ProgressionMetricRenderer>();

            ProgressionMetric playerMetric = null;
            if(shownProgressionData != null && shownProgressionData.HasMetric(metric.GetName())) {
                playerMetric = shownProgressionData.GetMetric(metric.GetName());
            }

            renderer.Render(dispName, metric, playerMetric);
        }
    }

    public enum DisplayMode { INVISIBLE, UNLOCKED, NEXT_STEP, CAN_UNLOCK }

    [Serializable]
    public struct DisplayModeSettings {
        public DisplayMode displayMode;
        public Color background;
        public Color titleText;
        public Color descriptionText;
    }

}