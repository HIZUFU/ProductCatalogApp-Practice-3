using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ProductCatalogApp.Infrastructure
{
    public class FlexibleDoubleModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider
                .GetValue(bindingContext.ModelName);

            if (valueResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(
                bindingContext.ModelName,
                valueResult);

            string text = (valueResult.FirstValue ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.');

            if (text.Length == 0)
            {
                if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
                {
                    bindingContext.Result = ModelBindingResult.Success(null);
                }
                else
                {
                    bindingContext.ModelState.TryAddModelError(
                        bindingContext.ModelName,
                        "Введите значение.");
                }

                return Task.CompletedTask;
            }

            if (double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value) &&
                double.IsFinite(value))
            {
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    "Введите число.");
            }

            return Task.CompletedTask;
        }
    }

    public class FlexibleDoubleModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            return context.Metadata.UnderlyingOrModelType == typeof(double)
                ? new FlexibleDoubleModelBinder()
                : null;
        }
    }
}
