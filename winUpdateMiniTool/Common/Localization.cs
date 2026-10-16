using System;
using System.Globalization;
using winUpdateMiniTool.Properties;

namespace winUpdateMiniTool;

/// <summary>
///     Selects the language the user interface and the log messages are shown in.
/// </summary>
internal static class Localization {

  /// <summary>Empty setting: pick the language from the operating system.</summary>
  public const string Auto = "";

  public const string English = "en";

  public const string ChineseSimplified = "zh-CN";

  // Read before any Apply() call changes CurrentUICulture, so switching back to "Auto" still
  // sees the language of the operating system rather than the one selected last.
  private static readonly string SystemLanguage = DetectFromSystem();

  /// <summary>The language the user chose; empty when the operating system decides.</summary>
  public static string Setting { get; private set; } = Auto;

  /// <summary>The culture actually in effect.</summary>
  public static string Current { get; private set; } = Auto;

  /// <summary>
  ///     The languages offered in the Options menu, in menu order. Built on call so the names always
  ///     come from the language currently in effect.
  /// </summary>
  public static (string Id, string Name)[] GetLanguages() {
    return [
      (Auto, Strings.Lang_Auto),
      (English, Strings.Lang_English),
      (ChineseSimplified, Strings.Lang_ChineseSimplified)
    ];
  }

  /// <summary>
  ///     Switches the application to the given language.
  /// </summary>
  /// <param name="setting">One of the language ids, or an empty value to follow the operating system.</param>
  public static void Apply(string setting) {
    var requested = (setting ?? "").Trim();

    string choice;
    string name;
    if (requested.Equals(English, StringComparison.OrdinalIgnoreCase)) {
      choice = English;
      name = English;
    }
    else if (requested.Equals(ChineseSimplified, StringComparison.OrdinalIgnoreCase)) {
      choice = ChineseSimplified;
      name = ChineseSimplified;
    }
    else {
      choice = Auto;
      name = SystemLanguage;
    }

    Setting = choice;
    Current = name;

    var culture = CultureInfo.GetCultureInfo(name);
    CultureInfo.DefaultThreadCurrentUICulture = culture;
    CultureInfo.CurrentUICulture = culture;
    Strings.Culture = culture;
  }

  private static string DetectFromSystem() {
    var name = CultureInfo.CurrentUICulture.Name;
    return name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? ChineseSimplified : English;
  }
}
