using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;

namespace winUpdateMiniTool.Common;

internal class MiscFunc {

  public static int ParseInt(string str, int def = 0) {
    return int.TryParse(str, out var result) ? result : def;
  }

  public static Color? ParseColor(string input) {
    ColorConverter c = new();
    if (Regex.IsMatch(input, "^(#[0-9A-Fa-f]{3})$|^(#[0-9A-Fa-f]{6})$"))
      return (Color)c.ConvertFromString(input)!;

    var svc = (TypeConverter.StandardValuesCollection)c.GetStandardValues();
    foreach (Color o in svc!)
      if (o.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
        return (Color)c.ConvertFromString(input)!;
    return null;
  }
}
