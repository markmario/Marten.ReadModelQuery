using ReadModelQuery.Core.Interfaces;
using ReadModelQuery.Core.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ReadModelQuery.Core.ModelBinding;

/// <summary>
/// Model binder provider for ISearchQuery interface
/// </summary>
public class SearchQueryModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(ISearchQuery) || 
            typeof(ISearchQuery).IsAssignableFrom(context.Metadata.ModelType))
        {
            var searchQueryFactory = context.Services.GetRequiredService<ISearchQueryFactory>();
            return new SearchQueryModelBinder(searchQueryFactory);
        }

        return null;
    }
} 