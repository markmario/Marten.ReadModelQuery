using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Core.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace ReadModelQuery.Core.ModelBinding;

/// <summary>
/// Model binder for ISearchQuery interface from query string parameters
/// </summary>
public class SearchQueryModelBinder : IModelBinder
{
    private readonly ISearchQueryFactory _searchQueryFactory;

    public SearchQueryModelBinder(ISearchQueryFactory searchQueryFactory)
    {
        _searchQueryFactory = searchQueryFactory ?? throw new ArgumentNullException(nameof(searchQueryFactory));
    }

    public bool CanBind(Type type)
    {
        return typeof(ISearchQuery).IsAssignableFrom(type);
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        // Try to get queryType from query string
        var queryTypeProvider = bindingContext.ValueProvider.GetValue("queryType");
        if (queryTypeProvider == ValueProviderResult.None || string.IsNullOrEmpty(queryTypeProvider.FirstValue))
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var queryType = queryTypeProvider.FirstValue!;

        try
        {
            // Build JSON from query string parameters
            var jsonElement = BuildJsonFromQueryString(bindingContext, queryType);
            
            // Create the query object
            var query = _searchQueryFactory.CreateQuery(queryType, jsonElement);
            
            bindingContext.Result = ModelBindingResult.Success(query);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, ex.Message);
            bindingContext.Result = ModelBindingResult.Failed();
        }

        await Task.CompletedTask;
    }

    private JsonElement BuildJsonFromQueryString(ModelBindingContext context, string queryType)
    {
        var jsonObject = new Dictionary<string, object> { { "queryType", queryType } };

        // Get all query string keys
        var allKeys = context.HttpContext.Request.Query.Keys;

        foreach (var key in allKeys)
        {
            if (key.Equals("queryType", StringComparison.OrdinalIgnoreCase))
                continue;

            var values = context.HttpContext.Request.Query[key];
            if (values.Count == 1)
            {
                var value = values[0];
                if (!string.IsNullOrEmpty(value))
                {
                    // Try to parse as different types
                    if (int.TryParse(value, out var intValue))
                    {
                        jsonObject[key] = intValue;
                    }
                    else if (decimal.TryParse(value, out var decimalValue))
                    {
                        jsonObject[key] = decimalValue;
                    }
                    else if (bool.TryParse(value, out var boolValue))
                    {
                        jsonObject[key] = boolValue;
                    }
                    else
                    {
                        jsonObject[key] = value;
                    }
                }
            }
            else if (values.Count > 1)
            {
                jsonObject[key] = values.ToArray();
            }
        }

        var json = JsonSerializer.Serialize(jsonObject);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
} 