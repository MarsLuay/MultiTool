using MultiTool.Core.Models;
using MultiTool.App.Localization;

namespace MultiTool.App.Models;

public sealed class DisplayRefreshRecommendationItem
{
    public DisplayRefreshRecommendationItem(DisplayRefreshRecommendation recommendation)
    {
        Recommendation = recommendation;
    }

    public DisplayRefreshRecommendation Recommendation { get; }

    public string DeviceName => Recommendation.DeviceName;

    public string DisplayName => Recommendation.DisplayName;

    public string Resolution => Recommendation.Resolution;

    public bool NeedsChange => Recommendation.NeedsChange;

    public string CurrentFrequencyText => FormatFrequency(Recommendation.CurrentFrequency);

    public string RecommendedFrequencyText => FormatFrequency(Recommendation.RecommendedFrequency);

    public string FrequencySummaryText =>
        NeedsChange
            ? AppLanguageStrings.FormatForCurrentLanguage(
                AppLanguageKeys.DisplayRefreshFrequencySummaryNeedsChange,
                Resolution,
                CurrentFrequencyText,
                RecommendedFrequencyText)
            : AppLanguageStrings.FormatForCurrentLanguage(
                AppLanguageKeys.DisplayRefreshFrequencySummaryBestAvailable,
                Resolution,
                CurrentFrequencyText);

    public string StatusText => Recommendation.Message;

    private static string FormatFrequency(int frequency) =>
        frequency <= 1
            ? AppLanguageStrings.GetForCurrentLanguage(AppLanguageKeys.DisplayRefreshDefaultFrequency)
            : $"{frequency} Hz";
}
