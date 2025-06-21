using System;
using System.Windows.Markup;
using System.Windows.Data;
using SYSTools.Helpers;

namespace SYSTools.Model
{
    public class ResourceExtension : MarkupExtension
    {
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {

            if (string.IsNullOrEmpty(Key))
                return string.Empty;

            // 返回一个绑定到 LocalizationManager 的动态资源
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}