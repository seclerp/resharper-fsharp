using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.Compiled
{
  public static class AlternativeNameTypeElementUtil
  {
    public static string ToStringWithAlternativeName(this IAlternativeNameOwner alternativeNameOwner, string baseString) =>
      alternativeNameOwner?.AlternativeName is { } alternativeName
        ? $"({alternativeName}) {baseString}"
        : baseString;
  }
}
