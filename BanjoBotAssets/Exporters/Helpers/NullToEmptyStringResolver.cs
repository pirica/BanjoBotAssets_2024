﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace BanjoBotAssets.Exporters.Helpers
{
    // https://stackoverflow.com/questions/23830206/json-convert-empty-string-instead-of-null

    public class NullToEmptyStringResolver : DefaultContractResolver
    {
        public static readonly NullToEmptyStringResolver Instance = new();

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return type.GetProperties()
                    .Select(p =>
                    {
                        var jp = base.CreateProperty(p, memberSerialization);
                        jp.ValueProvider = new NullToEmptyStringValueProvider(p);
                        return jp;
                    }).ToList();
        }
    }

    public class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo _MemberInfo;

        public NullToEmptyStringValueProvider(PropertyInfo memberInfo)
        {
            _MemberInfo = memberInfo;
        }

        public object? GetValue(object target)
        {
            return _MemberInfo.GetValue(target) ?? (object?)"";
        }

        public void SetValue(object target, object? value)
        {
            _MemberInfo.SetValue(target, value);
        }
    }
}
