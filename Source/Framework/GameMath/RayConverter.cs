// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.ComponentModel;

namespace Framework.GameMath;

/// <summary>
///     Converts a <see cref="Ray" /> to and from string representation.
/// </summary>
public class RayConverter : ExpandableObjectConverter
{
    /// <summary>
    ///     Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
    /// </summary>
    /// <param name="context"> An <see cref="ITypeDescriptorContext" /> that provides a format context. </param>
    /// <param name="sourceType"> A <see cref="Type" /> that represents the type you want to convert from. </param>
    /// <returns> <b> true </b> if this converter can perform the conversion; otherwise, <b> false </b>. </returns>
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(string))
            return true;

        return base.CanConvertFrom(context, sourceType);
    }

    /// <summary>
    ///     Returns whether this converter can convert the object to the specified type, using the specified context.
    /// </summary>
    /// <param name="context"> An <see cref="ITypeDescriptorContext" /> that provides a format context. </param>
    /// <param name="destinationType"> A <see cref="Type" /> that represents the type you want to convert to. </param>
    /// <returns> <b> true </b> if this converter can perform the conversion; otherwise, <b> false </b>. </returns>
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(string))
            return true;

        return base.CanConvertTo(context, destinationType);
    }

    /// <summary>
    ///     Converts the given object to the type of this converter, using the specified context and culture information.
    /// </summary>
    /// <param name="context"> An <see cref="ITypeDescriptorContext" /> that provides a format context. </param>
    /// <param name="culture"> The <see cref="System.Globalization.CultureInfo" /> to use as the current culture. </param>
    /// <param name="value"> The <see cref="Object" /> to convert. </param>
    /// <returns> An <see cref="Object" /> that represents the converted value. </returns>
    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
        if (value.GetType() == typeof(string))
            return Ray.Parse((string)value);

        return base.ConvertFrom(context, culture, value);
    }

    /// <summary>
    ///     Converts the given value object to the specified type, using the specified context and culture information.
    /// </summary>
    /// <param name="context"> An <see cref="ITypeDescriptorContext" /> that provides a format context. </param>
    /// <param name="culture"> A <see cref="System.Globalization.CultureInfo" /> object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed. </param>
    /// <param name="value"> The <see cref="Object" /> to convert. </param>
    /// <param name="destinationType"> The Type to convert the <paramref name="value" /> parameter to. </param>
    /// <returns> An <see cref="Object" /> that represents the converted value. </returns>
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
        if ((destinationType == typeof(string)) && (value is Ray))
        {
            var r = (Ray)value;

            return r.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}