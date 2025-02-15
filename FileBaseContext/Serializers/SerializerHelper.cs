﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Globalization;

namespace kDg.FileBaseContext.Serializers;

public static class SerializerHelper
{
    public static object Deserialize(this string input, Type type)
    {
        if (string.IsNullOrEmpty(input))
        {
            return type.GetDefaultValue();
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(input, CultureInfo.InvariantCulture);
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Parse(input, CultureInfo.InvariantCulture);
        }

        if (type == typeof(Guid))
        {
            return Guid.Parse(input);
        }

        if (type.IsArray)
        {
            Type arrType = type.GetElementType();
            List<object> arr = new List<object>();

            foreach (string s in input.Split(','))
            {
                arr.Add(s.Deserialize(arrType));
            }

            return arr.ToArray();
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, input);
        }

        return Convert.ChangeType(input, type, CultureInfo.InvariantCulture);
    }

    public static string Serialize(this object input)
    {
        if (input != null)
        {
            if (input.GetType().IsArray)
            {
                string result = "";

                object[] arr = (object[])input;

                for (int i = 0; i < arr.Length; i++)
                {
                    result += arr[i].Serialize();

                    if (i + 1 < arr.Length)
                    {
                        result += ",";
                    }
                }

                return result;
            }

            return input is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : input.ToString();
        }

        return "";
    }

    public static TKey GetKey<TKey>(object keyValueFactoryObject, IEntityType entityType, Func<string, string> valueSelector)
    {
        IPrincipalKeyValueFactory<TKey> keyValueFactory = (IPrincipalKeyValueFactory<TKey>)keyValueFactoryObject;

        return (TKey)keyValueFactory.CreateFromKeyValues(
            entityType.FindPrimaryKey().Properties
                      .Select(p => valueSelector(p.GetColumnName())
                                  .Deserialize(p.GetValueConverter()?.ProviderClrType ?? p.ClrType)).ToArray());
    }
}