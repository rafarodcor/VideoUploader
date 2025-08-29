using System.ComponentModel.DataAnnotations;

namespace VideoUploader.Models.Helpers;

public static class EnumExtensions
{
    #region Methods

    public static string GetDisplayNameEnum(this Enum enumValue)
    {
        var attribute = enumValue.GetType()
                                 .GetField(enumValue.ToString())
                                 ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                 .FirstOrDefault() as DisplayAttribute;

        return attribute?.Name ?? enumValue.ToString();
    }

    #endregion
}