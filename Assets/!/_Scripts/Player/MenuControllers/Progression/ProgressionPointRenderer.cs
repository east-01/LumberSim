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
    private GameObject progressionMetricPrefab;
    [SerializeField]
    private Transform metricsHolder;
    [SerializeField]
    private List<Image> leadingLines;

    [Header("References - UI")]
    [SerializeField]
    private Image backgroundImage;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text descriptionText;
    [SerializeField]
    private TMP_Text priceText;

    [Header("Settings")]
    [SerializeField]
    private ProgressionPoint point;
    public ProgressionPoint Point => point;
    public DisplayMode displayMode;
    [SerializeField]
    private List<DisplayModeSettings> displayModeSettings;

    private float lastTimeRendered;
    public ProgressionData shownProgressionData;

    private int TargetMetricCnt => point.targetMetrics != null ? point.targetMetrics.Count : 0;

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

        DisplayModeSettings dispSettings = FindDisplayModeSettings(displayMode);
        UpdateDisplayMode(dispSettings);

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
        UpdateMetrics(dispSettings);
        UpdateLines(dispSettings);

        descriptionText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionText.rectTransform);
    }

    public void Clicked() {
        BLog.Highlight($"clicked {point.progressionDisplayName}");
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
        this.priceText.gameObject.SetActive(point != null && point.price > 0);
        this.priceText.text = priceText;
    }

    private void UpdateMetrics(DisplayModeSettings settings) 
    {
        if(metricsHolder.childCount != TargetMetricCnt) {
            if(metricsHolder.childCount > TargetMetricCnt) {
                for(int i = metricsHolder.childCount-1; i >= point.targetMetrics.Count; i--) {
                    DestroyImmediate(metricsHolder.GetChild(i).gameObject);
                }
            } else {
                for(int i = 0; i < TargetMetricCnt - metricsHolder.childCount; i++) {
                    Instantiate(progressionMetricPrefab, metricsHolder);
                }
            }
        }

        for(int i = 0; i < TargetMetricCnt; i++) {
            GameObject progMetricObj = metricsHolder.GetChild(i).gameObject;
            ProgressionMetricRenderer renderer = progMetricObj.GetComponent<ProgressionMetricRenderer>();

            if(displayMode == DisplayMode.INVISIBLE) {
                renderer.RenderInvisible();
                continue;
            }

            ProgressionPoint.ProgressionMetricData pmd = point.targetMetrics[i];
            string dispName = pmd.displayName;
            ProgressionMetric metric = pmd.metric;

            ProgressionMetric playerMetric = null;
            if(shownProgressionData != null && shownProgressionData.HasMetric(metric.GetName())) {
                playerMetric = shownProgressionData.GetMetric(metric.GetName());
            }

            renderer.Render(settings, dispName, metric, playerMetric);
        }
    }

    private void UpdateLines(DisplayModeSettings settings) 
    {
        leadingLines.ForEach(lineRenderer => lineRenderer.color = settings.leadingLineColor);
    }

    public enum DisplayMode { INVISIBLE, UNLOCKED, NEXT_STEP, CAN_UNLOCK }

    [Serializable]
    public struct DisplayModeSettings {
        public DisplayMode displayMode;
        public Color background;
        public Color titleText;
        public Color descriptionText;
        public Color metricNameText;
        public Color metricValueComplete;
        public Color metricValueIncomplete;
        public Color leadingLineColor;
    }

}