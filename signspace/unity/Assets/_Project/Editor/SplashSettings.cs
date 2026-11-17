using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// The splash screen, reached by reflection.
    ///
    /// WHY REFLECTION: this project has already been bitten by a Unity 6 rename (`activeInputHandler`
    /// has no setter), and the splash animation property is not named the same way in every 6.x build.
    /// A compile-time call to a name that does not exist stops the whole build for a cosmetic setting;
    /// a reflected one reports what it found and lets the build continue.
    ///
    /// The rule is P7.5's: **no logo animation longer than 1 s**. On this licence Unity's logo cannot be
    /// removed, so "static" is the honest maximum: one frame, no dolly, no zoom.
    /// </summary>
    public static class SplashSettings
    {
        static Type SplashType => typeof(PlayerSettings).GetNestedType(
            "SplashScreen", BindingFlags.Public | BindingFlags.NonPublic);

        static PropertyInfo AnimationProperty()
        {
            var type = SplashType;
            if (type == null) return null;
            foreach (var name in new[] { "animationType", "animationMode" })
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
                if (property != null && property.CanWrite) return property;
            }
            return null;
        }

        /// <summary>The current animation mode's name, or "unknown".</summary>
        public static string AnimationName
        {
            get
            {
                var property = AnimationProperty();
                if (property == null) return "unknown";
                try
                {
                    var value = property.GetValue(null);
                    return value != null ? value.ToString() : "unknown";
                }
                catch (Exception) { return "unknown"; }
            }
        }

        /// <summary>Sets the animation mode to Static. Returns what happened, for the log.</summary>
        public static string MakeStatic()
        {
            var property = AnimationProperty();
            if (property == null) return "no splash animation property (static splash already, or SDK differs)";

            var enumType = property.PropertyType;
            if (!enumType.IsEnum) return "splash animation property is not an enum";

            try
            {
                var name = Array.Find(Enum.GetNames(enumType), n =>
                    string.Equals(n, "Static", StringComparison.OrdinalIgnoreCase));
                if (name == null) return "enum has no Static member";

                property.SetValue(null, Enum.Parse(enumType, name));
                return "splash set to " + name;
            }
            catch (Exception error)
            {
                return "could not set the splash: " + error.Message;
            }
        }

        public static bool IsStatic => string.Equals(AnimationName, "Static", StringComparison.OrdinalIgnoreCase);
    }
}
